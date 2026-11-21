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

        [Header("Раскладка сетки по вайрфрейму (1920x1080)")]
        [Tooltip("Размер одной клетки в пикселях Canvas (вайрфрейм: 154x101).")]
        public Vector2 cellSize = new Vector2(154f, 101f);

        [Tooltip("Шаг между центрами клеток по X и Y (вайрфрейм: 262 по X, 186 по Y).")]
        public Vector2 cellStep = new Vector2(262f, 186f);

        [Tooltip("Левый верхний угол первой клетки (0, 0) от левого верхнего угла экрана (109, 125).")]
        public Vector2 firstCellTopLeft = new Vector2(109f, 125f);

        [Tooltip("Размер рамки вокруг всей сетки 5х5 (38..1402, 111..1034 -> 1364x923).")]
        public Rect gridFrameRect = new Rect(38f, 111f, 1364f, 923f);

        [Tooltip("Размер полоски времени под клеткой (154x24).")]
        public Vector2 timeBarSize = new Vector2(154f, 24f);

        [Tooltip("Зазор между клеткой и полоской времени (5 px).")]
        public float timeBarGap = 5f;

        [Header("Устаревшие параметры (для обратной совместимости)")]
        [Tooltip("Отступы между клетками по горизонтали и вертикали.")]
        public Vector2 cellSpacing = new Vector2(108f, 85f);

        [Tooltip("Смещение центра сетки.")]
        public Vector2 gridCenterOffset = Vector2.zero;

        [Header("Экономика удобрений")]
        [Tooltip("Базовая доля срезания оставшегося времени роста при удобрении (15%).")]
        [Range(0.05f, 0.9f)] public float defaultFertilizerCutPercent = 0.15f;

        [Tooltip("Максимальное число удобрений на одно растение (по умолчанию 1).")]
        public int maxFertilizerPerPlant = 1;
    }
}
