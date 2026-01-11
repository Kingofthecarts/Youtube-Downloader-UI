# Changelog

## 1.5.2
### Playlist Downloads
- Fixed "yt-dlp exited with code 1" error when downloading selected items from Browse Playlist
- Root cause: File locking conflict between TagLib (setting video ID) and ffmpeg (embedding thumbnail)
- Video ID comments are now set after yt-dlp fully completes, preventing race condition with postprocessing

## 1.5.1
### Channel Monitor
- Changed "Play" button to "Redownload" for downloaded videos in Show All view
- Clicking "Redownload" now re-downloads the video instead of opening the songs page

### YouTube Sign-In
- Fixed stale session issue by clearing WebView2 data before each sign-in
- Sign-in now always starts fresh, preventing "Failed to save cookies" errors
- Fixed cookie expiry conversion for session cookies (DateTime.MinValue handling)

## 1.5.0
### Song Browser
- Added Previous button to play the previously played song (based on play history, not table order)
- Random button no longer plays the same song that's currently playing
- Song Browser now auto-refreshes after downloads complete to show new songs
- Moved "Enable Delete" checkbox to bottom panel (left of Reset View)

### Channel Monitor
- Added "Show Ignored" checkbox beside "Show all" to toggle visibility of ignored videos
- Ignored videos are now hidden by default (must check "Show Ignored" to see them)
- "Show all" now auto-unchecks "Show Ignored" when enabled
- Download buttons are now disabled in Channel Monitor while a download is in progress

### Rename Song
- Added colon (:) and comma (,) as filename delimiters for artist/title splitting

## 1.4.1
- Added Deno runtime check on startup with download prompt if missing
- Deno is required for yt-dlp to handle YouTube signature protection

## 1.4.0
- Added automatic update checking on startup for both the app and yt-dlp
- Checks GitHub releases for newer versions and prompts to install
- Auto-update checks can be enabled/disabled in Options
- Both options enabled by default
- Removed Node.js runtime (Deno is used instead for YouTube signature solving)
- Deno is now automatically downloaded during first-run setup if missing

## 1.3.1
- Fixed update download progress getting stuck
- Added "Install Update" button after download completes
- App now auto-reopens after update is installed
- Changelog now automatically opens after an update

## 1.3.0
- Added YouTube sign-in feature using WebView2 for authentication
- Added encrypted cookie storage in config.xml (DPAPI encryption)
- Added "Sign in to YouTube" and "Sign out of YouTube" options in Tools menu
- Added YouTube login status indicator in status bar
- Added first-run prompt for YouTube sign-in
- Added smart error handling that prompts to sign in on authentication errors
- Added Deno runtime support for yt-dlp JavaScript execution
- Added "Redownload Deno" option in Tools menu
- Added "Install/Update WebView2" option in Tools menu
- Added WebView2 check during first-run setup with install prompt
- Added Deno URL and WebView2 URL configuration in Edit Configuration
- Cookies are now passed to all yt-dlp invocations when signed in
- Security: Cookie file is temporary and deleted after each download/form close

## 1.2.1
- Fixed JIT crash (PictureBox image stream disposal) in all forms with album art display

## 1.2.0
- Added changelog viewer under Tools menu
- Fixed "Saved to" showing temp folder instead of output folder
- Added Play button after single video download to play song in Song Browser
- Reset Configuration now also deletes the Source folder (downloaded tools)
- Fixed PictureBox crash when Channel Monitor window repaints after main form minimizes
