using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BillDebugger
{
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "BillDebugger/Create Config")]
    public class DebugConfig : ScriptableSingleton<DebugConfig>
    {
        [System.Serializable]
        public class UserConfig
        {
            [HorizontalGroup("Config", 20)]
            [ToggleLeft]
            public bool Enabled = true;

            [HorizontalGroup("Config")]
            public Color Color = Color.white;

            [TextArea(2, 4)]
            public string Description = "";
        }

        [SerializeField, HideInInspector]
        private uint enabledMask = 0;

        [Title("User Runtime Configurations")]
        [DictionaryDrawerSettings(KeyLabel = "User", ValueLabel = "Details")]
        [OnValueChanged("ApplyChanges")] // Tự động gọi ApplyChanges khi có bất kỳ thay đổi nào
        [SerializeField]
        private Dictionary<DebugUser, UserConfig> userConfigs = new Dictionary<DebugUser, UserConfig>();

        public uint EnabledMask => enabledMask;
        internal Dictionary<DebugUser, UserConfig> UserConfigs => userConfigs;

#if UNITY_EDITOR
        [Title("Actions")]
        [Button(ButtonSizes.Large), HorizontalGroup("Actions")]
        private void EnableAll()
        {
            foreach (var config in userConfigs.Values) config.Enabled = true;
            ApplyChanges();
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Actions")]
        private void DisableAll()
        {
            foreach (var config in userConfigs.Values) config.Enabled = false;
            ApplyChanges();
        }

        [Button(ButtonSizes.Large), HorizontalGroup("Actions")]
        public void SynchronizeEnum()
        {
            bool changed = false;
            foreach (DebugUser user in System.Enum.GetValues(typeof(DebugUser)))
            {
                if (user != DebugUser.NONE && !userConfigs.ContainsKey(user))
                {
                    userConfigs[user] = new UserConfig
                    {
                        Color = Random.ColorHSV(0f, 1f, 0.9f, 1f, 1f, 1f)
                    };
                    changed = true;
                }
            }
            if (changed) ApplyChanges();
        }

        public void ApplyChanges()
        {
            uint newMask = 0;
            if (userConfigs != null)
            {
                foreach (var pair in userConfigs)
                {
                    if (pair.Value.Enabled)
                    {
                        newMask |= (1u << (int)pair.Key);
                    }
                }
            }
            enabledMask = newMask;
            EditorUtility.SetDirty(this);
            BillDebug.RequestReinitialization();
        }
#endif
    }
}