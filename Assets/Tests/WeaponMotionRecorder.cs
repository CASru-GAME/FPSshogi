#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Editor-only recording of real rendered frames; captureDeltaTime keeps video and
// simulation at the same rate even when reading back a screenshot takes longer.
public sealed class WeaponMotionRecorder : MonoBehaviour
{
    public bool Busy { get; private set; }
    float previousCaptureDelta;
    public void Record(string directory, int frames, Action press, Action release)
    {
        Directory.CreateDirectory(directory);
        previousCaptureDelta = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 30;
        Busy = true;
        StartCoroutine(Capture(directory, frames, press, release));
    }
    IEnumerator Capture(string directory, int frames, Action press, Action release)
    {
        for (int i = 0; i < frames; i++)
        {
            if (i == 9) press();
            if (i == 10) release();
            yield return new WaitForEndOfFrame();
            var source = ScreenCapture.CaptureScreenshotAsTexture();
            // Encode the screenshot directly. A render-texture resize can apply the
            // project's linear/sRGB conversion again and wash out the review footage.
            File.WriteAllBytes(Path.Combine(directory, i.ToString("D4") + ".jpg"), source.EncodeToJPG(82));
            Destroy(source);
        }
        release();
        Cleanup();
    }
    void Cleanup()
    {
        if (!Busy) return;
        Time.captureDeltaTime = previousCaptureDelta;
        Busy = false;
    }
    void OnDisable() => Cleanup();
}
#endif
