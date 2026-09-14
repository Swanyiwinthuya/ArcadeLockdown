using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Reusable attract-mode animation for arcade cabinets, CRTs, vending machines
    /// and the power console. All motion is subtle so clues remain readable.
    /// </summary>
    public sealed class MachineAnimator : MonoBehaviour
    {
        public Renderer screenRenderer;
        public Transform joystick;
        public Transform pushButton;
        public Transform screenText;
        public PowerReactive powerState;
        public bool requiresPower;
        public Color glowColor = new Color(0.05f, 0.9f, 1f);
        public float speed = 1f;

        private Quaternion joystickRest;
        private Vector3 buttonRestScale;
        private Vector3 textRestPosition;
        private Material screenMaterial;
        private float phase;

        private void Start()
        {
            phase = Mathf.Abs(transform.position.x * 0.73f + transform.position.z * 0.41f);
            if (joystick != null) joystickRest = joystick.localRotation;
            if (pushButton != null) buttonRestScale = pushButton.localScale;
            if (screenText != null) textRestPosition = screenText.localPosition;
            if (screenRenderer != null) screenMaterial = screenRenderer.material;
        }

        private void Update()
        {
            bool active = !requiresPower || (powerState != null && powerState.IsPowered);
            float time = Time.time * speed + phase;
            float pulse = active ? 0.72f + Mathf.Sin(time * 3.1f) * 0.18f : 0.02f;

            if (screenMaterial != null && screenMaterial.HasProperty("_EmissionColor"))
            {
                screenMaterial.EnableKeyword("_EMISSION");
                screenMaterial.SetColor("_EmissionColor", glowColor * pulse * 2f);
            }

            if (joystick != null)
            {
                Quaternion target = joystickRest * Quaternion.Euler(active ? Mathf.Sin(time * 2.2f) * 10f : 0f, 0f,
                    active ? Mathf.Cos(time * 1.7f) * 7f : 0f);
                joystick.localRotation = Quaternion.Slerp(joystick.localRotation, target, Time.deltaTime * 8f);
            }

            if (pushButton != null)
            {
                float press = active ? 1f - Mathf.Max(0f, Mathf.Sin(time * 2.6f)) * 0.18f : 1f;
                pushButton.localScale = Vector3.Lerp(pushButton.localScale,
                    new Vector3(buttonRestScale.x, buttonRestScale.y * press, buttonRestScale.z), Time.deltaTime * 10f);
            }

            if (screenText != null)
            {
                float scan = active ? Mathf.Sin(time * 4.5f) * 0.008f : 0f;
                screenText.localPosition = Vector3.Lerp(screenText.localPosition,
                    textRestPosition + Vector3.up * scan, Time.deltaTime * 8f);
            }
        }
    }
}
