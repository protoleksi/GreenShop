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
            const string ScenePath = "Assets/Scenes/MainGame.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

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
                soGm.FindProperty("startingIncomePerSecond").doubleValue = 0.0;
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

            // 6. Актуализируем экранный Canvas (ScreenSpaceOverlay) и навигационные кнопки
            var overlayCanvas = Object.FindAnyObjectByType<Canvas>();
            var camCtrl = Object.FindAnyObjectByType<ZoneCameraController>();
            if (camCtrl != null)
            {
                // Находим созданный EdgeButton в Оранжерее и привязываем к ZoneCameraController
                var ghEdgeBtn = greenhouseCanvas.transform.Find("Btn_GreenhouseToShop");
                if (ghEdgeBtn != null)
                {
                    var btnComp = ghEdgeBtn.GetComponent<Button>();
                    var soCam = new SerializedObject(camCtrl);
                    soCam.FindProperty("buttonGreenhouseToShop").objectReferenceValue = btnComp;
                    soCam.ApplyModifiedProperties();
                    EditorUtility.SetDirty(camCtrl);
                }

                // Находим созданный EdgeButton в Дереве улучшений и привязываем к ZoneCameraController
                var upEdgeBtn = upgradesCanvas.transform.Find("Btn_UpgradesToShop");
                if (upEdgeBtn != null)
                {
                    var btnComp = upEdgeBtn.GetComponent<Button>();
                    var soCam = new SerializedObject(camCtrl);
                    soCam.FindProperty("buttonUpgradesToShop").objectReferenceValue = btnComp;
                    soCam.ApplyModifiedProperties();
                    EditorUtility.SetDirty(camCtrl);
                }

                // Удаляем старые плавающие кнопки из NavigationPanel на экранном канвасе
                var navPanel = GameObject.Find("NavigationPanel");
                if (navPanel != null)
                {
                    var oldGhBtn = navPanel.transform.Find("Btn_GreenhouseToShop");
                    if (oldGhBtn != null)
                    {
                        Object.DestroyImmediate(oldGhBtn.gameObject);
                    }
                    var oldUpBtn = navPanel.transform.Find("Btn_UpgradesToShop");
                    if (oldUpBtn != null)
                    {
                        Object.DestroyImmediate(oldUpBtn.gameObject);
                    }

                    // Сдвигаем кнопку перехода в Улучшения влево под стеллаж (-520 px), чтобы не перекрывать кота и прилавок
                    var toUpgradesBtn = navPanel.transform.Find("Btn_ToUpgrades");
                    if (toUpgradesBtn != null)
                    {
                        var rt = toUpgradesBtn.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(-520f, 25f);
                        }
                    }
                }

                // Привязываем экранный CoinsLabel к ZoneCameraController, чтобы он скрывался в других зонах
                var oldOverlayCoins = GameObject.Find("Canvas/CoinsLabel");
                if (oldOverlayCoins != null)
                {
                    var soCam = new SerializedObject(camCtrl);
                    soCam.FindProperty("screenSpaceCoins").objectReferenceValue = oldOverlayCoins;
                    soCam.ApplyModifiedProperties();
                    EditorUtility.SetDirty(camCtrl);
                }

                camCtrl.UpdateButtonVisibility(ZoneCameraController.ZoneShop);
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
            // Загружаем сгенерированные UI спрайты
            var frameRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/frame_rect.png");
            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/circle.png");
            var circleOutlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/circle_outline.png");
            var coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_coin.png");
            var lockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_lock.png");
            var dropIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_drop.png");
            var fertilizerIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_fertilizer.png");
            var wateringCanIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_watering_can.png");
            var seedIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_seed.png");
            var arrowRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/arrow_right.png");

            // A2. Деньги в левом верхнем углу (30, 36): иконка монеты + число (без "Баланс" и "монет")
            var coinsHudGo = CreateTopLeftUI("CoinsHUD", parent, new Vector2(200, 44), new Vector2(30, 36), Color.clear);
            var coinIconGo = CreateTopLeftUI("CoinIcon", coinsHudGo.transform, new Vector2(36, 36), Vector2.zero, Color.white, coinIconSprite);
            var coinsLabel = CreateTopLeftText("GreenhouseCoins", coinsHudGo.transform, new Vector2(150, 36), new Vector2(46, 0), "15", 28, FontStyles.Bold, new Color(0.98f, 0.78f, 0.20f), TextAlignmentOptions.MidlineLeft);

            // A3. Тонкая контурная рамка вокруг сетки 5x5: x 38->1402, y 111->1034 (1364x923 px)
            var gridFrame = CreateTopLeftUI("GridFrame", parent, new Vector2(1364, 923), new Vector2(38, 111), new Color(1f, 1f, 1f, 0.35f), frameRectSprite, Image.Type.Sliced);
            gridFrame.GetComponent<Image>().raycastTarget = false;

            // Контейнер клеток 5х5
            var cellsContainer = CreateTopLeftUI("CellsContainer", parent, new Vector2(1920, 1080), Vector2.zero, Color.clear);
            cellsContainer.GetComponent<Image>().raycastTarget = false;
            var cellsContainerRt = cellsContainer.GetComponent<RectTransform>();

            int w = config.gridWidth;
            int h = config.gridHeight;
            var createdCells = new List<GridCell>();
            int startingUnlocked = config.startingUnlockedCount;
            int index = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Позиция клетки: первая (109, 125), шаг X = 262, шаг Y = 186
                    Vector2 cellPos = new Vector2(config.firstCellTopLeft.x + x * config.cellStep.x, config.firstCellTopLeft.y + y * config.cellStep.y);

                    var cellGo = CreateTopLeftUI($"Cell_{x}_{y}", cellsContainer.transform, config.cellSize, cellPos, new Color(0.12f, 0.16f, 0.22f, 0.95f), frameRectSprite, Image.Type.Sliced);
                    var cellImg = cellGo.GetComponent<Image>();
                    var cell = cellGo.AddComponent<GridCell>();

                    // Рамка подсветки (валидно / невалидно)
                    var highlightGo = CreateCenteredUI("HighlightBorder", cellGo.transform, config.cellSize + new Vector2(4f, 4f), Vector2.zero, Color.clear, frameRectSprite, Image.Type.Sliced);
                    var highlightImg = highlightGo.GetComponent<Image>();
                    highlightGo.SetActive(false);

                    // Визуал цветка
                    var flowerVisualGo = CreateCenteredUI("FlowerVisual", cellGo.transform, new Vector2(64, 64), new Vector2(0, 5), Color.white);
                    var flowerVisual = flowerVisualGo.GetComponent<Image>();
                    flowerVisualGo.SetActive(false);

                    // Оверлей блокировки (затемнен, значок замка по центру, без текста [X])
                    var lockOverlay = CreateCenteredUI("LockOverlay", cellGo.transform, config.cellSize, Vector2.zero, new Color(0.05f, 0.07f, 0.10f, 0.80f));
                    CreateCenteredUI("LockIcon", lockOverlay.transform, new Vector2(36, 36), Vector2.zero, new Color(0.75f, 0.75f, 0.75f, 0.85f), lockIconSprite);
                    bool unlocked = (index < startingUnlocked);
                    lockOverlay.SetActive(!unlocked);

                    // Полоска времени TimeBar под клеткой (154x24 px, зазор 5 px)
                    Vector2 timeBarPos = new Vector2(cellPos.x, cellPos.y + config.cellSize.y + config.timeBarGap);
                    var timeBarGo = CreateTopLeftUI($"TimeBar_{x}_{y}", cellsContainer.transform, config.timeBarSize, timeBarPos, new Color(0.08f, 0.10f, 0.15f, 0.95f), frameRectSprite, Image.Type.Sliced);

                    // Заполнение прогресса
                    var fillGo = CreateCenteredUI("Fill", timeBarGo.transform, config.timeBarSize - new Vector2(4f, 4f), Vector2.zero, new Color(0.31f, 0.76f, 0.97f, 1f));
                    var fillImg = fillGo.GetComponent<Image>();
                    fillImg.type = Image.Type.Filled;
                    fillImg.fillMethod = Image.FillMethod.Horizontal;
                    fillImg.fillAmount = 0f;

                    // Оставшееся время (м:сс)
                    var timeLabel = CreateCenteredText("TimeLabel", timeBarGo.transform, config.timeBarSize, Vector2.zero, "0:00", 14, FontStyles.Bold, Color.white);

                    // Иконка капли (пульсирует при NeedsWater)
                    var waterDropGo = CreateCenteredUI("WaterDrop", timeBarGo.transform, new Vector2(20, 20), Vector2.zero, Color.white, dropIconSprite);
                    var waterDropImg = waterDropGo.GetComponent<Image>();
                    waterDropGo.SetActive(false);

                    // Надпись «Готово!»
                    var statusLabel = CreateCenteredText("StatusLabel", timeBarGo.transform, config.timeBarSize, Vector2.zero, "Готово!", 14, FontStyles.Bold, new Color(0.48f, 0.82f, 0.42f, 1f));
                    statusLabel.gameObject.SetActive(false);

                    var timeBar = timeBarGo.AddComponent<TimeBar>();
                    var soTb = new SerializedObject(timeBar);
                    soTb.FindProperty("background").objectReferenceValue = timeBarGo.GetComponent<Image>();
                    soTb.FindProperty("fillImage").objectReferenceValue = fillImg;
                    soTb.FindProperty("timeLabel").objectReferenceValue = timeLabel;
                    soTb.FindProperty("waterDropIcon").objectReferenceValue = waterDropImg;
                    soTb.FindProperty("statusLabel").objectReferenceValue = statusLabel;
                    soTb.ApplyModifiedProperties();
                    timeBar.SetHidden();

                    // Сериализуем поля GridCell
                    var soCell = new SerializedObject(cell);
                    soCell.FindProperty("gridX").intValue = x;
                    soCell.FindProperty("gridY").intValue = y;
                    soCell.FindProperty("isUnlocked").boolValue = unlocked;
                    soCell.FindProperty("cellBackground").objectReferenceValue = cellImg;
                    soCell.FindProperty("highlightBorder").objectReferenceValue = highlightImg;
                    soCell.FindProperty("plantVisual").objectReferenceValue = flowerVisual;
                    soCell.FindProperty("timeBar").objectReferenceValue = timeBar;
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

            // A4. Правая колонка предметов (центры X ≈ 1617)
            float colCenterX = 1617f;

            // 1. Пакет удобрений (153х140, верх y 201)
            Vector2 fertPos = new Vector2(colCenterX - 153f * 0.5f, 201f);
            var fertCard = CreateTopLeftUI("Shelf_Fertilizer", parent, new Vector2(153, 140), fertPos, new Color(0.12f, 0.16f, 0.22f, 0.95f), frameRectSprite, Image.Type.Sliced);
            var fertLabel = CreateCenteredText("Label", fertCard.transform, new Vector2(145, 26), new Vector2(0, 42), "Пакет удобрений", 14, FontStyles.Bold, Color.white);
            var fertIcon = CreateCenteredUI("Icon", fertCard.transform, new Vector2(48, 48), new Vector2(0, 5), Color.white, fertilizerIconSprite);
            var fertPrice = CreateCenteredText("PriceBadge", fertCard.transform, new Vector2(145, 22), new Vector2(0, -42), "5", 14, FontStyles.Bold, new Color(0.98f, 0.78f, 0.20f));

            var fertSource = fertCard.AddComponent<ShelfItemSource>();
            var soFert = new SerializedObject(fertSource);
            soFert.FindProperty("itemType").intValue = (int)HandItemType.Fertilizer;
            soFert.FindProperty("isUnlocked").boolValue = true;
            soFert.FindProperty("iconImage").objectReferenceValue = fertIcon.GetComponent<Image>();
            soFert.FindProperty("labelText").objectReferenceValue = fertLabel;
            soFert.FindProperty("priceBadgeText").objectReferenceValue = fertPrice;
            soFert.ApplyModifiedProperties();
            fertSource.Setup(HandItemType.Fertilizer, null, true);

            // 2. Лейка (153х140, верх y 389, бесплатно, без цены)
            Vector2 waterPos = new Vector2(colCenterX - 153f * 0.5f, 389f);
            var waterCard = CreateTopLeftUI("Shelf_WateringCan", parent, new Vector2(153, 140), waterPos, new Color(0.12f, 0.16f, 0.22f, 0.95f), frameRectSprite, Image.Type.Sliced);
            var waterLabel = CreateCenteredText("Label", waterCard.transform, new Vector2(145, 26), new Vector2(0, 45), "Лейка", 16, FontStyles.Bold, Color.white);
            var waterIcon = CreateCenteredUI("Icon", waterCard.transform, new Vector2(52, 52), new Vector2(0, -8), Color.white, wateringCanIconSprite);

            var waterSource = waterCard.AddComponent<ShelfItemSource>();
            var soWater = new SerializedObject(waterSource);
            soWater.FindProperty("itemType").intValue = (int)HandItemType.WateringCan;
            soWater.FindProperty("isUnlocked").boolValue = true;
            soWater.FindProperty("iconImage").objectReferenceValue = waterIcon.GetComponent<Image>();
            soWater.FindProperty("labelText").objectReferenceValue = waterLabel;
            soWater.ApplyModifiedProperties();
            waterSource.Setup(HandItemType.WateringCan, null, true);

            // A5. Колесо семян SeedWheel (диаметр 318, центр (1617, 755))
            var wheelGo = new GameObject("SeedWheel", typeof(RectTransform));
            wheelGo.transform.SetParent(parent, false);
            var wheelRt = wheelGo.GetComponent<RectTransform>();
            wheelRt.anchorMin = new Vector2(0f, 1f);
            wheelRt.anchorMax = new Vector2(0f, 1f);
            wheelRt.pivot = new Vector2(0.5f, 0.5f);
            wheelRt.sizeDelta = new Vector2(318f, 318f);
            wheelRt.anchoredPosition = new Vector2(colCenterX, -755f);

            // Круглый фон и контурная рамка (raycastTarget = false)
            var bgGo = CreateCenteredUI("CircleBg", wheelGo.transform, new Vector2(318, 318), Vector2.zero, new Color(0.08f, 0.10f, 0.15f, 0.95f), circleSprite);
            bgGo.GetComponent<Image>().raycastTarget = false;

            var outlineGo = CreateCenteredUI("CircleOutline", wheelGo.transform, new Vector2(318, 318), Vector2.zero, new Color(1f, 1f, 1f, 0.40f), circleOutlineSprite);
            outlineGo.GetComponent<Image>().raycastTarget = false;

            var seedWheel = wheelGo.AddComponent<SeedWheel>();

            // 3 сектора по 120°:
            // 1 — Маргаритка (справа: 15°..135°, биссектриса 75°, открыта со старта)
            // 2 — Лаванда (слева-сверху: 255°..375°/15°, биссектриса 315°, закрыта)
            // 3 — Подсолнух (снизу: 135°..255°, биссектриса 195°, закрыт)
            var daisyPlant = (plantDb != null && plantDb.Count > 0) ? plantDb.Plants[0] : null;
            var lavenderPlant = (plantDb != null && plantDb.Count > 1) ? plantDb.Plants[1] : null;
            var sunflowerPlant = (plantDb != null && plantDb.Count > 2) ? plantDb.Plants[2] : null;

            CreateWheelSector(wheelGo.transform, wheelRt, seedWheel, daisyPlant, true, 15f, 135f, 75f, seedIconSprite, lockIconSprite, circleSprite);
            CreateWheelSector(wheelGo.transform, wheelRt, seedWheel, lavenderPlant, false, 255f, 375f, 315f, seedIconSprite, lockIconSprite, circleSprite);
            CreateWheelSector(wheelGo.transform, wheelRt, seedWheel, sunflowerPlant, false, 135f, 255f, 195f, seedIconSprite, lockIconSprite, circleSprite);

            // A6. Кнопка возврата в Торговый зал EdgeButton (правый край, x 1876->1920, ширина 44, высота 1080, треугольник ▶)
            var edgeBtnGo = CreateTopLeftUI("Btn_GreenhouseToShop", parent, new Vector2(44, 1080), new Vector2(1876, 0), new Color(0.12f, 0.16f, 0.22f, 0.85f));
            var edgeArrow = CreateCenteredUI("ArrowIcon", edgeBtnGo.transform, new Vector2(24, 36), Vector2.zero, Color.white, arrowRightSprite);

            var edgeBtn = edgeBtnGo.AddComponent<Button>();
            var edgeNav = edgeBtnGo.AddComponent<EdgeButton>();
            var soEdge = new SerializedObject(edgeNav);
            soEdge.FindProperty("arrowIcon").objectReferenceValue = edgeArrow.GetComponent<Image>();
            soEdge.ApplyModifiedProperties();

            // Плавающий текст обратной связи (Feedback)
            var feedbackLabel = CreateTopLeftText("FloatingFeedback", parent, new Vector2(800, 50), new Vector2(560, 370), string.Empty, 24, FontStyles.Bold, new Color(1f, 0.85f, 0.35f));
            feedbackLabel.gameObject.SetActive(false);

            // Курсор-фолловер с иконкой и тултипом
            var cursorFollower = CreateCenteredUI("CursorFollower", parent, new Vector2(50, 50), Vector2.zero, Color.clear);
            var cfImg = cursorFollower.GetComponent<Image>();
            cfImg.raycastTarget = false;

            var cfCg = cursorFollower.AddComponent<CanvasGroup>();
            cfCg.blocksRaycasts = false;
            cfCg.interactable = false;

            var cursorIconGo = CreateCenteredUI("Icon", cursorFollower.transform, new Vector2(40, 40), Vector2.zero, Color.white);
            var cursorIcon = cursorIconGo.GetComponent<Image>();
            cursorIcon.raycastTarget = false;

            var cursorTooltip = CreateCenteredText("Tooltip", cursorFollower.transform, new Vector2(170, 30), new Vector2(100, 0), string.Empty, 14, FontStyles.Bold, Color.white);
            cursorTooltip.raycastTarget = false;

            cursorFollower.SetActive(false);

            // Сериализуем InteractionController
            var soInter = new SerializedObject(interactionCtrl);
            soInter.FindProperty("cursorFollower").objectReferenceValue = cursorFollower.GetComponent<RectTransform>();
            soInter.FindProperty("cursorIcon").objectReferenceValue = cursorIcon;
            soInter.FindProperty("cursorTooltip").objectReferenceValue = cursorTooltip;
            soInter.FindProperty("floatingFeedbackLabel").objectReferenceValue = feedbackLabel;
            soInter.FindProperty("parentCanvas").objectReferenceValue = parent.GetComponent<Canvas>();
            soInter.FindProperty("greenhouseCoinsLabel").objectReferenceValue = coinsLabel;
            soInter.FindProperty("fertilizerSprite").objectReferenceValue = fertilizerIconSprite;
            soInter.FindProperty("wateringCanSprite").objectReferenceValue = wateringCanIconSprite;
            soInter.FindProperty("seedSprite").objectReferenceValue = seedIconSprite;
            soInter.ApplyModifiedProperties();
        }

        private static GameObject CreateWheelSector(Transform parentWheel, RectTransform wheelRt, SeedWheel wheel, PlantData plant, bool unlocked, float startAngle, float endAngle, float bisectorAngle, Sprite seedSprite, Sprite lockSprite, Sprite circleSprite)
        {
            string plantName = plant != null ? plant.PlantName : "Сорт";
            var sectorGo = CreateCenteredUI($"Sector_{plantName}", parentWheel, new Vector2(318, 318), Vector2.zero, Color.clear);
            var sectorImg = sectorGo.GetComponent<Image>();
            sectorImg.raycastTarget = true;

            // Заполненный сегмент круга (Radial360)
            var fillGo = CreateCenteredUI("Fill", sectorGo.transform, new Vector2(318, 318), Vector2.zero, unlocked ? new Color(0.12f, 0.16f, 0.24f, 0.95f) : new Color(0.08f, 0.10f, 0.15f, 0.85f), circleSprite);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillClockwise = true;
            fillImg.fillAmount = 1f / 3f;
            fillImg.raycastTarget = false;
            fillGo.transform.localRotation = Quaternion.Euler(0f, 0f, -startAngle);

            // Разделительная линия в начале сектора
            var dividerGo = CreateCenteredUI("Divider", sectorGo.transform, new Vector2(2f, 159f), Vector2.zero, new Color(1f, 1f, 1f, 0.35f));
            var divRt = dividerGo.GetComponent<RectTransform>();
            divRt.pivot = new Vector2(0.5f, 0f);
            divRt.anchoredPosition = Vector2.zero;
            dividerGo.GetComponent<Image>().raycastTarget = false;
            dividerGo.transform.localRotation = Quaternion.Euler(0f, 0f, -startAngle);

            // Содержимое сектора (не повёрнуто, размещено по биссектрисе на радиусе 85 px)
            float rad = bisectorAngle * Mathf.Deg2Rad;
            float dist = 85f;
            Vector2 contentPos = new Vector2(dist * Mathf.Sin(rad), dist * Mathf.Cos(rad));

            var contentGo = CreateCenteredUI("Content", sectorGo.transform, new Vector2(90, 70), contentPos, Color.clear);
            contentGo.GetComponent<Image>().raycastTarget = false;

            var nameText = CreateCenteredText("Name", contentGo.transform, new Vector2(90, 18), new Vector2(0, 18), plantName, 12, FontStyles.Bold, Color.white);
            nameText.raycastTarget = false;

            var iconGo = CreateCenteredUI("Icon", contentGo.transform, new Vector2(22, 22), Vector2.zero, Color.white);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.raycastTarget = false;
            if (plant != null && plant.PlantIcon != null)
            {
                iconImg.sprite = plant.PlantIcon;
                iconImg.color = unlocked ? Color.white : Color.gray;
            }
            else if (plant != null)
            {
                iconImg.color = unlocked ? plant.PlantColor : Color.gray;
            }

            var priceText = CreateCenteredText("Price", contentGo.transform, new Vector2(90, 16), new Vector2(0, -18), unlocked ? $"{plant?.SeedPrice}" : "Закрыто", 12, FontStyles.Bold, unlocked ? new Color(0.98f, 0.85f, 0.35f) : Color.gray);
            priceText.raycastTarget = false;

            // Иконка замка для заблокированных секторов
            var lockOverlay = CreateCenteredUI("LockOverlay", contentGo.transform, new Vector2(22, 22), Vector2.zero, new Color(0.85f, 0.85f, 0.85f, 0.95f), lockSprite);
            lockOverlay.GetComponent<Image>().raycastTarget = false;
            lockOverlay.SetActive(!unlocked);

            // Компонент сектора колеса семян
            var sector = sectorGo.AddComponent<SeedWheelSector>();
            var soSec = new SerializedObject(sector);
            soSec.FindProperty("plant").objectReferenceValue = plant;
            soSec.FindProperty("isUnlocked").boolValue = unlocked;
            soSec.FindProperty("startAngle").floatValue = startAngle;
            soSec.FindProperty("endAngle").floatValue = endAngle;
            soSec.FindProperty("minRadius").floatValue = 15f;
            soSec.FindProperty("maxRadius").floatValue = 158f;
            soSec.FindProperty("wheelRect").objectReferenceValue = wheelRt;
            soSec.FindProperty("fillImage").objectReferenceValue = fillImg;
            soSec.FindProperty("iconImage").objectReferenceValue = iconImg;
            soSec.FindProperty("nameLabel").objectReferenceValue = nameText;
            soSec.FindProperty("priceLabel").objectReferenceValue = priceText;
            soSec.FindProperty("lockOverlay").objectReferenceValue = lockOverlay;
            soSec.ApplyModifiedProperties();

            wheel.RegisterSector(sector);
            return sectorGo;
        }

        private static void BuildShopUI(Transform parent, ShopInventory inventory, CustomerManager customerManager, PlantDatabase plantDb)
        {
            // 1. Загрузка ассетов и спрайтов
            var shopBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Торговый зал.jpg");
            var frameRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/frame_rect.png");
            var tooltipPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/tooltip_panel.png");
            var customerSilhouette = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/customer_silhouette.png");
            var flowerPotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_flower_pot.png");

            // Загрузка или создание CustomerConfig
            const string CustConfigPath = "Assets/Data/CustomerConfig.asset";
            var custConfig = AssetDatabase.LoadAssetAtPath<CustomerConfig>(CustConfigPath);
            if (custConfig == null)
            {
                custConfig = ScriptableObject.CreateInstance<CustomerConfig>();
                AssetDatabase.CreateAsset(custConfig, CustConfigPath);
                AssetDatabase.SaveAssets();
            }

            // 2. Фон Торгового зала (1920x1080)
            var bgGo = CreateCenteredUI("Background", parent, new Vector2(1920, 1080), Vector2.zero, Color.white, shopBgSprite);
            bgGo.GetComponent<Image>().raycastTarget = false;

            // 3. Витрина на стеллаже (ShelfSlots) — 12 позиций (4 ряда x 3 колонки)
            var shelfGo = new GameObject("ShelfSlots", typeof(RectTransform));
            shelfGo.transform.SetParent(parent, false);
            var shelfRt = shelfGo.GetComponent<RectTransform>();
            shelfRt.anchorMin = new Vector2(0.5f, 0.5f);
            shelfRt.anchorMax = new Vector2(0.5f, 0.5f);
            shelfRt.pivot = new Vector2(0.5f, 0.5f);
            shelfRt.sizeDelta = new Vector2(1920f, 1080f);
            shelfRt.anchoredPosition = Vector2.zero;

            var shelfSlots = shelfGo.AddComponent<ShelfSlots>();

            // Координаты слотов (px от левого верхнего угла):
            // Col X: 204, 443, 672 -> canvas X (px - 960): -756, -517, -288
            // Row Y: 367, 600, 823, 1048 -> canvas Y (540 - py): 173, -60, -283, -508
            float[] colX = new float[] { -756f, -517f, -288f };
            float[] rowY = new float[] {  173f,  -60f, -283f, -508f };

            Transform[] slotRoots = new Transform[12];
            Image[] flowerImages = new Image[12];

            int slotIdx = 0;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    Vector2 slotPos = new Vector2(colX[c], rowY[r]);
                    var slotRoot = new GameObject($"Slot_{r}_{c}", typeof(RectTransform));
                    slotRoot.transform.SetParent(shelfGo.transform, false);
                    var srt = slotRoot.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0.5f, 0.5f);
                    srt.anchorMax = new Vector2(0.5f, 0.5f);
                    srt.pivot = new Vector2(0.5f, 0f); // основание горшка
                    srt.sizeDelta = new Vector2(170f, 170f);
                    srt.anchoredPosition = slotPos;

                    // Цветок
                    var flwGo = CreateCenteredUI("FlowerVisual", slotRoot.transform, new Vector2(150f, 150f), new Vector2(0f, 75f), Color.white, flowerPotSprite);
                    var flwImg = flwGo.GetComponent<Image>();
                    flwImg.raycastTarget = false;
                    flwGo.SetActive(false);

                    slotRoots[slotIdx] = slotRoot.transform;
                    flowerImages[slotIdx] = flwImg;
                    slotIdx++;
                }
            }
            shelfSlots.SetupSlots(slotRoots, null, flowerImages, plantDb, flowerPotSprite);

            // 4. Опорные точки перемещения покупателя (DoorPoint, CounterPoint, ExitPoint)
            // Дверь: px (1800, 880) -> canvas (840, -340)
            // Прилавок: px (1330, 880) -> canvas (370, -340)
            var doorPoint = CreateEmptyPoint("DoorPoint", parent, new Vector2(840f, -340f));
            var counterPoint = CreateEmptyPoint("CounterPoint", parent, new Vector2(370f, -340f));
            var exitPoint = CreateEmptyPoint("ExitPoint", parent, new Vector2(840f, -340f));

            // 5. Покупатель (Customer) — рендерится ДО прилавка
            var custGo = new GameObject("Customer", typeof(RectTransform));
            custGo.transform.SetParent(parent, false);
            var custRt = custGo.GetComponent<RectTransform>();
            custRt.anchorMin = new Vector2(0.5f, 0.5f);
            custRt.anchorMax = new Vector2(0.5f, 0.5f);
            custRt.pivot = new Vector2(0.5f, 0f); // ноги на полу
            custRt.sizeDelta = new Vector2(180f, 420f);
            custRt.anchoredPosition = new Vector2(840f, -340f);

            var custVisual = CreateCenteredUI("Visual", custGo.transform, new Vector2(180f, 420f), new Vector2(0f, 210f), new Color(0.35f, 0.55f, 0.75f, 1f), customerSilhouette);
            var custImg = custVisual.GetComponent<Image>();
            custImg.raycastTarget = false;

            var customerComp = custGo.AddComponent<Customer>();
            var soCustomer = new SerializedObject(customerComp);
            soCustomer.FindProperty("customerImage").objectReferenceValue = custImg;
            soCustomer.FindProperty("visualRoot").objectReferenceValue = custVisual.transform;
            soCustomer.FindProperty("config").objectReferenceValue = custConfig;
            soCustomer.ApplyModifiedProperties();
            custGo.SetActive(false);

            // 6. Передний план / Окклюзия прилавка (CounterOcclusion)
            // Покупатель визуально стоит за прилавком: прилавок (x >= 804, y >= 886) и касса (x 1550-1860, y 702-1002) перекрывают его ноги
            var occlRoot = new GameObject("CounterOcclusion", typeof(RectTransform));
            occlRoot.transform.SetParent(parent, false);
            var occlRt = occlRoot.GetComponent<RectTransform>();
            occlRt.anchorMin = new Vector2(0.5f, 0.5f);
            occlRt.anchorMax = new Vector2(0.5f, 0.5f);
            occlRt.pivot = new Vector2(0.5f, 0.5f);
            occlRt.sizeDelta = new Vector2(1920f, 1080f);
            occlRt.anchoredPosition = Vector2.zero;

            // 6.1 Прилавок: x >= 804, y >= 886
            // X: 804..1920 (width 1116, center px 1362 -> canvas X 402)
            // Y: 886..1080 (height 194, center px 983 -> canvas Y -443)
            var counterCutout = CreateCutoutMask("CounterCutout", occlRoot.transform, new Vector2(1116f, 194f), new Vector2(402f, -443f), shopBgSprite);

            // 6.2 Касса: x 1550..1860, y 702..1002
            // X: 1550..1860 (width 310, center px 1705 -> canvas X 745)
            // Y: 702..1002 (height 300, center px 852 -> canvas Y -312)
            var registerCutout = CreateCutoutMask("RegisterCutout", occlRoot.transform, new Vector2(310f, 300f), new Vector2(745f, -312f), shopBgSprite);

            // 7. Пузырь заказа (OrderBubble) — над головой покупателя
            // Центр px (1330, 380) -> canvas (370, 160), размер 150x130
            var bubbleGo = CreateCenteredUI("OrderBubble", parent, new Vector2(150f, 130f), new Vector2(370f, 160f), new Color(0.12f, 0.16f, 0.24f, 0.96f), tooltipPanelSprite, Image.Type.Sliced);
            bubbleGo.GetComponent<Image>().raycastTarget = false;

            // Иконка цветка в пузыре
            var bubbleIconGo = CreateCenteredUI("PlantIcon", bubbleGo.transform, new Vector2(58f, 58f), new Vector2(0f, 20f), Color.white, flowerPotSprite);
            var bubbleIconImg = bubbleIconGo.GetComponent<Image>();
            bubbleIconImg.raycastTarget = false;

            // Индикатор терпения
            var patBg = CreateCenteredUI("PatienceBg", bubbleGo.transform, new Vector2(120f, 10f), new Vector2(0f, -24f), new Color(0.08f, 0.10f, 0.15f, 0.95f), frameRectSprite, Image.Type.Sliced);
            var patFill = CreateCenteredUI("Fill", patBg.transform, new Vector2(116f, 6f), Vector2.zero, new Color(0.29f, 0.87f, 0.50f, 1f));
            var patFillImg = patFill.GetComponent<Image>();
            patFillImg.type = Image.Type.Filled;
            patFillImg.fillMethod = Image.FillMethod.Horizontal;
            patFillImg.fillAmount = 1f;
            patFillImg.raycastTarget = false;

            // Пометка «нет в наличии»
            var outOfStockText = CreateCenteredText("OutOfStock", bubbleGo.transform, new Vector2(140f, 20f), new Vector2(0f, -44f), "<color=#f87171>нет в наличии</color>", 12, FontStyles.Normal, Color.white);

            var orderBubbleComp = bubbleGo.AddComponent<OrderBubble>();
            var soBubble = new SerializedObject(orderBubbleComp);
            soBubble.FindProperty("bubbleRoot").objectReferenceValue = bubbleGo;
            soBubble.FindProperty("plantIcon").objectReferenceValue = bubbleIconImg;
            soBubble.FindProperty("patienceFill").objectReferenceValue = patFillImg;
            soBubble.FindProperty("outOfStockLabel").objectReferenceValue = outOfStockText;
            soBubble.ApplyModifiedProperties();
            bubbleGo.SetActive(false);

            // 8. Кнопки кассы (CounterButtons) — столбик у прилавка
            // Центр px (1290, 960) -> canvas (330, -420), две кнопки 240x56, зазор 12
            var buttonsRoot = new GameObject("CounterButtons", typeof(RectTransform));
            buttonsRoot.transform.SetParent(parent, false);
            var btnRt = buttonsRoot.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(240f, 124f);
            btnRt.anchoredPosition = new Vector2(330f, -420f);

            // Кнопка «Продать» (сверху, y = +34)
            var sellBtnGo = CreateCenteredUI("Btn_Sell", buttonsRoot.transform, new Vector2(240f, 56f), new Vector2(0f, 34f), new Color(0.18f, 0.65f, 0.32f, 1f), tooltipPanelSprite, Image.Type.Sliced);
            var sellBtn = sellBtnGo.AddComponent<Button>();
            var sellBtnImg = sellBtnGo.GetComponent<Image>();
            var sellText = CreateCenteredText("Text", sellBtnGo.transform, new Vector2(230f, 40f), Vector2.zero, "Продать (+8 🪙)", 17, FontStyles.Bold, Color.white);

            // Кнопка «Отказать» (снизу, y = -34)
            var refuseBtnGo = CreateCenteredUI("Btn_Refuse", buttonsRoot.transform, new Vector2(240f, 56f), new Vector2(0f, -34f), new Color(0.40f, 0.45f, 0.50f, 0.9f), tooltipPanelSprite, Image.Type.Sliced);
            var refuseBtn = refuseBtnGo.AddComponent<Button>();
            var refuseBtnImg = refuseBtnGo.GetComponent<Image>();
            var refuseText = CreateCenteredText("Text", refuseBtnGo.transform, new Vector2(230f, 40f), Vector2.zero, "Отказать", 16, FontStyles.Normal, Color.white);

            var counterButtonsComp = buttonsRoot.AddComponent<CounterButtons>();
            var soButtons = new SerializedObject(counterButtonsComp);
            soButtons.FindProperty("buttonsRoot").objectReferenceValue = buttonsRoot;
            soButtons.FindProperty("sellButton").objectReferenceValue = sellBtn;
            soButtons.FindProperty("refuseButton").objectReferenceValue = refuseBtn;
            soButtons.FindProperty("sellButtonText").objectReferenceValue = sellText;
            soButtons.FindProperty("refuseButtonText").objectReferenceValue = refuseText;
            soButtons.FindProperty("sellButtonBg").objectReferenceValue = sellBtnImg;
            soButtons.FindProperty("refuseButtonBg").objectReferenceValue = refuseBtnImg;
            soButtons.ApplyModifiedProperties();
            buttonsRoot.SetActive(false);

            // 9. Настройка CustomerManager
            customerManager.SetupReferences(custConfig, customerComp, orderBubbleComp, counterButtonsComp,
                doorPoint, counterPoint, exitPoint, plantDb);
        }

        private static void BuildUpgradesUI(Transform parent, UpgradeManager manager, PlantDatabase plantDb)
        {
            // 1. Загружаем спрайты
            var frameRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/frame_rect.png");
            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/circle.png");
            var availableRingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/node_ring_available.png");
            var coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_coin.png");
            var lockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_lock.png");
            var arrowUpSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/arrow_up.png");
            var tooltipPointerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/tooltip_pointer.png");
            var flowerPotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_flower_pot.png");
            var flowerGraySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/icon_flower_gray.png");
            var tooltipPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/tooltip_panel.png");

            // 2. Загружаем или создаём TreeStyle
            var style = AssetDatabase.LoadAssetAtPath<TreeStyle>("Assets/Data/TreeStyle.asset");
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<TreeStyle>();
                AssetDatabase.CreateAsset(style, "Assets/Data/TreeStyle.asset");
                AssetDatabase.SaveAssets();
            }

            // B1 & B5. Интерактивная область карты (MapViewport: 1920x1030 с маской отсечения)
            // Создаем первым дочерним элементом канваса, чтобы фон и карта были снизу
            var viewportGo = CreateTopLeftUI("MapViewport", parent, new Vector2(1920, 1030), new Vector2(0, 50), Color.clear);
            viewportGo.GetComponent<Image>().raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();

            // Контейнер карты (MapContent)
            var contentGo = new GameObject("MapContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(TreeBackgroundGraphic));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(1920f, 1030f);
            contentRt.anchoredPosition = Vector2.zero;

            // Двухцветный фон (РОСТ: зеленый, УЮТ и ТОРГОВЛЯ: серый)
            var bgGraphic = contentGo.GetComponent<TreeBackgroundGraphic>();
            bgGraphic.raycastTarget = false;
            bgGraphic.SetStyle(style);

            // Крупные подписи регионов
            var lblGrowth = CreateTopLeftText("Label_Growth", contentGo.transform, new Vector2(200, 50), new Vector2(422f - 100f, 772f - 25f), "РОСТ", 32, FontStyles.Bold, new Color(0.10f, 0.22f, 0.12f, 0.75f));
            lblGrowth.raycastTarget = false;
            var lblComfort = CreateTopLeftText("Label_Comfort", contentGo.transform, new Vector2(200, 50), new Vector2(1114f - 100f, 608f - 25f), "УЮТ", 32, FontStyles.Bold, new Color(0.35f, 0.35f, 0.35f, 0.75f));
            lblComfort.raycastTarget = false;
            var lblTrade = CreateTopLeftText("Label_Trade", contentGo.transform, new Vector2(260, 50), new Vector2(1613f - 130f, 917f - 25f), "ТОРГОВЛЯ", 32, FontStyles.Bold, new Color(0.35f, 0.35f, 0.35f, 0.75f));
            lblTrade.raycastTarget = false;

            // Контейнер линий связей (рендерится под узлами)
            var linesContainer = new GameObject("LinesContainer", typeof(RectTransform));
            linesContainer.transform.SetParent(contentGo.transform, false);
            var linesRt = linesContainer.GetComponent<RectTransform>();
            linesRt.anchorMin = Vector2.zero;
            linesRt.anchorMax = Vector2.one;
            linesRt.sizeDelta = Vector2.zero;
            linesRt.anchoredPosition = Vector2.zero;

            // Контейнер узлов
            var nodesContainer = new GameObject("NodesContainer", typeof(RectTransform));
            nodesContainer.transform.SetParent(contentGo.transform, false);
            var nodesRt = nodesContainer.GetComponent<RectTransform>();
            nodesRt.anchorMin = Vector2.zero;
            nodesRt.anchorMax = Vector2.one;
            nodesRt.sizeDelta = Vector2.zero;
            nodesRt.anchoredPosition = Vector2.zero;

            // B4. Тултип (плашка 263x115 с хвостиком)
            var tooltip = CreateTooltipUI(contentGo.transform, style, tooltipPanelSprite, frameRectSprite, tooltipPointerSprite, coinIconSprite);

            // B1. Верхняя полоса на всю ширину (1920x50, y=0..50) с контурным треугольником ▲
            var topBarGo = CreateTopLeftUI("Btn_UpgradesToShop", parent, new Vector2(1920, 50), Vector2.zero, new Color(0.20f, 0.22f, 0.25f, 0.98f));
            var arrowIcon = CreateCenteredUI("ArrowIcon", topBarGo.transform, new Vector2(140, 36), Vector2.zero, Color.white, arrowUpSprite);
            arrowIcon.GetComponent<Image>().raycastTarget = false;

            var topBtn = topBarGo.AddComponent<Button>();
            var edgeNav = topBarGo.AddComponent<EdgeButton>();
            var soEdge = new SerializedObject(edgeNav);
            soEdge.FindProperty("arrowIcon").objectReferenceValue = arrowIcon.GetComponent<Image>();
            soEdge.ApplyModifiedProperties();

            // B1. Счётчик монет в левом верхнем углу под полосой (24, 65): крупное число + жёлтая монета
            var coinsHudGo = CreateTopLeftUI("CoinsHUD", parent, new Vector2(240, 44), new Vector2(24, 65), Color.clear);
            coinsHudGo.GetComponent<Image>().raycastTarget = false;
            var hlg = coinsHudGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var coinsLabel = CreateTopLeftText("UpgradeCoins", coinsHudGo.transform, new Vector2(70, 40), Vector2.zero, "15", 34, FontStyles.Bold, new Color(0.08f, 0.12f, 0.10f, 1f), TextAlignmentOptions.MidlineLeft);
            coinsLabel.raycastTarget = false;
            var csf = coinsLabel.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var coinIconGo = CreateTopLeftUI("CoinIcon", coinsHudGo.transform, new Vector2(36, 36), Vector2.zero, Color.white, coinIconSprite);
            coinIconGo.GetComponent<Image>().raycastTarget = false;

            // Плавающий текст обратной связи (Feedback)
            var feedbackLabel = CreateTopLeftText("FloatingFeedback", parent, new Vector2(800, 50), new Vector2(560, 480), string.Empty, 24, FontStyles.Bold, new Color(1f, 0.85f, 0.35f));
            feedbackLabel.raycastTarget = false;
            feedbackLabel.gameObject.SetActive(false);

            // B5. Контроллер карты UpgradeMapView
            var mapView = viewportGo.AddComponent<UpgradeMapView>();

            // Создаем полный граф узлов по таблице из ТЗ
            var nodesList = GetInitialTreeNodes();
            var nodeViews = new Dictionary<string, UpgradeNodeView>();

            foreach (var nodeData in nodesList)
            {
                var view = CreateNodeUI(nodesContainer.transform, nodeData, style, circleSprite, availableRingSprite, flowerPotSprite, flowerGraySprite, lockIconSprite);
                mapView.RegisterNode(view);
                nodeViews[nodeData.id] = view;
            }

            // Создаем линии связей между родителями и детьми
            foreach (var nodeData in nodesList)
            {
                if (nodeData.prerequisiteIds == null) continue;
                if (!nodeViews.TryGetValue(nodeData.id, out var childView)) continue;

                foreach (var parentId in nodeData.prerequisiteIds)
                {
                    if (nodeViews.TryGetValue(parentId, out var parentView))
                    {
                        var line = CreateConnectionLine(linesContainer.transform, parentView, childView, style);
                        mapView.RegisterLine(line);
                    }
                }
            }

            var soMap = new SerializedObject(mapView);
            soMap.FindProperty("contentRect").objectReferenceValue = contentGo.GetComponent<RectTransform>();
            soMap.FindProperty("style").objectReferenceValue = style;
            soMap.FindProperty("tooltip").objectReferenceValue = tooltip;
            soMap.FindProperty("coinsLabel").objectReferenceValue = coinsLabel;
            soMap.FindProperty("floatingFeedbackLabel").objectReferenceValue = feedbackLabel;
            soMap.ApplyModifiedProperties();

            // Сериализуем UpgradeManager
            var soUp = new SerializedObject(manager);
            soUp.FindProperty("plantDatabase").objectReferenceValue = plantDb;
            soUp.FindProperty("upgradesContainer").objectReferenceValue = null;
            soUp.FindProperty("statusHintLabel").objectReferenceValue = null;
            soUp.ApplyModifiedProperties();
            soUp.ApplyModifiedProperties();
        }

        private static List<TreeNodeData> GetInitialTreeNodes()
        {
            var list = new List<TreeNodeData>();

            // Корень: внизу по центру (0.45, 0.985)
            list.Add(new TreeNodeData
            {
                id = "root",
                title = "Зелёная лавка",
                description = "Центр вашего цветочного бизнеса.",
                cost = 0,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(0.45f, 0.985f),
                isPlaceholder = false
            });

            // РОСТ:
            list.Add(new TreeNodeData
            {
                id = "pot_2",
                title = "Второй горшок",
                description = "Открывает дополнительный стол-горшок #2 в оранжерее.",
                cost = 20,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(0.37f, 0.80f),
                prerequisiteIds = new List<string> { "root" },
                isPlaceholder = false
            });

            list.Add(new TreeNodeData
            {
                id = "seed_lavender",
                title = "Семена Лаванды",
                description = "Разблокирует посадку Лаванды и заказы покупателей.",
                cost = 35,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(0.31f, 0.57f),
                prerequisiteIds = new List<string> { "pot_2" },
                isPlaceholder = false
            });

            list.Add(new TreeNodeData
            {
                id = "fertilizer_boost",
                title = "Минеральный концентрат",
                description = "Ускоряет базовый рост всех растений на +25%.",
                cost = 50,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(0.20f, 0.61f),
                prerequisiteIds = new List<string> { "seed_lavender" },
                isPlaceholder = false
            });

            list.Add(new TreeNodeData
            {
                id = "pot_3",
                title = "Третий горшок",
                description = "Открывает горшок #3 для одновременного выращивания.",
                cost = 65,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(0.09f, 0.63f),
                prerequisiteIds = new List<string> { "fertilizer_boost" },
                isPlaceholder = false
            });

            list.Add(new TreeNodeData
            {
                id = "growth_edge",
                title = "Новые просторы",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Growth,
                mapPosition = new Vector2(-0.03f, 0.50f),
                prerequisiteIds = new List<string> { "pot_3" },
                isPlaceholder = true
            });

            // ТОРГОВЛЯ:
            list.Add(new TreeNodeData
            {
                id = "fast_customers",
                title = "Приток покупателей",
                description = "Покупатели приходят к прилавку в 2 раза быстрее.",
                cost = 60,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.59f, 0.86f),
                prerequisiteIds = new List<string> { "root" },
                isPlaceholder = false
            });

            list.Add(new TreeNodeData
            {
                id = "t2",
                title = "Витрина сувениров",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.72f, 0.92f),
                prerequisiteIds = new List<string> { "fast_customers" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "t3",
                title = "Рекламный щит",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.79f, 0.78f),
                prerequisiteIds = new List<string> { "t2" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "t4",
                title = "Оптовые поставки",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.84f, 0.64f),
                prerequisiteIds = new List<string> { "t3" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "t5",
                title = "Торговый караван",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.85f, 0.42f),
                prerequisiteIds = new List<string> { "t4" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "t6",
                title = "Ярмарочный прилавок",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.91f, 0.86f),
                prerequisiteIds = new List<string> { "t4" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "t7",
                title = "Гильдия купцов",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(0.98f, 0.67f),
                prerequisiteIds = new List<string> { "t5", "t6" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "trade_edge",
                title = "Морской экспорт",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Trade,
                mapPosition = new Vector2(1.05f, 0.80f),
                prerequisiteIds = new List<string> { "t6" },
                isPlaceholder = true
            });

            // УЮТ:
            list.Add(new TreeNodeData
            {
                id = "u1",
                title = "Тёплый свет",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.47f, 0.62f),
                prerequisiteIds = new List<string> { "root" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "u2",
                title = "Мягкий ковёр",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.52f, 0.46f),
                prerequisiteIds = new List<string> { "u1" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "u3",
                title = "Ароматная мята",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.55f, 0.30f),
                prerequisiteIds = new List<string> { "u2" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "u6",
                title = "Уголок чая",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.56f, 0.16f),
                prerequisiteIds = new List<string> { "u3" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "u5",
                title = "Книжная полка",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.69f, 0.28f),
                prerequisiteIds = new List<string> { "u6" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "u4",
                title = "Кошачья лежанка",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.65f, 0.49f),
                prerequisiteIds = new List<string> { "u5" },
                isPlaceholder = true
            });

            list.Add(new TreeNodeData
            {
                id = "comfort_edge",
                title = "Мансарда",
                description = "Скоро в следующих обновлениях.",
                cost = 0,
                branch = TreeBranch.Comfort,
                mapPosition = new Vector2(0.52f, 0.05f),
                prerequisiteIds = new List<string> { "u6" },
                isPlaceholder = true
            });

            return list;
        }

        private static UpgradeNodeView CreateNodeUI(Transform parent, TreeNodeData data, TreeStyle style, Sprite circleSprite, Sprite ringSprite, Sprite flowerPotSprite, Sprite grayFlowerSprite, Sprite lockSprite)
        {
            bool isRoot = data.id == "root";
            float diameter = isRoot ? (style != null ? style.rootNodeDiameter : 100f) : (style != null ? style.nodeDiameter : 85f);

            var nodeGo = new GameObject($"Node_{data.id}", typeof(RectTransform), typeof(Image));
            nodeGo.transform.SetParent(parent, false);

            var rt = nodeGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = new Vector2(data.mapPosition.x * 1920f, -data.mapPosition.y * 1030f);

            var bgImg = nodeGo.GetComponent<Image>();
            bgImg.sprite = circleSprite;
            bgImg.color = style != null ? style.lockedNodeBgColor : new Color(0.46f, 0.46f, 0.46f, 1f);
            bgImg.raycastTarget = true;

            // 2. Зелёное контурное кольцо (Available)
            var ringGo = CreateCenteredUI("RingAvailable", nodeGo.transform, new Vector2(diameter + 6f, diameter + 6f), Vector2.zero, Color.white, ringSprite);
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.raycastTarget = false;
            ringGo.SetActive(false);

            // 3. Иконка цветка в горшке
            var iconGo = CreateCenteredUI("Icon", nodeGo.transform, new Vector2(diameter * 0.70f, diameter * 0.70f), Vector2.zero, Color.white, grayFlowerSprite);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.raycastTarget = false;

            // 4. Замок для заблокированных
            var lockGo = CreateCenteredUI("LockIcon", nodeGo.transform, new Vector2(24, 24), Vector2.zero, new Color(0.7f, 0.7f, 0.7f, 0.9f), lockSprite);
            var lockImg = lockGo.GetComponent<Image>();
            lockImg.raycastTarget = false;
            lockGo.SetActive(false);

            var view = nodeGo.AddComponent<UpgradeNodeView>();
            view.Setup(data, isRoot ? UpgradeNodeState.Purchased : UpgradeNodeState.Locked, style, flowerPotSprite, grayFlowerSprite);

            var so = new SerializedObject(view);
            so.FindProperty("rectTransform").objectReferenceValue = rt;
            so.FindProperty("bgImage").objectReferenceValue = bgImg;
            so.FindProperty("ringImage").objectReferenceValue = ringImg;
            so.FindProperty("iconImage").objectReferenceValue = iconImg;
            so.FindProperty("lockIcon").objectReferenceValue = lockImg;
            so.FindProperty("purchasedIcon").objectReferenceValue = flowerPotSprite;
            so.FindProperty("grayIcon").objectReferenceValue = grayFlowerSprite;
            so.FindProperty("style").objectReferenceValue = style;
            so.ApplyModifiedProperties();

            return view;
        }

        private static UpgradeConnectionLine CreateConnectionLine(Transform parent, UpgradeNodeView parentView, UpgradeNodeView childView, TreeStyle style)
        {
            var lineGo = new GameObject($"Line_{parentView.Data.id}_{childView.Data.id}", typeof(RectTransform), typeof(Image), typeof(UpgradeConnectionLine));
            lineGo.transform.SetParent(parent, false);

            var line = lineGo.GetComponent<UpgradeConnectionLine>();
            line.Setup(parentView, childView, style);

            var so = new SerializedObject(line);
            so.FindProperty("lineImage").objectReferenceValue = lineGo.GetComponent<Image>();
            so.FindProperty("rectTransform").objectReferenceValue = lineGo.GetComponent<RectTransform>();
            so.FindProperty("fromNode").objectReferenceValue = parentView.RectTransform;
            so.FindProperty("toNode").objectReferenceValue = childView.RectTransform;
            so.FindProperty("parentNode").objectReferenceValue = parentView;
            so.FindProperty("childNode").objectReferenceValue = childView;
            so.FindProperty("style").objectReferenceValue = style;
            so.ApplyModifiedProperties();

            return line;
        }

        private static UpgradeTooltip CreateTooltipUI(Transform parent, TreeStyle style, Sprite bgSprite, Sprite borderSprite, Sprite pointerSprite, Sprite coinSprite)
        {
            var tooltipGo = CreateTopLeftUI("Tooltip", parent, new Vector2(263, 115), Vector2.zero, style.tooltipBgColor, bgSprite, Image.Type.Sliced);
            var rootRt = tooltipGo.GetComponent<RectTransform>();
            var bgImg = tooltipGo.GetComponent<Image>();
            bgImg.raycastTarget = false;

            // Контурная рамка вокруг тултипа
            var borderGo = CreateCenteredUI("Border", tooltipGo.transform, new Vector2(263, 115), Vector2.zero, style.tooltipBorderColor, borderSprite, Image.Type.Sliced);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = Vector2.zero;
            borderRt.anchoredPosition = Vector2.zero;
            borderGo.GetComponent<Image>().raycastTarget = false;

            // Tail (Pointer)
            var tailGo = CreateCenteredUI("Tail", tooltipGo.transform, new Vector2(32, 16), Vector2.zero, style.tooltipBgColor, pointerSprite);
            var tailRt = tailGo.GetComponent<RectTransform>();
            var tailImg = tailGo.GetComponent<Image>();
            tailImg.raycastTarget = false;

            // Title - белый жирный
            var title = CreateTopLeftText("Title", tooltipGo.transform, new Vector2(235, 24), new Vector2(14, 12), "Чернозем", 17, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);

            // Description - контрастный белый
            var desc = CreateTopLeftText("Description", tooltipGo.transform, new Vector2(235, 36), new Vector2(14, 38), "+ 10% скорость роста", 14, FontStyles.Normal, new Color(0.92f, 0.94f, 0.96f, 1f), TextAlignmentOptions.TopLeft);

            // Price Row
            var priceRow = CreateTopLeftUI("PriceRow", tooltipGo.transform, new Vector2(235, 26), new Vector2(14, 76), Color.clear);
            priceRow.GetComponent<Image>().raycastTarget = false;
            var coinIcon = CreateTopLeftUI("CoinIcon", priceRow.transform, new Vector2(22, 22), new Vector2(0, 2), Color.white, coinSprite);
            coinIcon.GetComponent<Image>().raycastTarget = false;
            var priceText = CreateTopLeftText("PriceText", priceRow.transform, new Vector2(100, 24), new Vector2(28, 0), "50", 17, FontStyles.Bold, new Color(0.98f, 0.82f, 0.22f), TextAlignmentOptions.MidlineLeft);

            // Status Text
            var statusText = CreateTopLeftText("StatusText", tooltipGo.transform, new Vector2(235, 24), new Vector2(14, 76), "Куплено", 15, FontStyles.Bold, new Color(0.29f, 0.87f, 0.50f), TextAlignmentOptions.MidlineLeft);
            statusText.gameObject.SetActive(false);

            var tooltip = tooltipGo.AddComponent<UpgradeTooltip>();
            var so = new SerializedObject(tooltip);
            so.FindProperty("rootRect").objectReferenceValue = rootRt;
            so.FindProperty("bgImage").objectReferenceValue = bgImg;
            so.FindProperty("tailRect").objectReferenceValue = tailRt;
            so.FindProperty("tailImage").objectReferenceValue = tailImg;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("descriptionText").objectReferenceValue = desc;
            so.FindProperty("priceRow").objectReferenceValue = priceRow;
            so.FindProperty("priceText").objectReferenceValue = priceText;
            so.FindProperty("coinIcon").objectReferenceValue = coinIcon.GetComponent<Image>();
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("style").objectReferenceValue = style;
            so.ApplyModifiedProperties();

            tooltipGo.SetActive(false);
            return tooltip;
        }

        private static GameObject CreateTopLeftUI(string name, Transform parent, Vector2 size, Vector2 topLeftPos, Color color, Sprite sprite = null, Image.Type imgType = Image.Type.Simple)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(topLeftPos.x, -topLeftPos.y);

            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = imgType;
            }
            return go;
        }

        private static TextMeshProUGUI CreateTopLeftText(string name, Transform parent, Vector2 size, Vector2 topLeftPos, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(topLeftPos.x, -topLeftPos.y);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateCenteredUI(string name, Transform parent, Vector2 size, Vector2 localPos, Color color, Sprite sprite = null, Image.Type imgType = Image.Type.Simple)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = localPos;

            var img = go.GetComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = imgType;
            }
            return go;
        }

        private static TextMeshProUGUI CreateCenteredText(string name, Transform parent, Vector2 size, Vector2 localPos, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = localPos;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
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

        private static GameObject CreateCutoutMask(string name, Transform parent, Vector2 size, Vector2 localPos, Sprite bgSprite)
        {
            var cutout = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            cutout.transform.SetParent(parent, false);
            var crt = cutout.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = size;
            crt.anchoredPosition = localPos;

            var maskImg = cutout.GetComponent<Image>();
            maskImg.color = Color.clear;
            maskImg.raycastTarget = false;

            // Внутреннее изображение фона с обратным смещением для идеального совпадения пикселей
            var innerGo = new GameObject("BgSlice", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(cutout.transform, false);
            var irt = innerGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(1920f, 1080f);
            irt.anchoredPosition = -localPos;

            var innerImg = innerGo.GetComponent<Image>();
            innerImg.sprite = bgSprite;
            innerImg.color = Color.white;
            innerImg.raycastTarget = false;

            return cutout;
        }

        private static Transform CreateEmptyPoint(string name, Transform parent, Vector2 canvasPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = canvasPos;
            return go.transform;
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
