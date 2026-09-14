# v5 verification

Validated in Unity 6000.5.4f1 on macOS, using the included URP project.

Automated Play-mode checks passed:

- Runtime scene/camera bootstrap and restart back to the main menu.
- Character selection starts a fresh 900-second countdown; menus do not consume time.
- Leo and Maya each complete all six puzzle stages.
- Visible champion, computer, token-order and tournament clues match the selected route.
- The other character's locker answer is rejected.
- Both routes keep identical room transform positions.
- Red, blue and yellow tokens collect using the real nearby-pickup component.
- Separate completion records: Leo does not complete Maya; Maya does not erase Leo.
- Pause freezes the countdown; the journal consumes time.
- Expiration in normal play and inside a modal gives Game Over and disables controls.
- Closing an expired modal cannot resume the game.
- Both humanoid rigs, idle/walk/sprint states, normal human-scale bounds and supported materials.

Rendered office and character previews were inspected. These are detailed student-game assets, not photorealistic scans. Clothing is original generated game geometry; animation is retargeted from the free Quaternius library.

The tests use controlled interactions and camera renders, not a full manual mouse-and-keyboard playthrough of every UI button. An unrelated Unity Editor Search indexing exception occurred in batch logs and was excluded from runtime-error checks. It did not prevent the gameplay tests completing.

Run the included Editor test entry point `ArcadeLockdown.Editor.ArcadeV5Validation.Run` in Unity batch mode to repeat these checks. Tests temporarily isolate and then restore the two completion records.
