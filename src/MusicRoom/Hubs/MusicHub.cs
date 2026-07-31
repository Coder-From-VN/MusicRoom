using Microsoft.AspNetCore.SignalR;
using MusicRoom.Models;
using MusicRoom.Services;

namespace MusicRoom.Hubs;

public class MusicHub : Hub
{
    private const string RoomItemsKey = "room";

    private readonly IRoomService _rooms;
    private readonly PlaybackService _playback;
    private readonly IYouTubeMetadataService _metadata;
    private readonly ILogger<MusicHub> _logger;

    public MusicHub(IRoomService rooms, PlaybackService playback, IYouTubeMetadataService metadata, ILogger<MusicHub> logger)
    {
        _rooms = rooms;
        _playback = playback;
        _metadata = metadata;
        _logger = logger;
    }

    public async Task CreateRoom()
    {
        var room = _rooms.CreateRoom();
        lock (room.Lock) room.ConnectionIds.Add(Context.ConnectionId);

        Context.Items[RoomItemsKey] = room.Code;
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);
        await Clients.Caller.SendAsync("RoomCreated", room.Code);
    }

    public async Task JoinRoom(string code)
    {
        var room = _rooms.GetRoom(code);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found.");
            return;
        }

        lock (room.Lock)
        {
            room.ConnectionIds.Add(Context.ConnectionId);
            room.EmptySince = null;
        }

        Context.Items[RoomItemsKey] = room.Code;
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

        List<QueueItem> queueSnapshot;
        QueueItem? current;
        DateTimeOffset? startedAt;
        lock (room.Lock)
        {
            queueSnapshot = room.Queue.ToList();
            current = room.CurrentSong;
            startedAt = room.SongStartedAt;
        }

        await Clients.Caller.SendAsync("JoinedRoom", room.Code, queueSnapshot, current, startedAt);
    }

    public async Task AddToQueue(string code, string youtubeUrl)
    {
        var room = _rooms.GetRoom(code);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "Room not found.");
            return;
        }

        var videoId = _metadata.ExtractVideoId(youtubeUrl);
        if (videoId == null)
        {
            await Clients.Caller.SendAsync("Error", "That doesn't look like a YouTube link.");
            return;
        }

        var meta = await _metadata.GetMetadataAsync(videoId, Context.ConnectionAborted);
        if (meta == null)
        {
            await Clients.Caller.SendAsync("Error", "Couldn't find that video. It may be private or removed.");
            return;
        }

        var item = new QueueItem
        {
            VideoId = videoId,
            Title = meta.Title,
            Thumbnail = meta.Thumbnail,
            AddedBy = Context.ConnectionId
        };

        bool shouldStartPlaying;
        List<QueueItem> queueSnapshot;
        lock (room.Lock)
        {
            room.Queue.Add(item);
            shouldStartPlaying = room.CurrentSong == null;
            queueSnapshot = room.Queue.ToList();
        }

        await Clients.Group(code).SendAsync("QueueUpdated", queueSnapshot);

        if (shouldStartPlaying)
            await _playback.AdvanceQueueAsync(code);
    }

    public Task SkipSong(string code) => _playback.AdvanceQueueAsync(code);

    /// <summary>Called once the client's YT player actually loads a video and knows its real length.</summary>
    public void ReportSongDuration(string code, string songId, double durationSeconds)
    {
        var room = _rooms.GetRoom(code);
        if (room == null || durationSeconds <= 0) return;

        lock (room.Lock)
        {
            if (room.CurrentSong != null && room.CurrentSong.Id == songId && room.CurrentSong.Duration <= 0)
                room.CurrentSong.Duration = durationSeconds;
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(RoomItemsKey, out var codeObj) && codeObj is string code)
        {
            var room = _rooms.GetRoom(code);
            if (room != null)
            {
                lock (room.Lock)
                {
                    room.ConnectionIds.Remove(Context.ConnectionId);
                    if (room.ConnectionIds.Count == 0)
                        room.EmptySince = DateTimeOffset.UtcNow;
                }
            }
        }

        return base.OnDisconnectedAsync(exception);
    }
}
