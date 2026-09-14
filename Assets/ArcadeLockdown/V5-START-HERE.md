# Arcade Lockdown v5 — Two escape routes

## Import and play

1. Use Unity 6 and a **Universal 3D (URP)** project. This release is tested with Unity 6000.5.4f1.
2. Stop Play mode. Back up your project, especially personal edits inside `Assets/ArcadeLockdown`.
3. Select **Assets > Import Package > Custom Package**, choose `ArcadeLockdown-TwoRoutes-v5.unitypackage`, and import **all** files.
4. Keep the existing folder name. Matching assets update in place; renaming/copying the old script folder can create duplicate C# classes.
5. Wait for compilation. Select **Arcade Lockdown > 1 - Open Game Scene**.
6. Press Play. The rooms and camera are generated at runtime, so an empty edit-mode scene is expected.
7. Select **NEW GAME**, then **PLAY AS LEO** or **PLAY AS MAYA**.

Your existing imported v4 scene is updated by the scripts. No need to drag another room/player prefab into it. Prefabs are provided separately for editing and reuse, not automatically substituted into the generated scene.

## What changed

- Office: new beveled walnut desk, drawer pulls, monitor/stand, separate keyboard keys, tower vents/ports, mouse/mat, telephone, document trays and pens.
- Office chair: cushioned seat/back/lumbar support, armrests, gas lift, five-star metal base, twin casters and height lever.
- Smaller office signs, fitted readable labels, password note moved from the keyboard to the wall.
- Male Leo and female Maya selection after New Game; textured faces/hair, casual tops, long trousers and shoes; humanoid idle/walk/sprint animations.
- Same room architecture, objects and pickup positions for both routes. Locker/computer codes, token order, wire rotations and tournament order differ. Shared tool/fuse steps are retained.
- Separate best escape time for each character. Both character cards start as NOT YET ESCAPED, then show that character's best time after winning.

The new characters are **late-teen-inspired adaptations** of the free Quaternius Universal Base Characters adult bases. They are not the dedicated Teen meshes from the paid edition, and are not photorealistic scans. No paid assets are bundled.

## The 15-minute rule

- Countdown starts at **15:00 only after choosing a character**.
- Reading clues, entering codes and opening the journal use time.
- Pressing **Esc during normal play** opens the pause menu and stops the timer. Resume continues the same run. Esc inside a puzzle closes that panel instead.
- At **00:00**, Game Over stops controls and interactions, including any open puzzle panel.
- Retry returns to the main menu with fresh puzzles, inventory and a full 15 minutes after choosing again.
- Winning records elapsed escape time separately for Leo and Maya.

## Controls

WASD move; mouse look; mouse wheel camera zoom; Shift sprint; E interact; H hint; Tab journal; Esc pause/close panel.

Tokens collect automatically when close enough and reachable. Red is on the side table near the exit; blue is on the office desk; yellow is in front of the prize shelf. Their positions are identical for both characters.

## Walkthrough — spoilers

| Puzzle | Leo | Maya |
| --- | --- | --- |
| Champion / locker | MAX, **1994** | AVA, **1997** |
| Manager computer | machine 07 + 1992 = **071992** | machine 12 + 1993 = **121993** |
| Token drawer | Blue, Red, Yellow, Red = **2585** | Yellow, Red, Blue, Red = **8525** |
| Storage | Use screwdriver, collect fuse from Box 42 | Same tool/fuse step |
| Circuit left to right | **0°, 90°, 270°, 180°** | **180°, 0°, 90°, 270°** |
| Powered machines | SPACE, RACE, BLOCK, PAC = **3178** | BLOCK, PAC, SPACE, RACE = **7831** |

## Editing

`Scripts/OfficeFurniture.cs` builds the office props. `TeenCharacters.cs` assembles and dresses the characters. `TeenCharacterAnimator.cs` drives the humanoid animations. `EscapeRoute.cs` keeps clues and answers consistent. `ArcadeGameManager.cs` owns selection, timer, progression and endings. `ArcadeBootstrap.cs` also rebuilds the world on retry.

Editable reusable models are in `Prefabs`: Manager-desk, Manager-chair---upholstered-swivel, Leo-Character and Maya-Character, alongside the existing arcade machines and rooms. Changes to these standalone prefabs do not replace the runtime-generated versions; change the corresponding builder code for those.

## Assets and credits

See `ThirdPartyNotices/V5_ASSET_SOURCES.txt` and the included licenses. The existing arcade/PBR resources remain in place. Basketball and air hockey are still display animations, not new playable sports minigames.

If Unity light/speaker symbols obstruct the Game view, turn **Gizmos off** or use **Arcade Lockdown > 4 - Hide Game View Icons**. If Play does not start, inspect the first red Console error; the yellow Input Manager deprecation notice is not a compilation error.
