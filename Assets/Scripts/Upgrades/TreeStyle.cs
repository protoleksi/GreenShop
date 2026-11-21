using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Стили оформления Дерева улучшений (TreeStyle).
    /// Вынесены в ScriptableObject, чтобы художник мог настраивать палитру и размеры без правки кода.
    /// </summary>
    [CreateAssetMenu(fileName = "TreeStyle", menuName = "GreenShop/TreeStyle")]
    public class TreeStyle : ScriptableObject
    {
        [Header("Цвета регионов фона")]
        public Color growthBgColor = new Color(0.243f, 0.482f, 0.263f, 1f); // #3E7B43 (зеленый регион РОСТ)
        public Color neutralBgColor = new Color(0.741f, 0.741f, 0.741f, 1f); // #BDBDBD (серый регион УЮТ и ТОРГОВЛЯ)
        public Color dividingLineColor = new Color(0.33f, 0.33f, 0.33f, 0.75f); // #555555 (диагональные разделители)

        [Header("Цвета узлов")]
        public Color lockedNodeBgColor = new Color(0.46f, 0.46f, 0.46f, 1f); // #757575
        public Color lockedNodeBorderColor = new Color(0.29f, 0.29f, 0.29f, 1f); // #4A4A4A
        public Color availableNodeBorderColor = new Color(0.29f, 0.87f, 0.50f, 1f); // #4ADE80
        public Color purchasedNodeBgColor = new Color(0.22f, 0.56f, 0.24f, 1f); // #388E3C

        [Header("Цвета линий связей")]
        public Color connectionLineNormal = new Color(0.29f, 0.42f, 0.31f, 0.95f); // приглушённый зелёный
        public Color connectionLinePurchased = new Color(0.29f, 0.87f, 0.50f, 1f); // яркий зелёный

        [Header("Геометрия")]
        public float normalNodeDiameter = 85f;
        public float nodeDiameter => normalNodeDiameter;
        public float rootNodeDiameter = 100f;
        public float connectionLineWidth = 3f;

        [Header("Тултип")]
        public Color tooltipBgColor = new Color(0.15f, 0.165f, 0.19f, 1f); // #262A30 - плотный непрозрачный темно-серый фон
        public Color tooltipBorderColor = new Color(0.38f, 0.42f, 0.48f, 0.85f); // #616B7A - аккуратный контур
        public Color priceAffordableColor = new Color(0.98f, 0.82f, 0.22f, 1f);
        public Color priceUnaffordableColor = new Color(0.95f, 0.30f, 0.30f, 1f);
    }
}
