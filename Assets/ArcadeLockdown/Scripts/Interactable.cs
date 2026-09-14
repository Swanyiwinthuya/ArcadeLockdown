using UnityEngine;

namespace ArcadeLockdown
{
    public enum InteractionKind
    {
        Information,
        LockerKeypad,
        OfficeDoor,
        OfficeComputer,
        PrizeKeypad,
        StorageDoor,
        Box42,
        PowerConsole,
        ExitKeypad,
        CollectItem,
        UnlockedDoor
    }

    /// <summary>Attach this to any prop the player can inspect or use.</summary>
    public sealed class Interactable : MonoBehaviour
    {
        public InteractionKind kind;
        public string prompt = "Interact";
        [TextArea(2, 10)] public string information;
        public string itemId;
        public string itemDisplayName;

        public void Use()
        {
            if (ArcadeGameManager.Instance != null)
                ArcadeGameManager.Instance.HandleInteraction(this);
        }
    }
}
