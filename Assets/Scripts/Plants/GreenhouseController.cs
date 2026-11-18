using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Контроллер Оранжереи (Зона 1).
    /// Управляет столом с горшками, выбором семян и пересылкой собранных цветов на склад магазина.
    /// </summary>
    public class GreenhouseController : MonoBehaviour
    {
        public static GreenhouseController Instance { get; private set; }

        [Header("Горшки")]
        [SerializeField] private PlantPot[] pots = new PlantPot[4];

        [Header("База семян")]
        [SerializeField] private PlantDatabase plantDatabase;
        [SerializeField] private List<PlantData> unlockedSeeds = new List<PlantData>();

        [Header("UI Панель выбора семян")]
        [SerializeField] private GameObject seedPickerModal;
        [SerializeField] private Transform seedButtonsContainer;
        [SerializeField] private Button closePickerButton;
        [SerializeField] private TMP_Text seedPickerTitle;

        [Header("UI Инструменты")]
        [SerializeField] private Button waterAllButton;
        [SerializeField] private Button fertilizeButton;
        [SerializeField] private TMP_Text greenhouseHintLabel;

        private PlantPot targetPotForPlanting;
        private float globalGrowthMultiplier = 1f;

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
            // По умолчанию разблокируем Маргаритку, если список пуст
            if (unlockedSeeds.Count == 0 && plantDatabase != null && plantDatabase.Count > 0)
            {
                unlockedSeeds.Add(plantDatabase.Plants[0]); // Маргаритка
            }

            // Подписка на события горшков
            for (int i = 0; i < pots.Length; i++)
            {
                if (pots[i] != null)
                {
                    pots[i].PotClicked += OnPotClicked;
                    pots[i].Harvested += OnPlantHarvested;
                }
            }

            if (closePickerButton != null)
                closePickerButton.onClick.AddListener(CloseSeedPicker);

            if (waterAllButton != null)
                waterAllButton.onClick.AddListener(WaterAllPots);

            if (fertilizeButton != null)
                fertilizeButton.onClick.AddListener(FertilizeAllPots);

            if (seedPickerModal != null)
                seedPickerModal.SetActive(false);

            UpdateHint("Кликните на пустой горшок, чтобы посадить семя. Когда появится 💧 — полейте, а при ✨ — соберите цветок.");
        }

        private void OnDestroy()
        {
            for (int i = 0; i < pots.Length; i++)
            {
                if (pots[i] != null)
                {
                    pots[i].PotClicked -= OnPotClicked;
                    pots[i].Harvested -= OnPlantHarvested;
                }
            }
        }

        private void OnPotClicked(PlantPot pot)
        {
            if (pot.State == PotState.Empty)
            {
                OpenSeedPicker(pot);
            }
        }

        private void OnPlantHarvested(PlantPot pot, PlantData plant)
        {
            if (ShopInventory.Instance != null)
            {
                ShopInventory.Instance.AddFlower(plant);
                UpdateHint($"<color=#4ade80>Собран цветок «{plant.PlantName}»! Он отправлен на витрину в Торговый зал.</color>");
            }
        }

        public void OpenSeedPicker(PlantPot targetPot)
        {
            targetPotForPlanting = targetPot;
            if (seedPickerModal != null)
            {
                seedPickerModal.SetActive(true);
            }

            if (seedPickerTitle != null)
            {
                seedPickerTitle.text = $"Посадка в горшок #{targetPot.PotIndex + 1}";
            }

            PopulateSeedButtons();
        }

        public void CloseSeedPicker()
        {
            if (seedPickerModal != null)
            {
                seedPickerModal.SetActive(false);
            }
            targetPotForPlanting = null;
        }

        public void TryPlantSeed(PlantData plant)
        {
            if (targetPotForPlanting == null || plant == null)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                if (!GameManager.Instance.TrySpendCoins(plant.SeedPrice))
                {
                    UpdateHint($"<color=#f87171>Не хватает монет на семя «{plant.PlantName}» (нужно {plant.SeedPrice} 🪙)!</color>");
                    return;
                }
            }

            targetPotForPlanting.SetSpeedMultiplier(globalGrowthMultiplier);
            targetPotForPlanting.Plant(plant);
            CloseSeedPicker();
            UpdateHint($"Посажено семя «{plant.PlantName}». Рост: {plant.GrowTime} сек.");
        }

        public void WaterAllPots()
        {
            int count = 0;
            foreach (var pot in pots)
            {
                if (pot != null && pot.State == PotState.NeedsWater)
                {
                    pot.Water();
                    count++;
                }
            }
            if (count > 0)
            {
                UpdateHint($"<color=#38bdf8>Полито горшков: {count}!</color>");
            }
        }

        public void FertilizeAllPots()
        {
            int count = 0;
            foreach (var pot in pots)
            {
                if (pot != null && (pot.State == PotState.Growing || pot.State == PotState.NeedsWater))
                {
                    pot.Fertilize(0.35f);
                    count++;
                }
            }
            if (count > 0)
            {
                UpdateHint($"<color=#fbbf24>Удобрения применены к {count} горшкам! Рост ускорен на 35%.</color>");
            }
        }

        public void UnlockPot(int index)
        {
            if (index >= 0 && index < pots.Length && pots[index] != null)
            {
                pots[index].Unlock();
                UpdateHint($"<color=#4ade80>Разблокирован горшок #{index + 1}!</color>");
            }
        }

        public void UnlockSeed(PlantData plant)
        {
            if (plant != null && !unlockedSeeds.Contains(plant))
            {
                unlockedSeeds.Add(plant);
                UpdateHint($"<color=#38bdf8>Открыт новый сорт: {plant.PlantName}!</color>");
            }
        }

        public void SetGrowthMultiplier(float multiplier)
        {
            globalGrowthMultiplier = Mathf.Max(0.5f, multiplier);
            foreach (var pot in pots)
            {
                if (pot != null) pot.SetSpeedMultiplier(globalGrowthMultiplier);
            }
        }

        private void PopulateSeedButtons()
        {
            if (seedButtonsContainer == null) return;

            // Очищаем старые кнопки
            for (int i = seedButtonsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(seedButtonsContainer.GetChild(i).gameObject);
            }

            // Создаем кнопки для всех доступных семян
            foreach (var plant in unlockedSeeds)
            {
                if (plant == null) continue;

                var btnGo = new GameObject($"SeedBtn_{plant.Id}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(seedButtonsContainer, false);

                var rt = btnGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(380f, 60f);

                var img = btnGo.GetComponent<Image>();
                img.color = new Color(0.18f, 0.24f, 0.35f, 1f);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(btnGo.transform, false);

                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;

                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{plant.PlantName}</b>  •  Цена: {plant.SeedPrice} 🪙  •  Рост: {plant.GrowTime}с";
                tmp.fontSize = 17f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                var btn = btnGo.GetComponent<Button>();
                var targetPlant = plant;
                btn.onClick.AddListener(() => TryPlantSeed(targetPlant));
            }
        }

        private void UpdateHint(string message)
        {
            if (greenhouseHintLabel != null)
            {
                greenhouseHintLabel.text = message;
            }
        }
    }
}
