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
    /// Данные одного растения по дизайн-документу Green Shop.
    /// Содержит время роста, стоимость семени, цену продажи и параметры полива.
    /// </summary>
    [CreateAssetMenu(fileName = "Plant_New", menuName = "GreenShop/Растение (PlantData)")]
    public class PlantData : ScriptableObject
    {
        [Header("Основные данные")]
        [SerializeField] private string plantName = "Новое растение";

        [Tooltip("Время роста в секундах.")]
        [SerializeField, Min(0.1f)] private float growTime = 10f;

        [Tooltip("Стоимость покупки семени.")]
        [SerializeField, Min(0)] private int seedPrice = 5;

        [Tooltip("Цена продажи готового цветка покупателю.")]
        [SerializeField, Min(0)] private int sellPrice = 8;

        [Tooltip("Опыт / репутация (XP) за продажу.")]
        [SerializeField, Min(0)] private int xpReward = 1;

        [Tooltip("Через сколько секунд роста требуется полив (0 = не просит воды).")]
        [SerializeField, Min(0f)] private float needsWaterTime = 0f;

        [SerializeField] private PlantRole role = PlantRole.Flower;
        [SerializeField] private Season season = Season.Summer;

        [Header("Отображение")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite grownSprite;
        [SerializeField] private Color plantColor = new Color(0.95f, 0.85f, 0.35f, 1f);

        [TextArea(2, 4)]
        [SerializeField] private string description;

        // Обратная совместимость со старыми полями
        [SerializeField, HideInInspector] private int price = 10;

        public string PlantName => plantName;
        public float GrowTime => growTime;
        public int SeedPrice => seedPrice > 0 ? seedPrice : Mathf.Max(1, price / 2);
        public int SellPrice => sellPrice > 0 ? sellPrice : price;
        public int Price => SellPrice;
        public int XpReward => xpReward;
        public float NeedsWaterTime => needsWaterTime;
        public PlantRole Role => role;
        public Season Season => season;
        public Sprite Icon => icon;
        public Sprite GrownSprite => grownSprite;
        public Color PlantColor => plantColor;
        public string Description => description;

        /// <summary>Уникальный идентификатор — имя ассета.</summary>
        public string Id => name;

        private void OnValidate()
        {
            growTime = Mathf.Max(0.1f, growTime);
            seedPrice = Mathf.Max(0, seedPrice);
            sellPrice = Mathf.Max(0, sellPrice);
            xpReward = Mathf.Max(0, xpReward);

            if (string.IsNullOrWhiteSpace(plantName))
            {
                plantName = name;
            }
        }
    }
}
