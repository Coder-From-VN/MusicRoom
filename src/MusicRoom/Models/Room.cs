namespace MusicRoom.Models;

public class Room
{
    public string Code { get; init; } = "";
    public List<QueueItem> Queue { get; } = new();
    public QueueItem? CurrentSong { get; set; }
    public DateTimeOffset? SongStartedAt { get; set; }
    public HashSet<string> ConnectionIds { get; } = new();
    public DateTimeOffset? EmptySince { get; set; }

    /// <summary>
    /// Guards mutations to this room's Queue/CurrentSong/SongStartedAt.
    /// The ConcurrentDictionary in RoomService only protects the collection
    /// of rooms, not the mutable state inside each Room instance.
    /// </summary>
    public object Lock { get; } = new();
}
