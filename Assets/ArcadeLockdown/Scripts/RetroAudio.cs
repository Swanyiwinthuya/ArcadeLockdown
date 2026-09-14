using UnityEngine;

namespace ArcadeLockdown
{
    /// <summary>Creates original chiptune-style sound effects at runtime.</summary>
    public static class RetroAudio
    {
        private const int SampleRate = 44100;

        public static AudioClip Beep(string name, float frequency, float duration, float volume = 0.35f)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Clamp01((duration - t) * 12f) * Mathf.Clamp01(t * 80f);
                float square = Mathf.Sin(2f * Mathf.PI * frequency * t) >= 0f ? 1f : -1f;
                data[i] = square * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Success()
        {
            const float duration = 0.55f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            float[] notes = { 392f, 523.25f, 659.25f, 783.99f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                int note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / (duration / notes.Length)));
                float local = t % (duration / notes.Length);
                float envelope = Mathf.Clamp01(local * 70f) * Mathf.Clamp01((duration / notes.Length - local) * 18f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * notes[note] * t) >= 0f ? 0.28f : -0.28f) * envelope;
            }
            AudioClip clip = AudioClip.Create("Puzzle solved", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip Hum()
        {
            const float duration = 2f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.025f) +
                          (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.008f);
            }
            AudioClip clip = AudioClip.Create("Arcade electrical hum", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
