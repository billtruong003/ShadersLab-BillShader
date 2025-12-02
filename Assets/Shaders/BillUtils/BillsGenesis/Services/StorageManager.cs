using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BillsGenesis.Core;

namespace BillsGenesis.Services
{
    public sealed class StorageManager : GenesisSingletonService<StorageManager>
    {
        private const string KEY_IV = "3928471048273849";
        private const string KEY_PHRASE = "BillsGenesis_Secure_Storage_Key_2025";

        private string _basePath;
        private byte[] _keyBytes;
        private byte[] _ivBytes;

        public override Task InitializeAsync()
        {
            _basePath = Application.persistentDataPath;
            _keyBytes = Encoding.UTF8.GetBytes(KEY_PHRASE.Substring(0, 16));
            _ivBytes = Encoding.UTF8.GetBytes(KEY_IV.Substring(0, 16));
            return base.InitializeAsync();
        }

        #region PlayerPrefs (Settings & Lightweight Data)

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public float GetFloat(string key, float defaultValue = 0.0f) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public bool GetBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        public void SetDateTime(string key, DateTime date) => PlayerPrefs.SetString(key, date.ToBinary().ToString());
        public DateTime GetDateTime(string key)
        {
            string s = PlayerPrefs.GetString(key, string.Empty);
            return string.IsNullOrEmpty(s) ? DateTime.MinValue : DateTime.FromBinary(long.Parse(s));
        }

        public void SetVector3(string key, Vector3 value) => PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        public Vector3 GetVector3(string key)
        {
            string s = PlayerPrefs.GetString(key, string.Empty);
            return string.IsNullOrEmpty(s) ? Vector3.zero : JsonUtility.FromJson<Vector3>(s);
        }

        public void SetColor(string key, Color value) => PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        public Color GetColor(string key)
        {
            string s = PlayerPrefs.GetString(key, string.Empty);
            return string.IsNullOrEmpty(s) ? Color.white : JsonUtility.FromJson<Color>(s);
        }

        public void SavePrefs() => PlayerPrefs.Save();
        public void ClearPrefs() => PlayerPrefs.DeleteAll();
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        #endregion

        #region File System (Complex Data & Save Slots)

        public void SaveJson<T>(string filename, T data, bool encrypt = true)
        {
            string json = JsonUtility.ToJson(data, !encrypt);
            WriteFile(filename, json, encrypt);
        }

        public T LoadJson<T>(string filename, bool encrypt = true) where T : new()
        {
            string json = ReadFile(filename, encrypt);
            return string.IsNullOrEmpty(json) ? new T() : JsonUtility.FromJson<T>(json);
        }

        public void SaveList<T>(string filename, List<T> list, bool encrypt = true)
        {
            string json = JsonUtility.ToJson(new ListWrapper<T>(list), !encrypt);
            WriteFile(filename, json, encrypt);
        }

        public List<T> LoadList<T>(string filename, bool encrypt = true)
        {
            string json = ReadFile(filename, encrypt);
            if (string.IsNullOrEmpty(json)) return new List<T>();
            ListWrapper<T> wrapper = JsonUtility.FromJson<ListWrapper<T>>(json);
            return wrapper != null ? wrapper.Items : new List<T>();
        }

        public async Task SaveJsonAsync<T>(string filename, T data, bool encrypt = true)
        {
            string json = JsonUtility.ToJson(data);
            await WriteFileAsync(filename, json, encrypt);
        }

        public async Task<T> LoadJsonAsync<T>(string filename, bool encrypt = true) where T : new()
        {
            string json = await ReadFileAsync(filename, encrypt);
            return string.IsNullOrEmpty(json) ? new T() : JsonUtility.FromJson<T>(json);
        }

        public void SaveRaw(string filename, byte[] data)
        {
            string path = GetPath(filename);
            File.WriteAllBytes(path, data);
        }

        public byte[] LoadRaw(string filename)
        {
            string path = GetPath(filename);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void SaveTexture(string filename, Texture2D texture, bool asJpeg = false)
        {
            byte[] bytes = asJpeg ? texture.EncodeToJPG() : texture.EncodeToPNG();
            SaveRaw(filename, bytes);
        }

        public Texture2D LoadTexture(string filename)
        {
            byte[] bytes = LoadRaw(filename);
            if (bytes == null) return null;
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }

        #endregion

        #region File Management

        public bool FileExists(string filename) => File.Exists(GetPath(filename));

        public void DeleteFile(string filename)
        {
            string path = GetPath(filename);
            if (File.Exists(path)) File.Delete(path);
        }

        public string[] GetAllFiles(string extension = "*")
        {
            return Directory.GetFiles(_basePath, extension, SearchOption.TopDirectoryOnly);
        }

        public long GetFileSize(string filename)
        {
            string path = GetPath(filename);
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }

        public void ClearAllFiles()
        {
            DirectoryInfo di = new DirectoryInfo(_basePath);
            foreach (FileInfo file in di.GetFiles()) file.Delete();
            foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);
        }

        #endregion

        #region Internal Logic

        private string GetPath(string filename)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentException("Filename cannot be empty");
            return Path.Combine(_basePath, filename.EndsWith(".dat") || filename.Contains(".") ? filename : filename + ".dat");
        }

        private void WriteFile(string filename, string content, bool encrypt)
        {
            string path = GetPath(filename);
            if (encrypt)
            {
                byte[] encrypted = Encrypt(content);
                File.WriteAllBytes(path, encrypted);
            }
            else
            {
                File.WriteAllText(path, content, Encoding.UTF8);
            }
        }

        private string ReadFile(string filename, bool decrypt)
        {
            string path = GetPath(filename);
            if (!File.Exists(path)) return null;

            if (decrypt)
            {
                byte[] bytes = File.ReadAllBytes(path);
                return Decrypt(bytes);
            }
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private async Task WriteFileAsync(string filename, string content, bool encrypt)
        {
            string path = GetPath(filename);
            if (encrypt)
            {
                byte[] encrypted = Encrypt(content);
                using (FileStream sourceStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await sourceStream.WriteAsync(encrypted, 0, encrypted.Length);
                }
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    await writer.WriteAsync(content);
                }
            }
        }

        private async Task<string> ReadFileAsync(string filename, bool decrypt)
        {
            string path = GetPath(filename);
            if (!File.Exists(path)) return null;

            if (decrypt)
            {
                byte[] bytes;
                using (FileStream sourceStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bytes = new byte[sourceStream.Length];
                    await sourceStream.ReadAsync(bytes, 0, (int)sourceStream.Length);
                }
                return Decrypt(bytes);
            }

            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
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
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
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
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
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
            public ListWrapper(List<T> items) => Items = items;
        }

        #endregion
    }
}