﻿using chat_application.Models;
using ChatApp.Data;
using ChatApp.Dtos;
using ChatApp.Extensions;
using ChatApp.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ChatApp.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDictionary<string, ConnectedUserDto> _connections;

        public ChatRoomService(ApplicationDbContext db, 
            IDictionary<string, ConnectedUserDto> connections)
        {
            _db = db;
            _connections = connections;
        }


        public async Task<List<ChatRoom>?> GetAllChatRoomsAsync()
        {
            var foundRooms = await _db.ChatRooms.ToListAsync();

            return foundRooms;
        }

        public async Task<bool> AddChatRoomAsync(string chatRoomName)
        {
            var foundRoom = await _db.ChatRooms.FirstOrDefaultAsync(x => x.RoomName == chatRoomName);

            if (foundRoom is not null)
                return false;
            var room = new ChatRoom(chatRoomName);
            _db.ChatRooms.Add(room);

            return true;
        }

        public async Task<bool> RemoveChatRoomAsync(string chatRoomName)
        {
            var foundRoom = await _db.ChatRooms.FirstOrDefaultAsync(x => x.RoomName == chatRoomName);

            if (foundRoom is null)
                return false;

            _db.ChatRooms.Remove(foundRoom);

            return true;
        }

        public async Task<string?> UpdateOnlineUserChatRoom(int chatRoomId,string userName)
        {
            var chatRooms = await GetAllChatRoomsAsync();

            var foundRoom = chatRooms?.FirstOrDefault(x => x.Id == chatRoomId);

            if (foundRoom is not null && _connections.Count > 0)
            {
                var userHubConnectionId = _connections.GetConnectionStringByUserName(userName);
                if(userHubConnectionId is not null)
                {
                    if (_connections.TryGetValue(userHubConnectionId, out var connectedUser))
                    {
                        connectedUser.SelectedRoomName = foundRoom.RoomName;
                        _connections[userHubConnectionId] = connectedUser;
                    }
                }

            }

            return foundRoom?.RoomName;
        }

    }
}
