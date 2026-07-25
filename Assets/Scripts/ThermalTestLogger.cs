using UnityEngine;
using System.IO;

// Diagnostic-only overlay for the Day 18 phone thermal test (35+ min soak in headset).
// Intentionally plain/utilitarian - this is Google's own Hello Cardboard sample used as a
// test rig, not a game scene, so the project's game-quality directive does not apply here.
public class ThermalTestLogger : MonoBehaviour
{
    private TextMesh displayText;
    private StreamWriter csvWriter;
    private string csvPath;

    private float elapsedTotal = 0f;

    private float uiTimer = 0f;
    private int uiFrameCount = 0;
    private float uiFrameTimeAccum = 0f;
    private float fpsAverage = 0f;

    private float logTimer = 0f;
    private const float LogIntervalSeconds = 10f;

    private float lastBatteryPct = -1f;
    private float lastBatteryTempC = -1f;

    void Start()
    {
        // Force an uncapped frame pacing target - Android/Unity defaults observed to cap
        // near 30fps otherwise (vSyncCount>0 makes targetFrameRate get ignored, so both
        // must be set together). 2026-07-25: confirmed via adb dumpsys display that this
        // Pixel 10 panel natively supports 120Hz, so target that instead of the 60fps floor -
        // this scene is trivial enough to sustain it, and higher Hz reduces sample-and-hold
        // motion blur during head turns (each frame shown for less time).
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        RequestHighestDisplayRefreshRate();

        displayText = GetComponent<TextMesh>();
        if (displayText == null)
        {
            displayText = gameObject.AddComponent<TextMesh>();
        }
        if (GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }

        displayText.characterSize = 0.12f;
        displayText.fontSize = 48;
        displayText.anchor = TextAnchor.MiddleCenter;
        displayText.alignment = TextAlignment.Center;
        displayText.color = Color.yellow;
        displayText.text = "Thermal test starting...";

        csvPath = Path.Combine(Application.persistentDataPath, "thermal_log.csv");
        bool isNewFile = !File.Exists(csvPath);
        csvWriter = new StreamWriter(csvPath, true);
        if (isNewFile)
        {
            csvWriter.WriteLine("elapsed_s,fps_avg,battery_pct,battery_temp_c");
            csvWriter.Flush();
        }

        lastBatteryPct = GetBatteryPercent();
        lastBatteryTempC = GetBatteryTemperatureC();

        Debug.Log("ThermalTestLogger: logging to " + csvPath);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        elapsedTotal += dt;

        uiTimer += dt;
        uiFrameCount++;
        uiFrameTimeAccum += dt;

        if (uiTimer >= 1f && uiFrameTimeAccum > 0f)
        {
            fpsAverage = uiFrameCount / uiFrameTimeAccum;
            uiTimer = 0f;
            uiFrameCount = 0;
            uiFrameTimeAccum = 0f;

            lastBatteryPct = GetBatteryPercent();
            lastBatteryTempC = GetBatteryTemperatureC();

            UpdateDisplayText();
        }

        logTimer += dt;
        if (logTimer >= LogIntervalSeconds)
        {
            logTimer = 0f;
            WriteCsvRow();
        }
    }

    private void UpdateDisplayText()
    {
        string battPctStr = lastBatteryPct >= 0f ? lastBatteryPct.ToString("0") + "%" : "n/a";
        string battTempStr = lastBatteryTempC >= 0f ? lastBatteryTempC.ToString("0.0") + "C" : "n/a";
        displayText.text = string.Format(
            "T+{0:0}s  FPS {1:0.0}\nBatt {2}  {3}",
            elapsedTotal, fpsAverage, battPctStr, battTempStr);
    }

    private void WriteCsvRow()
    {
        if (csvWriter == null) return;
        csvWriter.WriteLine(string.Format("{0:0.0},{1:0.00},{2:0.0},{3:0.0}",
            elapsedTotal, fpsAverage, lastBatteryPct, lastBatteryTempC));
        csvWriter.Flush();
    }

    private float GetBatteryPercent()
    {
        return SystemInfo.batteryLevel >= 0f ? SystemInfo.batteryLevel * 100f : -1f;
    }

    // 2026-07-25: Application.targetFrameRate alone is a Unity-side pacing hint - Android's own
    // frame-rate governor was still overriding us back to 60Hz (confirmed via
    // "adb shell dumpsys display" showing an active frameRateOverride). This directly asks
    // Android's WindowManager for the highest-refresh-rate display mode the panel supports,
    // which is the real, low-level "vote" the OS frame-rate policy responds to.
    private void RequestHighestDisplayRefreshRate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var window = activity.Call<AndroidJavaObject>("getWindow"))
            using (var windowManager = activity.Call<AndroidJavaObject>("getWindowManager"))
            using (var display = windowManager.Call<AndroidJavaObject>("getDefaultDisplay"))
            {
                AndroidJavaObject[] modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
                int bestModeId = 0;
                float bestRefreshRate = 0f;
                foreach (var mode in modes)
                {
                    float rate = mode.Call<float>("getRefreshRate");
                    if (rate > bestRefreshRate)
                    {
                        bestRefreshRate = rate;
                        bestModeId = mode.Call<int>("getModeId");
                    }
                    mode.Dispose();
                }

                using (AndroidJavaObject layoutParams = window.Call<AndroidJavaObject>("getAttributes"))
                {
                    layoutParams.Set<int>("preferredDisplayModeId", bestModeId);
                    window.Call("setAttributes", layoutParams);
                }

                Debug.Log("ThermalTestLogger: requested display mode id=" + bestModeId +
                    " refreshRate=" + bestRefreshRate);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ThermalTestLogger: failed to request high refresh rate: " + e.Message);
        }
#endif
    }

    private float GetBatteryTemperatureC()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            {
                string actionBatteryChanged = intentClass.GetStatic<string>("ACTION_BATTERY_CHANGED");
                using (var filter = new AndroidJavaObject("android.content.IntentFilter", actionBatteryChanged))
                {
                    AndroidJavaObject batteryStatus = activity.Call<AndroidJavaObject>("registerReceiver", null, filter);
                    if (batteryStatus == null) return -1f;
                    int tenthsOfDegree = batteryStatus.Call<int>("getIntExtra", "temperature", -1);
                    batteryStatus.Dispose();
                    if (tenthsOfDegree < 0) return -1f;
                    return tenthsOfDegree / 10f;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ThermalTestLogger: battery temp read failed: " + e.Message);
            return -1f;
        }
#else
        return -1f;
#endif
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && csvWriter != null) csvWriter.Flush();
    }

    void OnDestroy()
    {
        FlushAndClose();
    }

    void OnApplicationQuit()
    {
        FlushAndClose();
    }

    private void FlushAndClose()
    {
        if (csvWriter != null)
        {
            csvWriter.Flush();
            csvWriter.Close();
            csvWriter = null;
        }
    }
}
