using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Линия связи между двумя узлами Дерева улучшений.
    /// Автоматически ориентируется между центрами узлов и меняет цвет (яркий / приглушённый зелёный).
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public class UpgradeConnectionLine : MonoBehaviour
    {
        [SerializeField] private Image lineImage;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform fromNode;
        [SerializeField] private RectTransform toNode;
        [SerializeField] private UpgradeNodeView parentNode;
        [SerializeField] private UpgradeNodeView childNode;
        [SerializeField] private TreeStyle style;
        [SerializeField] private bool isHighlighted;

        private void Awake()
        {
            if (lineImage == null) lineImage = GetComponent<Image>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (lineImage != null) lineImage.raycastTarget = false;
            UpdateGeometry();
            UpdateVisuals();
        }

        private void Start()
        {
            UpdateGeometry();
            UpdateVisuals();
        }

        public void Setup(UpgradeNodeView parent, UpgradeNodeView child, TreeStyle treeStyle)
        {
            parentNode = parent;
            childNode = child;
            fromNode = parent != null ? parent.RectTransform : null;
            toNode = child != null ? child.RectTransform : null;
            style = treeStyle;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (lineImage == null) lineImage = GetComponent<Image>();
            if (lineImage != null) lineImage.raycastTarget = false;

            UpdateGeometry();
            UpdateVisuals();
        }

        public void Setup(RectTransform from, RectTransform to, TreeStyle treeStyle, bool highlighted = false)
        {
            fromNode = from;
            toNode = to;
            style = treeStyle;
            isHighlighted = highlighted;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (lineImage == null) lineImage = GetComponent<Image>();
            if (lineImage != null) lineImage.raycastTarget = false;

            UpdateGeometry();
            UpdateVisuals();
        }

        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
            UpdateVisuals();
        }

        public void UpdateGeometry()
        {
            if (fromNode == null && parentNode != null) fromNode = parentNode.RectTransform;
            if (toNode == null && childNode != null) toNode = childNode.RectTransform;
            if (fromNode == null || toNode == null) return;
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            Vector2 a = fromNode.anchoredPosition;
            Vector2 b = toNode.anchoredPosition;
            Vector2 diff = b - a;
            float dist = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = fromNode.anchorMin;
            rectTransform.anchorMax = fromNode.anchorMax;
            rectTransform.anchoredPosition = a + diff * 0.5f;

            float width = style != null ? style.connectionLineWidth : 3f;
            rectTransform.sizeDelta = new Vector2(dist, width);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void UpdateVisuals()
        {
            if (lineImage == null) lineImage = GetComponent<Image>();
            if (lineImage == null) return;

            bool highlighted = (parentNode != null && parentNode.IsPurchased) && (childNode != null && childNode.IsPurchased);
            isHighlighted = highlighted;

            if (style != null)
            {
                lineImage.color = isHighlighted ? style.connectionLinePurchased : style.connectionLineNormal;
            }
            else
            {
                lineImage.color = isHighlighted ? new Color(0.29f, 0.87f, 0.50f, 1f) : new Color(0.29f, 0.42f, 0.31f, 0.95f);
            }
        }
    }
}
