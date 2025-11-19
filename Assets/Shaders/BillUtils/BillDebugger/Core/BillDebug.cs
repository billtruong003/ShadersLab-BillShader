using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace BillDebugger
{
    public static class BillDebug
    {
        private static readonly StringBuilder CachedBuilder = new StringBuilder(1024);
        private static readonly Dictionary<DebugUser, string> ColorHeaders = new Dictionary<DebugUser, string>();
        private static bool isInitialized = false;

        private static DebugConfig Config => DebugConfig.Instance;
        private static uint EnabledMask => Config ? Config.EnabledMask : 0;

#if UNITY_EDITOR
        // This method is public but only exists in the editor.
        // It will be completely stripped from builds.
        public static void RequestReinitialization()
        {
            isInitialized = false;
        }
#endif

        private static void InitializeIfNeeded()
        {
            if (isInitialized || Config == null) return;

            ColorHeaders.Clear();
            if (Config.UserConfigs != null)
            {
                foreach (var pair in Config.UserConfigs)
                {
                    if (pair.Key == DebugUser.NONE) continue;
                    string hexColor = ColorUtility.ToHtmlStringRGB(pair.Value.Color);
                    ColorHeaders[pair.Key] = $"<color=#{hexColor}><b>[{pair.Key}]</b></color> ";
                }
            }
            isInitialized = true;
        }

        private static bool IsUserEnabled(DebugUser user)
        {
            // The enum cast to int is the bit position.
            return (EnabledMask & (1u << (int)user)) != 0;
        }

        [System.Diagnostics.Conditional("BILLDEBUG_ENABLED")]
        public static void Log(DebugUser user, string message)
        {
            if (user == DebugUser.NONE || !IsUserEnabled(user)) return;
            InitializeIfNeeded();

            CachedBuilder.Clear();
            CachedBuilder.Append(ColorHeaders[user]);
            CachedBuilder.Append(message);

            UnityEngine.Debug.Log(CachedBuilder.ToString());
        }

        [System.Diagnostics.Conditional("BILLDEBUG_ENABLED")]
        public static void LogClickableTrace(DebugUser user, string message, Object context = null)
        {
            if (user == DebugUser.NONE || !IsUserEnabled(user)) return;
            InitializeIfNeeded();

            CachedBuilder.Clear();
            CachedBuilder.Append(ColorHeaders[user]);
            CachedBuilder.Append(message);

            // Passing the context object allows clicking the log to highlight the object.
            // Appending "\n" helps Unity provide a better stack trace.
            UnityEngine.Debug.Log(CachedBuilder.ToString() + "\n", context);
        }
    }
}