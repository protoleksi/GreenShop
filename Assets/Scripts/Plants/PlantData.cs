using UnityEngine;

namespace GreenShop
{
    /// <summary>Роль растения в экономике магазина.</summary>
    public enum PlantRole
    {
        [InspectorName("Культура")] Crop = 0,
        [InspectorName("Цветок")] Flower = 1,
        [InspectorName("Дерево")] Tree = 2,
        [InspectorName("Особое")] Special = 3
    }

    /// <summary>Сезон, в который растение доступно/растёт.</summary>
    public enum Season
    {
        [InspectorName("Весна")] Spring = 0,
        [InspectorName("Лето")] Summer = 1,
        [InspectorName("Осень")] Autumn = 2,
        [InspectorName("Зима")] Winter = 3,
        [InspectorName("Все сезоны")] AllSeasons = 4
    }

    /// <summary>
    /// Данные одного растения. Создаётся через Assets > Create > GreenShop > Растение (PlantData)
    /// или автоматически через Tools > GreenShop > Растения > Создать 9 растений.
    /// </summary>
    [CreateAssetMenu(fileName = "Plant_New", menuName = "GreenShop/Растение (PlantData)")]
    public class PlantData : ScriptableObject
    {
        [Header("Основные данные")]
        [SerializeField] private string plantName = "Новое растение";

        [Tooltip("Время роста в секундах.")]
        [SerializeField, Min(0f)] private float growTime = 60f;

        [Tooltip("Цена покупки/продажи из таблицы ГД.")]
        [SerializeField, Min(0)] private int price = 10;

        [SerializeField] private PlantRole role = PlantRole.Crop;
        [SerializeField] private Season season = Season.AllSeasons;

        [Header("Отображение")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite grownSprite;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        public string PlantName => plantName;
        public float GrowTime => growTime;
        public int Price => price;
        public PlantRole Role => role;
        public Season Season => season;
        public Sprite Icon => icon;
        public Sprite GrownSprite => grownSprite;
        public string Description => description;

        /// <summary>Уникальный идентификатор — имя ассета.</summary>
        public string Id => name;

        private void OnValidate()
        {
            growTime = Mathf.Max(0f, growTime);
            price = Mathf.Max(0, price);

            if (string.IsNullOrWhiteSpace(plantName))
            {
                plantName = name;
            }
        }
    }
}
