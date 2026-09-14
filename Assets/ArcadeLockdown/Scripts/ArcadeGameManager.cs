using System;
using System.Collections.Generic;
using UnityEngine;
using Input = ArcadeLockdown.ArcadeInput;
using UnityEngine.SceneManagement;

namespace ArcadeLockdown
{
    /// <summary>
    /// Owns progression, inventory, clues, puzzle screens, HUD, timer and ending.
    /// The UI uses IMGUI so the package needs no font, canvas or UI dependencies.
    /// </summary>
    public sealed class ArcadeGameManager : MonoBehaviour
    {
        public static ArcadeGameManager Instance { get; private set; }
        public bool CanControlPlayer => state == GameState.Playing;

        private enum GameState { MainMenu, CharacterSelect, Playing, Modal, Paused, Escaped, GameOver }
        public const float TimeLimit = 15f * 60f;
        public float RemainingTime => Mathf.Max(0f, TimeLimit - elapsedTime);
        public EscapeRoute Route { get; private set; } = new EscapeRoute(false);
        public string StateName => state.ToString();
        private enum ModalKind { None, Information, Keypad, Circuit, Instructions, Journal }

        private GameState state = GameState.MainMenu;
        private ModalKind modal = ModalKind.None;
        private GameState stateBeforeModal = GameState.Playing;
        private Camera playerCamera;
        private ThirdPersonController player;
        private Interactable focused;
        private readonly Dictionary<string, DoorAnimator> doors = new Dictionary<string, DoorAnimator>();
        private readonly HashSet<string> inventory = new HashSet<string>();
        private readonly List<string> inventoryOrder = new List<string>();
        private readonly List<string> journal = new List<string>();

        private AudioSource effectsSource;
        private AudioSource ambienceSource;
        private AudioClip clickSound;
        private AudioClip errorSound;
        private AudioClip successSound;

        private string modalTitle = string.Empty;
        private string modalBody = string.Empty;
        private string keypadId = string.Empty;
        private string enteredCode = string.Empty;
        private string feedback = string.Empty;
        private float feedbackUntil;
        private float elapsedTime;
        private int puzzlesSolved;

        private bool lockerSolved;
        private bool officeOpened;
        private bool computerSolved;
        private bool prizeSolved;
        private bool storageOpened;
        private bool fuseCollected;
        private bool powerRestored;
        private bool exitSolved;
        private readonly int[] wireRotations = { 1, 3, 2, 1 };
        private readonly int[] wireSolution = { 0, 1, 3, 2 };

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centeredBodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;
        private GUIStyle codeStyle;
        private GUIStyle promptStyle;
        private GUIStyle objectiveStyle;
        private Texture2D darkTexture;
        private Texture2D panelTexture;
        private Texture2D cyanTexture;

        private void Awake()
        {
            Instance = this;

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            clickSound = RetroAudio.Beep("Key click", 560f, 0.055f, 0.16f);
            errorSound = RetroAudio.Beep("Invalid code", 105f, 0.22f, 0.25f);
            successSound = RetroAudio.Success();

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = RetroAudio.Hum();
            ambienceSource.loop = true;
            ambienceSource.volume = 0.55f;
            ambienceSource.Play();

            journal.Add("OBJECTIVE: Escape through the front security door.");
        }

        public void RegisterPlayer(ThirdPersonController controller, Camera camera)
        {
            player = controller;
            playerCamera = camera;
        }

        public void RegisterDoor(string id, DoorAnimator door)
        {
            doors[id] = door;
        }

        public void FinishSetup()
        {
            foreach (PowerReactive reactive in FindObjectsByType<PowerReactive>())
                reactive.SetPowered(false);
            SetCursor(false);
        }

        private void Update()
        {
            if (state == GameState.Playing || (state == GameState.Modal && stateBeforeModal == GameState.Playing))
            {
                elapsedTime = Mathf.Min(TimeLimit, elapsedTime + Time.deltaTime);
                if (RemainingTime <= 0f) EndByTimeout();
            }

            if (state == GameState.MainMenu || state == GameState.CharacterSelect || state == GameState.Escaped || state == GameState.GameOver)
                return;

            if (state == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) Resume();
                return;
            }

            if (state == GameState.Modal)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseModal();
                if (modal == ModalKind.Keypad) ReadKeypadKeyboard();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Pause();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                OpenJournal();
                return;
            }
            if (Input.GetKeyDown(KeyCode.H))
                ShowToast(CurrentHint(), 4f);

            UpdateFocus();
            if (focused != null && Input.GetKeyDown(KeyCode.E))
                focused.Use();
        }

        private void UpdateFocus()
        {
            focused = null;
            if (playerCamera == null) return;
            // Tokens remain easy to select even when the crosshair misses the coin.
            float nearestToken = 2.4f;
            foreach (TokenPickup token in FindObjectsByType<TokenPickup>())
            {
                float distance = player == null ? float.MaxValue : Vector3.Distance(player.transform.position, token.transform.position);
                if (distance < nearestToken && token.IsReachable(player.transform, 2.2f))
                {
                    nearestToken = distance;
                    focused = token.GetComponent<Interactable>();
                }
            }
            if (focused != null) return;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                    out RaycastHit hit, 9f, ~(1 << 2), QueryTriggerInteraction.Ignore) &&
                player != null && Vector3.Distance(player.transform.position, hit.point) <= 3.35f)
            {
                focused = hit.collider.GetComponentInParent<Interactable>();
            }
        }

        public void HandleInteraction(Interactable item)
        {
            if (state != GameState.Playing) return;
            switch (item.kind)
            {
                case InteractionKind.Information:
                    OpenInformation("INSPECT", item.information);
                    break;
                case InteractionKind.LockerKeypad:
                    if (lockerSolved) OpenInformation("STAFF LOCKER", "The locker is open. You already collected the OFFICE KEY.");
                    else OpenKeypad("LOCKER", "STAFF LOCKER", "A four-digit lock. The nearby MAX champion clue should reveal the year.");
                    break;
                case InteractionKind.OfficeDoor:
                    if (officeOpened) ShowToast("The office door is already unlocked.");
                    else if (!HasItem("OfficeKey")) ShowToast("OFFICE LOCKED — a key is required.");
                    else
                    {
                        OpenDoor("Office");
                        officeOpened = true;
                        AddJournal("The OFFICE KEY opened the manager's office.");
                        ShowToast("OFFICE UNLOCKED");
                        PlaySuccess();
                    }
                    break;
                case InteractionKind.OfficeComputer:
                    if (computerSolved) OpenInformation("SECURITY CAMERAS", "CAM 03 — STORAGE\nA box marked 42 is visible behind the broken cabinets.");
                    else OpenKeypad("COMPUTER", "MANAGER TERMINAL", "PASSWORD HINT:\nFavorite game machine number + opening year");
                    break;
                case InteractionKind.PrizeKeypad:
                    if (prizeSolved) OpenInformation("PRIZE DRAWER", "The drawer is open. You already collected the SCREWDRIVER.");
                    else if (!HasItem("RedToken") || !HasItem("BlueToken") || !HasItem("YellowToken"))
                        ShowToast("Three colored tokens are needed to understand this lock.");
                    else OpenKeypad("PRIZE", "PRIZE DRAWER", "Enter the token values in the order shown on the clue card.");
                    break;
                case InteractionKind.StorageDoor:
                    if (storageOpened) ShowToast("The storage entrance is open.");
                    else if (!HasItem("Screwdriver")) ShowToast("Four screws cover the lock plate. You need a tool.");
                    else
                    {
                        OpenDoor("Storage");
                        storageOpened = true;
                        puzzlesSolved++;
                        AddJournal("The SCREWDRIVER removed the storage lock plate.");
                        ShowToast("STORAGE OPEN — PUZZLE 4/6");
                        PlaySuccess();
                    }
                    break;
                case InteractionKind.Box42:
                    if (!fuseCollected)
                    {
                        fuseCollected = true;
                        AddItem("Fuse", "REPLACEMENT FUSE");
                        AddJournal("Box 42 contained a REPLACEMENT FUSE for the power room.");
                        OpenInformation("BOX 42", "Inside the dusty box is a working REPLACEMENT FUSE.\n\nIt should fit the arcade's main power console.");
                        item.prompt = "Inspect empty Box 42";
                        item.information = "Box 42 is empty. The fuse is now in your inventory.";
                        item.kind = InteractionKind.Information;
                    }
                    else OpenInformation("BOX 42", "Empty. You already collected the replacement fuse.");
                    break;
                case InteractionKind.PowerConsole:
                    if (powerRestored) OpenInformation("POWER CIRCUIT", "MAIN POWER: ONLINE\nAll four tournament machines are now active.");
                    else if (!HasItem("Fuse")) ShowToast("FUSE MISSING — search the storage room.");
                    else OpenCircuit();
                    break;
                case InteractionKind.ExitKeypad:
                    if (!powerRestored) ShowToast("SECURITY SYSTEM OFFLINE — restore the main power first.");
                    else OpenKeypad("EXIT", "SECURITY LOCK", "ENTER 4-DIGIT EXIT CODE");
                    break;
                case InteractionKind.CollectItem:
                    if (!HasItem(item.itemId))
                    {
                        AddItem(item.itemId, item.itemDisplayName);
                        ShowToast(item.itemDisplayName + " COLLECTED");
                        PlaySuccess();
                        Destroy(item.gameObject);
                    }
                    break;
                case InteractionKind.UnlockedDoor:
                    OpenDoor(item.itemId);
                    item.prompt = item.itemDisplayName + " is open";
                    ShowToast(item.itemDisplayName + " OPEN");
                    PlayClick();
                    break;
            }
        }

        private void OpenDoor(string id)
        {
            if (doors.TryGetValue(id, out DoorAnimator door)) door.Open();
        }

        private bool HasItem(string id) => inventory.Contains(id);

        private void AddItem(string id, string displayName)
        {
            if (!inventory.Add(id)) return;
            inventoryOrder.Add(displayName);
        }

        private void AddJournal(string entry)
        {
            entry = Route.Clue(entry);
            if (!journal.Contains(entry)) journal.Add(entry);
        }

        private void StartPlaying()
        {
            state = GameState.Playing;
            modal = ModalKind.None;
            SetCursor(true);
            ShowToast("FIND THE 4-DIGIT EXIT CODE — press H for a hint", 5f);
        }

        public void ChooseCharacter(bool female)
        {
            if (state != GameState.CharacterSelect && state != GameState.MainMenu) return;
            Route = new EscapeRoute(female);
            Route.ApplyWorldClues();
            System.Array.Copy(Route.Circuit, wireSolution, 4);
            ArcadeWorldBuilder.ReplacePlayerCharacter(player, female);
            elapsedTime = 0f;
            Time.timeScale = 1f;
            AddJournal(Route.Name + " ROUTE: You have 15 minutes. Clues and answers belong to this character's run.");
            StartPlaying();
        }

        private void EndByTimeout()
        {
            state = GameState.GameOver;
            modal = ModalKind.None;
            focused = null;
            SetCursor(false);
            Time.timeScale = 0f;
            effectsSource.PlayOneShot(errorSound);
        }

        private void Pause()
        {
            state = GameState.Paused;
            Time.timeScale = 0f;
            SetCursor(false);
        }

        private void Resume()
        {
            state = GameState.Playing;
            Time.timeScale = 1f;
            SetCursor(true);
        }

        private void OpenInformation(string title, string text)
        {
            stateBeforeModal = state == GameState.MainMenu ? GameState.MainMenu : GameState.Playing;
            modalTitle = Route.Clue(title);
            modalBody = Route.Clue(text);
            modal = ModalKind.Information;
            state = GameState.Modal;
            SetCursor(false);
        }

        private void OpenKeypad(string id, string title, string text)
        {
            stateBeforeModal = GameState.Playing;
            keypadId = id;
            modalTitle = Route.Clue(title);
            modalBody = Route.Clue(text);
            enteredCode = string.Empty;
            feedback = string.Empty;
            modal = ModalKind.Keypad;
            state = GameState.Modal;
            SetCursor(false);
        }

        private void OpenCircuit()
        {
            stateBeforeModal = GameState.Playing;
            modalTitle = "MAIN POWER CIRCUIT";
            modalBody = "The replacement fuse fits. Rotate the four wire tiles to match the maintenance diagram, then activate the circuit.";
            modal = ModalKind.Circuit;
            state = GameState.Modal;
            SetCursor(false);
        }

        private void OpenJournal()
        {
            stateBeforeModal = GameState.Playing;
            modalTitle = "CLUE JOURNAL";
            modalBody = string.Join("\n\n", journal);
            modal = ModalKind.Journal;
            state = GameState.Modal;
            SetCursor(false);
        }

        private void CloseModal(bool playClick = true)
        {
            if (state != GameState.Modal) return;
            if (playClick) PlayClick();
            modal = ModalKind.None;
            state = stateBeforeModal == GameState.MainMenu ? GameState.MainMenu : GameState.Playing;
            SetCursor(state == GameState.Playing);
        }

        private void ReadKeypadKeyboard()
        {
            for (int i = 0; i <= 9; i++)
            {
                KeyCode alpha = (KeyCode)((int)KeyCode.Alpha0 + i);
                KeyCode keypad = (KeyCode)((int)KeyCode.Keypad0 + i);
                if (Input.GetKeyDown(alpha) || Input.GetKeyDown(keypad)) AddDigit(i);
            }
            if (Input.GetKeyDown(KeyCode.Backspace)) Backspace();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) SubmitCode();
        }

        private void AddDigit(int digit)
        {
            int maxLength = keypadId == "COMPUTER" ? 6 : 4;
            if (enteredCode.Length >= maxLength) return;
            enteredCode += digit.ToString();
            PlayClick();
        }

        private void Backspace()
        {
            if (enteredCode.Length > 0) enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            PlayClick();
        }

        private void SubmitCode()
        {
            if (state != GameState.Modal || modal != ModalKind.Keypad || RemainingTime <= 0f) return;
            string answer = Route.Answer(keypadId);
            if (enteredCode != answer)
            {
                feedback = "ACCESS DENIED";
                enteredCode = string.Empty;
                effectsSource.PlayOneShot(errorSound);
                return;
            }

            if (keypadId == "LOCKER" && !lockerSolved)
            {
                lockerSolved = true;
                puzzlesSolved++;
                AddItem("OfficeKey", "OFFICE KEY");
                AddJournal("MAX was the first champion. His high score year, 1994, opened the staff locker.");
                CloseModal(false);
                ShowToast("LOCKER OPEN — OFFICE KEY COLLECTED — PUZZLE 1/6", 4f);
                PlaySuccess();
            }
            else if (keypadId == "COMPUTER" && !computerSolved)
            {
                computerSolved = true;
                puzzlesSolved++;
                AddJournal("Manager password 071992 unlocked CAM 03. It showed a box marked 42 in storage.");
                CloseModal(false);
                OpenInformation("CAMERA SYSTEM ONLINE", "CAM 01 — MAIN ARCADE\nCAM 02 — PRIZE ROOM\nCAM 03 — STORAGE: BOX 42\nCAM 04 — POWER ROOM\n\nThe camera clearly shows 42 behind the storage boxes.");
                ShowToast("SECURITY CAMERAS UNLOCKED — PUZZLE 2/6", 4f);
                PlaySuccess();
            }
            else if (keypadId == "PRIZE" && !prizeSolved)
            {
                prizeSolved = true;
                puzzlesSolved++;
                AddItem("Screwdriver", "SCREWDRIVER");
                AddJournal("Token order BLUE, RED, YELLOW, RED gave 2585. The prize drawer held a screwdriver.");
                CloseModal(false);
                ShowToast("DRAWER OPEN — SCREWDRIVER COLLECTED — PUZZLE 3/6", 4f);
                PlaySuccess();
            }
            else if (keypadId == "EXIT" && !exitSolved)
            {
                exitSolved = true;
                puzzlesSolved++;
                OpenDoor("Exit");
                CompleteGame();
            }
        }

        private void CheckCircuit()
        {
            if (state != GameState.Modal || modal != ModalKind.Circuit || RemainingTime <= 0f) return;
            for (int i = 0; i < wireSolution.Length; i++)
            {
                if (wireRotations[i] != wireSolution[i])
                {
                    feedback = "NO CONTINUITY — one or more wires are misaligned.";
                    effectsSource.PlayOneShot(errorSound);
                    return;
                }
            }

            powerRestored = true;
            puzzlesSolved++;
            foreach (PowerReactive reactive in FindObjectsByType<PowerReactive>())
                reactive.SetPowered(true);
            AddJournal("Power restored. Tournament order: SPACE, RACE, BLOCK, PAC. The screens show 3, 1, 7, 8.");
            CloseModal(false);
            ShowToast("POWER RESTORED — ALL MACHINES ONLINE — PUZZLE 5/6", 5f);
            PlaySuccess();
        }

        private void CompleteGame()
        {
            state = GameState.Escaped;
            modal = ModalKind.None;
            SetCursor(false);
            float best = PlayerPrefs.GetFloat(Route.BestTimeKey, 0f);
            if (best <= 0f || elapsedTime < best)
            {
                PlayerPrefs.SetFloat(Route.BestTimeKey, elapsedTime);
                PlayerPrefs.Save();
            }
            PlaySuccess();
        }

        private string CurrentObjective()
        {
            if (!lockerSolved) return "Find the staff locker code";
            if (!officeOpened) return "Use the office key";
            if (!computerSolved) return "Unlock the manager terminal";
            if (!prizeSolved) return "Collect 3 tokens and open the prize drawer";
            if (!storageOpened) return "Use the screwdriver on storage";
            if (!fuseCollected) return "Find Box 42 in storage";
            if (!powerRestored) return "Repair the main power circuit";
            return "Read the four machines and unlock the exit";
        }

        private string CurrentHint()
        {
            if (!lockerSolved) return "HINT: Inspect the HIGH SCORES and the FIRST CHAMPION poster.";
            if (!officeOpened) return "HINT: The office door is on the east side of the main arcade.";
            if (!computerSolved) return "HINT: Combine machine 07 with the opening year 1992.";
            if (!prizeSolved) return "HINT: BLUE=2, RED=5, YELLOW=8. Read the clue card's order.";
            if (!storageOpened) return "HINT: The storage lock plate is behind the prize room.";
            if (!fuseCollected) return "HINT: CAM 03 showed Box 42.";
            if (!powerRestored) return "HINT: Match every wire rotation to the maintenance diagram.";
            return "HINT: Tournament order is SPACE, RACE, BLOCK, PAC.";
        }

        private void ShowToast(string text, float duration = 2.8f)
        {
            feedback = Route.Clue(text);
            feedbackUntil = Time.unscaledTime + duration;
        }

        private void PlayClick() => effectsSource.PlayOneShot(clickSound);
        private void PlaySuccess() => effectsSource.PlayOneShot(successSound);

        private void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private string FormatTime(float value)
        {
            int total = Mathf.FloorToInt(value);
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (state == GameState.MainMenu) DrawMainMenu();
            else if (state == GameState.CharacterSelect) DrawCharacterSelection();
            else if (state == GameState.GameOver) DrawGameOver();
            else if (state == GameState.Modal && stateBeforeModal == GameState.MainMenu) DrawModal();
            else if (state == GameState.Paused) DrawPauseMenu();
            else if (state == GameState.Escaped) DrawEnding();
            else
            {
                DrawHud();
                if (state == GameState.Modal) DrawModal();
            }
        }

        private void DrawMainMenu()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            float center = Screen.width * 0.5f;
            GUI.Label(new Rect(center - 410, Screen.height * 0.12f, 820, 160), "ARCADE\nLOCKDOWN", titleStyle);
            GUI.Label(new Rect(center - 360, Screen.height * 0.35f, 720, 50), "A 199X THIRD-PERSON ESCAPE", subtitleStyle);

            float y = Screen.height * 0.48f;
            if (GUI.Button(new Rect(center - 150, y, 300, 58), "> NEW GAME", buttonStyle))
            {
                PlayClick();
                state = GameState.CharacterSelect;
            }
            if (GUI.Button(new Rect(center - 150, y + 72, 300, 58), "HOW TO PLAY", buttonStyle))
            {
                PlayClick();
                stateBeforeModal = GameState.MainMenu;
                modalTitle = "HOW TO PLAY";
                modalBody = "WASD  Move\nMOUSE  Look\nSHIFT  Sprint\nE  Interact\nTAB  Clue journal\nH  Current hint\nESC  Pause / close panel\n\nExplore carefully. Codes found in one room unlock progress elsewhere.";
                modal = ModalKind.Instructions;
                state = GameState.Modal;
            }
            if (GUI.Button(new Rect(center - 150, y + 144, 300, 58), "QUIT", buttonStyle)) QuitGame();
            GUI.Label(new Rect(20, Screen.height - 40, Screen.width - 40, 30), "Two escape routes • 15 minutes each • Free art credits included in the project", smallStyle);
        }

        private void DrawCharacterSelection()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            float cx = Screen.width * .5f;
            GUI.Label(new Rect(cx - 450, 40, 900, 90), "CHOOSE YOUR CHARACTER", headingStyle);
            GUI.Label(new Rect(cx - 420, 115, 840, 60), "Same rooms. Different clues. One 15-minute escape per character.", centeredBodyStyle);
            float width = Mathf.Min(350, Screen.width * .38f);
            float top = 200, height = Mathf.Max(200, Screen.height - 345);
            for (int i = 0; i < 2; i++)
            {
                Rect card = new Rect(cx + (i == 0 ? -width - 20 : 20), top, width, height);
                GUI.Box(card, GUIContent.none, panelStyle);
                Texture portrait = Resources.Load<Texture2D>("Characters/Portraits/" + (i == 0 ? "Leo" : "Maya"));
                if (portrait != null) GUI.DrawTexture(new Rect(card.x + 12, card.y + 12, card.width - 24, card.height - 110), portrait, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(card.x, card.yMax - 96, card.width, 36), i == 0 ? "LEO / MALE" : "MAYA / FEMALE", headingStyle);
                float best = PlayerPrefs.GetFloat(new EscapeRoute(i == 1).BestTimeKey, 0);
                GUI.Label(new Rect(card.x + 12, card.yMax - 56, card.width - 24, 44), best > 0 ? "ESCAPED / BEST " + FormatTime(best) : "NOT YET ESCAPED", centeredBodyStyle);
                if (GUI.Button(new Rect(card.x, card.yMax + 15, card.width, 52), "PLAY AS " + (i == 0 ? "LEO" : "MAYA"), buttonStyle)) ChooseCharacter(i == 1);
            }
            if (GUI.Button(new Rect(20, Screen.height - 62, 130, 42), "BACK", buttonStyle)) state = GameState.MainMenu;
        }

        private void DrawGameOver()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            float cx = Screen.width * .5f;
            GUI.Label(new Rect(cx - 400, Screen.height * .18f, 800, 100), "TIME'S UP", titleStyle);
            GUI.Label(new Rect(cx - 350, Screen.height * .38f, 700, 140), "GAME OVER\n" + Route.Name + " did not escape within 15 minutes.\n" + puzzlesSolved + " / 6 puzzles solved", centeredBodyStyle);
            if (GUI.Button(new Rect(cx - 180, Screen.height * .66f, 360, 56), "CHOOSE CHARACTER / RETRY", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(cx - 180, Screen.height * .66f + 72, 360, 56), "QUIT", buttonStyle)) QuitGame();
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(18, 18, 465, 84), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34, 28, 430, 26), Route.Name + " / CURRENT OBJECTIVE", smallStyle);
            GUI.Label(new Rect(34, 53, 430, 38), Route.Clue(CurrentObjective()), objectiveStyle);
            Color previousColor = GUI.color;
            if (RemainingTime <= 60f) GUI.color = new Color(1f,.35f,.3f);
            GUI.Label(new Rect(Screen.width - 220, 22, 190, 40), FormatTime(Mathf.Ceil(RemainingTime)), headingStyle);
            GUI.color = previousColor;
            GUI.Label(new Rect(Screen.width - 190, 61, 160, 28), puzzlesSolved + " / 6 PUZZLES", smallStyle);

            string items = inventoryOrder.Count == 0 ? "INVENTORY: EMPTY" : "INVENTORY:  " + string.Join("   •   ", inventoryOrder);
            GUI.Box(new Rect(18, Screen.height - 62, Screen.width - 36, 42), GUIContent.none, panelStyle);
            GUI.Label(new Rect(32, Screen.height - 53, Screen.width - 64, 30), items, smallStyle);

            GUI.Label(new Rect(Screen.width * 0.5f - 15, Screen.height * 0.5f - 22, 30, 44), "+", codeStyle);
            if (focused != null && state == GameState.Playing)
            {
                string prompt = "[ E ]  " + focused.prompt;
                GUI.Label(new Rect(Screen.width * 0.5f - 260, Screen.height * 0.72f, 520, 48), prompt, promptStyle);
            }

            if (!string.IsNullOrEmpty(feedback) && Time.unscaledTime < feedbackUntil)
                GUI.Label(new Rect(Screen.width * 0.5f - 430, 112, 860, 48), feedback, promptStyle);
        }

        private void DrawModal()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            Rect panel = new Rect(Screen.width * 0.5f - 340, Screen.height * 0.5f - 300, 680, 600);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 35, panel.y + 24, panel.width - 70, 54), modalTitle, headingStyle);
            GUI.Label(new Rect(panel.x + 42, panel.y + 86, panel.width - 84, 120), modalBody, bodyStyle);

            if (modal == ModalKind.Keypad) DrawKeypad(panel);
            else if (modal == ModalKind.Circuit) DrawCircuit(panel);
            else
            {
                if (GUI.Button(new Rect(panel.center.x - 110, panel.yMax - 76, 220, 48), "CLOSE", buttonStyle))
                    CloseModal();
            }
        }

        private void DrawKeypad(Rect panel)
        {
            string display = enteredCode;
            int length = keypadId == "COMPUTER" ? 6 : 4;
            while (display.Length < length) display += "_";
            GUI.Label(new Rect(panel.x + 100, panel.y + 188, panel.width - 200, 58), display, codeStyle);

            float startX = panel.center.x - 126;
            float startY = panel.y + 258;
            int number = 1;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int digit = number++;
                    if (GUI.Button(new Rect(startX + col * 88, startY + row * 58, 76, 48), digit.ToString(), buttonStyle)) AddDigit(digit);
                }
            }
            if (GUI.Button(new Rect(startX, startY + 174, 76, 48), "<", buttonStyle)) Backspace();
            if (GUI.Button(new Rect(startX + 88, startY + 174, 76, 48), "0", buttonStyle)) AddDigit(0);
            if (GUI.Button(new Rect(startX + 176, startY + 174, 76, 48), "OK", buttonStyle)) SubmitCode();
            if (!string.IsNullOrEmpty(feedback))
                GUI.Label(new Rect(panel.x + 70, panel.yMax - 98, panel.width - 140, 34), feedback, promptStyle);
        }

        private void DrawCircuit(Rect panel)
        {
            GUI.Label(new Rect(panel.x + 55, panel.y + 205, panel.width - 110, 36), Route.Clue("MAINTENANCE TARGET:   0°     90°     270°     180°"), centeredBodyStyle);
            float startX = panel.center.x - 270;
            for (int i = 0; i < 4; i++)
            {
                string label = "WIRE " + (i + 1) + "\n" + (wireRotations[i] * 90) + "°\n↻";
                int captured = i;
                if (GUI.Button(new Rect(startX + i * 138, panel.y + 260, 120, 112), label, buttonStyle))
                {
                    wireRotations[captured] = (wireRotations[captured] + 1) % 4;
                    PlayClick();
                }
            }
            GUI.Label(new Rect(panel.x + 80, panel.y + 390, panel.width - 160, 42), "A ── [1] ── [2] ── [3] ── [4] ── C", codeStyle);
            if (GUI.Button(new Rect(panel.center.x - 145, panel.y + 455, 290, 52), "ACTIVATE POWER", buttonStyle)) CheckCircuit();
            if (!string.IsNullOrEmpty(feedback))
                GUI.Label(new Rect(panel.x + 45, panel.yMax - 80, panel.width - 90, 42), feedback, promptStyle);
        }

        private void DrawPauseMenu()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            float x = Screen.width * 0.5f - 160;
            GUI.Label(new Rect(x - 100, Screen.height * 0.25f, 520, 80), "PAUSED", titleStyle);
            if (GUI.Button(new Rect(x, Screen.height * 0.48f, 320, 56), "RESUME", buttonStyle)) Resume();
            if (GUI.Button(new Rect(x, Screen.height * 0.48f + 72, 320, 56), "RESTART", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(x, Screen.height * 0.48f + 144, 320, 56), "QUIT", buttonStyle)) QuitGame();
        }

        private void DrawEnding()
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkTexture);
            float best = PlayerPrefs.GetFloat(Route.BestTimeKey, elapsedTime);
            GUI.Label(new Rect(Screen.width * 0.5f - 450, Screen.height * 0.12f, 900, 120), "YOU ESCAPED!", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 300, Screen.height * 0.34f, 600, 220),
                Route.Name + " ESCAPED\n\nESCAPE TIME     " + FormatTime(elapsedTime) +
                "\nBEST TIME       " + FormatTime(best) +
                "\nPUZZLES SOLVED  " + puzzlesSolved + " / 6\n\nTHANKS FOR PLAYING", centeredBodyStyle);
            if (GUI.Button(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.72f, 300, 56), "PLAY AGAIN", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.72f + 70, 300, 56), "QUIT", buttonStyle)) QuitGame();
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            darkTexture = MakeTexture(new Color(0.005f, 0.004f, 0.015f, 0.91f));
            panelTexture = MakeTexture(new Color(0.025f, 0.02f, 0.065f, 0.96f));
            cyanTexture = MakeTexture(new Color(0.05f, 0.7f, 0.8f, 0.75f));

            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 68, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.1f, 0.95f, 1f) } };
            subtitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.15f, 0.75f) } };
            headingStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 27, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.15f, 0.95f, 1f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = 20, wordWrap = true, normal = { textColor = new Color(0.9f, 0.92f, 1f) } };
            centeredBodyStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.55f, 0.85f, 0.95f) } };
            objectiveStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.2f, 1f, 0.85f) } };
            codeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.25f, 0.78f) } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture } };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = panelTexture },
                hover = { textColor = Color.black, background = cyanTexture },
                active = { textColor = Color.white, background = cyanTexture }
            };
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
