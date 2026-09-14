using UnityEngine;

namespace ArcadeLockdown
{
    // One room layout; two consistent clue/answer sets. Neither route changes difficulty.
    public sealed class EscapeRoute
    {
        public readonly bool female;
        public string Name => female ? "MAYA" : "LEO";
        public string BestTimeKey => "ArcadeLockdownV5Best_" + Name;
        public string Locker => female ? "1997" : "1994";
        public string Computer => female ? "121993" : "071992";
        public string Prize => female ? "8525" : "2585";
        public string Exit => female ? "7831" : "3178";
        public int[] Circuit => female ? new[] { 2, 0, 1, 3 } : new[] { 0, 1, 3, 2 };
        public EscapeRoute(bool isFemale) { female = isFemale; }
        public string Answer(string id) => id == "LOCKER" ? Locker : id == "COMPUTER" ? Computer : id == "PRIZE" ? Prize : Exit;

        // Applies to displayed clues, inspection text, hints and journals, not geometry.
        public string Clue(string text)
        {
            if (!female || string.IsNullOrEmpty(text)) return text;
            return text.Replace("MAX", "AVA").Replace("His high score", "Her high score")
                .Replace("1994", "1997").Replace("071992", "121993").Replace("1992", "1993")
                .Replace("No. 07", "No. 12").Replace("NO. 07", "NO. 12").Replace("machine 07", "machine 12")
                .Replace("BLUE\nRED\nYELLOW\nRED", "YELLOW\nRED\nBLUE\nRED")
                .Replace("BLUE — RED — YELLOW — RED", "YELLOW — RED — BLUE — RED")
                .Replace("BLUE, RED, YELLOW, RED", "YELLOW, RED, BLUE, RED").Replace("2585", "8525")
                .Replace("SPACE\nRACE\nBLOCK\nPAC", "BLOCK\nPAC\nSPACE\nRACE")
                .Replace("1. SPACE\n2. RACE\n3. BLOCK\n4. PAC", "1. BLOCK\n2. PAC\n3. SPACE\n4. RACE")
                .Replace("SPACE, RACE, BLOCK, PAC", "BLOCK, PAC, SPACE, RACE")
                .Replace("3, 1, 7, 8", "7, 8, 3, 1").Replace("3178", "7831")
                .Replace("0°     90°     270°     180°", "180°     0°     90°     270°")
                .Replace("0°   90°   270°   180°", "180°   0°   90°   270°")
                .Replace("0° / 90° / 270° / 180°", "180° / 0° / 90° / 270°");
        }

        public void ApplyWorldClues()
        {
            foreach (TextMesh label in Object.FindObjectsByType<TextMesh>()) label.text = Clue(label.text);
            foreach (Interactable item in Object.FindObjectsByType<Interactable>())
            {
                item.information = Clue(item.information);
                item.prompt = Clue(item.prompt);
            }
        }
    }
}
