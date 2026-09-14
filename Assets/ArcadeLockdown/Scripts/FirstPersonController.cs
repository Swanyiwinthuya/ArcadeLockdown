using UnityEngine;
using Input = ArcadeLockdown.ArcadeInput;

namespace ArcadeLockdown
{
    /// <summary>A dependency-free first-person controller using Unity's CharacterController.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Camera viewCamera;
        public float walkSpeed = 4.2f;
        public float sprintSpeed = 6.3f;
        public float mouseSensitivity = 2.1f;
        public float gravity = -22f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Update()
        {
            if (ArcadeGameManager.Instance == null || !ArcadeGameManager.Instance.CanControlPlayer)
                return;

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -82f, 82f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) input.y += 1f;
            if (Input.GetKey(KeyCode.S)) input.y -= 1f;
            if (Input.GetKey(KeyCode.D)) input.x += 1f;
            if (Input.GetKey(KeyCode.A)) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 motion = (transform.forward * input.y + transform.right * input.x) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            // Safety reset if the controller ever falls outside the handmade level.
            if (transform.position.y < -5f)
                ResetPosition();
        }

        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
            controller.enabled = true;
            pitch = 0f;
            verticalVelocity = 0f;
        }
    }
}
