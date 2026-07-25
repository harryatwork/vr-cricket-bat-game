using UnityEngine;

// Procedural night ambience (soft wind bed + intermittent cricket-like chirps) so the
// Firefly Study room isn't dead silent. Synthesized at runtime, no external audio asset
// needed. First pass - swap in a real recorded night-ambience clip later for more realism.
public class NightAmbience : MonoBehaviour
{
    private AudioSource _windSource;
    private AudioSource _cricketSource;
    private float _nextChirpTime;

    void Start()
    {
        _windSource = gameObject.AddComponent<AudioSource>();
        _windSource.clip = GenerateWindBed();
        _windSource.loop = true;
        _windSource.spatialBlend = 0f;
        _windSource.volume = 0.06f;
        _windSource.Play();

        _cricketSource = gameObject.AddComponent<AudioSource>();
        _cricketSource.spatialBlend = 0f;
        _cricketSource.playOnAwake = false;

        ScheduleNextChirp();
    }

    void Update()
    {
        if (Time.time >= _nextChirpTime)
        {
            _cricketSource.PlayOneShot(GenerateCricketChirp(), 0.35f);
            ScheduleNextChirp();
        }
    }

    private void ScheduleNextChirp()
    {
        _nextChirpTime = Time.time + Random.Range(0.6f, 2.2f);
    }

    // Low-passed brown noise - a soft, unobtrusive wind/room-tone bed.
    private AudioClip GenerateWindBed()
    {
        int sampleRate = 44100;
        float duration = 4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float last = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float white = Random.Range(-1f, 1f);
            last = (last + 0.02f * white) / 1.02f; // integrate towards brown noise
            samples[i] = last * 3f; // renormalize gain lost by the integration
        }

        AudioClip clip = AudioClip.Create("NightWindBed", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Three short high-frequency pulses with fast attack/decay - a stylized cricket chirp.
    private AudioClip GenerateCricketChirp()
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float freq = Random.Range(3800f, 4400f);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float pulsePhase = (t * 3f / duration) % 1f; // 3 pulses across the clip
            float envelope = Mathf.Exp(-pulsePhase * 25f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
        }

        AudioClip clip = AudioClip.Create("CricketChirp", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
