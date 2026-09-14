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

        /// <summary>Dual-tone keypad beep with a short mechanical press click.</summary>
        public static AudioClip KeyTone(int digit)
        {
            float[] low = { 941f, 697f, 697f, 697f, 770f, 770f, 770f, 852f, 852f, 852f };
            float[] high = { 1336f, 1209f, 1336f, 1477f, 1209f, 1336f, 1477f, 1209f, 1336f, 1477f };
            const float duration = 0.1f;
            System.Random random = new System.Random(digit);
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float tone = (Mathf.Sin(2f * Mathf.PI * low[digit] * t) + Mathf.Sin(2f * Mathf.PI * high[digit] * t)) * 0.1f;
                float press = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 700f) * 0.35f;
                data[i] = tone * Mathf.Clamp01(t * 300f) * Mathf.Clamp01((duration - t) * 50f) + press;
            }
            AudioClip clip = AudioClip.Create("Keypad tone " + digit, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Two rough low buzzes, like a security lock rejecting a code.</summary>
        public static AudioClip Denied()
        {
            const float duration = 0.42f;
            int count = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float local = t < 0.2f ? t : t - 0.22f;
                float envelope = local < 0f || local > 0.16f ? 0f : Mathf.Clamp01(local * 200f) * Mathf.Clamp01((0.16f - local) * 60f);
                float buzz = (Mathf.Sin(2f * Mathf.PI * 150f * t) >= 0f ? 0.5f : -0.5f) + (Mathf.Sin(2f * Mathf.PI * 157f * t) >= 0f ? 0.5f : -0.5f);
                data[i] = buzz * envelope * 0.16f;
            }
            AudioClip clip = AudioClip.Create("Access denied", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Original looping dark-synthwave track: 16 bars in A minor at 112 BPM.</summary>
        public static AudioClip Music()
        {
            const int rate = 22050;
            const int bars = 16;
            int stepSamples = Mathf.RoundToInt(60f / 112f / 4f * rate);
            float step = stepSamples / (float)rate;
            int count = stepSamples * bars * 16;
            float[] data = new float[count];
            float[] echo = new float[count];
            float[] roots = { 110f, 110f, 87.31f, 87.31f, 73.42f, 73.42f, 82.41f, 82.41f };
            bool[] major = { false, false, true, true, false, false, true, true };
            System.Random random = new System.Random(1994);

            // Notes wrap around the end of the buffer so the loop point is seamless.
            void Note(float[] buffer, int start, float seconds, float gain, System.Func<float, float> voice)
            {
                int samples = Mathf.RoundToInt(seconds * rate);
                for (int i = 0; i < samples; i++) buffer[(start + i) % count] += voice(i / (float)rate) * gain;
            }
            float Saw(float frequency, float t) => 2f * Mathf.Repeat(frequency * t, 1f) - 1f;
            float Triangle(float frequency, float t) => 4f * Mathf.Abs(Mathf.Repeat(frequency * t, 1f) - 0.5f) - 1f;
            float Noise() => (float)random.NextDouble() * 2f - 1f;

            for (int s = 0; s < bars * 16; s++)
            {
                int bar = s / 16, start = s * stepSamples;
                float root = roots[bar % 8];
                int third = major[bar % 8] ? 4 : 3;
                bool full = bar >= 8;

                if (s % 16 == 0)
                {
                    float padLength = step * 16f;
                    foreach (int interval in new[] { 0, third, 7 })
                    {
                        float f = root * 2f * Mathf.Pow(2f, interval / 12f);
                        Note(data, start, padLength, 0.035f, t => (Triangle(f * 1.004f, t) + Triangle(f * 0.996f, t)) * Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((padLength - t) / 0.4f));
                    }
                }
                if (s % 2 == 0)
                {
                    float f = s % 8 == 6 ? root * 2f : root;
                    Note(data, start, step * 1.8f, 0.2f, t => (Saw(f, t) * 0.35f + Mathf.Sin(2f * Mathf.PI * f * t)) * Mathf.Exp(-t * 7f));
                }
                int[] arpeggio = { 0, 7, 12, 12 + third, 12, 7, third, 7 };
                float arp = root * 2f * Mathf.Pow(2f, arpeggio[s % 8] / 12f);
                Note(echo, start, step * 0.9f, full ? 0.08f : 0.055f, t => Triangle(arp, t) * Mathf.Exp(-t * 16f));

                if (s % 4 == 0)
                    Note(data, start, 0.3f, full ? 0.5f : 0.32f, t => Mathf.Sin(2f * Mathf.PI * (48f * t + 14f * (1f - Mathf.Exp(-t * 28f)))) * Mathf.Exp(-t * 10f));
                if (full && s % 8 == 4)
                    Note(data, start, 0.22f, 0.14f, t => (Noise() + Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.6f) * Mathf.Exp(-t * 18f));
                if (full && s % 2 == 1)
                    Note(data, start, 0.05f, 0.045f, t => Noise() * Mathf.Exp(-t * 90f));
            }

            int delay = stepSamples * 3;
            float peak = 0.001f;
            for (int i = 0; i < count; i++)
            {
                echo[i] += echo[(i - delay + count) % count] * 0.4f;
                data[i] += echo[i];
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            for (int i = 0; i < count; i++) data[i] *= 0.8f / peak;
            AudioClip clip = AudioClip.Create("Arcade Lockdown theme", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
