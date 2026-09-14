using UnityEngine;

namespace ArcadeLockdown
{
    [RequireComponent(typeof(Interactable))]
    public sealed class TokenPickup : MonoBehaviour
    {
        public Transform coin;
        public float pickupRadius = 1.35f;
        private Vector3 rest;
        private ThirdPersonController player;
        private bool collected;

        private void Start()
        {
            if (coin != null) rest = coin.localPosition;
            player = Object.FindFirstObjectByType<ThirdPersonController>();
        }

        public bool IsReachable(Transform playerTransform, float radius)
        {
            Vector3 delta = transform.position - playerTransform.position;
            if (new Vector2(delta.x, delta.z).magnitude > radius || Mathf.Abs(delta.y - 0.9f) > 1.1f) return false;
            Vector3 chest = playerTransform.position + Vector3.up * 1.15f;
            return !Physics.Linecast(chest, transform.position, ~(1 << 2), QueryTriggerInteraction.Ignore);
        }

        private void Update()
        {
            if (coin != null)
            {
                coin.localPosition = rest + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.035f);
                coin.localRotation = Quaternion.Euler(90f, Time.time * 45f, 0f);
            }
            if (collected || player == null || ArcadeGameManager.Instance == null || !ArcadeGameManager.Instance.CanControlPlayer) return;
            if (IsReachable(player.transform, pickupRadius))
            {
                collected = true;
                GetComponent<Interactable>().Use();
            }
        }
    }
}
