ARCADE LOCKDOWN — TWO ESCAPE ROUTES — v5

READ V5-START-HERE.md for the complete current guide and BOTH walkthroughs.
NEW: office monitor/keyboard/tower and upholstered swivel chair; NEW GAME
opens a male/female character selection; separate clue/answer sets and best
times; 15:00 countdown with Game Over at zero. The room layout stays the same.
The new animated characters use free CC0 adult bases adapted into fully clothed,
late-teen-inspired game characters, not photorealistic scans or paid teen models.

IMPORT AND PLAY
1. Stop Play mode. Back up your own edits inside Assets/ArcadeLockdown.
2. Assets > Import Package > Custom Package.
3. Choose ArcadeLockdown-TwoRoutes-v5.unitypackage and click Import All.
4. Wait for model importing and script compilation to finish.
5. Arcade Lockdown > 1 - Open Game Scene.
6. Press Play, click NEW GAME, then choose LEO or MAYA.

Updating v2/v3/v4: import v5 over the existing folder. Matching files keep their
GUIDs and update in place. Your own changes to matching files are replaced.
Do not rename the old ArcadeLockdown folder first: duplicate scripts can result.
Stop and restart Play after importing so the room is rebuilt.

RETAINED IMPROVEMENTS
- New characters normalized to 1.74 m (Leo) and 1.67 m (Maya).
- Camera framed over the shoulder; use the mouse wheel to adjust zoom.
- Detailed cabinets: softened edges, sloped controls, recessed CRT bezels,
  animated game displays, coin mechanisms, speaker grilles and steel fasteners.
- New glass claw cabinet, drink machine, basketball cage/hoop/net, air hockey
  rails/playfield and printed rotating prize wheel.
- Downloaded wood, plaster and metal PBR maps; wall paneling, acoustic ceiling
  grid, ventilation, door pulls and skirting.
- Two interior spotlights plus one directional light cast shadows; the old
  point-light shadow overload is removed.
- World lettering is depth tested: walls block it, and backs are not mirrored.
- Tokens collect automatically nearby or with E, without precise aiming.
- Game-view editor gizmos are hidden when the game enters Play mode.

CONTROLS
WASD move; mouse look; mouse wheel zoom; Left Shift sprint; E interact;
Tab clue journal; H current hint; Esc pause / close.

TOKEN LOCATIONS
RED: main arcade, on the small wooden table to the right of the EXIT.
BLUE: manager office, at the front-left edge of the desk.
YELLOW: prize room, in front of the prize shelf near the storage doorway.
Walk within about 1.35 metres to collect. The inventory confirms collection.
Walls and closed doors block pickup. Locker code 1994 (Leo) or 1997 (Maya)
gives the OFFICE KEY.

SPEAKER / LIGHT / CIRCLE ICONS
These are Unity editor gizmos. In the GAME tab, click Gizmos at top-right to
turn them off, or choose Arcade Lockdown > 4 - Hide Game View Icons.
Sound, lights and reflections remain active. Gizmos do not appear in builds.

PIPELINE AND INPUT
The playable scene supports Built-in and URP. Creating a URP project before
importing is fine. The full project ZIP uses URP.
Both legacy input and the new Input System are supported when enabled.
No separate download of models or textures is needed. HDRP is not configured.

FILES
Scripts: full gameplay, animation, camera, room and machine-building code.
Resources: FBX models, PBR maps, and the depth-tested world-text shader.
ThirdPartyNotices: source links, CC0 license notices and credit details.
Prefabs: 13 editable machine prefabs, desk/chair, two character prefabs and
ConnectedArcadeRooms.prefab, with
separate mesh and material assets. These previews use URP materials. The
playable scene chooses its materials automatically for Built-in or URP.
Original v3 Kenney models remain available as optional source assets.
Visible v4 machine bodies are original geometry made by ArcadeWorldDetails
and ArcadeAttractions, using the included PBR textures.

LEO WALKTHROUGH — SPOILERS (MAYA: see V5-START-HERE.md)
1. MAX's high score gives locker code 1994; collect OFFICE KEY.
2. Open the office and collect blue token. Computer password: 071992.
3. Collect red and yellow tokens. Prize drawer: 2585; get SCREWDRIVER.
4. Open storage; inspect BOX 42 to collect the replacement FUSE.
5. Open power room. Use fuse in the console.
6. Set circuit rotations left-to-right: 0, 90, 270, 180 degrees.
7. Read powered cabinets in SPACE, RACE, BLOCK, PAC order.
8. Exit keypad: 3178.

This is a more detailed student-project arcade with stylized character art
and realistic surface materials, rather than a photorealistic environment.
