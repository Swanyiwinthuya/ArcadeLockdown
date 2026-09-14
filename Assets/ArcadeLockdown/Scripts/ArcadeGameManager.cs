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
        private AudioSource musicSource;
        private readonly AudioClip[] keyTones = new AudioClip[10];

        private string modalTitle = string.Empty;
        private string modalBody = string.Empty;
        private string keypadId = string.Empty;
        private string enteredCode = string.Empty;
        private string feedback = string.Empty;
        private float feedbackUntil;
        private float elapsedTime;
        private float deniedAt = -10f;
        private string pressedKey = string.Empty;
        private float pressedUntil;
        private Vector2 modalScroll;
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
        private Texture2D tapeTexture;
        private Texture2D screwTexture;
        private Texture2D ledOffTexture;
        private Texture2D ledRedTexture;
        private Texture2D ledAmberTexture;
        private GUIStyle frameStyle;
        private GUIStyle tagStyle;
        private GUIStyle screenStyle;
        private GUIStyle monoStyle;
        private GUIStyle deviceStyle;
        private GUIStyle plateStyle;
        private GUIStyle ledLabelStyle;
        private GUIStyle lcdStyle;
        private GUIStyle lcdCaptionStyle;
        private GUIStyle lcdDigitStyle;
        private GUIStyle lcdGhostStyle;
        private GUIStyle lcdDeniedStyle;
        private GUIStyle noteStyle;
        private GUIStyle noteTextStyle;
        private GUIStyle brandStyle;
        private GUIStyle linkStyle;
        private GUIStyle[] keyStyles;
        private GUIStyle[] keyPressedStyles;
        private GUIStyle creditLabelStyle;
        private GUIStyle creditNamesStyle;
        private GUIStyle mayaCardStyle;
        private GUIStyle leoGlowStyle;
        private GUIStyle mayaGlowStyle;
        private GUIStyle leoNameStyle;
        private GUIStyle mayaNameStyle;
        private GUIStyle cardTagStyle;
        private Texture2D portraitFadeTexture;
        private Texture2D barBackTexture;
        private Texture2D redTexture;
        private GUIStyle timerWarningStyle;
        private GUIStyle timerDigitsStyle;
        private GUIStyle timerWarningDigitsStyle;
        private GUIStyle warningTagStyle;
        private GUIStyle smallRightStyle;
        private int tickSecond = -1;
        private GameObject previewRoot;
        private readonly RenderTexture[] previewTextures = new RenderTexture[2];
        private readonly Transform[] previewVisuals = new Transform[2];
        private readonly bool[] previewReady = new bool[2];

        // UI is laid out for a 1080p screen and scaled up on larger or Retina displays.
        private float uiScale = 1f;
        private float ScreenWidth => Screen.width / uiScale;
        private float ScreenHeight => Screen.height / uiScale;
        private static readonly string[] KeyLetters = { " ", " ", "ABC", "DEF", "GHI", "JKL", "MNO", "PQRS", "TUV", "WXYZ" };

        private void Awake()
        {
            Instance = this;

            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            clickSound = RetroAudio.Beep("Key click", 560f, 0.055f, 0.16f);
            errorSound = RetroAudio.Denied();
            successSound = RetroAudio.Success();
            for (int i = 0; i < keyTones.Length; i++) keyTones[i] = RetroAudio.KeyTone(i);

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = RetroAudio.Hum();
            ambienceSource.loop = true;
            ambienceSource.volume = 0.55f;
            ambienceSource.Play();

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = RetroAudio.Music();
            musicSource.loop = true;
            musicSource.volume = 0.22f;
            musicSource.spatialBlend = 0f;
            musicSource.Play();

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
            UpdateMusic();
            for (int i = 0; i < previewVisuals.Length; i++)
                if (previewVisuals[i] != null) previewVisuals[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.unscaledTime * 0.7f + i * 1.7f) * 18f, 0f);
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
            DestroyCharacterPreviews();
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
            modalScroll = Vector2.zero;
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
            deniedAt = -10f;
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
            modalScroll = Vector2.zero;
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
            PressKey(digit.ToString());
            int maxLength = keypadId == "COMPUTER" ? 6 : 4;
            if (enteredCode.Length >= maxLength) return;
            enteredCode += digit.ToString();
            effectsSource.PlayOneShot(keyTones[digit]);
        }

        private void Backspace()
        {
            PressKey("DEL");
            if (enteredCode.Length > 0) enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            PlayClick();
        }

        private void PressKey(string key)
        {
            pressedKey = key;
            pressedUntil = Time.unscaledTime + 0.12f;
        }

        private void SubmitCode()
        {
            if (state != GameState.Modal || modal != ModalKind.Keypad || RemainingTime <= 0f) return;
            PressKey("ENT");
            string answer = Route.Answer(keypadId);
            if (enteredCode != answer)
            {
                feedback = "ACCESS DENIED";
                enteredCode = string.Empty;
                deniedAt = Time.unscaledTime;
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
            uiScale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
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
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            float center = ScreenWidth * 0.5f;
            GUI.Label(new Rect(center - 410, ScreenHeight * 0.12f, 820, 160), "ARCADE\nLOCKDOWN", titleStyle);
            GUI.Label(new Rect(center - 360, ScreenHeight * 0.35f, 720, 50), "A 199X THIRD-PERSON ESCAPE", subtitleStyle);

            float y = ScreenHeight * 0.48f;
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

            float creditsY = Mathf.Max(y + 226, ScreenHeight - 150);
            GUI.DrawTexture(new Rect(center - 300, creditsY, 600, 2), cyanTexture);
            GUI.Label(new Rect(center - 300, creditsY + 12, 600, 28), "DEVELOPED BY", creditLabelStyle);
            GUI.Label(new Rect(center - 620, creditsY + 42, 1240, 42), "ARKAR PHYO    •    SWAN YI WIN THU YA    •    ZWE KHANT LIN", creditNamesStyle);
            GUI.Label(new Rect(20, ScreenHeight - 40, ScreenWidth - 40, 30), "Two escape routes • 15 minutes each • Free art credits included in the project", smallStyle);
        }

        private void DrawCharacterSelection()
        {
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            if (GUI.Button(new Rect(20, ScreenHeight - 62, 130, 42), "BACK", buttonStyle))
            {
                PlayClick();
                DestroyCharacterPreviews();
                state = GameState.MainMenu;
                return;
            }
            EnsureCharacterPreviews();

            Matrix4x4 previous = ScaleToFit(860f, 760f);
            float cx = ScreenWidth * .5f, cy = ScreenHeight * .5f;
            GUI.Label(new Rect(cx - 450, cy - 372, 900, 60), "CHOOSE YOUR CHARACTER", headingStyle);
            GUI.Label(new Rect(cx - 420, cy - 320, 840, 32), "Same rooms. Different clues. One 15-minute escape per character.", centeredBodyStyle);
            for (int i = 0; i < 2; i++)
            {
                bool female = i == 1;
                Rect card = new Rect(cx + (female ? 20 : -360), cy - 264, 340, 580);
                if (card.Contains(Event.current.mousePosition))
                    GUI.Box(new Rect(card.x - 7, card.y - 7, card.width + 14, card.height + 14), GUIContent.none, female ? mayaGlowStyle : leoGlowStyle);
                GUI.Box(card, GUIContent.none, female ? mayaCardStyle : frameStyle);

                // Full-bleed photo with a fade so the name sits on top of it.
                Rect photo = new Rect(card.x + 12, card.y + 12, card.width - 24, 396);
                Texture portrait = previewReady[i] ? previewTextures[i] : (Texture)Resources.Load<Texture2D>("Characters/Portraits/" + (female ? "Maya" : "Leo"));
                if (portrait != null) GUI.DrawTexture(photo, portrait, ScaleMode.ScaleAndCrop);
                GUI.DrawTexture(new Rect(photo.x, photo.yMax - 130, photo.width, 130), portraitFadeTexture);
                GUI.Label(new Rect(card.x, photo.yMax - 78, card.width, 54), female ? "MAYA" : "LEO", female ? mayaNameStyle : leoNameStyle);
                GUI.Label(new Rect(card.x, photo.yMax - 26, card.width, 22), female ? "FEMALE" : "MALE", cardTagStyle);

                float best = PlayerPrefs.GetFloat(new EscapeRoute(female).BestTimeKey, 0);
                GUI.Label(new Rect(card.x + 12, photo.yMax + 16, card.width - 24, 32), best > 0 ? "BEST ESCAPE  " + FormatTime(best) : "NOT YET ESCAPED", centeredBodyStyle);
                if (GUI.Button(new Rect(card.x + 24, card.yMax - 84, card.width - 48, 58), "PLAY AS " + (female ? "MAYA" : "LEO"), buttonStyle))
                {
                    PlayClick();
                    ChooseCharacter(female);
                }
            }
            GUI.matrix = previous;
        }

        private void DrawGameOver()
        {
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            float cx = ScreenWidth * .5f;
            GUI.Label(new Rect(cx - 400, ScreenHeight * .18f, 800, 100), "TIME'S UP", titleStyle);
            GUI.Label(new Rect(cx - 350, ScreenHeight * .38f, 700, 140), "GAME OVER\n" + Route.Name + " did not escape within 15 minutes.\n" + puzzlesSolved + " / 6 puzzles solved", centeredBodyStyle);
            if (GUI.Button(new Rect(cx - 180, ScreenHeight * .66f, 360, 56), "CHOOSE CHARACTER / RETRY", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(cx - 180, ScreenHeight * .66f + 72, 360, 56), "QUIT", buttonStyle)) QuitGame();
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(18, 18, 465, 84), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34, 28, 430, 26), Route.Name + " / CURRENT OBJECTIVE", smallStyle);
            GUI.Label(new Rect(34, 53, 430, 38), Route.Clue(CurrentObjective()), objectiveStyle);
            // Countdown panel: large digits, progress bar, red pulse in the final minute.
            bool warning = RemainingTime <= 60f;
            Rect timer = new Rect(ScreenWidth - 318, 18, 300, 144);
            GUI.Box(timer, GUIContent.none, warning ? timerWarningStyle : frameStyle);
            GUI.Label(new Rect(timer.x + 22, timer.y + 12, 130, 22), "TIME LEFT", warning ? warningTagStyle : smallStyle);
            GUI.Label(new Rect(timer.xMax - 152, timer.y + 12, 130, 22), puzzlesSolved + " / 6 SOLVED", smallRightStyle);
            Color previousColor = GUI.color;
            if (warning) GUI.color = new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
            GUI.Label(new Rect(timer.x, timer.y + 32, timer.width, 78), FormatTime(Mathf.Ceil(RemainingTime)), warning ? timerWarningDigitsStyle : timerDigitsStyle);
            GUI.color = previousColor;
            Rect bar = new Rect(timer.x + 22, timer.yMax - 24, timer.width - 44, 7);
            GUI.DrawTexture(bar, barBackTexture);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * RemainingTime / TimeLimit, bar.height), warning ? redTexture : cyanTexture);

            string items = inventoryOrder.Count == 0 ? "INVENTORY: EMPTY" : "INVENTORY:  " + string.Join("   •   ", inventoryOrder);
            GUI.Box(new Rect(18, ScreenHeight - 62, ScreenWidth - 36, 42), GUIContent.none, panelStyle);
            GUI.Label(new Rect(32, ScreenHeight - 53, ScreenWidth - 64, 30), items, smallStyle);

            GUI.Label(new Rect(ScreenWidth * 0.5f - 15, ScreenHeight * 0.5f - 22, 30, 44), "+", codeStyle);
            if (focused != null && state == GameState.Playing)
            {
                string prompt = "[ E ]  " + focused.prompt;
                GUI.Label(new Rect(ScreenWidth * 0.5f - 260, ScreenHeight * 0.72f, 520, 48), prompt, promptStyle);
            }

            if (!string.IsNullOrEmpty(feedback) && Time.unscaledTime < feedbackUntil)
                GUI.Label(new Rect(ScreenWidth * 0.5f - 430, 176, 860, 48), feedback, promptStyle);
        }

        private void UpdateMusic()
        {
            float target = state == GameState.GameOver ? 0f
                : state == GameState.Paused || state == GameState.Modal ? 0.1f
                : state == GameState.Escaped ? 0.12f : 0.22f;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * 0.5f);
            musicSource.pitch = state == GameState.Playing && RemainingTime <= 60f ? 1.06f : 1f;

            // Clock tick for each of the last ten seconds.
            int second = Mathf.CeilToInt(RemainingTime);
            if (state == GameState.Playing && second <= 10 && second > 0 && second != tickSecond) PlayClick();
            tickSecond = second;
        }

        private void DrawModal()
        {
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            if (modal == ModalKind.Keypad) DrawKeypad();
            else if (modal == ModalKind.Circuit)
            {
                Rect panel = new Rect(ScreenWidth * 0.5f - 340, ScreenHeight * 0.5f - 300, 680, 600);
                DrawFrame(panel, modalTitle, "REPAIR");
                GUI.Label(new Rect(panel.x + 42, panel.y + 96, panel.width - 84, 110), modalBody, bodyStyle);
                DrawCircuit(panel);
            }
            else DrawInformation();
        }

        /// <summary>Scales a fixed-size layout down around the screen centre on small windows.</summary>
        private Matrix4x4 ScaleToFit(float width, float height)
        {
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(1f, (ScreenWidth - 40f) / width, (ScreenHeight - 40f) / height);
            Vector3 pivot = previous.MultiplyPoint(new Vector3(ScreenWidth * 0.5f, ScreenHeight * 0.5f, 0f));
            GUI.matrix = Matrix4x4.TRS(pivot, Quaternion.identity, new Vector3(scale, scale, 1f)) * Matrix4x4.Translate(-pivot) * previous;
            return previous;
        }

        private void DrawFrame(Rect panel, string title, string tag)
        {
            GUI.Box(panel, GUIContent.none, frameStyle);
            GUI.Label(new Rect(panel.x + 28, panel.y + 12, 260, 22), tag, tagStyle);
            GUI.Label(new Rect(panel.x + 24, panel.y + 28, panel.width - 48, 50), title, headingStyle);
            GUI.DrawTexture(new Rect(panel.x + 24, panel.y + 82, panel.width - 48, 2), cyanTexture);
        }

        private void DrawInformation()
        {
            string title = modalTitle;
            string body = modalBody;
            string tag = modal == ModalKind.Journal ? "JOURNAL" : modal == ModalKind.Instructions ? "GUIDE" : "INSPECT";
            int split = body.IndexOf('\n');
            if (title == "INSPECT" && split > 0 && split <= 32 && body.Substring(0, split) == body.Substring(0, split).ToUpperInvariant())
            {
                // Promote a short heading such as "HIGH SCORES" to the panel title.
                title = body.Substring(0, split);
                body = body.Substring(split + 1).TrimStart('\n');
            }
            else if (title == "INSPECT") tag = "CLUE";

            const float width = 760f;
            float textWidth = width - 48f - 52f;
            float textHeight = monoStyle.CalcHeight(new GUIContent(body), textWidth);
            float screenHeight = Mathf.Clamp(textHeight + 44f, 120f, 480f);
            float height = screenHeight + 196f;
            Matrix4x4 previous = ScaleToFit(width, height);
            Rect panel = new Rect(ScreenWidth * 0.5f - width * 0.5f, ScreenHeight * 0.5f - height * 0.5f, width, height);
            DrawFrame(panel, title, tag);

            Rect screen = new Rect(panel.x + 24, panel.y + 100, panel.width - 48, screenHeight);
            GUI.Box(screen, GUIContent.none, screenStyle);
            Rect view = new Rect(screen.x + 26, screen.y + 22, textWidth, screenHeight - 44);
            if (textHeight > view.height)
            {
                float scrollHeight = monoStyle.CalcHeight(new GUIContent(body), textWidth - 20f);
                modalScroll = GUI.BeginScrollView(view, modalScroll, new Rect(0, 0, textWidth - 20f, scrollHeight));
                GUI.Label(new Rect(0, 0, textWidth - 20f, scrollHeight), body, monoStyle);
                GUI.EndScrollView();
            }
            else GUI.Label(view, body, monoStyle);

            if (GUI.Button(new Rect(panel.center.x - 110, panel.yMax - 72, 220, 48), "CLOSE", buttonStyle))
                CloseModal();
            GUI.matrix = previous;
        }

        private void DrawKeypad()
        {
            Matrix4x4 previous = ScaleToFit(760f, 700f);
            float cx = ScreenWidth * 0.5f, cy = ScreenHeight * 0.5f;
            float sinceDenied = Time.unscaledTime - deniedAt;
            bool denied = sinceDenied < 0.9f;
            bool blink = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f;
            float shake = sinceDenied < 0.4f ? Mathf.Sin(sinceDenied * 70f) * 10f * (1f - sinceDenied / 0.4f) : 0f;

            // Clue written on a sticky note taped beside the lock.
            Rect note = new Rect(cx - 370, cy - 220, 310, 280);
            Matrix4x4 beforeNote = GUI.matrix;
            Vector3 notePivot = GUI.matrix.MultiplyPoint(note.center);
            GUI.matrix = Matrix4x4.TRS(notePivot, Quaternion.Euler(0f, 0f, -3f), Vector3.one) * Matrix4x4.Translate(-notePivot) * GUI.matrix;
            GUI.Box(note, GUIContent.none, noteStyle);
            GUI.DrawTexture(new Rect(note.center.x - 50, note.y - 12, 100, 26), tapeTexture);
            GUI.Label(new Rect(note.x + 24, note.y + 28, note.width - 48, note.height - 52), modalBody, noteTextStyle);
            GUI.matrix = beforeNote;

            // Brushed-metal lock body.
            Rect device = new Rect(cx - 20 + shake, cy - 330, 390, 620);
            GUI.Box(device, GUIContent.none, deviceStyle);
            foreach (Vector2 corner in new[] { new Vector2(16, 16), new Vector2(device.width - 32, 16), new Vector2(16, device.height - 32), new Vector2(device.width - 32, device.height - 32) })
                GUI.DrawTexture(new Rect(device.x + corner.x, device.y + corner.y, 16, 16), screwTexture);
            GUI.Box(new Rect(device.x + 60, device.y + 20, device.width - 120, 34), modalTitle, plateStyle);

            DrawLed(new Rect(device.x + 44, device.y + 68, 16, 16), denied && !blink ? ledOffTexture : ledRedTexture, "LOCKED");
            DrawLed(new Rect(device.x + 156, device.y + 68, 16, 16), enteredCode.Length > 0 ? ledAmberTexture : ledOffTexture, "INPUT");
            DrawLed(new Rect(device.x + 264, device.y + 68, 16, 16), ledOffTexture, "OPEN");

            // LCD with faint unlit segments behind each digit.
            int length = keypadId == "COMPUTER" ? 6 : 4;
            Rect lcd = new Rect(device.x + 28, device.y + 100, device.width - 56, 92);
            GUI.Box(lcd, GUIContent.none, lcdStyle);
            GUI.Label(new Rect(lcd.x + 14, lcd.y + 6, lcd.width - 28, 18), keypadId == "COMPUTER" ? "PASSWORD" : "ENTER CODE", lcdCaptionStyle);
            if (denied) GUI.Label(new Rect(lcd.x, lcd.y + 16, lcd.width, lcd.height - 16), "DENIED", lcdDeniedStyle);
            else
            {
                float slot = (lcd.width - 28f) / length;
                for (int i = 0; i < length; i++)
                {
                    Rect cell = new Rect(lcd.x + 14 + i * slot, lcd.y + 18, slot, lcd.height - 22);
                    GUI.Label(cell, "8", lcdGhostStyle);
                    if (i < enteredCode.Length) GUI.Label(cell, enteredCode[i].ToString(), lcdDigitStyle);
                    else if (i == enteredCode.Length && blink) GUI.Label(cell, "_", lcdDigitStyle);
                }
            }

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "DEL", "0", "ENT" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                int kind = key == "ENT" ? 2 : key == "DEL" ? 1 : 0;
                bool pressed = pressedKey == key && Time.unscaledTime < pressedUntil;
                Rect rect = new Rect(device.x + 41 + (i % 3) * 108, device.y + 214 + (i / 3) * 82 + (pressed ? 3 : 0), 92, 70);
                string label = kind == 0
                    ? "<b><size=30>" + key + "</size></b>\n<size=11>" + KeyLetters[key[0] - '0'] + "</size>"
                    : "<b><size=20>" + key + "</size></b>\n<size=11>" + (kind == 2 ? "OK" : "BACK") + "</size>";
                if (!GUI.Button(rect, label, pressed ? keyPressedStyles[kind] : keyStyles[kind])) continue;
                if (kind == 2) SubmitCode();
                else if (kind == 1) Backspace();
                else AddDigit(key[0] - '0');
            }
            GUI.Label(new Rect(device.x, device.yMax - 62, device.width, 24), "SECURI-TEK  •  MODEL 199X", brandStyle);

            if (GUI.Button(new Rect(device.center.x - 100, device.yMax + 12, 200, 40), "CANCEL  [ESC]", linkStyle))
                CloseModal();
            GUI.matrix = previous;
        }

        /// <summary>Live 3D character previews for the selection cards, rendered far below the level.</summary>
        private void EnsureCharacterPreviews()
        {
            if (previewRoot != null) return;
            previewRoot = new GameObject("CHARACTER SELECT - live previews");
            previewRoot.transform.position = new Vector3(0f, -200f, 0f);
            for (int i = 0; i < 2; i++)
            {
                bool female = i == 1;
                Transform stage = new GameObject(female ? "Maya preview stage" : "Leo preview stage").transform;
                stage.SetParent(previewRoot.transform, false);
                stage.localPosition = new Vector3(i * 30f, 0f, 0f);
                previewVisuals[i] = new GameObject(female ? "Maya preview" : "Leo preview").transform;
                previewVisuals[i].SetParent(stage, false);
                try
                {
                    ArcadeWorldBuilder.CreateTeenCharacter(previewVisuals[i], female);
                    previewReady[i] = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Character preview failed: " + ex.Message);
                    previewReady[i] = false;
                }

                AddPreviewLight(stage, new Vector3(1.4f, 2.4f, 2.6f), new Color(1f, 0.96f, 0.9f), 4f);
                AddPreviewLight(stage, new Vector3(-1.6f, 1.4f, 2.2f), new Color(0.75f, 0.8f, 1f), 1.4f);
                AddPreviewLight(stage, new Vector3(-1.2f, 2.1f, -1.4f), female ? new Color(1f, 0.3f, 0.75f) : new Color(0.2f, 0.85f, 1f), 3.5f);

                previewTextures[i] = new RenderTexture(512, 640, 24) { antiAliasing = 4, name = stage.name };
                Camera camera = new GameObject("Preview camera").AddComponent<Camera>();
                camera.transform.SetParent(stage, false);
                camera.transform.localPosition = new Vector3(0f, 1.0f, 3.4f);
                camera.transform.LookAt(stage.position + Vector3.up * 0.9f);
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 12f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.05f, 0.04f, 0.12f);
                camera.targetTexture = previewTextures[i];
            }
        }

        private static void AddPreviewLight(Transform stage, Vector3 position, Color color, float intensity)
        {
            Light light = new GameObject("Preview light").AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 7f;
            light.color = color;
            light.intensity = intensity;
            light.transform.SetParent(stage, false);
            light.transform.localPosition = position;
        }

        private void DestroyCharacterPreviews()
        {
            if (previewRoot != null) Destroy(previewRoot);
            previewRoot = null;
            for (int i = 0; i < previewTextures.Length; i++)
            {
                if (previewTextures[i] != null)
                {
                    previewTextures[i].Release();
                    Destroy(previewTextures[i]);
                }
                previewTextures[i] = null;
                previewVisuals[i] = null;
                previewReady[i] = false;
            }
        }

        private void DrawLed(Rect rect, Texture2D texture, string label)
        {
            GUI.DrawTexture(new Rect(rect.x - 8, rect.y - 8, rect.width + 16, rect.height + 16), texture);
            GUI.Label(new Rect(rect.xMax + 6, rect.y - 3, 80, 22), label, ledLabelStyle);
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
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            float x = ScreenWidth * 0.5f - 160;
            GUI.Label(new Rect(x - 100, ScreenHeight * 0.25f, 520, 80), "PAUSED", titleStyle);
            if (GUI.Button(new Rect(x, ScreenHeight * 0.48f, 320, 56), "RESUME", buttonStyle)) Resume();
            if (GUI.Button(new Rect(x, ScreenHeight * 0.48f + 72, 320, 56), "RESTART", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(x, ScreenHeight * 0.48f + 144, 320, 56), "QUIT", buttonStyle)) QuitGame();
        }

        private void DrawEnding()
        {
            GUI.DrawTexture(new Rect(0, 0, ScreenWidth, ScreenHeight), darkTexture);
            float best = PlayerPrefs.GetFloat(Route.BestTimeKey, elapsedTime);
            GUI.Label(new Rect(ScreenWidth * 0.5f - 450, ScreenHeight * 0.12f, 900, 120), "YOU ESCAPED!", titleStyle);
            GUI.Label(new Rect(ScreenWidth * 0.5f - 300, ScreenHeight * 0.34f, 600, 220),
                Route.Name + " ESCAPED\n\nESCAPE TIME     " + FormatTime(elapsedTime) +
                "\nBEST TIME       " + FormatTime(best) +
                "\nPUZZLES SOLVED  " + puzzlesSolved + " / 6\n\nTHANKS FOR PLAYING", centeredBodyStyle);
            if (GUI.Button(new Rect(ScreenWidth * 0.5f - 150, ScreenHeight * 0.72f, 300, 56), "PLAY AGAIN", buttonStyle)) RestartGame();
            if (GUI.Button(new Rect(ScreenWidth * 0.5f - 150, ScreenHeight * 0.72f + 70, 300, 56), "QUIT", buttonStyle)) QuitGame();
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
            Texture2D buttonNormal = RoundedTexture(new Color(0.09f, 0.08f, 0.2f), new Color(0.04f, 0.035f, 0.11f), new Color(0.1f, 0.8f, 0.9f), 8, 2);
            Texture2D buttonHover = RoundedTexture(new Color(0.2f, 0.95f, 1f), new Color(0.05f, 0.65f, 0.78f), new Color(0.6f, 1f, 1f), 8, 2);
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(11, 11, 11, 11),
                normal = { textColor = Color.white, background = buttonNormal },
                hover = { textColor = Color.black, background = buttonHover },
                active = { textColor = Color.white, background = buttonNormal }
            };

            Font mono = Font.CreateDynamicFontFromOSFont(new[] { "Menlo", "Consolas", "Courier New" }, 22);
            Font marker = Font.CreateDynamicFontFromOSFont(new[] { "Marker Felt", "Segoe Print", "Comic Sans MS" }, 22);
            Color lcdGreen = new Color(0.4f, 1f, 0.55f);
            tapeTexture = MakeTexture(new Color(1f, 1f, 0.92f, 0.45f));
            screwTexture = ScrewTexture();
            ledOffTexture = LedTexture(new Color(0.2f, 0.09f, 0.07f), 0f);
            ledRedTexture = LedTexture(new Color(1f, 0.15f, 0.1f), 1f);
            ledAmberTexture = LedTexture(new Color(1f, 0.7f, 0.1f), 1f);

            frameStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(17, 17, 17, 17), normal = { background = RoundedTexture(new Color(0.07f, 0.06f, 0.16f), new Color(0.02f, 0.02f, 0.06f), new Color(0.1f, 0.85f, 0.95f), 14, 2) } };
            tagStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.25f, 0.75f) } };
            screenStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(11, 11, 11, 11), normal = { background = RoundedTexture(new Color(0.015f, 0.03f, 0.05f), new Color(0.01f, 0.015f, 0.03f), new Color(0.16f, 0.22f, 0.32f), 8, 1) } };
            monoStyle = new GUIStyle(GUI.skin.label) { font = mono, fontSize = 22, wordWrap = true, normal = { textColor = new Color(0.82f, 0.96f, 1f) } };

            deviceStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(25, 25, 25, 25), normal = { background = RoundedTexture(new Color(0.32f, 0.33f, 0.35f), new Color(0.13f, 0.135f, 0.15f), new Color(0.04f, 0.04f, 0.05f), 22, 3) } };
            plateStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold, border = new RectOffset(9, 9, 9, 9), normal = { textColor = new Color(0.85f, 0.87f, 0.9f), background = RoundedTexture(new Color(0.11f, 0.11f, 0.12f), new Color(0.06f, 0.06f, 0.07f), new Color(0.42f, 0.43f, 0.45f), 6, 1) } };
            ledLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.72f, 0.74f, 0.77f) } };
            lcdStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(11, 11, 11, 11), normal = { background = RoundedTexture(new Color(0.06f, 0.14f, 0.09f), new Color(0.025f, 0.07f, 0.045f), new Color(0.01f, 0.015f, 0.01f), 8, 3) } };
            lcdCaptionStyle = new GUIStyle(GUI.skin.label) { font = mono, fontSize = 12, normal = { textColor = new Color(lcdGreen.r, lcdGreen.g, lcdGreen.b, 0.6f) } };
            lcdDigitStyle = new GUIStyle(GUI.skin.label) { font = mono, fontSize = 50, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = lcdGreen } };
            lcdGhostStyle = new GUIStyle(lcdDigitStyle) { normal = { textColor = new Color(lcdGreen.r, lcdGreen.g, lcdGreen.b, 0.07f) } };
            lcdDeniedStyle = new GUIStyle(lcdDigitStyle) { fontSize = 42, normal = { textColor = new Color(1f, 0.3f, 0.25f) } };
            noteStyle = new GUIStyle(GUI.skin.box) { border = new RectOffset(6, 6, 6, 6), normal = { background = RoundedTexture(new Color(1f, 0.96f, 0.68f), new Color(0.96f, 0.87f, 0.5f), new Color(0.85f, 0.76f, 0.4f), 3, 1) } };
            noteTextStyle = new GUIStyle(GUI.skin.label) { font = marker, fontSize = 22, wordWrap = true, normal = { textColor = new Color(0.12f, 0.12f, 0.3f) } };
            brandStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.58f, 0.6f, 0.63f) } };
            linkStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.72f, 0.8f, 0.9f) }, hover = { textColor = new Color(0.2f, 0.95f, 1f) } };

            Color neonCyan = new Color(0.1f, 0.85f, 0.95f), neonPink = new Color(1f, 0.25f, 0.75f);
            creditLabelStyle = new GUIStyle(tagStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            creditNamesStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.9f, 0.95f, 1f) } };
            mayaCardStyle = new GUIStyle(frameStyle) { normal = { background = RoundedTexture(new Color(0.07f, 0.06f, 0.16f), new Color(0.02f, 0.02f, 0.06f), neonPink, 14, 2) } };
            leoGlowStyle = new GUIStyle(frameStyle) { border = new RectOffset(21, 21, 21, 21), normal = { background = RoundedTexture(new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.18f), new Color(neonCyan.r, neonCyan.g, neonCyan.b, 0.18f), neonCyan, 18, 4) } };
            mayaGlowStyle = new GUIStyle(frameStyle) { border = new RectOffset(21, 21, 21, 21), normal = { background = RoundedTexture(new Color(neonPink.r, neonPink.g, neonPink.b, 0.18f), new Color(neonPink.r, neonPink.g, neonPink.b, 0.18f), neonPink, 18, 4) } };
            leoNameStyle = new GUIStyle(titleStyle) { fontSize = 46, normal = { textColor = neonCyan } };
            mayaNameStyle = new GUIStyle(titleStyle) { fontSize = 46, normal = { textColor = neonPink } };
            cardTagStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.8f, 0.85f, 0.95f) } };
            portraitFadeTexture = FadeTexture(new Color(0.03f, 0.03f, 0.08f));

            Color alarmRed = new Color(1f, 0.28f, 0.24f);
            barBackTexture = MakeTexture(new Color(1f, 1f, 1f, 0.12f));
            redTexture = MakeTexture(alarmRed);
            timerWarningStyle = new GUIStyle(frameStyle) { normal = { background = RoundedTexture(new Color(0.16f, 0.03f, 0.05f), new Color(0.06f, 0.01f, 0.02f), alarmRed, 14, 3) } };
            timerDigitsStyle = new GUIStyle(GUI.skin.label) { font = mono, fontSize = 70, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = neonCyan } };
            timerWarningDigitsStyle = new GUIStyle(timerDigitsStyle) { normal = { textColor = alarmRed } };
            warningTagStyle = new GUIStyle(smallStyle) { normal = { textColor = alarmRed } };
            smallRightStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleRight };

            // Key caps: grey digits, amber DEL, green ENT.
            Color[][] keyColors =
            {
                new[] { new Color(0.9f, 0.9f, 0.91f), new Color(0.62f, 0.63f, 0.65f), new Color(0.12f, 0.12f, 0.14f) },
                new[] { new Color(1f, 0.78f, 0.35f), new Color(0.8f, 0.52f, 0.14f), new Color(0.18f, 0.1f, 0.02f) },
                new[] { new Color(0.42f, 0.85f, 0.5f), new Color(0.16f, 0.52f, 0.26f), new Color(0.02f, 0.12f, 0.05f) }
            };
            keyStyles = new GUIStyle[3];
            keyPressedStyles = new GUIStyle[3];
            for (int i = 0; i < keyColors.Length; i++)
            {
                Color top = keyColors[i][0], bottom = keyColors[i][1], ink = keyColors[i][2];
                Color edge = new Color(0.06f, 0.06f, 0.07f);
                Texture2D up = RoundedTexture(top, bottom, edge, 10, 2);
                Texture2D hover = RoundedTexture(Color.Lerp(top, Color.white, 0.25f), Color.Lerp(bottom, Color.white, 0.2f), edge, 10, 2);
                Texture2D down = RoundedTexture(new Color(bottom.r * 0.85f, bottom.g * 0.85f, bottom.b * 0.85f), new Color(top.r * 0.8f, top.g * 0.8f, top.b * 0.8f), edge, 10, 2);
                keyStyles[i] = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    border = new RectOffset(13, 13, 13, 13),
                    normal = { textColor = ink, background = up },
                    hover = { textColor = ink, background = hover },
                    active = { textColor = ink, background = down }
                };
                keyPressedStyles[i] = new GUIStyle(keyStyles[i]) { normal = { textColor = ink, background = down }, hover = { textColor = ink, background = down } };
            }
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>Rounded, vertically shaded panel texture for 9-slice GUI styles (border = radius + 3).</summary>
        private static Texture2D RoundedTexture(Color top, Color bottom, Color edge, int radius, int edgeWidth)
        {
            int width = radius * 2 + 8, height = Mathf.Max(64, radius * 2 + 8);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - radius);
                    float qy = Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - radius);
                    float distance = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    Color color = Color.Lerp(bottom, top, y / (height - 1f));
                    if (distance > -edgeWidth) color = edge;
                    else if (distance > -edgeWidth - 1.5f && y > height * 0.5f) color = Color.Lerp(color, Color.white, 0.22f);
                    color.a *= Mathf.Clamp01(0.5f - distance);
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Vertical fade: solid colour at the bottom to transparent at the top.</summary>
        private static Texture2D FadeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) texture.SetPixel(0, y, new Color(color.r, color.g, color.b, Mathf.SmoothStep(1f, 0f, y / 63f)));
            texture.Apply();
            return texture;
        }

        private static Texture2D LedTexture(Color color, float glow)
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                    float body = Mathf.Clamp01(8.5f - distance);
                    float halo = glow * Mathf.Clamp01(1f - (distance - 8f) / 8f) * 0.5f;
                    float shine = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(13f, 19f)) / 4f);
                    Color pixel = Color.Lerp(color, Color.white, shine * 0.6f * body);
                    pixel.a = Mathf.Max(body, halo);
                    pixels[y * size + x] = pixel;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D ScrewTexture()
        {
            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8f, 8f));
                    Color pixel = Color.Lerp(new Color(0.35f, 0.36f, 0.38f), new Color(0.78f, 0.79f, 0.8f), y / 15f);
                    if (Mathf.Abs(x - y) < 1.2f && distance < 5.5f) pixel = new Color(0.12f, 0.12f, 0.13f);
                    pixel.a = Mathf.Clamp01(7.5f - distance);
                    pixels[y * size + x] = pixel;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
