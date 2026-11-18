using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Система покупателей в Торговом зале (Зона 0).
    /// Клиенты приходят к прилавку, заказывают цветы и покупают их за монеты и опыт (XP).
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("База растений для заказов")]
        [SerializeField] private PlantDatabase plantDatabase;
        [SerializeField] private List<PlantData> possibleOrders = new List<PlantData>();

        [Header("UI Прилавка и Покупателя")]
        [SerializeField] private GameObject customerBubble;
        [SerializeField] private TMP_Text customerNameLabel;
        [SerializeField] private TMP_Text orderDetailsLabel;
        [SerializeField] private Image orderIcon;
        [SerializeField] private Image patienceFill;
        [SerializeField] private Button serveButton;
        [SerializeField] private TMP_Text serveButtonText;
        [SerializeField] private TMP_Text counterStatusLabel;
        [SerializeField] private Image customerAvatar;

        [Header("Настройки")]
        [SerializeField] private float customerPatienceDuration = 40f;
        [SerializeField] private float nextCustomerDelay = 4f;

        private PlantData currentOrder;
        private float currentPatience = 0f;
        private bool isCustomerActive = false;
        private int totalServedCount = 0;
        private int totalXp = 0;

        private readonly string[] customerNames = { "Анна", "Михаил", "София", "Артём", "Елена", "Дмитрий", "Алиса", "Иван" };
        private readonly Color[] customerColors = {
            new Color(0.98f, 0.75f, 0.65f),
            new Color(0.65f, 0.85f, 0.98f),
            new Color(0.85f, 0.70f, 0.95f),
            new Color(0.70f, 0.95f, 0.75f)
        };

        public int TotalServedCount => totalServedCount;
        public int TotalXp => totalXp;

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
            if (possibleOrders.Count == 0 && plantDatabase != null && plantDatabase.Count > 0)
            {
                possibleOrders.Add(plantDatabase.Plants[0]); // Маргаритка
                if (plantDatabase.Count > 1) possibleOrders.Add(plantDatabase.Plants[1]); // Лаванда
            }

            if (serveButton != null)
            {
                serveButton.onClick.AddListener(TryServeCurrentCustomer);
            }

            StartCoroutine(CustomerLoopRoutine());
        }

        private void Update()
        {
            if (isCustomerActive && currentOrder != null)
            {
                currentPatience -= Time.deltaTime;
                float progress = Mathf.Clamp01(currentPatience / customerPatienceDuration);

                if (patienceFill != null)
                {
                    patienceFill.fillAmount = progress;
                }

                // Обновляем доступность кнопки продажи
                bool inStock = ShopInventory.Instance != null && ShopInventory.Instance.HasFlower(currentOrder);
                if (serveButton != null)
                {
                    serveButton.interactable = inStock;
                }

                if (serveButtonText != null)
                {
                    serveButtonText.text = inStock
                        ? $"Продать (+{currentOrder.SellPrice} 🪙)"
                        : "Нет на витрине";
                }

                if (currentPatience <= 0f)
                {
                    OnCustomerTimedOut();
                }
            }
        }

        private IEnumerator CustomerLoopRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            while (true)
            {
                SpawnNewCustomer();

                while (isCustomerActive)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(nextCustomerDelay);
            }
        }

        private void SpawnNewCustomer()
        {
            if (possibleOrders.Count == 0) return;

            currentOrder = possibleOrders[Random.Range(0, possibleOrders.Count)];
            currentPatience = customerPatienceDuration;
            isCustomerActive = true;

            string customerName = customerNames[Random.Range(0, customerNames.Length)];
            Color avatarColor = customerColors[Random.Range(0, customerColors.Length)];

            if (customerAvatar != null)
            {
                customerAvatar.gameObject.SetActive(true);
                customerAvatar.color = avatarColor;
            }

            if (customerBubble != null)
            {
                customerBubble.SetActive(true);
            }

            if (customerNameLabel != null)
            {
                customerNameLabel.text = $"Покупатель: {customerName}";
            }

            if (orderDetailsLabel != null)
            {
                orderDetailsLabel.text = $"«Хочу купить <b>{currentOrder.PlantName}</b>!»\nПлачу: <color=#fbbf24>{currentOrder.SellPrice} 🪙</color>  (Опыт: +{currentOrder.XpReward} XP)";
            }

            if (orderIcon != null)
            {
                orderIcon.color = currentOrder.PlantColor;
            }

            if (counterStatusLabel != null)
            {
                counterStatusLabel.text = $"Клиент ждёт у прилавка. Продайте цветок «{currentOrder.PlantName}».";
            }
        }

        public void TryServeCurrentCustomer()
        {
            if (!isCustomerActive || currentOrder == null) return;

            if (ShopInventory.Instance == null || !ShopInventory.Instance.TryConsumeFlower(currentOrder))
            {
                if (counterStatusLabel != null)
                {
                    counterStatusLabel.text = $"<color=#f87171>Цветка «{currentOrder.PlantName}» нет на витрине! Вырастите его в оранжерее.</color>";
                }
                return;
            }

            // Успешная продажа!
            int coinsEarned = currentOrder.SellPrice;
            int xpEarned = currentOrder.XpReward;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coinsEarned);
            }

            totalServedCount++;
            totalXp += xpEarned;

            if (counterStatusLabel != null)
            {
                counterStatusLabel.text = $"<color=#4ade80>Успешно продано! Получено +{coinsEarned} 🪙 и +{xpEarned} XP! (Всего продано: {totalServedCount})</color>";
            }

            CompleteCustomer();
        }

        private void OnCustomerTimedOut()
        {
            if (counterStatusLabel != null)
            {
                counterStatusLabel.text = "<color=#94a3b8>Покупатель устал ждать и ушёл. Скоро подойдёт следующий.</color>";
            }
            CompleteCustomer();
        }

        private void CompleteCustomer()
        {
            isCustomerActive = false;
            currentOrder = null;

            if (customerBubble != null)
            {
                customerBubble.SetActive(false);
            }

            if (customerAvatar != null)
            {
                customerAvatar.gameObject.SetActive(false);
            }
        }

        public void AddPossibleOrder(PlantData plant)
        {
            if (plant != null && !possibleOrders.Contains(plant))
            {
                possibleOrders.Add(plant);
            }
        }

        public void SetCustomerDelay(float delay)
        {
            nextCustomerDelay = Mathf.Max(1.5f, delay);
        }
    }
}
