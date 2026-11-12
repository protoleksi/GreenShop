using System;
using System.Collections.Generic;

namespace GreenShop
{
    /// <summary>
    /// Корневая структура сохранения. Хранится в JSON-файле save.json.
    /// При добавлении новых полей увеличивай CurrentVersion и дописывай миграцию в SaveSystem.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public double coins;
        public double incomePerSecond;
        public long totalClicks;
        public double totalPlayTimeSeconds;
        public string lastSaveUtc = string.Empty;

        public List<PlantSaveData> plants = new List<PlantSaveData>();
        public List<string> unlockedZones = new List<string>();
    }

    /// <summary>Состояние одного растения на грядке.</summary>
    [Serializable]
    public class PlantSaveData
    {
        public string plantId = string.Empty;
        public int plotIndex = -1;
        public bool isPlanted;
        public double growthSeconds;
    }
}
