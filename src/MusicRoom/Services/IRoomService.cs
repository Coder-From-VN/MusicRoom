using MusicRoom.Models;

namespace MusicRoom.Services;

public interface IRoomService
{
    Room CreateRoom();
    Room? GetRoom(string code);
    void RemoveRoom(string code);
    IEnumerable<Room> GetAllRooms();
}
