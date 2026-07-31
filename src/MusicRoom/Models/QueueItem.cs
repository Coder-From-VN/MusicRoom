namespace MusicRoom.Models;

public class QueueItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string VideoId { get; init; } = "";
    public string Title { get; init; } = "";
    public string Thumbnail { get; init; } = "";

    /// <summary>Seconds. 0 until the client's player reports the real duration.</summary>
    public double Duration { get; set; }
    public string AddedBy { get; init; } = "";
}
