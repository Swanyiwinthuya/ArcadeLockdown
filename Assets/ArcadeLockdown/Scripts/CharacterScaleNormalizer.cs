using UnityEngine;

namespace ArcadeLockdown
{
    [DefaultExecutionOrder(1000)]
    public sealed class CharacterScaleNormalizer : MonoBehaviour
    {
        private int frames;
        private void Awake() => NormalizeNow();

        private void LateUpdate()
        {
            // Legacy FBX skinning must evaluate before its deformed size is valid.
            if (++frames < 3) return;
            NormalizeNow();
            enabled = false;
        }

        private void NormalizeNow()
        {
            Bounds measured = CharacterMeasurements.Measure(transform);
            float scale = 1.76f / Mathf.Max(.01f, measured.size.y);
            transform.localScale = Vector3.one * scale;
            transform.localPosition = new Vector3(-measured.center.x, -measured.min.y, -measured.center.z) * scale;
            Debug.Log("Arcade character normalized: sampled height=" + measured.size.y + ", scale=" + scale);
        }
    }
}
