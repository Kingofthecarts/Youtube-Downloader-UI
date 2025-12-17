# YouTube Downloader

A Windows application for downloading audio from YouTube videos and playlists as MP3 files.

## Features

- Download single videos or entire playlists as MP3
- Automatic MP3 conversion with embedded thumbnails
- Song Browser for managing your downloaded music
- Channel Monitor to track new uploads from your favorite channels
- Download history tracking
- YouTube sign-in support to avoid bot detection
- Configurable output folders and settings

## Requirements

- Windows 10/11 (64-bit)
- Internet connection
- WebView2 Runtime (for YouTube sign-in feature - prompted during first run)

## Installation

1. Download `Youtube Downloader.exe`
2. Run the application
3. On first run, the app will prompt to download required tools (yt-dlp, ffmpeg, Deno)
4. Choose your output folder for downloaded music
5. Optionally sign in to YouTube to avoid download restrictions

## How to Use

### Downloading a Single Video

1. Copy a YouTube video URL
2. Paste it into the URL field
3. (Optional) Select a subfolder from the dropdown or type a new folder name
4. Click **Go**
5. Wait for download and conversion to complete
6. Your MP3 will be in the output folder

### Downloading a Playlist

1. Switch to the **Playlist** tab
2. Paste the playlist URL
3. Enter a folder name for the playlist
4. Click **Load Playlist** to preview and select songs
5. Click **Go** to download selected songs

### Song Browser

Access via **Songs** menu or press `Ctrl+B`

- Browse all downloaded songs organized by folder
- Play songs directly in the app
- Edit metadata (title, artist, album)
- Delete songs you no longer want

### Channel Monitor

Access via **Channels** menu or press `Ctrl+M`

- Add YouTube channels to monitor for new uploads
- Get notified when channels upload new videos
- Download new videos directly from the monitor
- Auto-scan channels at configurable intervals

## YouTube Sign-In

To avoid "Sign in to confirm you're not a bot" errors:

1. Go to **Tools > Sign in to YouTube**
2. Sign in with your Google account in the browser window
3. Click **Done** when finished

Your session is stored securely (encrypted) and used for all downloads.

## Configuration

Access via **Tools > Edit Configuration**

- **Output Folder**: Where MP3 files are saved
- **Temp Folder**: Temporary download location
- **Download URLs**: Custom URLs for yt-dlp, ffmpeg, Deno, WebView2

## Tools Menu

- **Edit Configuration**: Change app settings
- **Folder List**: Manage output subfolders
- **Check for Updates**: Check for yt-dlp updates
- **Changelog**: View version history
- **Redownload ffmpeg/yt-dlp/Deno**: Re-download tools if corrupted
- **Install/Update WebView2**: Install browser component for YouTube sign-in
- **Sign in/out of YouTube**: Manage YouTube authentication

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+V` | Paste URL and start download |
| `Ctrl+B` | Open Song Browser |
| `Ctrl+M` | Open Channel Monitor |
| `Ctrl+H` | Open History |
| `Escape` | Cancel current download |

## Troubleshooting

**"Sign in to confirm you're not a bot" error**
- Sign in to YouTube via Tools menu

**Download fails with exit code 1**
- Check your internet connection
- Try signing in to YouTube
- Update yt-dlp via Tools > Check for Updates

**WebView2 not available**
- Install via Tools > Install/Update WebView2
- Or download from [Microsoft](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

## License

This application uses:
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) for downloading
- [ffmpeg](https://ffmpeg.org/) for audio conversion
- [Deno](https://deno.land/) for JavaScript execution
