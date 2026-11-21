using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    public enum UpgradeNodeState
    {
        Locked = 0,
        Available = 1,
        Purchased = 2
    }

    /// <summary>
    /// Представление отдельного узла на карте Дерева улучшений (UpgradeNodeView).
    /// Круг диаметром 85 px (корневой 100 px), меняющий цвет и иконку по состоянию:
    /// Locked (серый), Available (зелёный контур, покупка по клику), Purchased (зелёный круг, цветок в горшке).
    /// </summary>
    public class UpgradeNodeView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Данные узла")]
        [SerializeField] private TreeNodeData data;
        [SerializeField] private UpgradeNodeState state = UpgradeNodeState.Locked;

        [Header("UI Ссылки")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image bgImage;
        [SerializeField] private Image ringImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image lockIcon;

        [Header("Спрайты")]
        [SerializeField] private Sprite purchasedIcon;
        [SerializeField] private Sprite grayIcon;

        [Header("Стили")]
        [SerializeField] private TreeStyle style;

        private Coroutine shakeRoutine;
        private Vector2 originalPosition;

        public TreeNodeData Data => data;
        public UpgradeNodeState State => state;
        public bool IsPurchased => state == UpgradeNodeState.Purchased;
        public bool IsAvailable => state == UpgradeNodeState.Available;
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.anchoredPosition;
        }

        public void Setup(TreeNodeData nodeData, UpgradeNodeState nodeState, TreeStyle treeStyle, Sprite flowerPotSprite, Sprite grayFlowerSprite)
        {
            data = nodeData;
            state = nodeState;
            style = treeStyle;
            purchasedIcon = flowerPotSprite;
            grayIcon = grayFlowerSprite;

            UpdateVisuals();
        }

        public void SetState(UpgradeNodeState newState)
        {
            state = newState;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (data == null) return;

            bool isRoot = data.id == "root";
            float diameter = isRoot ? (style != null ? style.rootNodeDiameter : 100f) : (style != null ? style.nodeDiameter : 85f);
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(diameter, diameter);
            }

            // Фоновый круг
            if (bgImage != null)
            {
                if (isRoot)
                {
                    bgImage.color = style != null ? style.lockedNodeBgColor : new Color(0.46f, 0.46f, 0.46f, 1f);
                }
                else if (state == UpgradeNodeState.Purchased)
                {
                    bgImage.color = style != null ? style.purchasedNodeBgColor : new Color(0.22f, 0.56f, 0.24f, 1f);
                }
                else
                {
                    bgImage.color = style != null ? style.lockedNodeBgColor : new Color(0.46f, 0.46f, 0.46f, 1f);
                }
            }

            // Контурное кольцо (Available)
            if (ringImage != null)
            {
                if (state == UpgradeNodeState.Available && !data.isPlaceholder)
                {
                    ringImage.gameObject.SetActive(true);
                    ringImage.color = style != null ? style.availableNodeBorderColor : new Color(0.29f, 0.87f, 0.50f, 1f);
                }
                else
                {
                    ringImage.gameObject.SetActive(false);
                }
            }

            // Иконка цветка в горшке
            if (iconImage != null)
            {
                if (data.customIcon != null)
                {
                    iconImage.sprite = data.customIcon;
                    iconImage.color = (state == UpgradeNodeState.Purchased) ? Color.white : Color.gray;
                    iconImage.gameObject.SetActive(true);
                }
                else if (state == UpgradeNodeState.Purchased)
                {
                    if (purchasedIcon != null) iconImage.sprite = purchasedIcon;
                    iconImage.color = Color.white;
                    iconImage.gameObject.SetActive(!isRoot);
                }
                else
                {
                    if (grayIcon != null) iconImage.sprite = grayIcon;
                    iconImage.color = Color.white;
                    iconImage.gameObject.SetActive(!isRoot && !data.isPlaceholder);
                }
            }

            // Значок замка (только если Locked и не заглушка)
            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(state == UpgradeNodeState.Locked && !isRoot && !data.isPlaceholder);
            }
        }

        public void PlayShakeAnimation()
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                if (rectTransform != null) rectTransform.anchoredPosition = originalPosition;
            }
            shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            float magnitude = 7f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float offset = Mathf.Sin(elapsed * 45f) * magnitude * (1f - elapsed / duration);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = originalPosition + new Vector2(offset, 0f);
                }
                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            shakeRoutine = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (data == null) return;

            if (data.isPlaceholder)
            {
                UpgradeMapView.Instance?.ShowFloatingFeedback("Это улучшение станет доступно в следующих обновлениях!");
                return;
            }

            if (state == UpgradeNodeState.Purchased)
            {
                // Уже куплено
                return;
            }

            if (state == UpgradeNodeState.Available)
            {
                bool success = UpgradeMapView.Instance != null && UpgradeMapView.Instance.TryBuyNode(this);
                if (!success)
                {
                    PlayShakeAnimation();
                }
            }
            else
            {
                // Заблокирован
                UpgradeMapView.Instance?.ShowFloatingFeedback("Сначала откройте предыдущее улучшение в ветке!");
                PlayShakeAnimation();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            UpgradeMapView.Instance?.ShowTooltip(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UpgradeMapView.Instance?.HideTooltip();
        }
    }
}
