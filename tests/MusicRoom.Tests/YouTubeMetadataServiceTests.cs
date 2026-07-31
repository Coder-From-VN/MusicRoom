using Microsoft.Extensions.Logging.Abstractions;
using MusicRoom.Services;
using Xunit;

namespace MusicRoom.Tests;

public class YouTubeMetadataServiceTests
{
    private readonly YouTubeMetadataService _sut =
        new(new HttpClient(), NullLogger<YouTubeMetadataService>.Instance);

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=RDdQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_RecognizesCommonUrlShapes(string url, string expectedId)
    {
        var result = _sut.ExtractVideoId(url);
        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url at all")]
    [InlineData("https://www.google.com")]
    [InlineData("https://vimeo.com/12345678")]
    public void ExtractVideoId_ReturnsNullForNonYouTubeInput(string url)
    {
        var result = _sut.ExtractVideoId(url);
        Assert.Null(result);
    }
}
