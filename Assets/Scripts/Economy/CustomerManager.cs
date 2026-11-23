using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Система покупателей в Торговом зале (Зона 0).
    /// Управляет жизненным циклом одного покупателя у прилавка:
    /// спавн у двери, подход к кассе, ожидание заказа с таймером терпения,
    /// продажа / отказ / таймаут и уход через дверь.
    /// Работает непрерывно вне зависимости от текущей зоны камеры.
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [Header("Конфигурация")]
        [SerializeField] private CustomerConfig config;

        [Header("База растений и разблокированные заказы")]
        [SerializeField] private PlantDatabase plantDatabase;
        [SerializeField] private List<PlantData> unlockedOrders = new List<PlantData>();

        [Header("Компоненты покупателя и прилавка")]
        [SerializeField] private Customer customer;
        [SerializeField] private OrderBubble orderBubble;
        [SerializeField] private CounterButtons counterButtons;

        [Header("Точки перемещения")]
        [SerializeField] private Transform doorPoint;
        [SerializeField] private Transform counterPoint;
        [SerializeField] private Transform exitPoint;

        [Header("Статистика")]
        [SerializeField] private int totalServedCount = 0;
        [SerializeField] private int totalXp = 0;

        // Палитра приятных силуэтных оттенков одежды покупателей
        private readonly Color[] customerColors = new Color[]
        {
            new Color(0.35f, 0.55f, 0.75f, 1f), // Синий
            new Color(0.80f, 0.52f, 0.42f, 1f), // Терракотовый
            new Color(0.48f, 0.65f, 0.45f, 1f), // Шалфейный
            new Color(0.72f, 0.55f, 0.70f, 1f), // Лавандовый
            new Color(0.85f, 0.68f, 0.40f, 1f)  // Охристый
        };

        private PlantData currentOrder;
        private float currentPatience = 0f;
        private bool isCustomerActive = false;
        private Coroutine customerRoutine;

        public static event Action<int, int> XpChanged; // (delta, total)

        public int TotalServedCount => totalServedCount;
        public int TotalXp => totalXp;
        public bool IsCustomerActive => isCustomerActive;
        public PlantData CurrentOrder => currentOrder;
        public CustomerConfig Config => config;

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
            InitializeOrders();

            if (customer != null && config != null)
            {
                customer.Initialize(config);
            }

            if (counterButtons != null)
            {
                counterButtons.SellClicked += HandleSellClicked;
                counterButtons.RefuseClicked += HandleRefuseClicked;
            }

            if (ShopInventory.Instance != null)
            {
                ShopInventory.Instance.InventoryChanged += OnInventoryChanged;
            }

            // Запускаем непрерывный цикл покупателей
            if (Application.isPlaying)
            {
                customerRoutine = StartCoroutine(CustomerLifecycleRoutine());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (counterButtons != null)
            {
                counterButtons.SellClicked -= HandleSellClicked;
                counterButtons.RefuseClicked -= HandleRefuseClicked;
            }

            if (ShopInventory.Instance != null)
            {
                ShopInventory.Instance.InventoryChanged -= OnInventoryChanged;
            }

            if (customerRoutine != null)
            {
                StopCoroutine(customerRoutine);
            }
        }

        private void InitializeOrders()
        {
            unlockedOrders.Clear();
            if (plantDatabase != null && plantDatabase.Count > 0)
            {
                // Стартово открыта только Маргаритка (первое растение)
                unlockedOrders.Add(plantDatabase.Plants[0]);

                // Проверяем, не куплена ли уже Лаванда в UpgradeManager
                if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsPurchased("seed_lavender"))
                {
                    if (plantDatabase.Count > 1)
                    {
                        unlockedOrders.Add(plantDatabase.Plants[1]);
                    }
                }
            }
        }

        private void Update()
        {
            // Обработка ожидания у прилавка
            if (isCustomerActive && customer != null && customer.IsAtCounter && currentOrder != null)
            {
                float patienceDuration = config != null ? config.PatienceDuration : 30f;
                currentPatience -= Time.deltaTime;

                bool inStock = ShopInventory.Instance != null && ShopInventory.Instance.HasFlower(currentOrder);

                if (orderBubble != null)
                {
                    orderBubble.UpdateProgress(currentPatience, patienceDuration, inStock);
                }

                if (counterButtons != null)
                {
                    counterButtons.UpdateState(currentOrder, inStock);
                }

                if (currentPatience <= 0f)
                {
                    HandleTimeout();
                }
            }
        }

        private IEnumerator CustomerLifecycleRoutine()
        {
            // Задержка перед появлением первого покупателя (по умолчанию 8с)
            float initialDelay = config != null ? config.FirstCustomerDelay : 8f;
            yield return new WaitForSeconds(initialDelay);

            while (true)
            {
                SpawnAndWalkCustomer();

                // Ждем, пока покупатель не обслужен или не ушел
                while (isCustomerActive)
                {
                    yield return null;
                }

                // Интервал между покупателями (пока касса свободна)
                float interval = config != null ? config.SpawnInterval : 20f;
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnAndWalkCustomer()
        {
            if (unlockedOrders.Count == 0 || customer == null) return;

            isCustomerActive = true;
            currentOrder = unlockedOrders[UnityEngine.Random.Range(0, unlockedOrders.Count)];
            currentPatience = config != null ? config.PatienceDuration : 30f;

            // Выбираем цвет силуэта
            Color col = customerColors[UnityEngine.Random.Range(0, customerColors.Length)];
            customer.SetVisualColor(col);

            Vector3 spawnPos = doorPoint != null ? doorPoint.position : (config != null ? config.DoorPoint : new Vector3(8.4f, -3.4f, 0f));
            Vector3 targetPos = counterPoint != null ? counterPoint.position : (config != null ? config.CounterPoint : new Vector3(3.7f, -3.4f, 0f));

            customer.WalkToCounter(spawnPos, targetPos, OnCustomerArrivedAtCounter);
        }

        private void OnCustomerArrivedAtCounter()
        {
            if (!isCustomerActive || currentOrder == null) return;

            bool inStock = ShopInventory.Instance != null && ShopInventory.Instance.HasFlower(currentOrder);
            float patience = config != null ? config.PatienceDuration : 30f;

            if (orderBubble != null)
            {
                orderBubble.Show(currentOrder, patience, inStock);
            }

            if (counterButtons != null)
            {
                counterButtons.Show(currentOrder, inStock);
            }
        }

        private void OnInventoryChanged()
        {
            if (isCustomerActive && customer != null && customer.IsAtCounter && currentOrder != null)
            {
                bool inStock = ShopInventory.Instance != null && ShopInventory.Instance.HasFlower(currentOrder);
                if (orderBubble != null)
                {
                    float patience = config != null ? config.PatienceDuration : 30f;
                    orderBubble.UpdateProgress(currentPatience, patience, inStock);
                }
                if (counterButtons != null)
                {
                    counterButtons.UpdateState(currentOrder, inStock);
                }
            }
        }

        private void HandleSellClicked()
        {
            if (!isCustomerActive || currentOrder == null) return;

            if (ShopInventory.Instance == null || !ShopInventory.Instance.TryConsumeFlower(currentOrder))
            {
                return;
            }

            // Награда за продажу
            int coinsEarned = currentOrder.SellPrice;
            int xpEarned = currentOrder.XpReward;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(coinsEarned);
            }

            totalServedCount++;
            totalXp += xpEarned;
            XpChanged?.Invoke(xpEarned, totalXp);

            // Всплывающий текст над покупателем
            Vector3 textPos = customer != null ? customer.transform.position + Vector3.up * 2f : Vector3.zero;
            ShowFloatingReward($"+{coinsEarned} 🪙", textPos);

            // Глобальное событие продажи
            PlantEvents.TriggerSold(currentOrder, coinsEarned, xpEarned);

            // Покупатель доволен и уходит
            EndOrderAndLeave(true);
        }

        private void HandleRefuseClicked()
        {
            if (!isCustomerActive) return;
            // Покупатель уходит без штрафа
            EndOrderAndLeave(false);
        }

        private void HandleTimeout()
        {
            if (!isCustomerActive) return;
            // Таймаут терпения: покупатель уходит недовольным
            EndOrderAndLeave(false);
        }

        private void EndOrderAndLeave(bool happy)
        {
            if (orderBubble != null) orderBubble.Hide();
            if (counterButtons != null) counterButtons.Hide();

            currentOrder = null;

            Vector3 exitPos = exitPoint != null ? exitPoint.position : (config != null ? config.ExitPoint : new Vector3(8.4f, -3.4f, 0f));

            if (customer != null)
            {
                customer.Leave(exitPos, happy, () =>
                {
                    customer.gameObject.SetActive(false);
                    isCustomerActive = false;
                });
            }
            else
            {
                isCustomerActive = false;
            }
        }

        private void ShowFloatingReward(string text, Vector3 worldPos)
        {
            if (FloatingTextSpawner.Instance != null && Camera.main != null)
            {
                Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
                FloatingTextSpawner.Instance.Spawn(screenPoint, text, new Color(1f, 0.85f, 0.2f, 1f));
            }
        }

        /// <summary>Добавить растение в список доступных для заказа (вызывается при покупке улучшения семян).</summary>
        public void AddPossibleOrder(PlantData plant)
        {
            if (plant != null && !unlockedOrders.Contains(plant))
            {
                unlockedOrders.Add(plant);
                Debug.Log($"[CustomerManager] Сорт «{plant.PlantName}» добавлен в список заказов покупателей.");
            }
        }

        /// <summary>Улучшение «Приток покупателей» ускоряет появление в 2 раза (множитель 0.5).</summary>
        public void ApplyFastCustomersUpgrade(float multiplier = 0.5f)
        {
            if (config != null)
            {
                config.SetSpawnIntervalMultiplier(multiplier);
                Debug.Log($"[CustomerManager] Улучшение «Приток покупателей» применено! Новый интервал: {config.SpawnInterval} с.");
            }
        }

        // Для обратной совместимости
        public void SetCustomerDelay(float delay)
        {
            if (config != null)
            {
                float baseInterval = config.BaseSpawnInterval;
                if (baseInterval > 0f)
                {
                    config.SetSpawnIntervalMultiplier(delay / baseInterval);
                }
            }
        }

        public void TryServeCurrentCustomer()
        {
            HandleSellClicked();
        }

        public void SetTestOrder(PlantData plant)
        {
            isCustomerActive = true;
            currentOrder = plant;
            currentPatience = config != null ? config.PatienceDuration : 30f;
        }

        public void SetupReferences(CustomerConfig cfg, Customer cust, OrderBubble bubble, CounterButtons buttons,
            Transform door, Transform counter, Transform exit, PlantDatabase db)
        {
            config = cfg;
            customer = cust;
            orderBubble = bubble;
            counterButtons = buttons;
            doorPoint = door;
            counterPoint = counter;
            exitPoint = exit;
            plantDatabase = db;
        }
    }
}
