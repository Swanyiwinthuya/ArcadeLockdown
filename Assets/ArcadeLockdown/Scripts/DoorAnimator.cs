using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>Simple reliable sliding-door animation with no Animator asset required.</summary>
    public sealed class DoorAnimator : MonoBehaviour
    {
        public bool IsOpen { get; private set; }

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool initialized;
        private float openProgress;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            closedPosition = transform.position;
            openPosition = closedPosition + Vector3.up * 3.2f;
            initialized = true;
        }

        public void Open()
        {
            Initialize();
            IsOpen = true;
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider item in colliders) item.enabled = false;
        }

        private void Update()
        {
            if (!initialized) Initialize();
            float target = IsOpen ? 1f : 0f;
            openProgress = Mathf.MoveTowards(openProgress, target, Time.deltaTime * 1.35f);
            float eased = openProgress * openProgress * (3f - 2f * openProgress);
            transform.position = Vector3.Lerp(closedPosition, openPosition, eased);
        }
    }
}
