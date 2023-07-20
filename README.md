# crackpipe-app
![logo](https://gamevau.lt/img/logo-text-and-image-sbs.png)
[You can find the official Website/Documentation here](https://crackpipe.de)

## Library Technical Decision Matrix

> This is probably irrelevant to you if you are not a developer.
> GameVault internally needs to behave different in each of the following scenarios.

<details>
<summary>Scenario 1: No paths exist</summary>  

| Path        | State           |
| :------------- |:-------------:|
| ``D:/GameVault/Downloads/(74) Assassin's Creed Unity/`` | ``empty or non-existent`` |
| ``D:/GameVault/Installations/(74) Assassin's Creed Unity/`` | ``empty or non-existent`` |

**When does this happen**

- The Game was not even downloaded yet.
- The Game was deleted.

**What needs to be done**

- Don't show the game in download or library tab (obviously).

---

</details>

<details>
<summary>Scenario 2: Download Path exists</summary>
  
| Path        | State           |
| :------------- |:-------------:|
| ``D:/GameVault/Downloads/(74) Assassin's Creed Unity/`` | ``contains the (partial) game.zip`` |
| ``D:/GameVault/Installations/(74) Assassin's Creed Unity/`` | ``empty or non-existent`` |

**When does this happen**

- The Game is still downloading.
- The Game was freshly downloaded but not installed.
- The Game was installed sometime ago but then deleted but the download was never cleared (unlikely)

**What needs to be done**

- Identify the game using the id
- Show the Game as "Downloaded" in the Downloaded Tab
- Show the Game in the library but grey out the play button, inform the user that they **need to install** the game into the folder `D:/GameVault/Installations/(74) Assassin's Creed Unity/` to play and track it using gamevault.
- Change Download button to play button in Library View -> Game Details, link it to the Installations -> Game entry with greyed out play button.

---

</details>

<details>
<summary>Scenario 3: Both paths exist</summary>
  
| Path        | State           |
| :------------- |:-------------:|
| ``D:/GameVault/Downloads/(74) Assassin's Creed Unity/`` | ``contains the game.zip`` |
| ``D:/GameVault/Installations/(74) Assassin's Creed Unity/`` | ``contains game files (.exe)`` |

**When does this happen**

- The Game has been freshly installed and User has not deleted the download yet
- User forgot to delete download files or wants to keep it for offline/archival purposes.

**What needs to be done**

- Identify the game using the id
- Make the game playable in Installations tab
- Offer User to clear the download folder using "Clear All" button, now that the game is installed to save some space.
- Change Download button to play button in Library View -> Game Details, link it to the Installations -> Game entry.
- Cracktime Daemon monitors Game Folder for running exes

---

</details>

<details>
<summary>Scenario 4: Installations Path exists</summary>
  
| Path        | State           |
| :------------- |:-------------:|
| ``D:/GameVault/Downloads/(74) Assassin's Creed Unity/`` | ``empty or non-existent`` |
| ``D:/GameVault/Installations/(74) Assassin's Creed Unity/`` | ``contains game files (.exe)`` |

**When does this happen**

- The Game has been installed and the Download deleted.

**What needs to be done**

- Identify the game using the id
- Make the game playable in Installations tab
- Offer User to clear the download folder using "Clear All" button, now that the game is installed to save some space.
- Change Download button to play button in Library View -> Game Details, link it to the Installations -> Game entry.
- Cracktime Daemon monitors Game Folder for running exes

---

</details>

### License
[![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]

This work is licensed under a
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License][cc-by-nc-sa].

[![CC BY-NC-SA 4.0][cc-by-nc-sa-image]][cc-by-nc-sa]

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg
