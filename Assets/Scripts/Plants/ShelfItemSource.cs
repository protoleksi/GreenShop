using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Источник инструмента или семян на полке в Оранжерее.
    /// Поддерживает 2 режима взятия: клик для взятия в руку ИЛИ зажатие и перетаскивание (drag-and-drop).
    /// </summary>
    public class ShelfItemSource : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Тип предмета")]
        [SerializeField] private HandItemType itemType;
        [SerializeField] private PlantData seedPlant;
        [SerializeField] private bool isUnlocked = true;

        [Header("UI Ссылки")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text priceBadgeText;
        [SerializeField] private GameObject lockOverlay;

        public HandItemType ItemType => itemType;
        public PlantData SeedPlant => seedPlant;
        public bool IsUnlocked => isUnlocked;

        public ItemInHand Item => itemType == HandItemType.Seed
            ? ItemInHand.CreateSeed(seedPlant)
            : (itemType == HandItemType.WateringCan ? ItemInHand.CreateWateringCan() : ItemInHand.CreateFertilizer());

        public void Setup(HandItemType type, PlantData plant = null, bool unlocked = true)
        {
            itemType = type;
            seedPlant = plant;
            isUnlocked = unlocked;
            UpdateVisuals();
        }

        public void Unlock()
        {
            isUnlocked = true;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (labelText != null)
            {
                switch (itemType)
                {
                    case HandItemType.Seed:
                        labelText.text = seedPlant != null ? $"Семя:\n{seedPlant.PlantName}" : "Семена";
                        if (priceBadgeText != null) priceBadgeText.text = seedPlant != null ? $"{seedPlant.SeedPrice} монет" : "";
                        if (iconImage != null && seedPlant != null) iconImage.color = isUnlocked ? seedPlant.PlantColor : Color.gray;
                        break;

                    case HandItemType.WateringCan:
                        labelText.text = "Лейка\n(полив)";
                        if (priceBadgeText != null) priceBadgeText.text = "Бесплатно";
                        if (iconImage != null) iconImage.color = new Color(0.35f, 0.75f, 0.95f);
                        break;

                    case HandItemType.Fertilizer:
                        labelText.text = "Удобрение\n(+15%)";
                        if (priceBadgeText != null) priceBadgeText.text = "Платное";
                        if (iconImage != null) iconImage.color = new Color(0.95f, 0.75f, 0.25f);
                        break;
                }
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

            InteractionController.Instance?.StartDragItem(Item);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Позиция курсора обновляется в InteractionController.Update()
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isUnlocked) return;
            InteractionController.Instance?.EndDragItem();
        }
    }
}
