using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GreenShop.EditorTools
{
    public static class PlantAssetGenerator
    {
        private const string PlantsFolder = "Assets/Data/Plants";
        private const string DatabasePath = "Assets/Data/PlantDatabase.asset";

        public sealed class PlantSeedDef
        {
            public string FileName;
            public string Name;
            public float GrowTime;
            public int SeedPrice;
            public int SellPrice;
            public int FertilizerCost;
            public int XpReward;
            public float NeedsWaterTime;
            public float WaterPatienceTime;
            public Color Color;
            public Season Season;
            public string Description;

            public PlantSeedDef(string fileName, string name, float growTime, int seedPrice, int sellPrice, int fertilizerCost, int xpReward, float needsWaterTime, float waterPatienceTime, Color color, Season season, string description)
            {
                FileName = fileName;
                Name = name;
                GrowTime = growTime;
                SeedPrice = seedPrice;
                SellPrice = sellPrice;
                FertilizerCost = fertilizerCost;
                XpReward = xpReward;
                NeedsWaterTime = needsWaterTime;
                WaterPatienceTime = waterPatienceTime;
                Color = color;
                Season = season;
                Description = description;
            }
        }

        public static readonly PlantSeedDef[] DesignDocPlants =
        {
            new PlantSeedDef("Plant_01_Daisy", "Маргаритка", 5f, 5, 8, 1, 1, 0f, 0f, new Color(1f, 0.95f, 0.45f), Season.Summer, "Простой и быстрорастущий цветок. Не требует частого полива."),
            new PlantSeedDef("Plant_02_Lavender", "Лаванда", 15f, 15, 25, 3, 2, 5f, 10f, new Color(0.75f, 0.52f, 0.98f), Season.Summer, "Ароматная лаванда. Требует полива на 5-й секунде роста."),
            new PlantSeedDef("Plant_03_Sunflower", "Подсолнух", 45f, 45, 80, 10, 5, 15f, 20f, new Color(0.98f, 0.80f, 0.15f), Season.Summer, "Яркий солнечный цветок. Нуждается в поливе."),
            new PlantSeedDef("Plant_04_Cyclamen", "Цикламен", 60f, 120, 230, 25, 12, 25f, 40f, new Color(0.94f, 0.38f, 0.57f), Season.Summer, "Изящный цикламен с нежными лепестками."),
            new PlantSeedDef("Plant_05_Sansevieria", "Сансевиерия", 120f, 300, 625, 60, 25, 50f, 80f, new Color(0.20f, 0.65f, 0.35f), Season.Autumn, "Щучий хвост: выносливое растение для стильного интерьера."),
            new PlantSeedDef("Plant_06_Begonia", "Бегония", 180f, 500, 1140, 100, 40, 75f, 90f, new Color(0.95f, 0.28f, 0.38f), Season.Autumn, "Пышная бегония, привлекающая состоятельных покупателей."),
            new PlantSeedDef("Plant_07_Poinsettia", "Пуансеттия", 240f, 800, 1800, 160, 55, 100f, 120f, new Color(0.85f, 0.12f, 0.18f), Season.Winter, "Рождественская звезда: символ уюта и праздника."),
            new PlantSeedDef("Plant_08_Zamioculcas", "Замиокулькас", 300f, 1300, 3175, 260, 85, 120f, 150f, new Color(0.12f, 0.45f, 0.22f), Season.Winter, "Долларовое дерево: символ богатства лавки."),
            new PlantSeedDef("Plant_09_Snowberry", "Снежноягодник", 400f, 2000, 5200, 400, 120, 150f, 180f, new Color(0.88f, 0.92f, 0.98f), Season.Winter, "Редкий снежноягодник с жемчужными плодами.")
        };

        [MenuItem("Tools/GreenShop/Растения/Обновить базу растений из диздока")]
        public static void GeneratePlants()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(PlantsFolder);

            var plants = new List<PlantData>();

            for (int i = 0; i < DesignDocPlants.Length; i++)
            {
                var def = DesignDocPlants[i];
                string path = $"{PlantsFolder}/{def.FileName}.asset";
                PlantData plant = AssetDatabase.LoadAssetAtPath<PlantData>(path);

                if (plant == null)
                {
                    plant = ScriptableObject.CreateInstance<PlantData>();
                    AssetDatabase.CreateAsset(plant, path);
                }

                var so = new SerializedObject(plant);
                so.FindProperty("plantName").stringValue = def.Name;
                so.FindProperty("growTime").floatValue = def.GrowTime;
                so.FindProperty("seedPrice").intValue = def.SeedPrice;
                so.FindProperty("sellPrice").intValue = def.SellPrice;
                so.FindProperty("fertilizerCost").intValue = def.FertilizerCost;
                so.FindProperty("xpReward").intValue = def.XpReward;
                so.FindProperty("needsWaterTime").floatValue = def.NeedsWaterTime;
                so.FindProperty("waterPatienceTime").floatValue = def.WaterPatienceTime;
                so.FindProperty("plantColor").colorValue = def.Color;
                so.FindProperty("season").enumValueIndex = (int)def.Season;
                so.FindProperty("description").stringValue = def.Description;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(plant);
                plants.Add(plant);
            }

            PlantDatabase database = AssetDatabase.LoadAssetAtPath<PlantDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<PlantDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var serializedDatabase = new SerializedObject(database);
            SerializedProperty list = serializedDatabase.FindProperty("plants");
            list.arraySize = plants.Count;

            for (int i = 0; i < plants.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = plants[i];
            }

            serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GreenShop] Успешно создано/обновлено {plants.Count} растений из диздока в базе {DatabasePath}.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string normalized = path.Replace('\\', '/');
            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string folderName = Path.GetFileName(normalized);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
