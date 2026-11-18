using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    [Serializable]
    public class UpgradeItem
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int cost;
        public string category; // "Рост", "Торговля", "Уют"
        public bool isPurchased;

        // UI references
        [HideInInspector] public Button buyButton;
        [HideInInspector] public TMP_Text costLabel;
        [HideInInspector] public Image cardBackground;
    }

    /// <summary>
    /// Дерево улучшений (Зона 2).
    /// Позволяет тратить монеты на открытие новых горшков, сортов цветов и ускорение бизнеса.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("База растений")]
        [SerializeField] private PlantDatabase plantDatabase;

        [Header("UI Контейнер")]
        [SerializeField] private Transform upgradesContainer;
        [SerializeField] private TMP_Text statusHintLabel;

        [Header("Список доступных улучшений")]
        [SerializeField]
        private List<UpgradeItem> upgrades = new List<UpgradeItem>
        {
            new UpgradeItem
            {
                id = "pot_2",
                title = "Второй горшок",
                description = "Открывает дополнительный стол-горшок #2 в оранжерее.",
                cost = 20,
                category = "Рост"
            },
            new UpgradeItem
            {
                id = "seed_lavender",
                title = "Семена Лаванды",
                description = "Разблокирует посадку Лаванды и заказы покупателей.",
                cost = 35,
                category = "Рост"
            },
            new UpgradeItem
            {
                id = "fertilizer_boost",
                title = "Минеральный концентрат",
                description = "Ускоряет базовый рост всех растений на +25%.",
                cost = 50,
                category = "Рост"
            },
            new UpgradeItem
            {
                id = "pot_3",
                title = "Третий горшок",
                description = "Открывает горшок #3 для одновременного выращивания.",
                cost = 65,
                category = "Рост"
            },
            new UpgradeItem
            {
                id = "fast_customers",
                title = "Приток покупателей",
                description = "Покупатели приходят к прилавку в 2 раза быстрее.",
                cost = 60,
                category = "Торговля"
            }
        };

        private readonly HashSet<string> purchasedSet = new HashSet<string>();

        public event Action<UpgradeItem> UpgradePurchased;

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
            BuildUI();
            UpdateHint("Выберите улучшение для прокачки лавки. Прогресс моментально применяется к оранжерее и магазину.");
        }

        public bool IsPurchased(string id) => purchasedSet.Contains(id);

        public bool TryBuyUpgrade(string id)
        {
            var item = upgrades.Find(u => u.id == id);
            if (item == null || item.isPurchased) return false;

            if (GameManager.Instance == null || !GameManager.Instance.TrySpendCoins(item.cost))
            {
                UpdateHint($"<color=#f87171>Недостаточно монет для покупки «{item.title}» (нужно {item.cost} 🪙)!</color>");
                return false;
            }

            item.isPurchased = true;
            purchasedSet.Add(item.id);

            ApplyUpgradeEffect(item);
            RefreshUI();

            UpdateHint($"<color=#4ade80>Улучшение «{item.title}» успешно приобретено и активировано!</color>");
            UpgradePurchased?.Invoke(item);
            return true;
        }

        private void ApplyUpgradeEffect(UpgradeItem item)
        {
            switch (item.id)
            {
                case "pot_2":
                    GreenhouseController.Instance?.UnlockPot(1);
                    break;

                case "pot_3":
                    GreenhouseController.Instance?.UnlockPot(2);
                    break;

                case "seed_lavender":
                    if (plantDatabase != null && plantDatabase.Count > 1)
                    {
                        var lavender = plantDatabase.Plants[1];
                        GreenhouseController.Instance?.UnlockSeed(lavender);
                        CustomerManager.Instance?.AddPossibleOrder(lavender);
                    }
                    break;

                case "fertilizer_boost":
                    GreenhouseController.Instance?.SetGrowthMultiplier(1.25f);
                    break;

                case "fast_customers":
                    CustomerManager.Instance?.SetCustomerDelay(2f);
                    break;
            }
        }

        public void BuildUI()
        {
            if (upgradesContainer == null) return;

            // Очищаем старые элементы
            for (int i = upgradesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradesContainer.GetChild(i).gameObject);
            }

            foreach (var item in upgrades)
            {
                var cardGo = new GameObject($"Card_{item.id}", typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(upgradesContainer, false);

                var rt = cardGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(480f, 90f);

                var img = cardGo.GetComponent<Image>();
                img.color = item.isPurchased ? new Color(0.12f, 0.35f, 0.22f, 0.95f) : new Color(0.16f, 0.22f, 0.32f, 0.95f);
                item.cardBackground = img;

                // Заголовок и описание
                var textGo = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(cardGo.transform, false);

                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0f, 0f);
                textRt.anchorMax = new Vector2(0.68f, 1f);
                textRt.offsetMin = new Vector2(15f, 8f);
                textRt.offsetMax = new Vector2(-5f, -8f);

                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b>{item.title}</b> <size=13><color=#94a3b8>({item.category})</color></size>\n<size=14>{item.description}</size>";
                tmp.fontSize = 17f;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                // Кнопка покупки
                var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(cardGo.transform, false);

                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.70f, 0.15f);
                btnRt.anchorMax = new Vector2(0.97f, 0.85f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;

                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = item.isPurchased ? new Color(0.20f, 0.55f, 0.35f) : new Color(0.14f, 0.44f, 0.90f);

                var btnTextGo = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
                btnTextGo.transform.SetParent(btnGo.transform, false);

                var btnTextRt = btnTextGo.GetComponent<RectTransform>();
                btnTextRt.anchorMin = Vector2.zero;
                btnTextRt.anchorMax = Vector2.one;
                btnTextRt.offsetMin = Vector2.zero;
                btnTextRt.offsetMax = Vector2.zero;

                var btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
                btnTmp.text = item.isPurchased ? "Куплено ✓" : $"Купить ({item.cost} 🪙)";
                btnTmp.fontSize = 16f;
                btnTmp.fontStyle = FontStyles.Bold;
                btnTmp.alignment = TextAlignmentOptions.Center;
                btnTmp.color = Color.white;
                btnTmp.raycastTarget = false;

                var btn = btnGo.GetComponent<Button>();
                btn.interactable = !item.isPurchased;
                string upgradeId = item.id;
                btn.onClick.AddListener(() => TryBuyUpgrade(upgradeId));

                item.buyButton = btn;
                item.costLabel = btnTmp;
            }
        }

        public void RefreshUI()
        {
            foreach (var item in upgrades)
            {
                if (item.buyButton != null)
                {
                    item.buyButton.interactable = !item.isPurchased;
                }
                if (item.costLabel != null)
                {
                    item.costLabel.text = item.isPurchased ? "Куплено ✓" : $"Купить ({item.cost} 🪙)";
                }
                if (item.cardBackground != null)
                {
                    item.cardBackground.color = item.isPurchased
                        ? new Color(0.12f, 0.35f, 0.22f, 0.95f)
                        : new Color(0.16f, 0.22f, 0.32f, 0.95f);
                }
            }
        }

        private void UpdateHint(string message)
        {
            if (statusHintLabel != null)
            {
                statusHintLabel.text = message;
            }
        }
    }
}
