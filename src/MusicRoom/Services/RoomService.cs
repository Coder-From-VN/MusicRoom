using System.Collections.Concurrent;
using MusicRoom.Models;

namespace MusicRoom.Services;

public class RoomService : IRoomService
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();

    // No 0/O or 1/I — avoids friends misreading a room code out loud.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxGenerationAttempts = 20;

    public Room CreateRoom()
    {
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = GenerateCode();
            var room = new Room { Code = code };
            if (_rooms.TryAdd(code, room))
                return room;
        }

        throw new InvalidOperationException(
            $"Could not generate a unique room code after {MaxGenerationAttempts} attempts.");
    }

    public Room? GetRoom(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return _rooms.TryGetValue(code.Trim().ToUpperInvariant(), out var room) ? room : null;
    }

    public void RemoveRoom(string code) => _rooms.TryRemove(code, out _);

    public IEnumerable<Room> GetAllRooms() => _rooms.Values;

    private static string GenerateCode() =>
        new(Enumerable.Range(0, CodeLength)
            .Select(_ => CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)])
            .ToArray());
}
