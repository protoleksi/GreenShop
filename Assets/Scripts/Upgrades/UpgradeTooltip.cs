using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Всплывающий тултип описания улучшения (UpgradeTooltip).
    /// Плашка 263x115 px с треугольным хвостиком-указателем на узел.
    /// Переворачивается вниз, если узел находится у верхнего края экрана.
    /// </summary>
    public class UpgradeTooltip : MonoBehaviour
    {
        [Header("UI Ссылки")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Image bgImage;
        [SerializeField] private RectTransform tailRect;
        [SerializeField] private Image tailImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject priceRow;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TMP_Text statusText;

        [Header("Стили")]
        [SerializeField] private TreeStyle style;

        private UpgradeNodeView currentNode;

        private void Awake()
        {
            if (rootRect == null) rootRect = GetComponent<RectTransform>();
            Hide();
        }

        public void Setup(TreeStyle treeStyle)
        {
            style = treeStyle;
            if (style != null && bgImage != null)
            {
                bgImage.color = style.tooltipBgColor;
                if (tailImage != null) tailImage.color = style.tooltipBgColor;
            }
        }

        public void Show(UpgradeNodeView node)
        {
            if (node == null || node.Data == null) return;
            currentNode = node;

            var data = node.Data;
            bool isPurchased = node.IsPurchased;
            bool isAvailable = node.IsAvailable;
            bool isPlaceholder = data.isPlaceholder;

            // Текст заголовка и описания
            if (titleText != null)
            {
                titleText.text = data.title;
                titleText.color = Color.white;
            }

            if (descriptionText != null)
            {
                descriptionText.text = !string.IsNullOrEmpty(data.description) ? data.description : "";
                descriptionText.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            }

            // Статус / Цена
            if (isPlaceholder)
            {
                if (priceRow != null) priceRow.SetActive(false);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "<color=#94a3b8>Скоро</color>";
                }
            }
            else if (isPurchased)
            {
                if (priceRow != null) priceRow.SetActive(false);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "<color=#4ade80>Куплено</color>";
                }
            }
            else if (isAvailable)
            {
                if (statusText != null) statusText.gameObject.SetActive(false);
                if (priceRow != null)
                {
                    priceRow.SetActive(true);
                    UpdatePriceDisplay(data.cost);
                }
            }
            else
            {
                // Заблокирован
                if (priceRow != null) priceRow.SetActive(false);
                if (statusText != null)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "<color=#94a3b8>Требуются предшествующие улучшения</color>";
                }
            }

            PositionAboveNode(node.RectTransform);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            currentNode = null;
            gameObject.SetActive(false);
        }

        public void RefreshIfCurrent(UpgradeNodeView node)
        {
            if (currentNode == node && gameObject.activeSelf)
            {
                Show(node);
            }
        }

        private void UpdatePriceDisplay(int cost)
        {
            if (priceText == null) return;

            priceText.text = $"{cost}";
            double coins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
            bool canAfford = coins >= cost;

            Color affordableCol = style != null ? style.priceAffordableColor : new Color(0.98f, 0.82f, 0.22f, 1f);
            Color unaffordableCol = style != null ? style.priceUnaffordableColor : new Color(0.95f, 0.30f, 0.30f, 1f);

            priceText.color = canAfford ? affordableCol : unaffordableCol;
        }

        private void PositionAboveNode(RectTransform nodeRt)
        {
            if (rootRect == null || nodeRt == null) return;

            // Координаты центра узла в родительской системе координат тултипа
            Vector2 nodePos = nodeRt.anchoredPosition;
            float nodeHalfH = nodeRt.rect.height * 0.5f;

            // Проверяем, близко ли узел к верхнему краю экрана
            // В нашей системе координат anchorMin=(0,1), поэтому Y отрицательный (0 вверху, -1030 внизу)
            bool isNearTop = nodePos.y > -220f;

            float tooltipW = rootRect.rect.width > 0 ? rootRect.rect.width : 263f;
            float halfW = tooltipW * 0.5f;
            float clampedX = Mathf.Clamp(nodePos.x, halfW + 15f, 1920f - halfW - 15f);
            float tailOffset = nodePos.x - clampedX;

            if (isNearTop)
            {
                // Размещаем тултип снизу от узла, хвостик смотрит вверх
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.anchoredPosition = new Vector2(clampedX, nodePos.y - nodeHalfH - 12f);

                if (tailRect != null)
                {
                    tailRect.anchorMin = new Vector2(0.5f, 1f);
                    tailRect.anchorMax = new Vector2(0.5f, 1f);
                    tailRect.pivot = new Vector2(0.5f, 0f);
                    tailRect.anchoredPosition = new Vector2(tailOffset, 0f);
                    tailRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
                }
            }
            else
            {
                // Размещаем тултип сверху от узла, хвостик смотрит вниз
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.anchoredPosition = new Vector2(clampedX, nodePos.y + nodeHalfH + 12f);

                if (tailRect != null)
                {
                    tailRect.anchorMin = new Vector2(0.5f, 0f);
                    tailRect.anchorMax = new Vector2(0.5f, 0f);
                    tailRect.pivot = new Vector2(0.5f, 1f);
                    tailRect.anchoredPosition = new Vector2(tailOffset, 0f);
                    tailRect.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
