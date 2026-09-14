using UnityEngine;
using Input = ArcadeLockdown.ArcadeInput;

namespace ArcadeLockdown
{
    /// <summary>
    /// Camera-relative third-person movement with an orbit camera, smooth turning,
    /// sprinting and wall-aware camera collision.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonController : MonoBehaviour
    {
        public Camera viewCamera;
        public Transform visualRoot;
        public YoungManAnimator characterAnimator;
        public ImportedCharacterAnimator importedCharacterAnimator;
        public TeenCharacterAnimator teenCharacterAnimator;
        public float walkSpeed = 2.8f;
        public float sprintSpeed = 4.6f;
        public float turnSpeed = 13f;
        public float mouseSensitivity = 2.1f;
        public float cameraDistance = 3.8f;
        public float cameraHeight = 1.32f;
        public float shoulderOffset = 0.65f;
        public float gravity = -22f;

        private CharacterController controller;
        private float cameraYaw;
        private float cameraPitch = 9f;
        private float verticalVelocity;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            cameraYaw = transform.eulerAngles.y;
            UpdateCamera(true);
        }

        private void Update()
        {
            bool canMove = ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.CanControlPlayer;
            if (!canMove)
            {
                if (characterAnimator != null) characterAnimator.SetMovement(0f, false);
                if (importedCharacterAnimator != null) importedCharacterAnimator.SetMovement(0f, false);
                if (teenCharacterAnimator != null) teenCharacterAnimator.SetMovement(0f, false);
                return;
            }

            cameraYaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -12f, 58f);
            cameraDistance = Mathf.Clamp(cameraDistance - Input.GetAxis("Mouse ScrollWheel") * 3f, 2.4f, 5.2f);

            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) input.y += 1f;
            if (Input.GetKey(KeyCode.S)) input.y -= 1f;
            if (Input.GetKey(KeyCode.D)) input.x += 1f;
            if (Input.GetKey(KeyCode.A)) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 cameraForward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized;
            Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;
            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

            bool sprinting = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f;
            float speed = sprinting ? sprintSpeed : walkSpeed;
            Vector3 motion = moveDirection * speed;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            if (moveDirection.sqrMagnitude > 0.015f && visualRoot != null)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            if (characterAnimator != null)
                characterAnimator.SetMovement(input.magnitude, sprinting);
            if (importedCharacterAnimator != null)
                importedCharacterAnimator.SetMovement(input.magnitude, sprinting);
            if (teenCharacterAnimator != null) teenCharacterAnimator.SetMovement(input.magnitude, sprinting);

            if (transform.position.y < -5f) ResetPosition();
        }

        private void LateUpdate()
        {
            UpdateCamera(false);
        }

        private void UpdateCamera(bool immediate)
        {
            if (viewCamera == null) return;

            Vector3 focus = transform.position + Vector3.up * cameraHeight;
            Quaternion orbit = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 orbitForward = orbit * Vector3.forward;
            Vector3 orbitRight = orbit * Vector3.right;
            Vector3 desired = focus - orbitForward * cameraDistance + orbitRight * shoulderOffset;
            Vector3 cast = desired - focus;
            float castDistance = cast.magnitude;

            if (castDistance > 0.01f && Physics.SphereCast(focus, 0.2f, cast.normalized,
                    out RaycastHit hit, castDistance, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                desired = focus + cast.normalized * Mathf.Max(0.25f, hit.distance - 0.12f);
            }

            float blend = immediate ? 1f : 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, desired, blend);
            Vector3 lookTarget = focus + orbitRight * shoulderOffset + orbitForward * 0.7f;
            viewCamera.transform.rotation = Quaternion.LookRotation(lookTarget - viewCamera.transform.position, Vector3.up);

            // Hide the body only when the camera is forced inside it by a wall.
            if (visualRoot != null)
                foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>())
                    renderer.enabled = Vector3.Distance(viewCamera.transform.position, focus) > 0.72f;
        }

        public void ResetPosition()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(startPosition, startRotation);
            controller.enabled = true;
            verticalVelocity = 0f;
            cameraYaw = startRotation.eulerAngles.y;
            cameraPitch = 9f;
            UpdateCamera(true);
        }
    }
}
