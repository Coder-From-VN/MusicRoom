# Music Room 🎵

Real-time shared YouTube queue. Create a room, share the 6-character code, everyone in
the room can add songs from a YouTube link, and the queue auto-advances for everyone
in sync — even if nobody's browser tab stays open.

## Stack

- ASP.NET Core 8 (Blazor Server + SignalR)
- No external YouTube API key needed — song titles/thumbnails come from YouTube's
  public `oEmbed` endpoint
- In-memory state (no database) — simple by design; rooms clean themselves up after
  10 minutes with nobody connected

## Run locally

```bash
dotnet restore
dotnet run --project src/MusicRoom
```

Then open the printed `https://localhost:XXXX` URL. Open it in a second tab/browser to
simulate a friend joining.

## Run tests

```bash
dotnet test
```

## How auto-advance works

The server — not the browser — owns the playback clock (`RoomBackgroundService`).
Every second it checks whether the current song's elapsed time has passed its known
duration, and if so, advances the queue and broadcasts the change to everyone in the
room. Each browser's YouTube player periodically resyncs (`ytInterop.syncTime`) if it
drifts more than ~2.5 seconds from where it should be. This means the queue keeps
moving even if every friend closes their laptop — the next person to open the room
just gets handed the correct current song and time.

## Deploy — Azure App Service

The GitHub Actions workflow in `.github/workflows/deploy.yml` builds, runs tests, and
(only if they pass) deploys to Azure App Service on every push to `main`.

**One-time setup:**

1. Create a Linux App Service running the **.NET 8** runtime.
2. In the App Service's **Configuration → General settings**, turn on:
   - **Always On** — without this, the app unloads after ~20 min idle and
     `RoomBackgroundService` stops running.
   - **Web sockets** — required for SignalR.
3. Download the app's **Publish Profile** (Overview → *Get publish profile*).
4. In your GitHub repo: **Settings → Secrets and variables → Actions**, add a secret
   named `AZURE_WEBAPP_PUBLISH_PROFILE` with that file's contents.
5. Edit `AZURE_WEBAPP_NAME` in `deploy.yml` to match your App Service's name.

Push to `main` and the workflow handles the rest.

## Deploying elsewhere

Swap the `deploy` job's last step for whatever your target needs — e.g. `flyctl deploy`
for Fly.io, or `docker build` + push to a registry for anything container-based. The
`build-and-test` job stays the same regardless of host.

## Known simple-by-design tradeoffs

- **In-memory state**: a server restart clears all rooms. Fine for a friend group;
  would need a persistence layer (e.g. Redis) if this needed to survive deploys
  without disrupting an active session.
- **No auth**: anyone with a room code can join. Fine for sharing with friends;
  would need real auth if this became public-facing.
