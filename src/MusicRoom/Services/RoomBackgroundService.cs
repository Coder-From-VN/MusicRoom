using MusicRoom.Models;

namespace MusicRoom.Services;

/// <summary>
/// Runs independently of any browser connection. Every tick it checks whether
/// each room's current song has run past its known duration, and if so, advances
/// the queue. This is what makes auto-next work even if nobody's tab is open.
/// It also sweeps rooms that have been empty for a while.
/// </summary>
public class RoomBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FallbackMaxDuration = TimeSpan.FromMinutes(15); // safety cap if duration was never reported
    private static readonly TimeSpan RoomIdleTimeout = TimeSpan.FromMinutes(10);

    private readonly IRoomService _rooms;
    private readonly PlaybackService _playback;
    private readonly ILogger<RoomBackgroundService> _logger;

    public RoomBackgroundService(IRoomService rooms, PlaybackService playback, ILogger<RoomBackgroundService> logger)
    {
        _rooms = rooms;
        _playback = playback;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RoomBackgroundService started.");
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var room in _rooms.GetAllRooms().ToList())
            {
                try
                {
                    await CheckSongEndedAsync(room);
                    CheckRoomExpired(room);
                }
                catch (Exception ex)
                {
                    // One room's failure should never stop the loop for every other room.
                    _logger.LogError(ex, "Error processing room {Code}", room.Code);
                }
            }
        }
    }

    private async Task CheckSongEndedAsync(Room room)
    {
        bool shouldAdvance;
        lock (room.Lock)
        {
            if (room.CurrentSong == null || room.SongStartedAt == null)
            {
                shouldAdvance = false;
            }
            else
            {
                var elapsed = DateTimeOffset.UtcNow - room.SongStartedAt.Value;
                var limit = room.CurrentSong.Duration > 0
                    ? TimeSpan.FromSeconds(room.CurrentSong.Duration + 1)
                    : FallbackMaxDuration;
                shouldAdvance = elapsed >= limit;
            }
        }

        if (shouldAdvance)
            await _playback.AdvanceQueueAsync(room.Code);
    }

    private void CheckRoomExpired(Room room)
    {
        lock (room.Lock)
        {
            if (room.EmptySince != null && DateTimeOffset.UtcNow - room.EmptySince > RoomIdleTimeout)
                _rooms.RemoveRoom(room.Code);
        }
    }
}
