using System;
using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>
    /// Drives the legacy animation clips embedded in the CC0 Quaternius character.
    /// The FBX is imported as Legacy so no Animator Controller asset is required.
    /// </summary>
    public sealed class ImportedCharacterAnimator : MonoBehaviour
    {
        private Animation animationComponent;
        private string idleClip;
        private string walkClip;
        private string runClip;
        private string currentClip;

        public bool IsReady => animationComponent != null && !string.IsNullOrEmpty(idleClip);

        public void Configure(Animation targetAnimation, AnimationClip[] clips)
        {
            animationComponent = targetAnimation;
            if (animationComponent == null || clips == null) return;

            animationComponent.playAutomatically = false;
            foreach (AnimationClip clip in clips)
            {
                if (clip == null || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) continue;

                string shortName = ShortClipName(clip.name);
                clip.wrapMode = WrapMode.Loop;
                if (animationComponent.GetClip(shortName) == null)
                    animationComponent.AddClip(clip, shortName);

                if (shortName.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0) idleClip = shortName;
                else if (shortName.IndexOf("Walk", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         shortName.IndexOf("Running", StringComparison.OrdinalIgnoreCase) < 0) walkClip = shortName;
                else if (shortName.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         shortName.IndexOf("Jump", StringComparison.OrdinalIgnoreCase) < 0) runClip = shortName;
            }

            if (string.IsNullOrEmpty(walkClip)) walkClip = idleClip;
            if (string.IsNullOrEmpty(runClip)) runClip = walkClip;
            AnimationClip idle = animationComponent.GetClip(idleClip);
            if (idle != null) idle.SampleAnimation(animationComponent.gameObject, 0f);
            Play(idleClip, true);
        }

        public void SetMovement(float amount, bool sprinting)
        {
            if (!IsReady) return;
            string requested = amount < 0.08f ? idleClip : sprinting ? runClip : walkClip;
            Play(requested, false);

            AnimationState state = animationComponent[requested];
            if (state != null)
            {
                float targetSpeed = requested == idleClip ? 0.9f : requested == runClip ? 1.16f : 1.03f;
                state.speed = Mathf.Lerp(state.speed, targetSpeed, Time.deltaTime * 8f);
            }
        }

        private void Play(string clipName, bool immediate)
        {
            if (string.IsNullOrEmpty(clipName) || clipName == currentClip || animationComponent == null) return;
            if (immediate) animationComponent.Play(clipName);
            else animationComponent.CrossFade(clipName, 0.18f, PlayMode.StopAll);
            currentClip = clipName;
        }

        private static string ShortClipName(string fullName)
        {
            int separator = fullName.LastIndexOf('|');
            return separator >= 0 && separator < fullName.Length - 1 ? fullName.Substring(separator + 1) : fullName;
        }
    }
}
