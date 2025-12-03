using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public class LoggerService : GenesisSingletonService<LoggerService>
    {
        private readonly StringBuilder _fileBuffer = new StringBuilder();
        private readonly List<LogData> _runtimeLogs = new List<LogData>();

        private string _path;
        private bool _showConsole;
        private Vector2 _scrollPos;
        private bool _collapse = true;

        // Filter Toggles
        private bool _showInfo = true;
        private bool _showWarn = true;
        private bool _showErr = true;

        private const int MAX_RUNTIME_LOGS = 200;

        private struct LogData
        {
            public string Time;
            public LogType Type;
            public string Message;
            public string StackTrace;
        }

        public override Task InitializeAsync()
        {
            _path = Path.Combine(Application.persistentDataPath, "genesis_log.txt");
            File.WriteAllText(_path, $"Session Started: {DateTime.Now}\nDevice: {SystemInfo.deviceModel}\nOS: {SystemInfo.operatingSystem}\n\n");

            Application.logMessageReceivedThreaded += OnLogReceived;
            return Task.CompletedTask;
        }

        public override void OnUpdate()
        {
            // Toggle Input: F1 or 3-finger touch
            if (Input.GetKeyDown(KeyCode.F1) || (Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                _showConsole = !_showConsole;
            }
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            // 1. Add to Runtime List (Thread Safe Logic needed if high volume, keeping simple for MainThread)
            lock (_runtimeLogs)
            {
                if (_runtimeLogs.Count >= MAX_RUNTIME_LOGS) _runtimeLogs.RemoveAt(0);
                _runtimeLogs.Add(new LogData
                {
                    Time = time,
                    Type = type,
                    Message = condition,
                    StackTrace = stackTrace
                });
            }

            // 2. Buffer for File
            lock (_fileBuffer)
            {
                _fileBuffer.AppendLine($"[{time}] [{type}] {condition}");
                if (type == LogType.Exception || type == LogType.Error)
                {
                    _fileBuffer.AppendLine(stackTrace);
                }

                if (_fileBuffer.Length > 2048) FlushToFile();
            }
        }

        private void FlushToFile()
        {
            if (_fileBuffer.Length == 0) return;
            try
            {
                File.AppendAllText(_path, _fileBuffer.ToString());
                _fileBuffer.Clear();
            }
            catch { /* Ignored */ }
        }

        public override void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;
            FlushToFile();
        }

        // =================================================================================================
        // IN-GAME CONSOLE RENDERING (IMGUI - lightweight & prefab-less)
        // =================================================================================================
        private void OnGUI()
        {
            if (!_showConsole) return;

            float scale = Screen.width / 1080f; // Scaling for mobile
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

            float width = 1080f;
            float height = Screen.height / scale;

            // Background
            GUI.Box(new Rect(0, 0, width, height), "Genesis Console");

            // Toolbar
            GUILayout.BeginArea(new Rect(10, 50, width - 20, height - 60));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", GUILayout.Height(50), GUILayout.Width(150)))
            {
                lock (_runtimeLogs) _runtimeLogs.Clear();
            }

            _collapse = GUILayout.Toggle(_collapse, "Collapse", GUILayout.Height(50), GUILayout.Width(150));
            _showInfo = GUILayout.Toggle(_showInfo, "Info", GUILayout.Height(50), GUILayout.Width(100));
            _showWarn = GUILayout.Toggle(_showWarn, "Warn", GUILayout.Height(50), GUILayout.Width(100));
            _showErr = GUILayout.Toggle(_showErr, "Error", GUILayout.Height(50), GUILayout.Width(100));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Height(50), GUILayout.Width(100))) _showConsole = false;

            GUILayout.EndHorizontal();

            // Log Area
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUI.skin.box);

            lock (_runtimeLogs)
            {
                for (int i = _runtimeLogs.Count - 1; i >= 0; i--)
                {
                    var log = _runtimeLogs[i];
                    if (!IsLogVisible(log.Type)) continue;

                    GUI.color = GetLogColor(log.Type);
                    GUILayout.Label($"[{log.Time}] {log.Message}");

                    if (!_collapse && (log.Type == LogType.Error || log.Type == LogType.Exception))
                    {
                        GUILayout.Label(log.StackTrace);
                    }
                }
            }

            GUI.color = Color.white;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool IsLogVisible(LogType type)
        {
            if (type == LogType.Log && !_showInfo) return false;
            if (type == LogType.Warning && !_showWarn) return false;
            if ((type == LogType.Error || type == LogType.Exception) && !_showErr) return false;
            return true;
        }

        private Color GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return Color.yellow;
                case LogType.Error:
                case LogType.Exception: return Color.red;
                default: return Color.white;
            }
        }
    }
}