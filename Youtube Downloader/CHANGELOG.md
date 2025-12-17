# Changelog

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
