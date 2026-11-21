using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Отрисовка двухцветного фона Дерева улучшений через OnPopulateMesh.
    /// Рисует зелёную трапецию «РОСТ» слева, серый полигон «УЮТ / ТОРГОВЛЯ» справа
    /// и тонкую диагональную линию, разделяющую Уют и Торговлю.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TreeBackgroundGraphic : MaskableGraphic
    {
        [Header("Стили")]
        [SerializeField] private TreeStyle style;

        [Header("Цвета по умолчанию")]
        [SerializeField] private Color growthColor = new Color(0.243f, 0.482f, 0.263f, 1f); // #3E7B43
        [SerializeField] private Color neutralColor = new Color(0.741f, 0.741f, 0.741f, 1f); // #BDBDBD
        [SerializeField] private Color dividerColor = new Color(0.33f, 0.33f, 0.33f, 0.85f); // #555555

        [Header("Пропорции разделения")]
        [Range(0.1f, 0.9f)] [SerializeField] private float growthTopX = 0.33f;
        [Range(0.1f, 0.9f)] [SerializeField] private float growthBottomX = 0.43f;
        [Range(0.1f, 0.9f)] [SerializeField] private float diagonalRootX = 0.45f;
        [Range(0.1f, 1.0f)] [SerializeField] private float diagonalTopX = 0.98f;

        public void SetStyle(TreeStyle newStyle)
        {
            style = newStyle;
            if (style != null)
            {
                growthColor = style.growthBgColor;
                neutralColor = style.neutralBgColor;
                dividerColor = style.dividingLineColor;
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = rectTransform.rect;
            float w = r.width;
            float topY = r.yMax;
            float bottomY = r.yMin;

            if (style != null)
            {
                growthColor = style.growthBgColor;
                neutralColor = style.neutralBgColor;
                dividerColor = style.dividingLineColor;
            }

            // Точки перегиба
            float splitTop = r.xMin + growthTopX * w;
            float splitBottom = r.xMin + growthBottomX * w;
            float diagRoot = r.xMin + diagonalRootX * w;
            float diagTop = r.xMin + diagonalTopX * w;

            // 1. Зелёный полигон РОСТ (трапеция слева)
            int v0 = AddVert(vh, new Vector2(r.xMin, bottomY), growthColor);
            int v1 = AddVert(vh, new Vector2(r.xMin, topY), growthColor);
            int v2 = AddVert(vh, new Vector2(splitTop, topY), growthColor);
            int v3 = AddVert(vh, new Vector2(splitBottom, bottomY), growthColor);
            vh.AddTriangle(v0, v1, v2);
            vh.AddTriangle(v2, v3, v0);

            // 2. Серый полигон УЮТ / ТОРГОВЛЯ (справа)
            int v4 = AddVert(vh, new Vector2(splitBottom, bottomY), neutralColor);
            int v5 = AddVert(vh, new Vector2(splitTop, topY), neutralColor);
            int v6 = AddVert(vh, new Vector2(r.xMax, topY), neutralColor);
            int v7 = AddVert(vh, new Vector2(r.xMax, bottomY), neutralColor);
            vh.AddTriangle(v4, v5, v6);
            vh.AddTriangle(v6, v7, v4);

            // 3. Линия границы РОСТ / НЕЙТРАЛЬ
            AddQuadLine(vh, new Vector2(splitTop, topY), new Vector2(splitBottom, bottomY), 2.5f, dividerColor);

            // 4. Диагональная линия между УЮТ и ТОРГОВЛЯ (от корня к правому верхнему углу)
            AddQuadLine(vh, new Vector2(diagRoot, bottomY), new Vector2(diagTop, topY), 2.0f, dividerColor);
        }

        private int AddVert(VertexHelper vh, Vector2 pos, Color col)
        {
            var vert = UIVertex.simpleVert;
            vert.position = pos;
            vert.color = col;
            vh.AddVert(vert);
            return vh.currentVertCount - 1;
        }

        private void AddQuadLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color col)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 norm = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

            int i0 = AddVert(vh, a - norm, col);
            int i1 = AddVert(vh, a + norm, col);
            int i2 = AddVert(vh, b + norm, col);
            int i3 = AddVert(vh, b - norm, col);

            vh.AddTriangle(i0, i1, i2);
            vh.AddTriangle(i2, i3, i0);
        }
    }
}
