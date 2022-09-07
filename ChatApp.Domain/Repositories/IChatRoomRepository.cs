using ChatApp.Domain.Models;

namespace ChatApp.Domain.Repositories;

public interface IChatRoomRepository {

    Task<ChatRoom?> GetByRoomNameAsync(string roomName);
    Task<ChatRoom?> GetByRoomIdAsync(int roomId);
    Task<ChatRoom> Add(ChatRoom input);
    Task<List<ChatRoom>> GetRooms();
}