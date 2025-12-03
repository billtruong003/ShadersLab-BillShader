using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class StorageManager : GenesisSingletonService<StorageManager>
    {
        private const string ENCRYPTION_KEY = "BillsGenesis_Secure_Key_2025_Hero";
        private const string ENCRYPTION_IV = "8492018472910283";

        private string _persistentPath;
        private byte[] _keyBytes;
        private byte[] _ivBytes;

        public override Task InitializeAsync()
        {
            _persistentPath = Application.persistentDataPath;
            _keyBytes = Encoding.UTF8.GetBytes(ENCRYPTION_KEY.Substring(0, 16));
            _ivBytes = Encoding.UTF8.GetBytes(ENCRYPTION_IV.Substring(0, 16));
            return Task.CompletedTask;
        }

        #region PlayerPrefs Extended (Settings & Lightweight Data)

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public bool GetBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public void SetDateTime(string key, DateTime date) => PlayerPrefs.SetString(key, date.ToBinary().ToString());
        public DateTime GetDateTime(string key)
        {
            string s = PlayerPrefs.GetString(key);
            return string.IsNullOrEmpty(s) ? DateTime.MinValue : DateTime.FromBinary(long.Parse(s));
        }

        public void SetVector3(string key, Vector3 value) => PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        public Vector3 GetVector3(string key)
        {
            string s = PlayerPrefs.GetString(key);
            return string.IsNullOrEmpty(s) ? Vector3.zero : JsonUtility.FromJson<Vector3>(s);
        }

        public void SetColor(string key, Color value) => PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        public Color GetColor(string key)
        {
            string s = PlayerPrefs.GetString(key);
            return string.IsNullOrEmpty(s) ? Color.white : JsonUtility.FromJson<Color>(s);
        }

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void SavePrefs() => PlayerPrefs.Save();

        #endregion

        #region File System (Async & Encrypted)

        public void Save<T>(string filename, T data, bool encrypt = true)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                WriteToFile(filename, json, encrypt);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Storage] Save failed: {e.Message}");
            }
        }

        public T Load<T>(string filename, bool encrypt = true) where T : new()
        {
            try
            {
                if (!FileExists(filename)) return new T();
                string json = ReadFromFile(filename, encrypt);
                return string.IsNullOrEmpty(json) ? new T() : JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Storage] Load failed: {e.Message}");
                return new T();
            }
        }

        public async Task SaveAsync<T>(string filename, T data, bool encrypt = true)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                await WriteToFileAsync(filename, json, encrypt);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Storage] Async Save failed: {e.Message}");
            }
        }

        public async Task<T> LoadAsync<T>(string filename, bool encrypt = true) where T : new()
        {
            try
            {
                if (!FileExists(filename)) return new T();
                string json = await ReadFromFileAsync(filename, encrypt);
                return string.IsNullOrEmpty(json) ? new T() : JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Storage] Async Load failed: {e.Message}");
                return new T();
            }
        }

        public void SaveList<T>(string filename, List<T> list, bool encrypt = true)
        {
            var wrapper = new ListWrapper<T> { Items = list };
            Save(filename, wrapper, encrypt);
        }

        public List<T> LoadList<T>(string filename, bool encrypt = true)
        {
            var wrapper = Load<ListWrapper<T>>(filename, encrypt);
            return wrapper?.Items ?? new List<T>();
        }

        public void DeleteFile(string filename)
        {
            string path = GetPath(filename);
            if (File.Exists(path)) File.Delete(path);
        }

        public bool FileExists(string filename) => File.Exists(GetPath(filename));

        public void BackupFile(string sourceName, string destName)
        {
            string srcPath = GetPath(sourceName);
            string dstPath = GetPath(destName);
            if (File.Exists(srcPath)) File.Copy(srcPath, dstPath, true);
        }

        #endregion

        #region Internal I/O Logic

        private string GetPath(string filename)
        {
            return Path.Combine(_persistentPath, filename.Contains(".") ? filename : $"{filename}.dat");
        }

        private void WriteToFile(string filename, string content, bool encrypt)
        {
            string path = GetPath(filename);
            byte[] bytes = encrypt ? Encrypt(content) : Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(path, bytes);
        }

        private string ReadFromFile(string filename, bool decrypt)
        {
            string path = GetPath(filename);
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            return decrypt ? Decrypt(bytes) : Encoding.UTF8.GetString(bytes);
        }

        private async Task WriteToFileAsync(string filename, string content, bool encrypt)
        {
            string path = GetPath(filename);
            byte[] bytes = encrypt ? Encrypt(content) : Encoding.UTF8.GetBytes(content);
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private async Task<string> ReadFromFileAsync(string filename, bool decrypt)
        {
            string path = GetPath(filename);
            if (!File.Exists(path)) return null;

            byte[] bytes;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                bytes = new byte[fs.Length];
                await fs.ReadAsync(bytes, 0, (int)fs.Length);
            }
            return decrypt ? Decrypt(bytes) : Encoding.UTF8.GetString(bytes);
        }

        private byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _keyBytes;
                aes.IV = _ivBytes;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }

        private string Decrypt(byte[] cipherText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _keyBytes;
                    aes.IV = _ivBytes;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream ms = new MemoryStream(cipherText))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private class ListWrapper<T>
        {
            public List<T> Items;
        }

        #endregion

        #region Odin Debug Tools

        [Title("Maintenance")]
        [Button(ButtonSizes.Large, Icon = SdfIconType.Folder), GUIColor(1, 0.8f, 0)]
        private void OpenDataFolder()
        {
            Application.OpenURL($"file://{_persistentPath}");
        }

        [Button(ButtonSizes.Medium, Icon = SdfIconType.Trash), GUIColor(1, 0.4f, 0.4f)]
        private void DeleteAllData()
        {
            PlayerPrefs.DeleteAll();
            var di = new DirectoryInfo(_persistentPath);
            foreach (FileInfo file in di.GetFiles()) file.Delete();
            Debug.Log("[Storage] All data cleared.");
        }

        #endregion
    }
}