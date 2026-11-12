using System;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Автосохранение прогресса и запись при выходе/сворачивании игры.
    /// Ставится на отдельный объект "GameRoot" вместе с GameManager.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField, Min(1f)] private float autoSaveIntervalSeconds = 30f;
        [SerializeField] private bool saveOnApplicationPause = true;
        [SerializeField] private bool logSaves;

        private float timer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            timer += Time.unscaledDeltaTime;
            if (timer >= autoSaveIntervalSeconds)
            {
                timer = 0f;
                SaveNow();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (saveOnApplicationPause && paused)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        /// <summary>Сохранить игру прямо сейчас.</summary>
        public void SaveNow()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            SaveData data = GameManager.Instance.CaptureSaveData();
            data.lastSaveUtc = DateTime.UtcNow.ToString("o");
            SaveSystem.Save(data);

            if (logSaves)
            {
                Debug.Log($"[SaveManager] Сохранено в {SaveSystem.SavePath}");
            }
        }
    }
}
