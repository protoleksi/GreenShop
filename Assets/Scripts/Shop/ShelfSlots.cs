using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Витрина на стеллаже Торгового зала (Этап 3).
    /// Управляет 12 позициями на полках (4 ряда × 3 колонки).
    /// Количество доступных слотов определяется вместимостью ShopInventory.MaxCapacity (по умолчанию 6).
    /// Слоты заполняются строго по порядку (ряд за рядом, слева направо) на основе реального склада.
    /// </summary>
    public class ShelfSlots : MonoBehaviour
    {
        [Header("База растений для сопоставления")]
        [SerializeField] private PlantDatabase plantDatabase;

        [Header("Спрайт-заглушка для цветка")]
        [SerializeField] private Sprite defaultFlowerSprite;

        [Header("Точки слотов (12 позиций)")]
        [SerializeField] private Transform[] slotRoots = new Transform[12];
        [SerializeField] private SpriteRenderer[] flowerRenderers = new SpriteRenderer[12];
        [SerializeField] private UnityEngine.UI.Image[] flowerImages = new UnityEngine.UI.Image[12];

        // 4 ряда × 3 колонки опорных мировых координат по основанию горшка:
        // Col X: 204, 443, 672 px -> -7.56, -5.17, -2.88
        // Row Y: 367, 600, 823, 1048 px -> 1.73, -0.60, -2.83, -5.08
        public static readonly Vector3[] DefaultSlotPositions = new Vector3[]
        {
            new Vector3(-7.56f,  1.73f, 0f), // Ряд 0, Кол 0
            new Vector3(-5.17f,  1.73f, 0f), // Ряд 0, Кол 1
            new Vector3(-2.88f,  1.73f, 0f), // Ряд 0, Кол 2

            new Vector3(-7.56f, -0.60f, 0f), // Ряд 1, Кол 0
            new Vector3(-5.17f, -0.60f, 0f), // Ряд 1, Кол 1
            new Vector3(-2.88f, -0.60f, 0f), // Ряд 1, Кол 2

            new Vector3(-7.56f, -2.83f, 0f), // Ряд 2, Кол 0
            new Vector3(-5.17f, -2.83f, 0f), // Ряд 2, Кол 1
            new Vector3(-2.88f, -2.83f, 0f), // Ряд 2, Кол 2

            new Vector3(-7.56f, -5.08f, 0f), // Ряд 3, Кол 0
            new Vector3(-5.17f, -5.08f, 0f), // Ряд 3, Кол 1
            new Vector3(-2.88f, -5.08f, 0f), // Ряд 3, Кол 2
        };

        private void Start()
        {
            if (ShopInventory.Instance != null)
            {
                ShopInventory.Instance.InventoryChanged += RefreshSlots;
            }

            RefreshSlots();
        }

        private void OnDestroy()
        {
            if (ShopInventory.Instance != null)
            {
                ShopInventory.Instance.InventoryChanged -= RefreshSlots;
            }
        }

        /// <summary>
        /// Перерисовывает витрину на основе актуального содержимого ShopInventory.
        /// </summary>
        public void RefreshSlots()
        {
            if (ShopInventory.Instance == null) return;

            int capacity = ShopInventory.Instance.MaxCapacity;
            var stock = ShopInventory.Instance.GetAllStock();

            // Собираем развёрнутый список всех цветков на витрине
            List<PlantData> flowersOnShelf = new List<PlantData>();
            foreach (var kvp in stock)
            {
                int count = kvp.Value;
                if (count <= 0) continue;

                PlantData plant = FindPlantById(kvp.Key);
                if (plant != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        flowersOnShelf.Add(plant);
                    }
                }
            }

            // Обновляем отображение 12 позиций
            int totalSlots = Mathf.Max(flowerRenderers != null ? flowerRenderers.Length : 0, flowerImages != null ? flowerImages.Length : 0);
            totalSlots = Mathf.Min(12, totalSlots);

            for (int i = 0; i < totalSlots; i++)
            {
                var renderer = (flowerRenderers != null && i < flowerRenderers.Length) ? flowerRenderers[i] : null;
                var img = (flowerImages != null && i < flowerImages.Length) ? flowerImages[i] : null;

                if (renderer == null && img == null) continue;

                if (i < flowersOnShelf.Count && i < capacity)
                {
                    // Занятый слот
                    PlantData plant = flowersOnShelf[i];
                    Sprite sprite = plant.GrownSprite != null ? plant.GrownSprite : plant.Icon;
                    if (sprite == null) sprite = defaultFlowerSprite;
                    Color col = (sprite == defaultFlowerSprite || sprite == null) ? plant.PlantColor : Color.white;

                    if (renderer != null)
                    {
                        renderer.gameObject.SetActive(true);
                        renderer.sprite = sprite;
                        renderer.color = col;
                    }
                    if (img != null)
                    {
                        img.gameObject.SetActive(true);
                        img.sprite = sprite;
                        img.color = col;
                    }
                }
                else
                {
                    // Пустой слот (или превышающий вместимость)
                    if (renderer != null) renderer.gameObject.SetActive(false);
                    if (img != null) img.gameObject.SetActive(false);
                }
            }
        }

        private PlantData FindPlantById(string id)
        {
            if (plantDatabase != null)
            {
                return plantDatabase.GetById(id);
            }
            return null;
        }

        public void SetupSlots(Transform[] roots, SpriteRenderer[] renderers, Image[] images, PlantDatabase db, Sprite flowerSprite)
        {
            slotRoots = roots;
            flowerRenderers = renderers;
            flowerImages = images;
            plantDatabase = db;
            defaultFlowerSprite = flowerSprite;
        }
    }
}
