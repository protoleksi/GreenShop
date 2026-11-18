using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Сетка Оранжереи (5х5 по умолчанию).
    /// Управляет размещением клеток, вычислением ортогональных соседей
    /// и вызовом интерфейса эффектов ICellEffect.
    /// </summary>
    public class GreenhouseGrid : MonoBehaviour
    {
        public static GreenhouseGrid Instance { get; private set; }

        [Header("Конфигурация")]
        [SerializeField] private GreenhouseConfig config;

        [Header("Контейнер клеток")]
        [SerializeField] private RectTransform cellsContainer;

        [Header("Префаб / Шаблон клетки")]
        [SerializeField] private GameObject cellPrefab;

        private GridCell[,] cells;
        private readonly List<GridCell> allCellsList = new List<GridCell>();
        private readonly List<ICellEffect> cellEffects = new List<ICellEffect>();

        public GreenhouseConfig Config => config;
        public IReadOnlyList<GridCell> AllCells => allCellsList;
        public int Width => config != null ? config.gridWidth : 5;
        public int Height => config != null ? config.gridHeight : 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeGrid();
        }

        private void OnEnable()
        {
            PlantEvents.Planted += OnCellPlanted;
            PlantEvents.Harvested += OnCellHarvested;
        }

        private void OnDisable()
        {
            PlantEvents.Planted -= OnCellPlanted;
            PlantEvents.Harvested -= OnCellHarvested;
        }

        /// <summary>
        /// Возвращает строго 4 ортогональных соседа (вверх, вниз, влево, вправо) без диагоналей.
        /// </summary>
        public IReadOnlyList<GridCell> GetOrthogonalNeighbors(GridCell cell)
        {
            var neighbors = new List<GridCell>(4);
            if (cell == null) return neighbors;

            if (cells == null)
            {
                InitializeGrid();
            }

            int x = cell.X;
            int y = cell.Y;

            // Слева (x - 1, y)
            if (x > 0 && cells[x - 1, y] != null)
                neighbors.Add(cells[x - 1, y]);

            // Справа (x + 1, y)
            if (x < Width - 1 && cells[x + 1, y] != null)
                neighbors.Add(cells[x + 1, y]);

            // Снизу (x, y - 1)
            if (y > 0 && cells[x, y - 1] != null)
                neighbors.Add(cells[x, y - 1]);

            // Сверху (x, y + 1)
            if (y < Height - 1 && cells[x, y + 1] != null)
                neighbors.Add(cells[x, y + 1]);

            return neighbors;
        }

        public GridCell GetCell(int x, int y)
        {
            if (cells == null)
            {
                InitializeGrid();
            }

            if (cells == null || x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return cells[x, y];
        }

        /// <summary>
        /// Инициализировать сетку из конфига.
        /// </summary>
        public void InitializeGrid(GreenhouseConfig newConfig = null)
        {
            if (newConfig != null) config = newConfig;
            if (config == null) return;

            int w = config.gridWidth;
            int h = config.gridHeight;
            cells = new GridCell[w, h];
            allCellsList.Clear();

            var existingCells = (cellsContainer != null)
                ? cellsContainer.GetComponentsInChildren<GridCell>(true)
                : GetComponentsInChildren<GridCell>(true);

            foreach (var c in existingCells)
            {
                if (c != null && c.X >= 0 && c.X < w && c.Y >= 0 && c.Y < h)
                {
                    cells[c.X, c.Y] = c;
                    allCellsList.Add(c);
                }
            }
        }

        /// <summary>
        /// Разблокировать следующую закрытую клетку по порядку (для дерева улучшений).
        /// </summary>
        public GridCell UnlockNextCell()
        {
            foreach (var cell in allCellsList)
            {
                if (cell != null && !cell.IsUnlocked)
                {
                    cell.Unlock();
                    Debug.Log($"[GreenhouseGrid] Разблокирована клетка ({cell.X}, {cell.Y})!");
                    return cell;
                }
            }
            return null;
        }

        public void RegisterEffect(ICellEffect effect)
        {
            if (effect != null && !cellEffects.Contains(effect))
            {
                cellEffects.Add(effect);
            }
        }

        public void UnregisterEffect(ICellEffect effect)
        {
            if (effect != null)
            {
                cellEffects.Remove(effect);
            }
        }

        private void OnCellPlanted(GridCell cell, PlantData plant)
        {
            if (cell == null || cellEffects.Count == 0) return;
            var neighbors = GetOrthogonalNeighbors(cell);
            foreach (var effect in cellEffects)
            {
                effect?.OnPlanted(cell, neighbors);
            }
        }

        private void OnCellHarvested(GridCell cell, PlantData plant)
        {
            if (cell == null || cellEffects.Count == 0) return;
            var neighbors = GetOrthogonalNeighbors(cell);
            foreach (var effect in cellEffects)
            {
                effect?.OnHarvested(cell, neighbors);
            }
        }
    }
}
