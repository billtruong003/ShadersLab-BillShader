using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class NativeBridge : GenesisSingletonService<NativeBridge>
    {
        public enum HapticType { Light, Medium, Heavy, Success, Warning, Error }

        public override void OnAppReady()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void Vibrate(HapticType type)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(GetDuration(type));
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate(); 
#endif
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
                    if (vibrator.Call<bool>("hasVibrator")) vibrator.Call("vibrate", milliseconds);
                }
            }
            catch { }
        }
#endif
    }
}