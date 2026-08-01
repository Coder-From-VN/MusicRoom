#WebLink: http://freetimeproject.somee.com/
# SyncWave 🎵

Real-time shared YouTube queue. Create a room, share the 6-character code, everyone in
the room can add songs from a YouTube link, and the queue auto-advances for everyone
in sync — even if nobody's browser tab stays open.

---

## 🎧 How to Use the App (UI Guide)

SyncWave features a sleek, responsive dark/light theme interface designed for quick synchronization with friends.

### 1. Getting Started (The Lobby)
When you first open SyncWave, you are greeted by the Lobby Card:
* **Create a Room:** Click the **`✨ Create New Room`** button. You will instantly enter a new room with a unique 6-character code.
* **Join an Existing Room:** If a friend already made a room, paste or type their 6-digit code into the input box and press **`Join`** (or hit `Enter`).

### 2. The Room Badge & Theme Toggle
At the top right of the header bar inside a room:
* **📋 Copy Room Code:** Click the clipboard icon next to your `Room: XXXXXX` badge to instantly copy the room code to your clipboard to send to friends.
* **☀️ / 🌙 Theme Toggle:** Click the sun/moon icon to switch between Dark Mode (default lofi aesthetic) and Light Mode.

### 3. Playing Music & Controls (Left Column)
* **Video Player:** Embedded YouTube video automatically syncs playback across all connected users' browsers.
* **Now Playing:** Displays the current track title and "NOW PLAYING" status.
* **Skip Track:** Click the **`Skip ⏭`** button to immediately end the current track and start the next song in the queue.
* **Add to Queue:** Paste any YouTube video URL into the **"Paste YouTube video or track URL..."** box at the bottom left and click **`Add Song`** (or press `Enter`). The track title is automatically fetched and added.

### 4. Up Next Queue (Right Column)
* **Real-time Queue:** See all upcoming songs listed in order (`#1`, `#2`, etc.) along with the total track count.
* **Empty State:** If the queue runs out of songs, it displays an empty state indicator until someone pastes a new YouTube link.

---

## 🛠️ Stack

- ASP.NET Core 8 (Blazor Server + SignalR)
- No external YouTube API key needed — song titles/thumbnails come from YouTube's
  public `oEmbed` endpoint
- In-memory state (no database) — simple by design; rooms clean themselves up after
  10 minutes with nobody connected

---

## 🚀 Run locally

```bash
dotnet restore
dotnet run --project src/MusicRoom
