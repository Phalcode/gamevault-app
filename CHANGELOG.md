# GameVault App Changelog

## 1.17.2
Recommended Gamevault Server Version: `v15.0.2`
### Changes
- Added authorization to the admin panel’s registration action, allowing administrators to register users even when server-side registration is disabled.
- Extended the existing installation overwrite warning to allow users to continue the installation at their own risk.
- Performance optimization in the community tab
- Bug fix: OAuth access token expiration was not calculated correctly under certain circumstances.

## 1.17.1
Recommended Gamevault Server Version: `v15.0.2`
### Changes
- User registration has also been added to the Admin Panel.
- Better handling of the game settings early access check box
- Added 'getallgames' to the clients query API

## 1.17.0
Recommended Gamevault Server Version: `v15.0.0`
### Changes

- Replaced setup wizard with a new login window
- Introduced multi-profile support: if no user profile is configured, the demo user is used by default
- Login window now dynamically adapts to the server configuration
- Upgraded all connections to use OAuth 2.0 authentication flow
- Added support for login and registration via configured identity providers (SSO)
- Implemented pending activation state in the login process
- Added options for logout from this device and logout from all devices
- Introduced support for multiple root directories, including selection during download
- Improved image cache performance
- Added support for additional request headers
- New game settings: default parameters for Un/Installer
- Cloud Saves: root path is now included in the generated config.yaml
- Offline cache now auto-renews when outdated
- Added visual indicator on the install game card when an update is available
- Implemented skeleton loading animations in the community tab to better indicate loading states
- Added "Go to Game" button after installation is complete
- Pressing F5 in the Library now also refreshes the list of installed games
- Build temporary offline cache if it does not exist, so that you can still see your installed games in offline mode even if you have deleted the offline cache or it is corrupted.
- Copy button for own users API key in the user settings
- Bug fix: extraction time remaining now displays correctly
- Bug fix: Duplicate entries when typing in the library search

## 1.16.1
Recommended Gamevault Server Version: `v14.1.0`
### Changes

- Added a setting to mount ISO instead of extracting
- Bug fix: Custom Cloud Save config is not applied to 'find' action
- Bug fix: Cloud Save Backup/Restore not working on windows username with space in it

## 1.16.0
Recommended Gamevault Server Version: `v14.1.0`
### Changes

- Added auto Cloud Save Redirects
- Added support for custom Cloud Save Manifests
- Setting to deactivate the primary cloud save manifest in order to use only the user-defined manifests
- Library can now filter for Developers and Publishers
- You can now uninstall community themes from the settings
- Improved the game metadata search UI
- Bug fix: Startup crash when gamevault tries to set the windows jumplist

## 1.15.0
Recommended Gamevault Server Version: `v14.0.0`
### Changes

- Cloud Saves (+)
- Added retrieval and installation of community themes
- Added Community Themes Repository link to the settings 
- Bug fix: Youtube url not loading in gameview
- Bug fix: Game window sometimes started in the background when launched via gamevault uri
- Bug fix: Crash on saving user details
- Bug fix: Removed all dotted keyboard focus lines
- Bug fix: App protocol of the microsoft version won't start a install if a instance of gamevault is already running
- Bug fix: The game executable selection was always empty if the ignore list had an empty string as an entry

## 1.14.0
Recommended Gamevault Server Version: `v13.1.2`
### Changes

- Sync the currently played game with the discord Presence API (+)
- Sync installed Gamevault games with Steam shortcuts (+)
- The popup window now also closes when you have uninstalled a setup game
- Enhanced offline handling
- Added a new Christmas theme (+).
- Bug fix: Added increasing tick rate to download retry timer to avoid continuous spam
- Bug fix: Media slider video was sometimes rendered on top of a popup
- Bug fix: Crash on Media Slider navigation
- Bug fix: Crash on create shortcut after installation
- Bug fix: Prevent the message copy button from taking focus away from the current control
- Bug fix: Animated profile pictures were still displayed when switching to a profile without a set profile picture
- Bug fix: Improved Gif decoder

## 1.13.1
Recommended Gamevault Server Version: `v13.1.0`
### Changes

- Updated to .NET 8
- The game type filter now replaces the previous entry when you select a new one
- Bug fix: Admin tab reindex button did nothing
- Bug fix: Crash on startup if the installed .NET version is too old for some use cases

## 1.13.0
Recommended Gamevault Server Version: `v13.1.0`
### Changes

- You can now filter by game state in the library
- GameVault now stores the forced installation type for the uninstallation
- Bug fix: Application name is blank in the taskbar
- Bug fix: Sometimes the colors of the Genre/Tag pills were too bright
- Bug fix: Sometimes the add buttons on the pill selector were cut off

## 1.12.5
Recommended Gamevault Server Version: `v13.0.0`
### Changes

- Switched to new server API.
- Added a news button to show the latest updates from GameVault and your own server. When news is available, a red badge appears next to the button. Also supports Markdown.
- Layout changes to the title bar, settings page and admin page.
- Moved title bar action buttons.
- You can now copy the app bar text.
- The executable ignore list is now editable in the settings tab.
- New setting: 'Retain sorting and filter preferences after restart.'
- Added a new Halloween theme (+).
- The last 'Community Tab Sort By' option will be remembered.
- Downloads are safely paused, even if GameVault crashes or is forced to close.
- Overhauled the layout.
- Added a media slider to display trailers, gameplay, and screenshots.
- Added age ratings.
- Added notes.
- Descriptions and notes now support Markdown.
- Bug fix: Uploading large images error.
- Display available metadata providers.
- Change the priority of selected providers.
- Unmap selected metadata providers.
- Recache selected metadata providers.
- Added a new 'Edit Details' tab, giving you full control over every detail of the game. You can undo manual changes at any time with the 'Wipe Custom Metadata' button.
- You can now specify the default launch/setup executable and default launch parameters.

## 1.11.2
Recommended Gamevault Server Version: `v12.2.0`
### Changes

- Reversed buttons for row count for installed games section

## 1.11.1
Recommended Gamevault Server Version: `v12.2.0`
### Changes

- Added share button to gameview
- Bug fix: Desktop shortcut was created when opening the game settings
- Bug fix: Crash on download/extraction notification if box image is not set
- Bug fix: Rare crash on Phalcode sign-in window

## 1.11.0
Recommended Gamevault Server Version: `v12.2.0`
### Changes

- You can now delete all downloads at once
- Added setting to automatically delete files from portable games after successful installation
- Added setting to automatically install portable games
- Ability to change and overwrite the Installation Procedure
- Desktop shortcuts start the game via GameVault. So changed executable or parameters are updated immediately as well.
- Option to create a shortcut upon clicking install. Remembers the last input as default.
- Implemented Jumplist to taskbar icon.
- Allow user to specify rows displayed of installed games.
- Notifications about download/extraction events now also display the game box image
- Library filter is set when you click on game type/Genre/Tag in the gameview.
- Added setting to the admin panel to hide/show deleted users
- Bug fix: Crash on local download directory has wrong name format
- Bug fix: The header of the image downloader was sometimes encoded incorrectly

## 1.10.1
Recommended Gamevault Server Version: `v12.1.1`
### Changes

- Total size of all files is now displayed on the download cards
- An empty release year is no longer displayed in the gameview
- Bug fix: The escape key sometimes did not work in the game settings
- Bug fix: Game settings recache button did nothing
- Bug fix: Rare crash when starting the app if at least one non-extracted game is downloaded. 
- Bug fix: Crash on download game - restart app - extract game
- Bug fix: Download UI sometimes did not display the status correctly when the game was installed

## 1.10.0
Recommended Gamevault Server Version: `v12.1.0`
### Changes
- Added Encrypted (password protected) archive support. Including a default password setting.
- Added the possibility to pause/resume downloads. Paused downloads are restored after the restart. Also pause is triggered on error.
- Added auto retry after download failed.
- Show system notifications for download/extraction
- Bug fix: Center game view background image on window resize
- Bug fix: Tag names that are too long could not be selected in the filters
- Bug fix: Displayed download speed has adjusted very slowly with larger fluctuations
- Bug fix: Too much offset of the bookmark button if game setting button was not visible

## 1.9.2
Recommended Gamevault Server Version: `v12.0.0`
### Changes
- Added Bookmarks to library and gameview. Also the corresponding library filter
- Changed Early Access library filter visual
- Bug fix: Auto extract sometimes did not work when trying to unpack very large files

## 1.9.1
Recommended Gamevault Server Version: `v11.0.2`
### Changes
- Bug fix: GameVault+ license was not loaded correctly

## 1.9.0
Recommended Gamevault Server Version: `v11.0.2`
### Changes
- Added GameVault+ infrastructure
- Added GameVault Client APIs (+)
- Added GIF support for user profile pictures (+)
- Added Theming and custom theme loader (+)
- All buttons have now icons and effects
- Applies new brand design
- Added refresh button to library. Can also be triggered by F5 Key
- Added F5 Key refresh to Community Tab and Admin Panel
- Added About Setting Page
- Display freshly installed games first in Installed Games View
- Added text trimming to game settings archive file path
- The library is now updated when reindexing
- Improved error messages
- Extended Windows context menu
- Bug fix: Edge case where image was shown as not found
- Bug fix: Game website link no longer clickable if empty
- Bug fix: Redownload icon not appearing on library card
- Bug fix: Bring window of running instance to foreground if you start gamevault
- Bug fix: Center user background image on window resize
- Bug fix: Game settings form data flies in from outside the window

## 1.8.3
Recommended Gamevault Server Version: `v10.2.0`
### Changes
- Bug fix: Re-Download icon not appearing on library cards. Also improved loading times by create library cards content only by hover over.
- Bug fix: Game webite text no longer has the clickable style if no value has been set on GameView 

## 1.8.2
Recommended Gamevault Server Version: `v10.1.0`
### Changes
- Write permissions of the root folder are now checked on selection
- The scrollbar in the download tab now covers the entire tab
- Improved error messages
- Sort installed games by last played
- The download icon in the game view now changes depending on whether the game has already been downloaded
- Added more details to gamesettings ring chart
- Changed default background of game view
- You can now also open the user settings from the settings tab
- The mouse wheel now scrolls horizontally in the installed game scrollbar when visible
- A message is now displayed when an admin tries to change their role
- Added Troubleshoot message to the library expander if the library fails to load
- Bug fix: The library does not load automatically on first login

## 1.8.1
Recommended Gamevault Server Version: `v10.0.2`
### Changes
- Implemented to display whether and how many filters are active
- Added clear all filters Button
- Added Early Access Flag to the Game View
- Library outer Scrollbar is now scrolling if inner Scrollbar has zero offset
- Bug fix: Standard release date filters did not work if no release year was set in existing games
- Bug fix: If the profile picture of another user has been changed, the profile picture has been changed in the top left-hand corner.
- Bug fix: Game Type is now displayed in more user friendly values
- Bug fix: Crash when searching in the installed games search bar while the list was empty
- Bug fix: Installed Games executable auto picker only worked if you were in the game settings at least once
- Bug fix: It was possible to save an image while it was not fully loaded
- Bug fix: Crash if you click on the Download or Settings button before the GameView has fully loaded
- Bug fix: Progesses from other users were cut off in GameView
- Bug fix: Lower boxart quality in the GameView

## 1.8.0
Recommended Gamevault Server Version: `v10.0.0`
### Changes
-Installation tab has been integrated into the Library tab
-Graphic overhaul of the Library
-Graphic overhaul of the GameView
-New user interface for Game Settings and User Settings
-All text icons have been replaced with graphic icons
-Added optional User Message to the Crash Report Window
-Added Game Launch Parameter
-Games can now be started and downloaded from the Library and the GameView
-Progresses from other users are displayed in the GameView

## 1.7.3
Recommended Gamevault Server Version: `v9.0.0`
### Changes
- Switched to non-deprecated APIs
- Bug fix: Rare case in the time tracker where a game was not recognized as a game

## 1.7.2
### Changes
- Bug fix: The installer executable selection list gave an error if there was an executable file with the same name in one of the subfolders

## 1.7.1
### Changes
- Current server version is now displayed in the Admin Console. Additionally a message is displayed when the server is outdated
- Bug fix: Error when selecting script as installer in the Download Tab
- Bug fix: Current selected executable turned Blank after open the executable dropdown in the Installation Tab

## 1.7.0
### Changes
- Added support for more executable files in the download tab
- When you remove the download, the installation folder will be deleted automatically if it is empty
- Added Refresh button to Admin Console
- Added desktop shortcut creation button to install tab
- Added Databse Backup-Restore functionality to the admin console
- Bug fix: Auto extraction sometimes failed
- Bug fix: The executable selection sometimes did not respond correctly to the mouse input
- Bug fix: The One Instance functionality was incorrectly executed after the update check
- Bug fix: Problems in the installation Tab when uninstall->reinstall Games
- Bug fix: New users do not show on the community tab without client restart
- Bug fix: Last selected user is not refreshed, when entering the community tab
- Bug fix: Added a few more safeties in the offline cache

## 1.6.2
### Changes
- Bug fix: Update Notification for non Microsoft Store Version was shown in the Microsoft Store Version
- Bug fix: Error while setting the installation path to Clipboard

## 1.6.1
### Changes
- Admins can delete user progress
- Bug fix: Setup games were not uninstalled correctly
- Bug fix: Users could start a download in offline mode
- Bug fix: When changing the role of a user, the user was deactivated
- Bug fix: Progress for deleted games are now displayed. Also added validation for this in the game view

## 1.6.0
### Changes
- Faster Rawg search in the game view
- More detailed error messages when downloading
- Added support for more executable files
- Added get random game button to library
- Added new Exception Window where you can choose whether to send a crash report or not
- Show certain data in the gameview only if they are available
- Update Notification for new Versions in the non Microsoft Store Releases
- Check for offline cache integrity
- Uninstall games
- Bug fix: Crash if you navigate to Settings/Data and never downloaded a game before
- Bug fix: Image was requested although an invalid image id was specified
- Bug fix: Game Title was also not displayed when the Release Date was not set
- Bug fix: Could not navigate to errorlog on Microsoft Store version

## 1.5.0
### Changes
- Reorganized settings tab
- "Auto-Extract Downloaded Games" option added to settings
- Image cache and offline cache size is displayed in settings
- Added Logout option to settings
- Added Download Limit to settings
- Added re-index button to Admin Console
- Email, First Name, and Last Name are not mandatory for registration anymore
- Download progress is now displayed in the taskbar
- It is now possible to upload images from the client 

## 1.4.1
### Changes
- Bug fix: Image optimization was skipped if it was not a Microsoft store version
- Bug fix: System tray icon was still visible after closing the application until hovering over it with the mouse
- Bug fix: Incompatible request header for download in server version 3. Fallback included for downloading regardless.

## 1.4.0
### Changes
- In case of an unhandled exception, you can now navigate directly to the error logs
- In the admin console, you can now jump directly to the profiles
- Support single file binary
- The selectable executables are now trimmed in the installation tab
- Enter key on forms now submits forms
- Bug fix: Dealing with undetected game types in the installation process
- Bug fix: A long error message in the download tab, no longer rescales the individual elements
- Bug fix: Download speed not shown, when it's KB/s and below 

## 1.3.0
### Changes
- Installation pipeline implemented. The UI for the download tab has been completely overhauled. GameVault is now able to extract downloaded game archives and perform the installation process depending on the game type.
- Added new game types filter in the library
- Bug fix: The box images were cut off in the Library tab. The format has now been changed to 2:3, so that you can always see the whole image.
- Bug fix: The box image titles in the library tab now have a wrap, so that even longer titles are readable
- The "App is still running in the system tray" message will now only be displayed once per installation
- Other UI changes especially in terms of spacing and round corners
- Bug fix: When re-loading a user in the community tab, the filter was not sorting the progresses correctly afterwards

## 1.2.1
### Changes
- Autostart fix (For self build and Microsoft Store)

## 1.2.0
### Changes
- Renamed Crackpipe to Gamevault
- Automatic migration of existing data

## 1.1.0
### Changes
- Show asterisks in registration password fields instead of clear text
- Notify user, when app is still running in the system tray
- Show mapped RAWG Game Title as Tooltip when you hover over game name on game view
- Make Images optional for registration
- Rework Playtime UI in Game View
- Auto load library as default in the settings

## 1.0.1
### Changes
- Bug fix: When registering a new user, the message "Each field must be filled" was displayed, although all fields were set.

## 1.0.0
### Changes
- Initial Release
