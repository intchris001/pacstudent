#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class A3AudioGenerator
{
    [MenuItem("PacStudent/Generate Audio Clips (A3)")]
    public static void GenerateAllAudio()
    {
        string dir = "Assets/Audio Clips";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // Simple tones with different frequencies/patterns
        WriteToneWav(Path.Combine(dir, "intro.wav"), 1.5f, 440f);
        WriteToneWav(Path.Combine(dir, "startscene.wav"), 4f, 220f);
        WriteToneWav(Path.Combine(dir, "ghosts_normal.wav"), 10f, 320f);
        WriteToneWav(Path.Combine(dir, "ghosts_fright.wav"), 10f, 520f);
        WritePatternWav(Path.Combine(dir, "ghosts_dead.wav"), 10f, new float[]{600f, 300f}, new float[]{0.4f, 0.4f});
        WritePatternWav(Path.Combine(dir, "sfx_move.wav"), 0.2f, new float[]{800f, 880f}, new float[]{0.05f, 0.05f});
        WriteToneWav(Path.Combine(dir, "sfx_pellet.wav"), 0.12f, 1200f);
        WriteToneWav(Path.Combine(dir, "sfx_wall.wav"), 0.18f, 180f);
        WritePatternWav(Path.Combine(dir, "sfx_death.wav"), 1.0f, new float[]{660f, 440f, 220f}, new float[]{0.25f,0.25f,0.25f});

        AssetDatabase.Refresh();
        Debug.Log("Generated basic audio clips in Assets/Audio Clips. Assign them in AudioManager.");
    }

    private static void WriteToneWav(string path, float duration, float freq)
    {
        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(duration * sampleRate);
        short[] pcm = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float s = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.3f;
            pcm[i] = (short)Mathf.Clamp(s * short.MaxValue, short.MinValue, short.MaxValue);
        }
        WriteWav16Mono(path, pcm, sampleRate);
    }

    private static void WritePatternWav(string path, float totalDuration, float[] freqs, float[] durations)
    {
        int sampleRate = 44100;
        using (var mem = new MemoryStream())
        using (var bw = new BinaryWriter(mem))
        {
            // We'll stitch segments then write header
            var samplesAll = new System.Collections.Generic.List<short>();
            for (int p = 0; p < freqs.Length && p < durations.Length; p++)
            {
                int segSamples = Mathf.CeilToInt(durations[p] * sampleRate);
                for (int i = 0; i < segSamples; i++)
                {
                    float t = i / (float)sampleRate;
                    float s = Mathf.Sin(2 * Mathf.PI * freqs[p] * t) * 0.3f;
                    short v = (short)Mathf.Clamp(s * short.MaxValue, short.MinValue, short.MaxValue);
                    samplesAll.Add(v);
                }
            }
            // Loop or trim to totalDuration
            int target = Mathf.CeilToInt(totalDuration * sampleRate);
            var final = new short[target];
            for (int i = 0; i < target; i++) final[i] = samplesAll[i % samplesAll.Count];
            WriteWav16Mono(path, final, sampleRate);
        }
    }

    private static void WriteWav16Mono(string path, short[] samples, int sampleRate)
    {
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2; // mono 16-bit
            int subChunk2Size = samples.Length * 2;
            int chunkSize = 36 + subChunk2Size;
            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            // fmt subchunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // PCM
            bw.Write((short)1); // Audio format = PCM
            bw.Write((short)1); // mono
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2); // block align
            bw.Write((short)16); // bits per sample
            // data subchunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subChunk2Size);
            foreach (var s in samples) bw.Write(s);
        }
    }
}
#endif

