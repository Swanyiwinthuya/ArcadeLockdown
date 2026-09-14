using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Prevents 3D text from showing as mirrored writing through the back of a sign.
    /// Text renders only when the camera is on the intended front side.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class ReadableWorldText : MonoBehaviour
    {
        private Renderer textRenderer;

        private void Awake()
        {
            textRenderer = GetComponent<Renderer>();
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null || textRenderer == null) return;
            TextMesh text = GetComponent<TextMesh>();
            if (text.font != null && textRenderer.sharedMaterial != null)
                textRenderer.sharedMaterial.mainTexture = text.font.material.mainTexture;
            Vector3 towardCamera = camera.transform.position - transform.position;
            if (towardCamera.sqrMagnitude < 0.01f)
            {
                textRenderer.enabled = true;
                return;
            }

            // Unity TextMesh faces its local negative-Z direction.
            Vector3 intendedFront = -transform.forward;
            textRenderer.enabled = Vector3.Dot(intendedFront, towardCamera.normalized) > 0.025f;
        }
    }
}
