using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Procedural humanoid animation for the young-man player model. It blends
    /// breathing, idle weight shifts, walking, running, arm swing and foot lift.
    /// </summary>
    public sealed class YoungManAnimator : MonoBehaviour
    {
        public Transform torso;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        private float movement;
        private bool sprinting;
        private float gaitPhase;
        private Vector3 torsoRestPosition;
        private Quaternion headRestRotation;
        private Quaternion leftArmRestRotation;
        private Quaternion rightArmRestRotation;
        private Quaternion leftLegRestRotation;
        private Quaternion rightLegRestRotation;
        private bool initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized || torso == null) return;
            torsoRestPosition = torso.localPosition;
            headRestRotation = head.localRotation;
            leftArmRestRotation = leftArm.localRotation;
            rightArmRestRotation = rightArm.localRotation;
            leftLegRestRotation = leftLeg.localRotation;
            rightLegRestRotation = rightLeg.localRotation;
            initialized = true;
        }

        public void SetMovement(float amount, bool isSprinting)
        {
            movement = Mathf.Clamp01(amount);
            sprinting = isSprinting;
        }

        private void LateUpdate()
        {
            Initialize();
            if (!initialized) return;

            float blendSpeed = 12f;
            float idleBreath = Mathf.Sin(Time.time * 1.8f);
            float amplitude = sprinting ? 43f : 29f;
            float frequency = sprinting ? 11.5f : 8f;
            gaitPhase += Time.deltaTime * Mathf.Lerp(1.6f, frequency, movement);
            float swing = Mathf.Sin(gaitPhase) * amplitude * movement;
            float footLift = Mathf.Max(0f, Mathf.Sin(gaitPhase)) * 8f * movement;
            float oppositeLift = Mathf.Max(0f, -Mathf.Sin(gaitPhase)) * 8f * movement;

            Quaternion leftArmTarget = leftArmRestRotation * Quaternion.Euler(-swing, 0f, 4f + idleBreath * (1f - movement));
            Quaternion rightArmTarget = rightArmRestRotation * Quaternion.Euler(swing, 0f, -4f - idleBreath * (1f - movement));
            Quaternion leftLegTarget = leftLegRestRotation * Quaternion.Euler(swing + footLift, 0f, 0f);
            Quaternion rightLegTarget = rightLegRestRotation * Quaternion.Euler(-swing + oppositeLift, 0f, 0f);
            Quaternion headTarget = headRestRotation * Quaternion.Euler(idleBreath * 1.2f * (1f - movement), Mathf.Sin(Time.time * 0.7f) * 2f * (1f - movement), 0f);

            float t = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, leftArmTarget, t);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, rightArmTarget, t);
            leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, leftLegTarget, t);
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, rightLegTarget, t);
            head.localRotation = Quaternion.Slerp(head.localRotation, headTarget, t * 0.65f);

            float stepBob = Mathf.Abs(Mathf.Sin(gaitPhase * 2f)) * 0.055f * movement;
            float breathing = idleBreath * 0.012f * (1f - movement);
            torso.localPosition = Vector3.Lerp(torso.localPosition,
                torsoRestPosition + Vector3.up * (stepBob + breathing), t);
        }
    }
}
