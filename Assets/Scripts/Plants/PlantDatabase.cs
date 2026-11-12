using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Общий список растений для магазина и системы грядок.
    /// Генерируется автоматически вместе с 9 растениями (Tools > GreenShop > Растения).
    /// </summary>
    [CreateAssetMenu(fileName = "PlantDatabase", menuName = "GreenShop/База растений (PlantDatabase)")]
    public class PlantDatabase : ScriptableObject
    {
        [SerializeField] private List<PlantData> plants = new List<PlantData>();

        public IReadOnlyList<PlantData> Plants => plants;
        public int Count => plants != null ? plants.Count : 0;

        public PlantData GetById(string plantId)
        {
            if (plants == null || string.IsNullOrWhiteSpace(plantId))
            {
                return null;
            }

            foreach (PlantData plant in plants)
            {
                if (plant != null && plant.Id == plantId)
                {
                    return plant;
                }
            }

            return null;
        }

        public PlantData GetByIndex(int index)
        {
            if (plants == null || index < 0 || index >= plants.Count)
            {
                return null;
            }

            return plants[index];
        }
    }
}
