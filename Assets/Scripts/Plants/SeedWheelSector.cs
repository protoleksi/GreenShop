using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Сектор колеса семян (SeedWheelSector).
    /// Реализует ICanvasRaycastFilter для идеального попадания клика строго в сектор круга (угол + радиус).
    /// Поддерживает клик для выбора и Drag-and-Drop для быстрой посадки.
    /// </summary>
    public class SeedWheelSector : MonoBehaviour, ICanvasRaycastFilter, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Данные сорта")]
        [SerializeField] private PlantData plant;
        [SerializeField] private bool isUnlocked = true;

        [Header("Геометрия сектора (градусы от 12 часов по часовой стрелке)")]
        [SerializeField] private float startAngle;
        [SerializeField] private float endAngle;
        [SerializeField] private float minRadius = 15f;
        [SerializeField] private float maxRadius = 158f;

        [Header("UI элементы")]
        [SerializeField] private RectTransform wheelRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private GameObject lockOverlay;

        [Header("Цвета")]
        [SerializeField] private Color normalFillColor = new Color(0.12f, 0.16f, 0.24f, 0.95f);
        [SerializeField] private Color lockedFillColor = new Color(0.08f, 0.10f, 0.15f, 0.85f);
        [SerializeField] private Color normalPriceColor = new Color(0.98f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color insufficientPriceColor = new Color(0.95f, 0.30f, 0.30f, 1f);

        public PlantData Plant => plant;
        public bool IsUnlocked => isUnlocked;
        public ItemInHand Item => plant != null ? ItemInHand.CreateSeed(plant) : ItemInHand.None;

        public void Setup(PlantData plantData, bool unlocked, float startDeg, float endDeg, RectTransform parentWheelRect)
        {
            plant = plantData;
            isUnlocked = unlocked;
            startAngle = startDeg;
            endAngle = endDeg;
            wheelRect = parentWheelRect;

            UpdateVisuals();
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (fillImage != null)
            {
                fillImage.color = isUnlocked ? normalFillColor : lockedFillColor;
            }

            if (plant != null)
            {
                if (nameLabel != null)
                {
                    nameLabel.text = plant.PlantName;
                }

                if (iconImage != null)
                {
                    if (plant.PlantIcon != null)
                    {
                        iconImage.sprite = plant.PlantIcon;
                        iconImage.color = isUnlocked ? Color.white : Color.gray;
                    }
                    else
                    {
                        iconImage.color = isUnlocked ? plant.PlantColor : Color.gray;
                    }
                }

                UpdatePriceColor();
            }
        }

        public void UpdatePriceColor()
        {
            if (priceLabel == null || plant == null) return;

            if (!isUnlocked)
            {
                priceLabel.text = "Закрыто";
                priceLabel.color = Color.gray;
                return;
            }

            priceLabel.text = $"{plant.SeedPrice}";

            double currentCoins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
            bool canAfford = currentCoins >= plant.SeedPrice;
            priceLabel.color = canAfford ? normalPriceColor : insufficientPriceColor;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            var rt = transform as RectTransform;
            if (rt == null) return true;

            if (eventCamera == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvas.worldCamera != null)
                {
                    eventCamera = canvas.worldCamera;
                }
                else
                {
                    eventCamera = Camera.main;
                }
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, sp, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            float dist = localPoint.magnitude;
            if (dist < minRadius || dist > maxRadius)
            {
                return false;
            }

            // Угол от 12 часов по часовой стрелке: (0, 1) -> 0°, (1, 0) -> 90°, (0, -1) -> 180°, (-1, 0) -> 270°
            float angle = Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            float normStart = (startAngle % 360f + 360f) % 360f;
            float normEnd = (endAngle % 360f + 360f) % 360f;

            if (normStart < normEnd)
            {
                return angle >= normStart && angle <= normEnd;
            }
            else
            {
                // Сектор пересекает 0° / 360°
                return angle >= normStart || angle <= normEnd;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!isUnlocked)
            {
                InteractionController.Instance?.ShowFloatingFeedback("Сорт не изучен! Откройте в Дереве улучшений.");
                return;
            }

            if (plant == null) return;

            InteractionController.Instance?.ToggleOrPickItem(Item);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!isUnlocked)
            {
                InteractionController.Instance?.ShowFloatingFeedback("Сорт не изучен! Откройте в Дереве улучшений.");
                return;
            }

            if (plant == null) return;

            InteractionController.Instance?.StartDragItem(Item);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Позиция отслеживается в InteractionController
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isUnlocked) return;
            InteractionController.Instance?.EndDragItem();
        }
    }
}
