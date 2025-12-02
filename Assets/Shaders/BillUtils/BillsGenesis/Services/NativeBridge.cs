using UnityEngine;
using System.Runtime.InteropServices;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class NativeBridge : GenesisSingletonService<NativeBridge>
    {
        public enum HapticType { Light, Medium, Heavy, Success, Warning, Error }

        public override void OnAppReady()
        {
            // Auto set optimal frame rate
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void SetFrameRate(int fps) => Application.targetFrameRate = fps;

        public void CopyToClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            ShowToast("Copied to clipboard");
        }

        public string GetFromClipboard() => GUIUtility.systemCopyBuffer;

        public void Vibrate(HapticType type)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(GetDuration(type));
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS implementation requires native plugin usually, 
            // using Handheld as fallback for basic vibration
            Handheld.Vibrate(); 
#else
            Debug.Log($"[Native] Vibrate: {type}");
#endif
        }

        public void ShowToast(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", currentActivity, message, 0);
                toast.Call("show");
            }));
#else
            Debug.Log($"[Toast] {message}");
#endif
        }

        public string GetDeviceInfo()
        {
            return $"Model: {SystemInfo.deviceModel} | RAM: {SystemInfo.systemMemorySize}MB | OS: {SystemInfo.operatingSystem}";
        }

        private long GetDuration(HapticType type)
        {
            switch (type)
            {
                case HapticType.Light: return 20;
                case HapticType.Medium: return 40;
                case HapticType.Heavy: return 80;
                case HapticType.Success: return 50;
                default: return 30;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void VibrateAndroid(long milliseconds)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator.Call<bool>("hasVibrator"))
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            catch { }
        }
#endif
    }
}