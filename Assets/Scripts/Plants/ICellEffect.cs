using System.Collections.Generic;

namespace GreenShop
{
    /// <summary>
    /// Точка расширения для синергий и эффектов соседства клеток.
    /// Вызывается при посадке и сборе урожая, передавая целевую клетку и её ортогональных соседей.
    /// </summary>
    public interface ICellEffect
    {
        void OnPlanted(GridCell cell, IReadOnlyList<GridCell> orthogonalNeighbors);
        void OnHarvested(GridCell cell, IReadOnlyList<GridCell> orthogonalNeighbors);
    }
}
