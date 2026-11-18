using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    public enum PotState
    {
        Locked = 0,
        Empty = 1,
        Growing = 2,
        NeedsWater = 3,
        Mature = 4
    }

    /// <summary>
    /// Горшок для выращивания растений в Оранжерее.
    /// Управляет жизненным циклом: Пусто -> Посадка -> Рост -> Полив (если требуется) -> Созревание -> Сбор.
    /// </summary>
    public class PlantPot : MonoBehaviour
    {
        [Header("Идентификация")]
        [SerializeField] private int potIndex = 0;
        [SerializeField] private bool isUnlocked = true;

        [Header("UI Ссылки")]
        [SerializeField] private Image potIcon;
        [SerializeField] private Image plantVisual;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject progressContainer;
        [SerializeField] private TMP_Text statusBadge;
        [SerializeField] private TMP_Text plantNameLabel;
        [SerializeField] private Button potButton;
        [SerializeField] private GameObject waterAlertBadge;

        [Header("Настройки")]
        [SerializeField] private Color lockedColor = new Color(0.2f, 0.22f, 0.28f, 0.6f);
        [SerializeField] private Color emptyColor = new Color(0.35f, 0.38f, 0.45f, 1f);

        private PotState state = PotState.Empty;
        private PlantData currentPlant;
        private float growthProgress = 0f;
        private float elapsedGrowthTime = 0f;
        private bool hasBeenWatered = false;
        private float speedMultiplier = 1f;

        public int PotIndex => potIndex;
        public bool IsUnlocked => isUnlocked;
        public PotState State => state;
        public PlantData CurrentPlant => currentPlant;
        public float GrowthProgress => growthProgress;

        public event Action<PlantPot> StateChanged;
        public event Action<PlantPot, PlantData> Harvested;
        public event Action<PlantPot> PotClicked;

        private void Awake()
        {
            if (potButton != null)
            {
                potButton.onClick.AddListener(OnPotClicked);
            }
        }

        private void Start()
        {
            if (!isUnlocked)
            {
                SetState(PotState.Locked);
            }
            else if (currentPlant == null)
            {
                SetState(PotState.Empty);
            }
            UpdateVisuals();
        }

        private void Update()
        {
            if (state == PotState.Growing && currentPlant != null)
            {
                float totalGrowTime = Mathf.Max(0.1f, currentPlant.GrowTime / speedMultiplier);

                // Проверка необходимости полива
                if (currentPlant.NeedsWaterTime > 0f && !hasBeenWatered && elapsedGrowthTime >= currentPlant.NeedsWaterTime)
                {
                    SetState(PotState.NeedsWater);
                    return;
                }

                elapsedGrowthTime += Time.deltaTime;
                growthProgress = Mathf.Clamp01(elapsedGrowthTime / totalGrowTime);

                if (progressFill != null)
                {
                    progressFill.fillAmount = growthProgress;
                }

                if (plantVisual != null)
                {
                    float scale = Mathf.Lerp(0.35f, 1.0f, growthProgress);
                    plantVisual.transform.localScale = new Vector3(scale, scale, 1f);
                }

                if (statusBadge != null)
                {
                    int pct = Mathf.RoundToInt(growthProgress * 100f);
                    statusBadge.text = $"Растёт {pct}%";
                }

                if (growthProgress >= 1f)
                {
                    SetState(PotState.Mature);
                }
            }
        }

        /// <summary>Посадить семя в горшок.</summary>
        public bool Plant(PlantData plant)
        {
            if (!isUnlocked || state != PotState.Empty || plant == null)
            {
                return false;
            }

            currentPlant = plant;
            growthProgress = 0f;
            elapsedGrowthTime = 0f;
            hasBeenWatered = false;

            SetState(PotState.Growing);
            return true;
        }

        /// <summary>Полить растение.</summary>
        public void Water()
        {
            if (state != PotState.NeedsWater)
            {
                return;
            }

            hasBeenWatered = true;
            SetState(PotState.Growing);
        }

        /// <summary>Удобрить растение (срезает указанный процент оставшегося времени).</summary>
        public void Fertilize(float cutPercent = 0.35f)
        {
            if (state != PotState.Growing && state != PotState.NeedsWater)
            {
                return;
            }

            float totalGrowTime = currentPlant.GrowTime / speedMultiplier;
            float timeToCut = totalGrowTime * cutPercent;
            elapsedGrowthTime += timeToCut;

            if (state == PotState.NeedsWater)
            {
                hasBeenWatered = true;
                SetState(PotState.Growing);
            }

            UpdateVisuals();
        }

        /// <summary>Собрать готовый урожай.</summary>
        public PlantData Harvest()
        {
            if (state != PotState.Mature || currentPlant == null)
            {
                return null;
            }

            PlantData harvestedPlant = currentPlant;
            currentPlant = null;
            growthProgress = 0f;
            elapsedGrowthTime = 0f;
            hasBeenWatered = false;

            SetState(PotState.Empty);
            Harvested?.Invoke(this, harvestedPlant);
            return harvestedPlant;
        }

        /// <summary>Разблокировать горшок (через дерево улучшений).</summary>
        public void Unlock()
        {
            isUnlocked = true;
            if (state == PotState.Locked)
            {
                SetState(PotState.Empty);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            speedMultiplier = Mathf.Max(0.1f, mult);
        }

        private void SetState(PotState newState)
        {
            state = newState;
            UpdateVisuals();
            StateChanged?.Invoke(this);
        }

        private void OnPotClicked()
        {
            switch (state)
            {
                case PotState.NeedsWater:
                    Water();
                    break;

                case PotState.Mature:
                    Harvest();
                    break;

                default:
                    PotClicked?.Invoke(this);
                    break;
            }
        }

        public void UpdateVisuals()
        {
            bool showProgress = state == PotState.Growing || state == PotState.NeedsWater;
            if (progressContainer != null)
            {
                progressContainer.SetActive(showProgress);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = growthProgress;
            }

            if (waterAlertBadge != null)
            {
                waterAlertBadge.SetActive(state == PotState.NeedsWater);
            }

            switch (state)
            {
                case PotState.Locked:
                    if (plantVisual != null) plantVisual.gameObject.SetActive(false);
                    if (plantNameLabel != null) plantNameLabel.text = "Закрыто";
                    if (statusBadge != null) statusBadge.text = "🔒 Улучшение";
                    if (potIcon != null) potIcon.color = lockedColor;
                    break;

                case PotState.Empty:
                    if (plantVisual != null) plantVisual.gameObject.SetActive(false);
                    if (plantNameLabel != null) plantNameLabel.text = "Пустой горшок";
                    if (statusBadge != null) statusBadge.text = "+ Посадить";
                    if (potIcon != null) potIcon.color = emptyColor;
                    break;

                case PotState.Growing:
                    if (plantVisual != null)
                    {
                        plantVisual.gameObject.SetActive(true);
                        plantVisual.color = currentPlant != null ? currentPlant.PlantColor : Color.white;
                    }
                    if (plantNameLabel != null) plantNameLabel.text = currentPlant?.PlantName ?? "Растение";
                    if (statusBadge != null)
                    {
                        int pct = Mathf.RoundToInt(growthProgress * 100f);
                        statusBadge.text = $"Растёт {pct}%";
                    }
                    if (potIcon != null) potIcon.color = Color.white;
                    break;

                case PotState.NeedsWater:
                    if (plantVisual != null) plantVisual.gameObject.SetActive(true);
                    if (statusBadge != null) statusBadge.text = "<color=#38bdf8>💧 Полей меня!</color>";
                    break;

                case PotState.Mature:
                    if (plantVisual != null)
                    {
                        plantVisual.gameObject.SetActive(true);
                        plantVisual.transform.localScale = Vector3.one;
                        plantVisual.color = currentPlant != null ? currentPlant.PlantColor : Color.white;
                    }
                    if (plantNameLabel != null) plantNameLabel.text = currentPlant?.PlantName ?? "Растение";
                    if (statusBadge != null) statusBadge.text = "<color=#4ade80>✨ Собери!</color>";
                    break;
            }
        }
    }
}
