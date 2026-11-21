using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    public enum PlantState
    {
        Empty = 0,
        Growing = 1,
        NeedsWater = 2,
        Ripe = 3
    }

    public enum CellHighlight
    {
        None = 0,
        Valid = 1,
        Invalid = 2
    }

    /// <summary>
    /// Модель клетки оранжереи (GridCell).
    /// Реализует CanApply(item), Apply(item), SetHighlight(highlight) и стейт-машину растения:
    /// Empty -> Growing -> NeedsWater -> Growing -> Ripe.
    /// Под клеткой размещается полоска времени TimeBar (154x24 px).
    /// </summary>
    public class GridCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        [Header("Координаты в сетке")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;
        [SerializeField] private bool isUnlocked;

        [Header("UI Ссылки")]
        [SerializeField] private Image cellBackground;
        [SerializeField] private Image highlightBorder;
        [SerializeField] private Image plantVisual;
        [SerializeField] private TimeBar timeBar;
        [SerializeField] private GameObject lockOverlay;

        [Header("Цвета подсветки")]
        [SerializeField] private Color highlightValidColor = new Color(0.2f, 0.85f, 0.4f, 0.85f);
        [SerializeField] private Color highlightInvalidColor = new Color(0.95f, 0.25f, 0.25f, 0.85f);
        [SerializeField] private Color defaultCellColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
        [SerializeField] private Color lockedCellColor = new Color(0.06f, 0.08f, 0.12f, 0.75f);

        private PlantState state = PlantState.Empty;
        private PlantData currentPlant;
        private float growthProgress = 0f;
        private float elapsedGrowthTime = 0f;
        private bool hasBeenWatered = false;
        private int fertilizeCount = 0;
        private float growthSpeedMultiplier = 1f;

        public int X => gridX;
        public int Y => gridY;
        public bool IsUnlocked => isUnlocked;
        public PlantState State => state;
        public PlantData CurrentPlant => currentPlant;
        public float GrowthProgress => growthProgress;
        public int FertilizeCount => fertilizeCount;
        public TimeBar TimeBarComponent => timeBar;

        public event Action<GridCell> StateChanged;
        public event Action<GridCell> CellClicked;

        private void Start()
        {
            if (!isUnlocked)
            {
                state = PlantState.Empty;
            }
            UpdateVisuals();
            SetHighlight(CellHighlight.None);
        }

        private void Update()
        {
            if (!isUnlocked || currentPlant == null) return;

            if (state == PlantState.Growing)
            {
                float totalGrowTime = Mathf.Max(0.1f, currentPlant.GrowTime / growthSpeedMultiplier);

                // Запрос полива
                if (currentPlant.NeedsWaterTime > 0f && !hasBeenWatered && elapsedGrowthTime >= currentPlant.NeedsWaterTime)
                {
                    state = PlantState.NeedsWater;
                    UpdateVisuals();
                    StateChanged?.Invoke(this);
                    return;
                }

                elapsedGrowthTime += Time.deltaTime;
                growthProgress = Mathf.Clamp01(elapsedGrowthTime / totalGrowTime);
                float remainingSec = Mathf.Max(0f, totalGrowTime - elapsedGrowthTime);

                if (timeBar != null)
                {
                    timeBar.SetGrowing(growthProgress, remainingSec);
                }

                if (plantVisual != null)
                {
                    float scale = Mathf.Lerp(0.35f, 1.0f, growthProgress);
                    plantVisual.transform.localScale = new Vector3(scale, scale, 1f);
                }

                if (growthProgress >= 1f)
                {
                    state = PlantState.Ripe;
                    UpdateVisuals();
                    StateChanged?.Invoke(this);
                }
            }
        }

        public void Initialize(int x, int y, bool unlocked)
        {
            gridX = x;
            gridY = y;
            isUnlocked = unlocked;
            state = PlantState.Empty;
            UpdateVisuals();
        }

        public void Unlock()
        {
            if (isUnlocked) return;
            isUnlocked = true;
            state = PlantState.Empty;
            UpdateVisuals();
            StateChanged?.Invoke(this);
        }

        /// <summary>
        /// Проверка, применим ли предмет в руке к данной клетке.
        /// Возвращает false и понятную причину, если действие недопустимо.
        /// </summary>
        public bool CanApply(ItemInHand item, out string reason)
        {
            reason = string.Empty;

            if (!isUnlocked)
            {
                reason = "Клетка заблокирована";
                return false;
            }

            switch (item.Type)
            {
                case HandItemType.Seed:
                    if (state != PlantState.Empty)
                    {
                        reason = "Клетка уже занята";
                        return false;
                    }
                    if (item.Plant == null)
                    {
                        reason = "Семя не выбрано";
                        return false;
                    }
                    if (GameManager.Instance != null && GameManager.Instance.Coins < item.Plant.SeedPrice)
                    {
                        reason = $"Не хватает монет ({item.Plant.SeedPrice} монет)";
                        return false;
                    }
                    return true;

                case HandItemType.WateringCan:
                    if (state != PlantState.NeedsWater)
                    {
                        reason = (state == PlantState.Empty) ? "В клетке ничего не растёт" : "Растению не нужна вода";
                        return false;
                    }
                    return true;

                case HandItemType.Fertilizer:
                    if (state != PlantState.Growing)
                    {
                        reason = (state == PlantState.Ripe) ? "Растение уже созрело" :
                                 (state == PlantState.NeedsWater) ? "Сначала полейте растение" : "Удобрять можно только растущее растение";
                        return false;
                    }
                    if (currentPlant != null)
                    {
                        int fertCost = currentPlant.FertilizerCost;
                        if (GameManager.Instance != null && GameManager.Instance.Coins < fertCost)
                        {
                            reason = $"Не хватает монет на удобрение ({fertCost} монет)";
                            return false;
                        }
                    }
                    int maxFert = GreenhouseGrid.Instance?.Config != null ? GreenhouseGrid.Instance.Config.maxFertilizerPerPlant : 1;
                    if (fertilizeCount >= maxFert)
                    {
                        reason = "Растение уже удобрено";
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Применить предмет из руки к клетке (посадка, полив, удобрение).
        /// </summary>
        public bool Apply(ItemInHand item)
        {
            if (!CanApply(item, out string reason))
            {
                InteractionController.Instance?.ShowFloatingFeedback(reason);
                SetHighlight(CellHighlight.Invalid);
                return false;
            }

            switch (item.Type)
            {
                case HandItemType.Seed:
                    int seedCost = item.Plant.SeedPrice;
                    if (GameManager.Instance != null && !GameManager.Instance.TrySpendCoins(seedCost))
                    {
                        InteractionController.Instance?.ShowFloatingFeedback($"Не хватает монет ({seedCost} монет)");
                        return false;
                    }
                    return Plant(item.Plant);

                case HandItemType.WateringCan:
                    return Water();

                case HandItemType.Fertilizer:
                    int fertCost = currentPlant != null ? currentPlant.FertilizerCost : 1;
                    if (GameManager.Instance != null && !GameManager.Instance.TrySpendCoins(fertCost))
                    {
                        InteractionController.Instance?.ShowFloatingFeedback($"Не хватает монет ({fertCost} монет)");
                        return false;
                    }
                    float cut = GreenhouseGrid.Instance?.Config != null ? GreenhouseGrid.Instance.Config.defaultFertilizerCutPercent : 0.15f;
                    return Fertilize(cut);

                default:
                    return false;
            }
        }

        public bool Plant(PlantData plant)
        {
            if (!isUnlocked || state != PlantState.Empty || plant == null)
            {
                return false;
            }

            currentPlant = plant;
            growthProgress = 0f;
            elapsedGrowthTime = 0f;
            hasBeenWatered = false;
            fertilizeCount = 0;
            state = PlantState.Growing;

            UpdateVisuals();
            StateChanged?.Invoke(this);
            PlantEvents.TriggerPlanted(this, plant);
            return true;
        }

        public bool Water()
        {
            if (!isUnlocked || state != PlantState.NeedsWater || currentPlant == null)
            {
                return false;
            }

            hasBeenWatered = true;
            state = PlantState.Growing;

            UpdateVisuals();
            StateChanged?.Invoke(this);
            PlantEvents.TriggerWatered(this, currentPlant);
            return true;
        }

        public bool Fertilize(float cutPercent)
        {
            if (!isUnlocked || state != PlantState.Growing || currentPlant == null)
            {
                return false;
            }

            float totalGrowTime = currentPlant.GrowTime / growthSpeedMultiplier;
            float remainingTime = Mathf.Max(0f, totalGrowTime - elapsedGrowthTime);
            float timeCut = remainingTime * Mathf.Clamp01(cutPercent);

            elapsedGrowthTime += timeCut;
            fertilizeCount++;

            UpdateVisuals();
            StateChanged?.Invoke(this);
            PlantEvents.TriggerFertilized(this, currentPlant);
            return true;
        }

        public PlantData Harvest()
        {
            if (!isUnlocked || state != PlantState.Ripe || currentPlant == null)
            {
                return null;
            }

            PlantData harvested = currentPlant;
            currentPlant = null;
            growthProgress = 0f;
            elapsedGrowthTime = 0f;
            hasBeenWatered = false;
            fertilizeCount = 0;
            state = PlantState.Empty;

            UpdateVisuals();
            StateChanged?.Invoke(this);
            PlantEvents.TriggerHarvested(this, harvested);
            return harvested;
        }

        public void SetSpeedMultiplier(float mult)
        {
            growthSpeedMultiplier = Mathf.Max(0.1f, mult);
        }

        public void SetHighlight(CellHighlight highlight)
        {
            if (highlightBorder == null) return;

            switch (highlight)
            {
                case CellHighlight.None:
                    highlightBorder.gameObject.SetActive(false);
                    break;
                case CellHighlight.Valid:
                    highlightBorder.gameObject.SetActive(true);
                    highlightBorder.color = highlightValidColor;
                    break;
                case CellHighlight.Invalid:
                    highlightBorder.gameObject.SetActive(true);
                    highlightBorder.color = highlightInvalidColor;
                    break;
            }
        }

        public void UpdateVisuals()
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (cellBackground != null)
            {
                cellBackground.color = isUnlocked ? defaultCellColor : lockedCellColor;
            }

            if (!isUnlocked || state == PlantState.Empty)
            {
                if (plantVisual != null) plantVisual.gameObject.SetActive(false);
                if (timeBar != null) timeBar.SetHidden();
                return;
            }

            switch (state)
            {
                case PlantState.Growing:
                    if (plantVisual != null)
                    {
                        plantVisual.gameObject.SetActive(true);
                        plantVisual.color = currentPlant != null ? currentPlant.PlantColor : Color.white;
                    }
                    if (timeBar != null && currentPlant != null)
                    {
                        float total = currentPlant.GrowTime / growthSpeedMultiplier;
                        float rem = Mathf.Max(0f, total - elapsedGrowthTime);
                        timeBar.SetGrowing(growthProgress, rem);
                    }
                    break;

                case PlantState.NeedsWater:
                    if (plantVisual != null) plantVisual.gameObject.SetActive(true);
                    if (timeBar != null)
                    {
                        timeBar.SetNeedsWater(growthProgress);
                    }
                    break;

                case PlantState.Ripe:
                    if (plantVisual != null)
                    {
                        plantVisual.gameObject.SetActive(true);
                        plantVisual.transform.localScale = Vector3.one;
                        plantVisual.color = currentPlant != null ? currentPlant.PlantColor : Color.white;
                    }
                    if (timeBar != null)
                    {
                        timeBar.SetRipe();
                    }
                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isUnlocked)
            {
                InteractionController.Instance?.ShowFloatingFeedback("Клетка заблокирована. Откройте в Дереве улучшений!");
                return;
            }

            CellClicked?.Invoke(this);
            InteractionController.Instance?.OnCellClicked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            InteractionController.Instance?.OnCellPointerEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            InteractionController.Instance?.OnCellPointerExit(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!isUnlocked) return;
            InteractionController.Instance?.OnCellDrop(this);
        }
    }
}
