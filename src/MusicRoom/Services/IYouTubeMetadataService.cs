namespace MusicRoom.Services;

public record VideoMetadata(string Title, string Thumbnail);

public interface IYouTubeMetadataService
{
    /// <summary>Extracts an 11-char YouTube video ID from any common URL shape. Returns null if none found.</summary>
    string? ExtractVideoId(string url);

    /// <summary>Looks up title/thumbnail via YouTube's public oEmbed endpoint. Returns null on failure.</summary>
    Task<VideoMetadata?> GetMetadataAsync(string videoId, CancellationToken cancellationToken = default);
}
