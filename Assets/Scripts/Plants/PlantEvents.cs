using System;

namespace GreenShop
{
    /// <summary>
    /// Глобальная шина событий жизненного цикла растений.
    /// Позволяет любой системе (квесты, статистика, эффекты, звук) реагировать на действия без жестких связей.
    /// </summary>
    public static class PlantEvents
    {
        public static event Action<GridCell, PlantData> Planted;
        public static event Action<GridCell, PlantData> Watered;
        public static event Action<GridCell, PlantData> Fertilized;
        public static event Action<GridCell, PlantData> Harvested;
        public static event Action<PlantData, int, int> Sold; // растение, полученные монеты, полученный XP

        public static void TriggerPlanted(GridCell cell, PlantData plant) => Planted?.Invoke(cell, plant);
        public static void TriggerWatered(GridCell cell, PlantData plant) => Watered?.Invoke(cell, plant);
        public static void TriggerFertilized(GridCell cell, PlantData plant) => Fertilized?.Invoke(cell, plant);
        public static void TriggerHarvested(GridCell cell, PlantData plant) => Harvested?.Invoke(cell, plant);
        public static void TriggerSold(PlantData plant, int coins, int xp) => Sold?.Invoke(plant, coins, xp);
    }
}
