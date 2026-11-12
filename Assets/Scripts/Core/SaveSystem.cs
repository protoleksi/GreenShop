using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Базовая система сохранений.
    /// Основное хранилище — JSON-файл в Application.persistentDataPath.
    /// PlayerPrefs используется как резервная копия, если файл недоступен или повреждён.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "save.json";
        public const string PlayerPrefsKey = "greenshop.save.v1";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static string BackupPath => SavePath + ".bak";

        /// <summary>Есть ли хоть какое-то сохранение (файл, бэкап или PlayerPrefs).</summary>
        public static bool HasSave =>
            File.Exists(SavePath) ||
            File.Exists(BackupPath) ||
            PlayerPrefs.HasKey(PlayerPrefsKey);

        /// <summary>Записать данные в JSON-файл и продублировать их в PlayerPrefs.</summary>
        public static void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveSystem] Попытка сохранить null.");
                return;
            }

            data.version = SaveData.CurrentVersion;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string tempPath = SavePath + ".tmp";

                File.WriteAllText(tempPath, json, new UTF8Encoding(false));

                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupPath, true);
                }

                File.Copy(tempPath, SavePath, true);
                File.Delete(tempPath);

                // Резервная копия на случай проблем с файловой системой.
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Не удалось сохранить игру: {exception.Message}");
            }
        }

        /// <summary>Загрузить сохранение. Если его нет — вернуть новую структуру.</summary>
        public static SaveData Load()
        {
            SaveData data = ReadJsonFile(SavePath) ?? ReadJsonFile(BackupPath);

            if (data == null && PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                data = ReadJsonString(PlayerPrefs.GetString(PlayerPrefsKey, string.Empty));
                if (data != null)
                {
                    Debug.LogWarning("[SaveSystem] Загружено из резервной копии PlayerPrefs.");
                }
            }

            if (data == null)
            {
                return new SaveData();
            }

            return Migrate(data);
        }

        /// <summary>Удалить файл, бэкап и запись в PlayerPrefs.</summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                if (File.Exists(BackupPath))
                {
                    File.Delete(BackupPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Не удалось удалить файл сохранения: {exception.Message}");
            }

            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        private static SaveData ReadJsonFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                return ReadJsonString(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Не удалось прочитать {path}: {exception.Message}");
                return null;
            }
        }

        private static SaveData ReadJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Некорректный JSON сохранения: {exception.Message}");
                return null;
            }
        }

        /// <summary>Привести старые сохранения к актуальной версии.</summary>
        private static SaveData Migrate(SaveData data)
        {
            if (data.plants == null)
            {
                data.plants = new System.Collections.Generic.List<PlantSaveData>();
            }

            if (data.unlockedZones == null)
            {
                data.unlockedZones = new System.Collections.Generic.List<string>();
            }

            if (data.version < SaveData.CurrentVersion)
            {
                // Здесь будут миграции при появлении новых версий формата.
                data.version = SaveData.CurrentVersion;
            }

            return data;
        }
    }
}
