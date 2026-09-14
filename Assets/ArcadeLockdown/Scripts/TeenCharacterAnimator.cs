using UnityEngine;

namespace ArcadeLockdown
{
    public sealed class TeenCharacterAnimator : MonoBehaviour
    {
        public Animator animator;
        private string current;
        public void SetMovement(float amount, bool sprinting)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
            string next = amount < .08f ? "Idle" : sprinting ? "Run" : "Walk";
            if (current != next) { animator.CrossFadeInFixedTime(next, .18f); current = next; }
        }
    }
}
