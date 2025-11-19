using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BillDebugger.Editor
{
    public class BillDebugWindow : OdinEditorWindow
    {
        private readonly List<string> userDisplayNames = new List<string>();
        private int filterMask;

        [MenuItem("Tools/BillDebugger/Open Config Panel")]
        private static void OpenWindow() => GetWindow<BillDebugWindow>("BillDebugger").Show();

        // Thuộc tính này sẽ được Odin vẽ ra, trỏ đến đối tượng config của chúng ta
        [ShowInInspector]
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        [Title("BillDebugger Control Panel")]
        private DebugConfig config;

        protected override void OnEnable()
        {
            base.OnEnable();
            config = DebugConfig.Instance;

            if (config != null)
            {
                config.SynchronizeEnum(); // Luôn đảm bảo enum được đồng bộ khi mở cửa sổ
            }

            PopulateUserCache();
            filterMask = EditorPrefs.GetInt("BillDebugger.FilterMask", -1);
            ApplyConsoleFilter();
        }

        // OdinEditorWindow cho phép ghi đè DrawEditor để vẽ thêm các control IMGUI tùy chỉnh
        protected override void DrawEditor(int index)
        {
            // Vẽ phần Console Filter trước
            DrawConsoleFilterGUI();

            // Sau đó để Odin vẽ phần còn lại (chính là đối tượng 'config')
            base.DrawEditor(index);
        }

        private void DrawConsoleFilterGUI()
        {
            if (config == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Unity Console Filter", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            filterMask = EditorGUILayout.MaskField("Show Logs For", filterMask, userDisplayNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("BillDebugger.FilterMask", filterMask);
                ApplyConsoleFilter();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void ApplyConsoleFilter()
        {
            var searchTerms = new List<string>();
            for (int i = 0; i < userDisplayNames.Count; i++)
            {
                if ((filterMask & (1 << i)) != 0)
                {
                    searchTerms.Add($"[{userDisplayNames[i]}]");
                }
            }

            var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var setSearchMethod = logEntriesType?.GetMethod("SetFilter", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            setSearchMethod?.Invoke(null, new object[] { string.Join(" ", searchTerms) });
        }

        private void PopulateUserCache()
        {
            userDisplayNames.Clear();
            var users = Enum.GetNames(typeof(DebugUser)).Where(n => n != "NONE").ToList();
            userDisplayNames.AddRange(users);
        }
    }
}