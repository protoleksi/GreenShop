using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Склад и витрина готовых растений в Торговом зале.
    /// Сюда попадают собранные из оранжереи цветы, и отсюда они продаются клиентам.
    /// Вместимость витрины ограничена (по умолчанию 6 слотов/цветов).
    /// </summary>
    public class ShopInventory : MonoBehaviour
    {
        public static ShopInventory Instance { get; private set; }

        [Header("Вместимость витрины")]
        [SerializeField] private int maxCapacity = 6;

        [Header("UI Ссылки")]
        [SerializeField] private TMP_Text shelfSummaryLabel;

        private readonly Dictionary<string, int> flowerStock = new Dictionary<string, int>();

        public event Action InventoryChanged;

        public int MaxCapacity => maxCapacity;

        public int TotalFlowerCount
        {
            get
            {
                int total = 0;
                foreach (var kvp in flowerStock)
                {
                    total += kvp.Value;
                }
                return total;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateUI();
        }

        /// <summary>Проверить, есть ли свободное место на витрине.</summary>
        public bool CanAcceptFlower()
        {
            return TotalFlowerCount < maxCapacity;
        }

        /// <summary>Попытаться добавить собранный цветок на витрину.</summary>
        public bool TryAddFlower(PlantData plant, int count = 1)
        {
            if (plant == null || count <= 0) return false;

            if (TotalFlowerCount + count > maxCapacity)
            {
                Debug.LogWarning($"[ShopInventory] Витрина переполнена! ({TotalFlowerCount}/{maxCapacity})");
                return false;
            }

            string id = plant.Id;
            if (!flowerStock.ContainsKey(id))
            {
                flowerStock[id] = 0;
            }

            flowerStock[id] += count;
            UpdateUI();
            InventoryChanged?.Invoke();
            Debug.Log($"[ShopInventory] Добавлен цветок: {plant.PlantName} (+{count}). Всего на витрине: {TotalFlowerCount}/{maxCapacity}");
            return true;
        }

        /// <summary>Добавить собранный цветок на витрину (алиас для совместимости).</summary>
        public bool AddFlower(PlantData plant, int count = 1)
        {
            return TryAddFlower(plant, count);
        }

        /// <summary>Проверить, есть ли цветок в наличии.</summary>
        public bool HasFlower(PlantData plant)
        {
            if (plant == null) return false;
            return flowerStock.TryGetValue(plant.Id, out int count) && count > 0;
        }

        /// <summary>Списать цветок при продаже покупателю.</summary>
        public bool TryConsumeFlower(PlantData plant)
        {
            if (!HasFlower(plant))
            {
                return false;
            }

            flowerStock[plant.Id]--;
            UpdateUI();
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Получить количество цветка в наличии.</summary>
        public int GetCount(PlantData plant)
        {
            if (plant == null) return 0;
            return flowerStock.TryGetValue(plant.Id, out int count) ? count : 0;
        }

        public IReadOnlyDictionary<string, int> GetAllStock() => flowerStock;

        public void UpdateUI()
        {
            if (shelfSummaryLabel == null) return;

            var items = new List<string>();
            foreach (var kvp in flowerStock)
            {
                if (kvp.Value > 0)
                {
                    items.Add($"{kvp.Key}: {kvp.Value} шт.");
                }
            }

            if (items.Count == 0)
            {
                shelfSummaryLabel.text = $"<color=#94a3b8>Витрина пуста (0/{maxCapacity} слотов). Соберите цветы в оранжерее!</color>";
            }
            else
            {
                shelfSummaryLabel.text = $"<b>Витрина ({TotalFlowerCount}/{maxCapacity}):</b> {string.Join("  •  ", items)}";
            }
        }
    }
}
