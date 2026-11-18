using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Конфигурация сетки и параметров Оранжереи.
    /// Все размеры, количество стартовых клеток и раскладка настраиваются здесь.
    /// </summary>
    [CreateAssetMenu(fileName = "GreenhouseConfig", menuName = "GreenShop/Конфиг Оранжереи (GreenhouseConfig)")]
    public class GreenhouseConfig : ScriptableObject
    {
        [Header("Размер сетки")]
        [Tooltip("Ширина сетки (число колонок).")]
        [Range(1, 10)] public int gridWidth = 5;

        [Tooltip("Высота сетки (число строк).")]
        [Range(1, 10)] public int gridHeight = 5;

        [Header("Стартовое состояние")]
        [Tooltip("Количество открытых клеток при старте новой игры.")]
        public int startingUnlockedCount = 2;

        [Header("Раскладка сетки на экране")]
        [Tooltip("Размер одной клетки в пикселях Canvas.")]
        public Vector2 cellSize = new Vector2(130f, 150f);

        [Tooltip("Отступы между клетками по горизонтали и вертикали.")]
        public Vector2 cellSpacing = new Vector2(12f, 12f);

        [Tooltip("Смещение центра сетки (сдвинуто влево, чтобы справа поместилась полка инструментов).")]
        public Vector2 gridCenterOffset = new Vector2(-180f, 15f);

        [Header("Экономика удобрений")]
        [Tooltip("Базовая доля срезания оставшегося времени роста при удобрении (15%).")]
        [Range(0.05f, 0.9f)] public float defaultFertilizerCutPercent = 0.15f;

        [Tooltip("Максимальное число удобрений на одно растение (по умолчанию 1).")]
        public int maxFertilizerPerPlant = 1;
    }
}
