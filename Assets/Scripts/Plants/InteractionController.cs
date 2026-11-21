using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GreenShop
{
    public enum HandItemType
    {
        None = 0,
        Seed = 1,
        WateringCan = 2,
        Fertilizer = 3
    }

    [Serializable]
    public struct ItemInHand
    {
        public HandItemType Type;
        public PlantData Plant;

        public static ItemInHand None => new ItemInHand { Type = HandItemType.None, Plant = null };
        public static ItemInHand CreateSeed(PlantData p) => new ItemInHand { Type = HandItemType.Seed, Plant = p };
        public static ItemInHand CreateWateringCan() => new ItemInHand { Type = HandItemType.WateringCan, Plant = null };
        public static ItemInHand CreateFertilizer() => new ItemInHand { Type = HandItemType.Fertilizer, Plant = null };

        public bool IsNone => Type == HandItemType.None;
    }

    /// <summary>
    /// Контроллер взаимодействия «предмет в руке» (семя, лейка, удобрение).
    /// Поддерживает два способа применения: клик-взять + клик-применить, либо Drag-and-Drop.
    /// Отмена по ПКМ, Esc или клику в пустоту.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }

        [Header("UI Курсор и подсказки")]
        [SerializeField] private RectTransform cursorFollower;
        [SerializeField] private Image cursorIcon;
        [SerializeField] private TMP_Text cursorTooltip;
        [SerializeField] private TMP_Text floatingFeedbackLabel;
        [SerializeField] private Canvas parentCanvas;

        [Header("Спрайты инструментов курсора")]
        [SerializeField] private Sprite fertilizerSprite;
        [SerializeField] private Sprite wateringCanSprite;
        [SerializeField] private Sprite seedSprite;

        [Header("Мини-счётчик монет в оранжерее")]
        [SerializeField] private TMP_Text greenhouseCoinsLabel;

        private ItemInHand currentItem = ItemInHand.None;
        private GridCell hoveredCell = null;
        private bool isDragging = false;
        private Coroutine feedbackRoutine;

        public ItemInHand CurrentItem => currentItem;
        public GridCell HoveredCell => hoveredCell;
        public bool IsDragging => isDragging;

        public event Action<ItemInHand> ItemChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            ClearItem();
            UpdateCoinsLabel();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CoinsChanged += _ => UpdateCoinsLabel();
            }
        }

        private void Update()
        {
            UpdateCursorFollowerPosition();
            HandleInputCancellation();
        }

        private void UpdateCursorFollowerPosition()
        {
            if (cursorFollower == null) return;

            Vector2 mousePos = Vector2.zero;
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }

            if (parentCanvas != null)
            {
                Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : (parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    mousePos,
                    cam,
                    out Vector2 localPos);

                cursorFollower.anchoredPosition = localPos + new Vector2(20f, -20f);
            }
        }

        private void HandleInputCancellation()
        {
            // Отмена по ПКМ или Esc
            bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool escapeKey = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (rightClick || escapeKey)
            {
                if (!currentItem.IsNone)
                {
                    ClearItem();
                    ShowFloatingFeedback("Действие отменено");
                }
            }
        }

        public void PickItem(ItemInHand item)
        {
            currentItem = item;
            isDragging = false;
            UpdateCursorVisuals();
            ItemChanged?.Invoke(currentItem);

            if (hoveredCell != null)
            {
                UpdateCellHighlight(hoveredCell);
            }
        }

        public void ToggleOrPickItem(ItemInHand item)
        {
            if (currentItem.Type == item.Type && currentItem.Plant == item.Plant)
            {
                ClearItem();
            }
            else
            {
                PickItem(item);
            }
        }

        public void StartDragItem(ItemInHand item)
        {
            currentItem = item;
            isDragging = true;
            UpdateCursorVisuals();
            ItemChanged?.Invoke(currentItem);
        }

        public void EndDragItem()
        {
            if (!isDragging) return;

            if (hoveredCell != null)
            {
                ApplyItemToCell(hoveredCell);
            }

            ClearItem();
        }

        public void ClearItem()
        {
            currentItem = ItemInHand.None;
            isDragging = false;

            if (cursorFollower != null)
            {
                cursorFollower.gameObject.SetActive(false);
            }

            if (cursorTooltip != null)
            {
                cursorTooltip.text = string.Empty;
            }

            if (hoveredCell != null)
            {
                hoveredCell.SetHighlight(CellHighlight.None);
            }

            ItemChanged?.Invoke(currentItem);
        }

        public void OnCellPointerEnter(GridCell cell)
        {
            hoveredCell = cell;
            UpdateCellHighlight(cell);
        }

        public void OnCellPointerExit(GridCell cell)
        {
            if (hoveredCell == cell)
            {
                cell.SetHighlight(CellHighlight.None);
                hoveredCell = null;

                if (cursorTooltip != null)
                {
                    cursorTooltip.text = string.Empty;
                }
            }
        }

        public void OnCellDrop(GridCell cell)
        {
            if (!currentItem.IsNone)
            {
                ApplyItemToCell(cell);
                ClearItem();
            }
        }

        public void OnCellClicked(GridCell cell)
        {
            if (cell == null || !cell.IsUnlocked) return;

            // Если в руке ничего нет — обрабатываем сбор созревшего урожая
            if (currentItem.IsNone)
            {
                if (cell.State == PlantState.Ripe)
                {
                    TryHarvestCell(cell);
                }
                else if (cell.State == PlantState.NeedsWater)
                {
                    ShowFloatingFeedback("Возьмите лейку на полке справа!");
                }
                else if (cell.State == PlantState.Empty)
                {
                    ShowFloatingFeedback("Возьмите семя на полке справа!");
                }
                return;
            }

            // Если в руке есть предмет — применяем его
            bool applied = ApplyItemToCell(cell);
            if (applied && !isDragging)
            {
                ClearItem();
            }
        }

        public bool ApplyItemToCell(GridCell cell)
        {
            if (cell == null || currentItem.IsNone) return false;

            bool success = cell.Apply(currentItem);
            if (success)
            {
                UpdateCoinsLabel();
                UpdateCellHighlight(cell);
            }
            return success;
        }

        public bool CanApplyToCell(GridCell cell, ItemInHand item, out string reason)
        {
            if (cell == null)
            {
                reason = "Клетка не найдена";
                return false;
            }
            return cell.CanApply(item, out reason);
        }

        private void TryHarvestCell(GridCell cell)
        {
            if (cell.State != PlantState.Ripe || cell.CurrentPlant == null) return;

            // Проверка наличия места на витрине (ShopInventory)
            if (ShopInventory.Instance != null && !ShopInventory.Instance.CanAcceptFlower())
            {
                ShowFloatingFeedback("Нет места на витрине!");
                return;
            }

            PlantData harvested = cell.Harvest();
            if (harvested != null && ShopInventory.Instance != null)
            {
                ShopInventory.Instance.AddFlower(harvested);
                ShowFloatingFeedback($"Собран цветок «{harvested.PlantName}»!");
            }
        }

        private void UpdateCellHighlight(GridCell cell)
        {
            if (cell == null) return;

            if (currentItem.IsNone)
            {
                cell.SetHighlight(CellHighlight.None);
                if (cursorTooltip != null) cursorTooltip.text = string.Empty;
                return;
            }

            string reason;
            bool canApply = cell.CanApply(currentItem, out reason);
            cell.SetHighlight(canApply ? CellHighlight.Valid : CellHighlight.Invalid);

            if (cursorTooltip != null)
            {
                cursorTooltip.text = canApply ? "<color=#4ade80>Применить</color>" : $"<color=#f87171>{reason}</color>";
            }
        }

        private void UpdateCursorVisuals()
        {
            if (cursorFollower == null) return;

            if (currentItem.IsNone)
            {
                cursorFollower.gameObject.SetActive(false);
                return;
            }

            cursorFollower.gameObject.SetActive(true);

            if (cursorIcon != null)
            {
                switch (currentItem.Type)
                {
                    case HandItemType.Seed:
                        if (currentItem.Plant != null && currentItem.Plant.PlantIcon != null)
                        {
                            cursorIcon.sprite = currentItem.Plant.PlantIcon;
                            cursorIcon.color = Color.white;
                        }
                        else
                        {
                            if (seedSprite != null) cursorIcon.sprite = seedSprite;
                            cursorIcon.color = currentItem.Plant != null ? currentItem.Plant.PlantColor : Color.white;
                        }
                        break;

                    case HandItemType.WateringCan:
                        if (wateringCanSprite != null) cursorIcon.sprite = wateringCanSprite;
                        cursorIcon.color = Color.white;
                        break;

                    case HandItemType.Fertilizer:
                        if (fertilizerSprite != null) cursorIcon.sprite = fertilizerSprite;
                        cursorIcon.color = Color.white;
                        break;
                }
            }
        }

        public void ShowFloatingFeedback(string message)
        {
            if (floatingFeedbackLabel == null) return;

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }

            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message));
        }

        private IEnumerator ShowFeedbackRoutine(string message)
        {
            floatingFeedbackLabel.text = message;
            floatingFeedbackLabel.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.8f);

            floatingFeedbackLabel.gameObject.SetActive(false);
            feedbackRoutine = null;
        }

        public void UpdateCoinsLabel()
        {
            if (greenhouseCoinsLabel != null && GameManager.Instance != null)
            {
                greenhouseCoinsLabel.text = $"{GameManager.Instance.Coins:N0}";
            }
        }
    }
}
