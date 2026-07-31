using MusicRoom.Services;
using Xunit;

namespace MusicRoom.Tests;

public class RoomServiceTests
{
    [Fact]
    public void CreateRoom_ReturnsRoomWithSixCharacterCode()
    {
        var sut = new RoomService();
        var room = sut.CreateRoom();
        Assert.Equal(6, room.Code.Length);
    }

    [Fact]
    public void CreateRoom_GeneratesUniqueCodesUnderConcurrentLoad()
    {
        var sut = new RoomService();
        const int roomCount = 200;

        var codes = RunConcurrently(sut, roomCount);

        Assert.Equal(roomCount, codes.Distinct().Count());
    }

    [Fact]
    public void GetRoom_IsCaseInsensitiveAndTrimsWhitespace()
    {
        var sut = new RoomService();
        var room = sut.CreateRoom();

        var found = sut.GetRoom($"  {room.Code.ToLowerInvariant()}  ");

        Assert.NotNull(found);
        Assert.Equal(room.Code, found!.Code);
    }

    [Fact]
    public void GetRoom_ReturnsNullForUnknownCode()
    {
        var sut = new RoomService();
        Assert.Null(sut.GetRoom("ZZZZZZ"));
    }

    [Fact]
    public void RemoveRoom_MakesRoomUnreachable()
    {
        var sut = new RoomService();
        var room = sut.CreateRoom();

        sut.RemoveRoom(room.Code);

        Assert.Null(sut.GetRoom(room.Code));
    }

    private static string[] RunConcurrently(RoomService sut, int count)
    {
        var codes = new string[count];
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            codes[i] = sut.CreateRoom().Code;
        });
        return codes;
    }
}
