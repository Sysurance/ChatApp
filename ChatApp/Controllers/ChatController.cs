using chat_application.Extensions;
using chat_application.Models;
using ChatApp.Dtos;
using ChatApp.Extensions;
using ChatApp.Hubs;
using ChatApp.Interfaces;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Plain.RabbitMQ;
using StockChatBot.Dto;

namespace ChatApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatRoomService _chatRoomService;
        private readonly IRoomMessageService _roomMessageService;
        private readonly IHubContext<MessageHub> _messageHub;
        private readonly IPublisher _publisher;
        private readonly IDictionary<int, ConnectedUserModel> _connections;

        public ChatController(IChatRoomService chatRoomService,
            IRoomMessageService roomMessageService,
            IHubContext<MessageHub> messageHub,
            IPublisher publisher,
            IDictionary<int, ConnectedUserModel> connections)
        {
            _chatRoomService = chatRoomService;
            _roomMessageService = roomMessageService;
            _messageHub = messageHub;
            _publisher = publisher;
            _connections = connections;
        }
        //id is the room Id
        public async Task<IActionResult> ChatRoom(int id=-1)
        {
            List<ChatRoom> namesOfRooms = new();
            List<RoomMessage?> roomMessages = new();

            var data = new ChatRoomDto { RoomNames = namesOfRooms, RoomMessages = roomMessages };

            if (User.Identity is null)
            {
                return View(data);
            }
            else
            {
                var defaultSeededRoomId = 1001;
                if (id == -1) id = defaultSeededRoomId; 

                data.SelectedRoomId = id;

                if(User.GetUsername() is not null)
                    data.SelectedRoom = await _chatRoomService.UpdateOnlineUserChatRoom(id,User.GetUserId()) ?? data.SelectedRoom;
                
                var chatRooms = await _chatRoomService.GetAllChatRoomsAsync();
                var foundRoomMessages = await _roomMessageService.GetRoomMessagesByIdAsync(id);

                if (foundRoomMessages is not null)
                {
                    roomMessages = foundRoomMessages;
                    data.RoomMessages = roomMessages;
                }

                if (chatRooms is not null)
                {
                    namesOfRooms = chatRooms;
                    data.RoomNames = namesOfRooms;
                }
            }

            return View(data);
        }

        public async Task<IActionResult> PrivateChat()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> CreateGroupMessage([FromBody] CreateRoomMessageDto createRoomMessage)
        {
            var userHubConnectionId = _connections.GetConnectionStringByUserName(User.GetUsername());

            var roomMessage = new RoomMessage
            {
                Message = createRoomMessage.Message,
                IsStockCode = false,
                ChatRoomId = createRoomMessage.RoomId,
            };

            

            if (userHubConnectionId is not null && _connections.TryGetValue(User.GetUserId(), out ConnectedUserModel connectedUser))
            {
                roomMessage.SenderId = connectedUser.UserId;
                roomMessage.SenderUsername = connectedUser.UserName;


                await _messageHub.Clients.Group(connectedUser.CurrentRoomName)
                    .SendAsync("ReceiveGroupMessage", new
                    {
                        message = roomMessage.Message,
                        username = roomMessage.SenderUsername,
                        timeStamp = roomMessage.Timestamp.ToString("g")
                    });

                
                if (MessageActionService.IsBotMessage(createRoomMessage.Message))
                {                   
                    var request = new RequestToStockBotDto
                    {
                        ChatRoomName = connectedUser.CurrentRoomName,
                        Message = createRoomMessage.Message,
                        IsRoomMessage = true,
                        ChatRoomId = createRoomMessage.RoomId
                    };
                    var serializedRequest = JsonConvert.SerializeObject(request);
                    _publisher.Publish(serializedRequest, "chatmessage.group", null);
                }
                else
                {
                    await _roomMessageService.CreateRoomMessage(createRoomMessage.RoomId, roomMessage);
                }
            }

            return Json(new { status = 1, message = "success" });

        }

    }
}
