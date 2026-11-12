using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Центральное состояние игры: монеты, доход в секунду, статистика и данные для сохранения.
    /// Должен быть один на всю игру (переживает смену сцен).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Стартовые значения (если сохранения нет)")]
        [SerializeField] private double startingCoins;
        [SerializeField] private double startingIncomePerSecond = 1.0;

        [Header("Сохранение")]
        [SerializeField] private bool loadSaveOnStart = true;
        [SerializeField] private bool persistBetweenScenes = true;

        /// <summary>Текущее количество монет.</summary>
        public double Coins { get; private set; }

        /// <summary>Пассивный доход в секунду.</summary>
        public double IncomePerSecond { get; private set; }

        /// <summary>Всего кликов за игру.</summary>
        public long TotalClicks { get; private set; }

        /// <summary>Суммарное время в игре, секунды.</summary>
        public double TotalPlayTimeSeconds { get; private set; }

        /// <summary>Состояния растений для сохранения (заполняется системой грядок).</summary>
        public List<PlantSaveData> PlantStates { get; } = new List<PlantSaveData>();

        /// <summary>Открытые зоны (имена зон или сцен).</summary>
        public List<string> UnlockedZones { get; } = new List<string>();

        public event Action<double> CoinsChanged;
        public event Action<double> IncomeChanged;
        public event Action<long> ClickCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (loadSaveOnStart)
            {
                ApplySaveData(SaveSystem.Load());
            }
            else
            {
                ResetToDefaults();
            }
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
            TotalPlayTimeSeconds += Time.deltaTime;
        }

        /// <summary>Начислить монеты (клик, доход, награда).</summary>
        public void AddCoins(double amount)
        {
            if (amount <= 0.0 || double.IsNaN(amount) || double.IsInfinity(amount))
            {
                return;
            }

            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        /// <summary>Списать монеты, если их достаточно. Возвращает false, если не хватает.</summary>
        public bool TrySpendCoins(double amount)
        {
            if (amount < 0.0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        /// <summary>Задать пассивный доход в секунду.</summary>
        public void SetIncome(double perSecond)
        {
            IncomePerSecond = Math.Max(0.0, perSecond);
            IncomeChanged?.Invoke(IncomePerSecond);
        }

        /// <summary>Добавить (или отнять) доход в секунду.</summary>
        public void AddIncome(double delta)
        {
            SetIncome(IncomePerSecond + delta);
        }

        /// <summary>Учесть один клик по кликеру.</summary>
        public void RegisterClick()
        {
            TotalClicks++;
            ClickCountChanged?.Invoke(TotalClicks);
        }

        /// <summary>Открыть зону (повторные вызовы игнорируются).</summary>
        public void UnlockZone(string zoneName)
        {
            if (string.IsNullOrWhiteSpace(zoneName) || UnlockedZones.Contains(zoneName))
            {
                return;
            }

            UnlockedZones.Add(zoneName);
        }

        /// <summary>Собрать данные для записи в файл.</summary>
        public SaveData CaptureSaveData()
        {
            var data = new SaveData
            {
                version = SaveData.CurrentVersion,
                coins = Coins,
                incomePerSecond = IncomePerSecond,
                totalClicks = TotalClicks,
                totalPlayTimeSeconds = TotalPlayTimeSeconds,
                plants = new List<PlantSaveData>(PlantStates),
                unlockedZones = new List<string>(UnlockedZones)
            };

            return data;
        }

        /// <summary>Применить загруженные данные (или сбросить состояние при null).</summary>
        public void ApplySaveData(SaveData data)
        {
            if (data == null)
            {
                ResetToDefaults();
                return;
            }

            Coins = Math.Max(0.0, data.coins);
            IncomePerSecond = Math.Max(0.0, data.incomePerSecond);
            TotalClicks = Math.Max(0L, data.totalClicks);
            TotalPlayTimeSeconds = Math.Max(0.0, data.totalPlayTimeSeconds);

            PlantStates.Clear();
            if (data.plants != null)
            {
                PlantStates.AddRange(data.plants);
            }

            UnlockedZones.Clear();
            if (data.unlockedZones != null)
            {
                UnlockedZones.AddRange(data.unlockedZones);
            }

            CoinsChanged?.Invoke(Coins);
            IncomeChanged?.Invoke(IncomePerSecond);
            ClickCountChanged?.Invoke(TotalClicks);
        }

        /// <summary>Сбросить прогресс в памяти (сам файл сохранения не удаляется).</summary>
        public void ResetProgress()
        {
            ResetToDefaults();
            CoinsChanged?.Invoke(Coins);
            IncomeChanged?.Invoke(IncomePerSecond);
            ClickCountChanged?.Invoke(TotalClicks);
        }

        private void ResetToDefaults()
        {
            Coins = Math.Max(0.0, startingCoins);
            IncomePerSecond = Math.Max(0.0, startingIncomePerSecond);
            TotalClicks = 0;
            TotalPlayTimeSeconds = 0.0;
            PlantStates.Clear();
            UnlockedZones.Clear();
        }
    }
}
