using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop.EditorTools
{
    public static class CoreSceneBuilder
    {
        private const string ConfigPath = "Assets/Data/GreenhouseConfig.asset";

        [MenuItem("Tools/GreenShop/Собрать Core Loop в сцене (Этап 1-2)")]
        public static void BuildCoreGameScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var plantDb = AssetDatabase.LoadAssetAtPath<PlantDatabase>("Assets/Data/PlantDatabase.asset");

            if (plantDb == null)
            {
                Debug.LogError("[CoreSceneBuilder] PlantDatabase.asset не найдена! Сначала сгенерируйте базу растений.");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[CoreSceneBuilder] Main Camera не найдена!");
                return;
            }

            // 1. Проверяем / настраиваем CameraAspectFitter
            var aspectFitter = cam.GetComponent<CameraAspectFitter>() ?? cam.gameObject.AddComponent<CameraAspectFitter>();
            aspectFitter.ApplyAspect();

            // 2. Настраиваем стартовые монеты в GameManager
            var gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                var soGm = new SerializedObject(gm);
                soGm.FindProperty("startingCoins").doubleValue = 15.0; // даем монеты на 3 первых семечка Маргаритки (5*3=15)
                soGm.ApplyModifiedProperties();
                EditorUtility.SetDirty(gm);
            }

            // Загружаем или создаем GreenhouseConfig
            var config = AssetDatabase.LoadAssetAtPath<GreenhouseConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GreenhouseConfig>();
                config.gridWidth = 5;
                config.gridHeight = 5;
                config.startingUnlockedCount = 2;
                config.cellSize = new Vector2(130f, 150f);
                config.cellSpacing = new Vector2(12f, 12f);
                config.gridCenterOffset = new Vector2(-180f, 15f);
                config.defaultFertilizerCutPercent = 0.15f;
                config.maxFertilizerPerPlant = 1;

                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CoreSceneBuilder] Создан ассет {ConfigPath}");
            }

            // Удаляем старые Canvas зон, если они были созданы
            DestroyIfExists("ZoneCanvas_Shop");
            DestroyIfExists("ZoneCanvas_Greenhouse");
            DestroyIfExists("ZoneCanvas_Upgrades");

            // 3. Создаем Canvas для Зоны 1: Оранжерея (-19.2, 0, 0)
            var greenhouseCanvas = CreateWorldCanvas("ZoneCanvas_Greenhouse", new Vector3(-19.20f, 0f, 0f), cam);
            var greenhouseGrid = greenhouseCanvas.AddComponent<GreenhouseGrid>();
            var interactionCtrl = greenhouseCanvas.AddComponent<InteractionController>();

            BuildGreenhouseUI(greenhouseCanvas.transform, greenhouseGrid, interactionCtrl, config, plantDb);

            // 4. Создаем Canvas для Зоны 0: Торговый зал (0, 0, 0)
            var shopCanvas = CreateWorldCanvas("ZoneCanvas_Shop", new Vector3(0f, 0f, 0f), cam);
            var shopInventory = shopCanvas.AddComponent<ShopInventory>();
            var customerManager = shopCanvas.AddComponent<CustomerManager>();

            BuildShopUI(shopCanvas.transform, shopInventory, customerManager, plantDb);

            // 5. Создаем Canvas для Зоны 2: Дерево улучшений (0, -10.8, 0)
            var upgradesCanvas = CreateWorldCanvas("ZoneCanvas_Upgrades", new Vector3(0f, -10.80f, 0f), cam);
            var upgradeManager = upgradesCanvas.AddComponent<UpgradeManager>();

            BuildUpgradesUI(upgradesCanvas.transform, upgradeManager, plantDb);

            // 6. Актуализируем экранный Canvas (ScreenSpaceOverlay)
            var overlayCanvas = Object.FindAnyObjectByType<Canvas>();
            if (overlayCanvas != null && overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                var navBtnHelper = Object.FindAnyObjectByType<ZoneCameraController>();
                if (navBtnHelper != null)
                {
                    navBtnHelper.UpdateButtonVisibility(ZoneCameraController.ZoneShop);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CoreSceneBuilder] Core-геймплей (Этап 1-2: Сетка 5х5 + Предмет в руке) успешно собран в сцене и сохранён!");
        }

        private static GameObject CreateWorldCanvas(string name, Vector3 worldPos, Camera cam)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.position = worldPos;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.localScale = new Vector3(0.01f, 0.01f, 1f); // 1920x1080 -> 19.2 x 10.8 units

            return go;
        }

        private static void BuildGreenhouseUI(Transform parent, GreenhouseGrid grid, InteractionController interactionCtrl, GreenhouseConfig config, PlantDatabase plantDb)
        {
            // Заголовок
            CreateText("Header", parent, new Vector2(900, 50), new Vector2(0, 470), "ОРАНЖЕРЕЯ — СЕТКА ВЫРАЩИВАНИЯ (5×5)", 32, FontStyles.Bold, Color.white);
            CreateText("Hint", parent, new Vector2(1200, 35), new Vector2(0, 425), "Возьмите инструмент или семя на полке справа и примените к клетке. ПКМ / Esc — отмена.", 17, FontStyles.Normal, new Color(0.7f, 0.82f, 0.95f));

            // Мини-счетчик монет в Оранжерее (допущение спецификации)
            var coinsLabel = CreateText("GreenhouseCoins", parent, new Vector2(300, 40), new Vector2(500, 440), "Баланс: 15 монет", 20, FontStyles.Bold, new Color(0.98f, 0.78f, 0.20f));

            // Область сетки 5x5
            Vector2 offset = config.gridCenterOffset;
            int w = config.gridWidth;
            int h = config.gridHeight;
            float totalW = w * config.cellSize.x + (w - 1) * config.cellSpacing.x;
            float totalH = h * config.cellSize.y + (h - 1) * config.cellSpacing.y;

            var gridBg = CreateUI("GridBackground", parent, new Vector2(totalW + 40f, totalH + 40f), offset, new Color(0.06f, 0.09f, 0.14f, 0.92f));
            var cellsContainer = CreateUI("CellsContainer", gridBg.transform, new Vector2(totalW, totalH), Vector2.zero, Color.clear);
            var cellsContainerRt = cellsContainer.GetComponent<RectTransform>();

            float startColX = - (w - 1) * (config.cellSize.x + config.cellSpacing.x) * 0.5f;
            float startRowY = (h - 1) * (config.cellSize.y + config.cellSpacing.y) * 0.5f;

            var createdCells = new List<GridCell>();
            int startingUnlocked = config.startingUnlockedCount;
            int index = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float posX = startColX + x * (config.cellSize.x + config.cellSpacing.x);
                    float posY = startRowY - y * (config.cellSize.y + config.cellSpacing.y);

                    var cellGo = CreateUI($"Cell_{x}_{y}", cellsContainer.transform, config.cellSize, new Vector2(posX, posY), new Color(0.13f, 0.18f, 0.26f, 0.95f));
                    var cellImg = cellGo.GetComponent<Image>();

                    var cell = cellGo.AddComponent<GridCell>();

                    // Рамка подсветки
                    var highlightGo = CreateUI("HighlightBorder", cellGo.transform, config.cellSize + new Vector2(6f, 6f), Vector2.zero, Color.clear);
                    var highlightImg = highlightGo.GetComponent<Image>();
                    highlightGo.SetActive(false);

                    // Координаты клетки
                    CreateText("Coords", cellGo.transform, new Vector2(config.cellSize.x - 10, 20), new Vector2(0, config.cellSize.y * 0.5f - 14), $"({x},{y})", 12, FontStyles.Normal, new Color(0.55f, 0.65f, 0.78f));

                    // Визуал цветка
                    var flowerVisualGo = CreateUI("FlowerVisual", cellGo.transform, new Vector2(64, 64), new Vector2(0, 12), Color.white);
                    var flowerVisual = flowerVisualGo.GetComponent<Image>();
                    flowerVisualGo.SetActive(false);

                    // Прогресс-бар
                    var progContainer = CreateUI("ProgressContainer", cellGo.transform, new Vector2(config.cellSize.x - 20, 14), new Vector2(0, -38), new Color(0.08f, 0.1f, 0.15f, 1f));
                    var fillGo = CreateUI("Fill", progContainer.transform, new Vector2(config.cellSize.x - 20, 14), Vector2.zero, new Color(0.25f, 0.75f, 0.95f, 1f));
                    var fillImg = fillGo.GetComponent<Image>();
                    fillImg.type = Image.Type.Filled;
                    fillImg.fillMethod = Image.FillMethod.Horizontal;
                    fillImg.fillAmount = 0f;
                    progContainer.SetActive(false);

                    // Бейдж статуса
                    var statusBadge = CreateText("StatusBadge", cellGo.transform, new Vector2(config.cellSize.x - 10, 24), new Vector2(0, -56), "Пусто", 14, FontStyles.Bold, Color.white);

                    // Оверлей блокировки
                    var lockOverlay = CreateUI("LockOverlay", cellGo.transform, config.cellSize, Vector2.zero, new Color(0.05f, 0.07f, 0.10f, 0.70f));
                    CreateText("LockIcon", lockOverlay.transform, new Vector2(60, 60), Vector2.zero, "[X]", 24, FontStyles.Bold, new Color(0.7f, 0.7f, 0.7f, 0.8f));
                    bool unlocked = (index < startingUnlocked);
                    lockOverlay.SetActive(!unlocked);

                    // Сериализуем поля GridCell
                    var soCell = new SerializedObject(cell);
                    soCell.FindProperty("gridX").intValue = x;
                    soCell.FindProperty("gridY").intValue = y;
                    soCell.FindProperty("isUnlocked").boolValue = unlocked;
                    soCell.FindProperty("cellBackground").objectReferenceValue = cellImg;
                    soCell.FindProperty("highlightBorder").objectReferenceValue = highlightImg;
                    soCell.FindProperty("plantVisual").objectReferenceValue = flowerVisual;
                    soCell.FindProperty("progressFill").objectReferenceValue = fillImg;
                    soCell.FindProperty("progressContainer").objectReferenceValue = progContainer;
                    soCell.FindProperty("statusBadge").objectReferenceValue = statusBadge;
                    soCell.FindProperty("lockOverlay").objectReferenceValue = lockOverlay;
                    soCell.ApplyModifiedProperties();

                    createdCells.Add(cell);
                    index++;
                }
            }

            // Сериализуем GreenhouseGrid
            var soGrid = new SerializedObject(grid);
            soGrid.FindProperty("config").objectReferenceValue = config;
            soGrid.FindProperty("cellsContainer").objectReferenceValue = cellsContainerRt;
            soGrid.ApplyModifiedProperties();
            grid.InitializeGrid(config);

            // Полка инструментов справа
            var shelfPanel = CreateUI("ToolShelfPanel", parent, new Vector2(260, totalH + 40f), new Vector2(490, offset.y), new Color(0.10f, 0.14f, 0.22f, 0.95f));
            CreateText("ShelfTitle", shelfPanel.transform, new Vector2(240, 36), new Vector2(0, (totalH + 40f) * 0.5f - 30f), "ПОЛКА ПРЕДМЕТОВ", 18, FontStyles.Bold, Color.white);
            CreateText("ShelfSubtitle", shelfPanel.transform, new Vector2(240, 24), new Vector2(0, (totalH + 40f) * 0.5f - 60f), "Клик или Drag & Drop", 13, FontStyles.Normal, new Color(0.65f, 0.75f, 0.88f));

            var shelfItemsContainer = CreateUI("ItemsList", shelfPanel.transform, new Vector2(240, totalH - 80f), new Vector2(0, -40f), Color.clear);
            var vlg = shelfItemsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // 1. Мешочек семян Маргаритки (открыт)
            var daisyPlant = (plantDb != null && plantDb.Count > 0) ? plantDb.Plants[0] : null;
            CreateShelfItem(shelfItemsContainer.transform, HandItemType.Seed, daisyPlant, true);

            // 2. Мешочек семян Лаванды (закрыт на старте, открывается через дерево улучшений)
            var lavenderPlant = (plantDb != null && plantDb.Count > 1) ? plantDb.Plants[1] : null;
            CreateShelfItem(shelfItemsContainer.transform, HandItemType.Seed, lavenderPlant, false);

            // 3. Лейка (бесплатная)
            CreateShelfItem(shelfItemsContainer.transform, HandItemType.WateringCan, null, true);

            // 4. Мешочек удобрений (платный, +15%)
            CreateShelfItem(shelfItemsContainer.transform, HandItemType.Fertilizer, null, true);

            // Плавающий текст обратной связи (Feedback)
            var feedbackLabel = CreateText("FloatingFeedback", parent, new Vector2(800, 50), new Vector2(0, 370), string.Empty, 24, FontStyles.Bold, new Color(1f, 0.85f, 0.35f));
            feedbackLabel.gameObject.SetActive(false);

            // Курсор-фолловер с иконкой и тултипом
            var cursorFollower = CreateUI("CursorFollower", parent, new Vector2(50, 50), Vector2.zero, Color.clear);
            var cursorIconGo = CreateUI("Icon", cursorFollower.transform, new Vector2(40, 40), Vector2.zero, Color.white);
            var cursorIcon = cursorIconGo.GetComponent<Image>();
            var cursorTooltip = CreateText("Tooltip", cursorFollower.transform, new Vector2(170, 30), new Vector2(100, 0), string.Empty, 14, FontStyles.Bold, Color.white);
            cursorFollower.SetActive(false);

            // Сериализуем InteractionController
            var soInter = new SerializedObject(interactionCtrl);
            soInter.FindProperty("cursorFollower").objectReferenceValue = cursorFollower.GetComponent<RectTransform>();
            soInter.FindProperty("cursorIcon").objectReferenceValue = cursorIcon;
            soInter.FindProperty("cursorTooltip").objectReferenceValue = cursorTooltip;
            soInter.FindProperty("floatingFeedbackLabel").objectReferenceValue = feedbackLabel;
            soInter.FindProperty("parentCanvas").objectReferenceValue = parent.GetComponent<Canvas>();
            soInter.FindProperty("greenhouseCoinsLabel").objectReferenceValue = coinsLabel;
            soInter.ApplyModifiedProperties();
        }

        private static GameObject CreateShelfItem(Transform parent, HandItemType type, PlantData plant, bool unlocked)
        {
            var itemGo = CreateUI($"Shelf_{type}_{(plant != null ? plant.name : "Tool")}", parent, new Vector2(230, 95), Vector2.zero, new Color(0.14f, 0.19f, 0.28f, 0.95f));
            var shelfSource = itemGo.AddComponent<ShelfItemSource>();

            // Иконка
            var iconGo = CreateUI("Icon", itemGo.transform, new Vector2(48, 48), new Vector2(-75, 5), Color.white);
            var iconImg = iconGo.GetComponent<Image>();

            // Текст названия
            var label = CreateText("Label", itemGo.transform, new Vector2(140, 40), new Vector2(30, 18), "Предмет", 16, FontStyles.Bold, Color.white);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Бейдж цены
            var priceBadge = CreateText("PriceBadge", itemGo.transform, new Vector2(140, 25), new Vector2(30, -18), "Бесплатно", 14, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
            priceBadge.alignment = TextAlignmentOptions.MidlineLeft;

            // Оверлей замка
            var lockOverlay = CreateUI("LockOverlay", itemGo.transform, new Vector2(230, 95), Vector2.zero, new Color(0.06f, 0.08f, 0.12f, 0.85f));
            CreateText("LockText", lockOverlay.transform, new Vector2(200, 30), Vector2.zero, "Закрыто в Дереве", 13, FontStyles.Bold, new Color(0.75f, 0.75f, 0.75f));
            lockOverlay.SetActive(!unlocked);

            var soSource = new SerializedObject(shelfSource);
            soSource.FindProperty("itemType").intValue = (int)type;
            soSource.FindProperty("seedPlant").objectReferenceValue = plant;
            soSource.FindProperty("isUnlocked").boolValue = unlocked;
            soSource.FindProperty("iconImage").objectReferenceValue = iconImg;
            soSource.FindProperty("labelText").objectReferenceValue = label;
            soSource.FindProperty("priceBadgeText").objectReferenceValue = priceBadge;
            soSource.FindProperty("lockOverlay").objectReferenceValue = lockOverlay;
            soSource.ApplyModifiedProperties();

            shelfSource.Setup(type, plant, unlocked);
            return itemGo;
        }

        private static void BuildShopUI(Transform parent, ShopInventory inventory, CustomerManager customerManager, PlantDatabase plantDb)
        {
            // Заголовок
            CreateText("Header", parent, new Vector2(800, 60), new Vector2(0, 460), "ТОРГОВЫЙ ЗАЛ — ЛАВКА", 34, FontStyles.Bold, Color.white);

            // Левая зона: Витрина / Стеллажи
            var shelfPanel = CreateUI("ShelfPanel", parent, new Vector2(540, 560), new Vector2(-460, -20), new Color(0.11f, 0.15f, 0.22f, 0.92f));
            CreateText("ShelfTitle", shelfPanel.transform, new Vector2(480, 40), new Vector2(0, 240), "Витрина магазина", 24, FontStyles.Bold, new Color(0.85f, 0.9f, 0.98f));
            CreateText("ShelfDesc", shelfPanel.transform, new Vector2(480, 50), new Vector2(0, 195), "Сюда попадают выращенные цветы из оранжереи. Отсюда они продаются покупателям.", 15, FontStyles.Normal, new Color(0.65f, 0.72f, 0.85f));

            var shelfSummary = CreateText("ShelfSummary", shelfPanel.transform, new Vector2(480, 280), new Vector2(0, 20), "Витрина пуста (соберите цветы в оранжерее)", 20, FontStyles.Normal, Color.white);

            var soInv = new SerializedObject(inventory);
            soInv.FindProperty("shelfSummaryLabel").objectReferenceValue = shelfSummary;
            soInv.ApplyModifiedProperties();

            // Правая зона: Прилавок и покупатель
            var counterPanel = CreateUI("CounterPanel", parent, new Vector2(680, 560), new Vector2(360, -20), new Color(0.12f, 0.17f, 0.26f, 0.95f));
            CreateText("CounterTitle", counterPanel.transform, new Vector2(600, 40), new Vector2(0, 240), "Прилавок обслуживания", 24, FontStyles.Bold, Color.white);

            var avatarGo = CreateUI("Avatar", counterPanel.transform, new Vector2(90, 90), new Vector2(-220, 120), new Color(0.95f, 0.8f, 0.65f));
            var avatarImg = avatarGo.GetComponent<Image>();

            var bubbleGo = CreateUI("Bubble", counterPanel.transform, new Vector2(440, 140), new Vector2(80, 120), new Color(0.18f, 0.25f, 0.38f, 1f));
            var custName = CreateText("Name", bubbleGo.transform, new Vector2(400, 25), new Vector2(0, 45), "Покупатель: Анна", 17, FontStyles.Bold, new Color(0.75f, 0.88f, 0.98f));
            var orderText = CreateText("OrderDetails", bubbleGo.transform, new Vector2(400, 60), new Vector2(0, -5), "«Хочу купить Маргаритку!»\nПлачу: 8 монет", 17, FontStyles.Normal, Color.white);

            // Шкала терпения
            var patCont = CreateUI("PatienceContainer", bubbleGo.transform, new Vector2(380, 12), new Vector2(0, -50), new Color(0.08f, 0.1f, 0.15f));
            var patFill = CreateUI("Fill", patCont.transform, new Vector2(380, 12), Vector2.zero, new Color(0.35f, 0.75f, 0.95f));
            var patFillImg = patFill.GetComponent<Image>();
            patFillImg.type = Image.Type.Filled;
            patFillImg.fillMethod = Image.FillMethod.Horizontal;
            patFillImg.fillAmount = 1f;

            // Кнопка продажи
            var sellBtnGo = CreateUI("Btn_Sell", counterPanel.transform, new Vector2(360, 60), new Vector2(0, -80), new Color(0.15f, 0.55f, 0.30f));
            var sellBtn = sellBtnGo.AddComponent<Button>();
            var sellBtnText = CreateText("Text", sellBtnGo.transform, new Vector2(350, 40), Vector2.zero, "Продать (+8 монет)", 20, FontStyles.Bold, Color.white);

            // Статус прилавка
            var counterStatus = CreateText("StatusLabel", counterPanel.transform, new Vector2(620, 60), new Vector2(0, -180), "Клиент ждёт у прилавка. Продайте цветок.", 17, FontStyles.Normal, new Color(0.7f, 0.8f, 0.95f));

            var soCust = new SerializedObject(customerManager);
            soCust.FindProperty("plantDatabase").objectReferenceValue = plantDb;
            soCust.FindProperty("customerBubble").objectReferenceValue = bubbleGo;
            soCust.FindProperty("customerNameLabel").objectReferenceValue = custName;
            soCust.FindProperty("orderDetailsLabel").objectReferenceValue = orderText;
            soCust.FindProperty("patienceFill").objectReferenceValue = patFillImg;
            soCust.FindProperty("serveButton").objectReferenceValue = sellBtn;
            soCust.FindProperty("serveButtonText").objectReferenceValue = sellBtnText;
            soCust.FindProperty("counterStatusLabel").objectReferenceValue = counterStatus;
            soCust.FindProperty("customerAvatar").objectReferenceValue = avatarImg;
            soCust.ApplyModifiedProperties();
        }

        private static void BuildUpgradesUI(Transform parent, UpgradeManager manager, PlantDatabase plantDb)
        {
            // Заголовок
            CreateText("Header", parent, new Vector2(800, 50), new Vector2(0, 460), "ДЕРЕВО УЛУЧШЕНИЙ", 34, FontStyles.Bold, Color.white);
            var hint = CreateText("Hint", parent, new Vector2(1000, 35), new Vector2(0, 415), "Приобретайте улучшения для открытия новых горшков и сортов цветов.", 18, FontStyles.Normal, new Color(0.75f, 0.82f, 0.95f));

            var cardsContainer = CreateUI("CardsContainer", parent, new Vector2(900, 580), new Vector2(0, 30), Color.clear);
            var vlg = cardsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var soUp = new SerializedObject(manager);
            soUp.FindProperty("plantDatabase").objectReferenceValue = plantDb;
            soUp.FindProperty("upgradesContainer").objectReferenceValue = cardsContainer.transform;
            soUp.FindProperty("statusHintLabel").objectReferenceValue = hint;
            soUp.ApplyModifiedProperties();
        }

        private static GameObject CreateUI(string name, Transform parent, Vector2 size, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 size, Vector2 anchoredPos, string text, float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void DestroyIfExists(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}
