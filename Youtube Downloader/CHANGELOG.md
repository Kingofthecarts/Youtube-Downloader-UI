# Changelog

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
