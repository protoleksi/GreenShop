using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GreenShop.EditorTools
{
    /// <summary>
    /// Быстрое создание 9 растений-заглушек и общей базы.
    /// Значения ниже — заглушки. Их нужно заменить реальными данными из таблицы ГД.
    /// </summary>
    public static class PlantAssetGenerator
    {
        private const string PlantsFolder = "Assets/Data/Plants";
        private const string DatabasePath = "Assets/Data/PlantDatabase.asset";

        private sealed class PlantSeed
        {
            public readonly string Name;
            public readonly float GrowTime;
            public readonly int Price;
            public readonly PlantRole Role;
            public readonly Season Season;

            public PlantSeed(string name, float growTime, int price, PlantRole role, Season season)
            {
                Name = name;
                GrowTime = growTime;
                Price = price;
                Role = role;
                Season = season;
            }
        }

        // 9 растений-заглушек. GrowTime в секундах.
        private static readonly PlantSeed[] Seeds =
        {
            new PlantSeed("Редис", 30f, 5, PlantRole.Crop, Season.Spring),
            new PlantSeed("Морковь", 60f, 10, PlantRole.Crop, Season.Spring),
            new PlantSeed("Клубника", 120f, 18, PlantRole.Crop, Season.Summer),
            new PlantSeed("Огурец", 150f, 22, PlantRole.Crop, Season.Summer),
            new PlantSeed("Томат", 180f, 28, PlantRole.Crop, Season.Summer),
            new PlantSeed("Подсолнух", 240f, 35, PlantRole.Flower, Season.Summer),
            new PlantSeed("Тыква", 300f, 45, PlantRole.Crop, Season.Autumn),
            new PlantSeed("Яблоня", 600f, 90, PlantRole.Tree, Season.Autumn),
            new PlantSeed("Лаванда", 420f, 70, PlantRole.Flower, Season.AllSeasons)
        };

        [MenuItem("Tools/GreenShop/Растения/Создать 9 растений (заглушки)")]
        public static void GeneratePlants()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(PlantsFolder);

            var plants = new List<PlantData>();
            int created = 0;
            int skipped = 0;

            for (int i = 0; i < Seeds.Length; i++)
            {
                string path = $"{PlantsFolder}/Plant_{i + 1:00}.asset";
                PlantData plant = AssetDatabase.LoadAssetAtPath<PlantData>(path);

                if (plant == null)
                {
                    plant = ScriptableObject.CreateInstance<PlantData>();
                    AssetDatabase.CreateAsset(plant, path);

                    PlantSeed seed = Seeds[i];
                    var serializedPlant = new SerializedObject(plant);
                    serializedPlant.FindProperty("plantName").stringValue = seed.Name;
                    serializedPlant.FindProperty("growTime").floatValue = seed.GrowTime;
                    serializedPlant.FindProperty("price").intValue = seed.Price;
                    serializedPlant.FindProperty("role").enumValueIndex = (int)seed.Role;
                    serializedPlant.FindProperty("season").enumValueIndex = (int)seed.Season;
                    serializedPlant.FindProperty("description").stringValue = "Заглушка: заменить данными из таблицы ГД.";
                    serializedPlant.ApplyModifiedPropertiesWithoutUndo();

                    EditorUtility.SetDirty(plant);
                    created++;
                }
                else
                {
                    skipped++;
                }

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

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            Debug.Log($"[GreenShop] Растения: создано {created}, уже было {skipped}. База: {DatabasePath}");

            EditorUtility.DisplayDialog(
                "GreenShop",
                $"Создано растений: {created} из {Seeds.Length}.\nУже существовало: {skipped}.\n\n" +
                $"Папка: {PlantsFolder}\nБаза: {DatabasePath}\n\n" +
                "Существующие ассеты не перезаписывались. Впиши реальные данные из таблицы ГД в инспекторе.",
                "OK");
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
