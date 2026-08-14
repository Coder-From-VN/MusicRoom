# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SyncWave — a real-time shared YouTube queue. Create a room, share the 6-character
code, everyone in the room adds songs from a YouTube link, and the queue
auto-advances in sync for all connected clients, even if no browser tab is
actively driving playback. ASP.NET Core 8, Blazor Server (interactive server-side
rendering) + SignalR. No database — all state is in-memory and rooms self-expire
after 10 minutes with nobody connected. No YouTube API key is needed; song
title/thumbnail come from YouTube's public `oEmbed` endpoint.

## Commands

```bash
dotnet restore
dotnet build MusicRoom.sln
dotnet run --project src/MusicRoom          # run the app locally
dotnet test MusicRoom.sln                   # run all tests
dotnet test --filter FullyQualifiedName~RoomServiceTests   # run a single test class
dotnet test --filter FullyQualifiedName~RoomServiceTests.GetRoom_IsCaseInsensitiveAndTrimsWhitespace  # single test
```

CI (`.github/workflows/deploy.yml`) runs `dotnet restore` / `build` / `test` on
every push and PR to `main`, then on push to `main` publishes and FTP-deploys to
a somee.com host (app is taken offline via an `app_offline.htm` marker during
deploy, then brought back). There's no staging environment — merging to `main`
ships to production.

## Architecture

**Two-project solution**: `src/MusicRoom` (the app) and `tests/MusicRoom.Tests`
(xUnit, references the app project directly — no test doubles for `RoomService`,
tests hit the real in-memory implementation).

### Real-time flow

The app has exactly one SignalR hub, `Hubs/MusicHub.cs`, mounted at `/musichub`.
The single Blazor page `Pages/Index.razor` opens a `HubConnection` from the
client and drives the whole UI off hub events (`RoomCreated`, `JoinedRoom`,
`QueueUpdated`, `SongChanged`, `Error`) — there's no other page/route.

Server-authoritative playback timing is the key design point: the server
stores `Room.SongStartedAt` (UTC) when a song starts, and clients compute
elapsed time as `now - SongStartedAt` rather than trusting local player state.
This is what lets the queue advance correctly even with zero clients connected.

- `Services/RoomService.cs` — in-memory `ConcurrentDictionary<string, Room>`
  keyed by a 6-char code drawn from an alphabet that excludes `0/O` and `1/I`
  (avoids misread codes read aloud). This is the only piece with dedicated
  unit tests (`RoomServiceTests.cs`) — concurrency-under-load is explicitly tested.
- `Services/PlaybackService.cs` — the single place that pops the next
  `QueueItem` into `Room.CurrentSong`, sets `SongStartedAt`, and broadcasts
  `SongChanged`/`QueueUpdated` to the room's SignalR group. Called both from
  the hub (`SkipSong`, or automatically when a song is added to an empty
  queue) and from the background service.
- `Services/RoomBackgroundService.cs` — a `BackgroundService` ticking every
  second. This is what makes auto-advance work independent of any client tab:
  it compares elapsed time against the current song's known `Duration` (or a
  15-minute fallback cap if duration was never reported) and calls
  `PlaybackService.AdvanceQueueAsync` when the song should end. It also sweeps
  rooms that have been empty (`Room.EmptySince`) for more than 10 minutes.
- `Services/YouTubeMetadataService.cs` — regex-extracts a video ID from any
  common YouTube URL shape (`watch?v=`, `youtu.be/`, `/embed/`, `/shorts/`),
  then fetches title/thumbnail from YouTube's public oEmbed endpoint (no API
  key). Covered by `YouTubeMetadataServiceTests.cs`.
- **`Room.Lock`**: each `Room` has its own lock object guarding its mutable
  state (`Queue`, `CurrentSong`, `SongStartedAt`, `ConnectionIds`,
  `EmptySince`). The `ConcurrentDictionary` in `RoomService` only protects the
  *collection of rooms*, not what's inside a given `Room` — always take
  `room.Lock` when reading or mutating those fields, and keep snapshots
  (`.ToList()`) taken inside the lock before sending data outside it.

### Client-side player sync

`wwwroot/js/youtube-player.js` wraps the YouTube IFrame API behind a
`window.ytInterop` object called via Blazor JS interop from `Index.razor`
(`ytInterop.init`, `ytInterop.loadVideo`, `ytInterop.syncTime`). The actual
song duration is only known once the client's YT player loads the video, so
`Index.razor` reports it back to the server via `ReportSongDuration` →
`MusicHub.ReportSongDuration`, which is what upgrades `QueueItem.Duration`
from `0` to a real value (letting `RoomBackgroundService` advance on time
instead of falling back to the 15-minute cap). A periodic `syncTimer` in
`Index.razor` re-syncs playback position every 5s, correcting only when drift
exceeds 2.5s to avoid audible stutter from constant seeking.

### Adding a new hub method / room event

Follow the existing pattern in `MusicHub.cs`: resolve the room via
`_rooms.GetRoom(code)` and send an `"Error"` client message if null, mutate
state inside `lock (room.Lock)`, take a snapshot inside the lock, then
broadcast outside the lock via `Clients.Group(code).SendAsync(...)`. Add the
corresponding `hub.On<...>(...)` handler in `Index.razor`'s
`OnInitializedAsync`.
