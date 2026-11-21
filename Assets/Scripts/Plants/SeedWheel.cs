using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Колесо выбора семян (SeedWheel).
    /// Круг диаметром ~318 px из равных секторов (по умолчанию 3 по 120°).
    /// Сектор 1 (справа): Маргаритка (открыта на старте).
    /// Сектор 2 (слева-сверху): Лаванда (открывается в Дереве улучшений).
    /// Сектор 3 (снизу): Подсолнух (открывается в Дереве улучшений).
    /// Динамически адаптируется под количество сортов (360 / count).
    /// </summary>
    public class SeedWheel : MonoBehaviour
    {
        [Header("Геометрия")]
        public float Diameter => 318f;

        public static SeedWheel Instance { get; private set; }

        [Header("Секторы")]
        [SerializeField] private List<SeedWheelSector> sectors = new List<SeedWheelSector>();

        public IReadOnlyList<SeedWheelSector> Sectors => sectors;

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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CoinsChanged += _ => UpdatePrices();
            }
            UpdatePrices();
        }

        public void RegisterSector(SeedWheelSector sector)
        {
            if (!sectors.Contains(sector))
            {
                sectors.Add(sector);
            }
        }

        public void ClearSectors()
        {
            sectors.Clear();
        }

        public void UpdatePrices()
        {
            foreach (var s in sectors)
            {
                if (s != null)
                {
                    s.UpdatePriceColor();
                }
            }
        }

        public void UnlockPlant(PlantData plant)
        {
            if (plant == null) return;
            foreach (var s in sectors)
            {
                if (s != null && s.Plant == plant)
                {
                    s.SetUnlocked(true);
                }
            }
        }

        public bool IsPlantUnlocked(PlantData plant)
        {
            if (plant == null) return false;
            foreach (var s in sectors)
            {
                if (s != null && s.Plant == plant)
                {
                    return s.IsUnlocked;
                }
            }
            return false;
        }
    }
}
