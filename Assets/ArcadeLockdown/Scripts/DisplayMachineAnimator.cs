using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Adds small, readable motions to imported attraction models: moving claws,
    /// spinning prize rotors, rolling pinballs and an air-hockey puck.
    /// </summary>
    public sealed class DisplayMachineAnimator : MonoBehaviour
    {
        public enum MotionStyle
        {
            Slide,
            Spin,
            Orbit,
            Bounce,
            Shot
        }

        public Transform movingPart;
        public Light accentLight;
        public MotionStyle motionStyle;
        public Vector3 axis = Vector3.right;
        public float amplitude = 0.25f;
        public float speed = 1f;
        public Vector3 destination;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private float baseIntensity;
        private float phase;

        private void Start()
        {
            if (movingPart != null)
            {
                startPosition = movingPart.localPosition;
                startRotation = movingPart.localRotation;
            }
            if (accentLight != null) baseIntensity = accentLight.intensity;
            phase = Mathf.Abs(transform.position.x * 0.31f + transform.position.z * 0.17f);
        }

        private void Update()
        {
            float time = Time.time * speed + phase;
            if (movingPart != null)
            {
                switch (motionStyle)
                {
                    case MotionStyle.Shot:
                        float progress = Mathf.Repeat(time * .45f, 1f);
                        if (progress < .7f)
                        {
                            float shot = progress / .7f;
                            movingPart.localPosition = Vector3.Lerp(startPosition, destination, shot) + Vector3.up * (Mathf.Sin(shot * Mathf.PI) * amplitude);
                        }
                        else
                            movingPart.localPosition = Vector3.Lerp(destination, startPosition, (progress - .7f) / .3f);
                        movingPart.Rotate(Vector3.right, Time.deltaTime * 180f, Space.Self);
                        break;
                    case MotionStyle.Spin:
                        movingPart.localRotation = startRotation * Quaternion.AngleAxis(time * 95f, axis.normalized);
                        break;
                    case MotionStyle.Orbit:
                        movingPart.localPosition = startPosition + new Vector3(Mathf.Sin(time), 0f, Mathf.Cos(time)) * amplitude;
                        break;
                    case MotionStyle.Bounce:
                        movingPart.localPosition = startPosition + axis.normalized * (Mathf.Abs(Mathf.Sin(time)) * amplitude);
                        break;
                    default:
                        movingPart.localPosition = startPosition + axis.normalized * (Mathf.Sin(time) * amplitude);
                        break;
                }
            }

            if (accentLight != null)
                accentLight.intensity = baseIntensity * (0.82f + Mathf.Sin(time * 1.7f) * 0.18f);
        }
    }
}
