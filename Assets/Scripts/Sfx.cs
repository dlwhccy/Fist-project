using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序合成音效：不导入任何 wav 文件，运行时用正弦/方波/三角波算出 AudioClip。
/// 用法：Sfx.Init(audioSource) 一次，然后 Sfx.Play("brick") 随处可调。
/// </summary>
public static class Sfx
{
    const int Rate = 44100;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static AudioSource src;

    public static void Init(AudioSource source)
    {
        src = source;
        if (clips.Count > 0) return;

        clips["paddle"] = Sweep("paddle", 560f, 520f, 0.08f, 0.45f, 0);
        clips["brick"] = Sweep("brick", 900f, 1250f, 0.07f, 0.40f, 2);
        clips["wall"] = Sweep("wall", 320f, 300f, 0.05f, 0.30f, 1);
        clips["lose"] = Sweep("lose", 420f, 90f, 0.45f, 0.45f, 1);
        clips["win"] = Arpeggio("win", new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.11f, 0.40f);
        clips["over"] = Sweep("over", 300f, 60f, 0.8f, 0.5f, 1);
    }

    public static void Play(string id, float pitch = 1f)
    {
        if (src == null) return;
        AudioClip c;
        if (!clips.TryGetValue(id, out c)) return;
        src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        src.PlayOneShot(c, 0.85f);
    }

    // ---------- 合成器 ----------

    // wave: 0 正弦 / 1 方波 / 2 三角波
    static float Waveform(float phase, int wave)
    {
        switch (wave)
        {
            case 1: return Mathf.Sign(Mathf.Sin(phase));
            case 2: return 2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f))) - 1f;
            default: return Mathf.Sin(phase);
        }
    }

    static AudioClip Sweep(string name, float f0, float f1, float dur, float vol, int wave)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(Rate * dur));
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            float env = Mathf.Exp(-3.2f * t) * Mathf.Min(1f, (float)i / (Rate * 0.004f)); // 指数衰减 + 4ms 起音
            data[i] = Waveform(phase, wave) * env * vol;
            phase += 2f * Mathf.PI * f / Rate;
        }
        return Make(name, data);
    }

    static AudioClip Arpeggio(string name, float[] notes, float noteDur, float vol)
    {
        int per = Mathf.CeilToInt(Rate * noteDur);
        int n = per * notes.Length;
        var data = new float[n];
        for (int k = 0; k < notes.Length; k++)
        {
            float phase = 0f;
            for (int i = 0; i < per; i++)
            {
                float t = (float)i / per;
                float env = Mathf.Exp(-2.6f * t) * Mathf.Min(1f, (float)i / (Rate * 0.004f));
                data[k * per + i] = Waveform(phase, 2) * env * vol;
                phase += 2f * Mathf.PI * notes[k] / Rate;
            }
        }
        return Make(name, data);
    }

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
