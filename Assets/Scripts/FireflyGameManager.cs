using UnityEngine;

// Comfort-test companion game: gaze at fireflies, dwell to catch them.
// Deliberately simple/low-pressure (US18b) - the goal is exercising full head range of motion
// while wearing the corrected lens profile for the 35-minute thermal soak, not a challenging game.
public class FireflyGameManager : MonoBehaviour
{
    public static FireflyGameManager Instance { get; private set; }

    private TextMesh scoreText;
    private int caughtCount = 0;
    private AudioSource audioSource;
    private AudioClip catchChime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        scoreText = GetComponent<TextMesh>();
        if (scoreText == null) scoreText = gameObject.AddComponent<TextMesh>();
        if (GetComponent<MeshRenderer>() == null) gameObject.AddComponent<MeshRenderer>();

        scoreText.characterSize = 0.12f;
        scoreText.fontSize = 48;
        scoreText.anchor = TextAnchor.MiddleCenter;
        scoreText.alignment = TextAlignment.Center;
        scoreText.color = new Color(1f, 0.85f, 0.3f);
        UpdateScoreText();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D - a catch chime should be heard regardless of gaze direction
        audioSource.playOnAwake = false;
        catchChime = GenerateChime();
    }

    public void OnFireflyCaught()
    {
        caughtCount++;
        UpdateScoreText();
        if (audioSource != null && catchChime != null)
        {
            audioSource.PlayOneShot(catchChime, 0.6f);
        }
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Fireflies caught: " + caughtCount;
    }

    // Procedurally generates a short two-note ascending chime (no external audio asset needed).
    private AudioClip GenerateChime()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float freq1 = 880f;  // A5
        float freq2 = 1318.5f; // E6
        int split = sampleCount / 2;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = i < split ? freq1 : freq2;
            float localT = i < split ? t : t - ((float)split / sampleRate);
            float envelope = Mathf.Exp(-localT * 12f); // quick decay per note, avoids clicks
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("FireflyCatchChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
