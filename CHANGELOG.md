# GameVault App Changelog

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
