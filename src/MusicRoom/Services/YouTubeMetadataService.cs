using System.Text.Json;
using System.Text.RegularExpressions;

namespace MusicRoom.Services;

public partial class YouTubeMetadataService : IYouTubeMetadataService
{
    private readonly HttpClient _http;
    private readonly ILogger<YouTubeMetadataService> _logger;

    public YouTubeMetadataService(HttpClient http, ILogger<YouTubeMetadataService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // Matches the video ID out of youtube.com/watch?v=, youtu.be/, /embed/, and /shorts/ links.
    [GeneratedRegex(@"(?:v=|youtu\.be\/|\/embed\/|\/shorts\/)([a-zA-Z0-9_-]{11})")]
    private static partial Regex VideoIdPattern();

    public string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = VideoIdPattern().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<VideoMetadata?> GetMetadataAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var watchUrl = $"https://www.youtube.com/watch?v={videoId}";
            var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(watchUrl)}&format=json";

            using var response = await _http.GetAsync(oembedUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("oEmbed lookup failed for {VideoId}: {StatusCode}", videoId, response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() : null;
            var thumbnail = doc.RootElement.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : null;

            return new VideoMetadata(title ?? "Untitled", thumbnail ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch metadata for video {VideoId}", videoId);
            return null;
        }
    }
}
