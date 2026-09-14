using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>Changes a screen or lamp when the building power is restored.</summary>
    public sealed class PowerReactive : MonoBehaviour
    {
        public bool IsPowered { get; private set; }
        public Color offColor = new Color(0.015f, 0.02f, 0.025f);
        public Color onColor = Color.cyan;
        public string offText = "NO SIGNAL";
        public string onText = "ONLINE";

        private Renderer cachedRenderer;
        private TextMesh cachedText;
        private Light cachedLight;

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            cachedText = GetComponent<TextMesh>();
            cachedLight = GetComponent<Light>();
            SetPowered(false);
        }

        public void SetPowered(bool powered)
        {
            IsPowered = powered;
            if (cachedRenderer == null) cachedRenderer = GetComponent<Renderer>();
            if (cachedText == null) cachedText = GetComponent<TextMesh>();
            if (cachedLight == null) cachedLight = GetComponent<Light>();

            Color color = powered ? onColor : offColor;
            if (cachedRenderer != null)
            {
                cachedRenderer.material.color = color;
                if (cachedRenderer.material.HasProperty("_EmissionColor"))
                {
                    cachedRenderer.material.EnableKeyword("_EMISSION");
                    cachedRenderer.material.SetColor("_EmissionColor", color * (powered ? 2.5f : 0.05f));
                }
            }
            if (cachedText != null)
            {
                cachedText.text = powered ? onText : offText;
                cachedText.color = color;
            }
            if (cachedLight != null)
            {
                cachedLight.color = onColor;
                cachedLight.enabled = powered;
            }
        }
    }
}
