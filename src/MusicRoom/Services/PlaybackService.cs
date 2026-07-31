using Microsoft.AspNetCore.SignalR;
using MusicRoom.Hubs;
using MusicRoom.Models;

namespace MusicRoom.Services;

public class PlaybackService
{
    private readonly IRoomService _rooms;
    private readonly IHubContext<MusicHub> _hub;
    private readonly ILogger<PlaybackService> _logger;

    public PlaybackService(IRoomService rooms, IHubContext<MusicHub> hub, ILogger<PlaybackService> logger)
    {
        _rooms = rooms;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>Pops the next queued song into CurrentSong and broadcasts the change. Safe to call when the queue is empty.</summary>
    public async Task AdvanceQueueAsync(string code)
    {
        var room = _rooms.GetRoom(code);
        if (room == null) return;

        QueueItem? next;
        DateTimeOffset? startedAt;
        List<QueueItem> queueSnapshot;

        lock (room.Lock)
        {
            next = room.Queue.Count > 0 ? room.Queue[0] : null;
            if (next != null) room.Queue.RemoveAt(0);

            room.CurrentSong = next;
            room.SongStartedAt = next != null ? DateTimeOffset.UtcNow : null;
            startedAt = room.SongStartedAt;
            queueSnapshot = room.Queue.ToList();
        }

        _logger.LogInformation("Room {Code} advanced to {Title}", code, next?.Title ?? "(nothing — queue empty)");

        await _hub.Clients.Group(code).SendAsync("SongChanged", next, startedAt);
        await _hub.Clients.Group(code).SendAsync("QueueUpdated", queueSnapshot);
    }

    public List<QueueItem> GetQueueSnapshot(Room room)
    {
        lock (room.Lock) return room.Queue.ToList();
    }
}
