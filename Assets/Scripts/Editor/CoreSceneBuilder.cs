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
        [MenuItem("Tools/GreenShop/Собрать Core Loop в сцене (Этап 2)")]
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
                soGm.FindProperty("startingCoins").doubleValue = 15.0; // даем монеты на 3 первых семечка
                soGm.ApplyModifiedProperties();
                EditorUtility.SetDirty(gm);
            }

            // Удаляем старые Canvas зон, если они были созданы
            DestroyIfExists("ZoneCanvas_Shop");
            DestroyIfExists("ZoneCanvas_Greenhouse");
            DestroyIfExists("ZoneCanvas_Upgrades");

            // 3. Создаем Canvas для Зоны 1: Оранжерея (-19.2, 0, 0)
            var greenhouseCanvas = CreateWorldCanvas("ZoneCanvas_Greenhouse", new Vector3(-19.20f, 0f, 0f), cam);
            var greenhouseController = greenhouseCanvas.AddComponent<GreenhouseController>();

            BuildGreenhouseUI(greenhouseCanvas.transform, greenhouseController, plantDb);

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
                // Настраиваем видимость и краевые кнопки
                var navBtnHelper = Object.FindAnyObjectByType<ZoneCameraController>();
                if (navBtnHelper != null)
                {
                    navBtnHelper.UpdateButtonVisibility(ZoneCameraController.ZoneShop);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CoreSceneBuilder] Core-геймплей (Этап 2) успешно собран в сцене и сохранён!");
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

        private static void BuildGreenhouseUI(Transform parent, GreenhouseController controller, PlantDatabase plantDb)
        {
            // Заголовок
            CreateText("Header", parent, new Vector2(800, 60), new Vector2(0, 460), "ОРАНЖЕРЕЯ — СТОЛ ВЫРАЩИВАНИЯ", 34, FontStyles.Bold, Color.white);
            var hint = CreateText("Hint", parent, new Vector2(1200, 40), new Vector2(0, 415), "Кликните на пустой горшок, чтобы посадить семя.", 19, FontStyles.Normal, new Color(0.7f, 0.8f, 0.95f));

            // Стол с горшками (4 горшка в ряд)
            var tableGo = CreateUI("PotsTable", parent, new Vector2(1400, 420), new Vector2(0, 40), new Color(0.08f, 0.12f, 0.18f, 0.85f));
            var potsList = new List<PlantPot>();

            float startX = -480f;
            float spacingX = 320f;

            for (int i = 0; i < 4; i++)
            {
                var potGo = CreateUI($"Pot_{i}", tableGo.transform, new Vector2(280, 360), new Vector2(startX + i * spacingX, 0), new Color(0.14f, 0.19f, 0.28f, 0.95f));
                var pot = potGo.AddComponent<PlantPot>();

                // Номер горшка
                CreateText("PotNum", potGo.transform, new Vector2(260, 30), new Vector2(0, 150), $"Горшок #{i + 1}", 16, FontStyles.Bold, new Color(0.6f, 0.7f, 0.85f));

                // Визуал цветка (круг/иконка)
                var flowerVisualGo = CreateUI("FlowerVisual", potGo.transform, new Vector2(110, 110), new Vector2(0, 50), Color.white);
                var flowerVisual = flowerVisualGo.GetComponent<Image>();

                // Название цветка
                var nameLabel = CreateText("NameLabel", potGo.transform, new Vector2(260, 30), new Vector2(0, -25), "Пустой горшок", 18, FontStyles.Bold, Color.white);

                // Статус-бейдж
                var badgeLabel = CreateText("StatusBadge", potGo.transform, new Vector2(260, 30), new Vector2(0, -65), "+ Посадить", 17, FontStyles.Bold, new Color(0.35f, 0.85f, 0.45f));

                // Прогресс-бар
                var progContainer = CreateUI("ProgressContainer", potGo.transform, new Vector2(220, 20), new Vector2(0, -105), new Color(0.08f, 0.1f, 0.15f, 1f));
                var fillGo = CreateUI("Fill", progContainer.transform, new Vector2(220, 20), Vector2.zero, new Color(0.25f, 0.75f, 0.95f, 1f));
                var fillImg = fillGo.GetComponent<Image>();
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 0f;

                // Кнопка горшка
                var potBtn = potGo.AddComponent<Button>();

                // Сериализуем поля горшка
                var soPot = new SerializedObject(pot);
                soPot.FindProperty("potIndex").intValue = i;
                soPot.FindProperty("isUnlocked").boolValue = (i == 0); // Первый горшок открыт сразу
                soPot.FindProperty("potIcon").objectReferenceValue = potGo.GetComponent<Image>();
                soPot.FindProperty("plantVisual").objectReferenceValue = flowerVisual;
                soPot.FindProperty("progressFill").objectReferenceValue = fillImg;
                soPot.FindProperty("progressContainer").objectReferenceValue = progContainer;
                soPot.FindProperty("statusBadge").objectReferenceValue = badgeLabel;
                soPot.FindProperty("plantNameLabel").objectReferenceValue = nameLabel;
                soPot.FindProperty("potButton").objectReferenceValue = potBtn;
                soPot.ApplyModifiedProperties();

                potsList.Add(pot);
            }

            // Инструменты внизу
            var toolsPanel = CreateUI("ToolsPanel", parent, new Vector2(600, 70), new Vector2(0, -260), new Color(0.10f, 0.14f, 0.22f, 0.9f));

            var waterBtnGo = CreateUI("Btn_WaterAll", toolsPanel.transform, new Vector2(240, 50), new Vector2(-130, 0), new Color(0.14f, 0.45f, 0.85f));
            var waterBtn = waterBtnGo.AddComponent<Button>();
            CreateText("Text", waterBtnGo.transform, new Vector2(230, 40), Vector2.zero, "💧 Полить все", 18, FontStyles.Bold, Color.white);

            var fertBtnGo = CreateUI("Btn_Fertilize", toolsPanel.transform, new Vector2(240, 50), new Vector2(130, 0), new Color(0.85f, 0.55f, 0.15f));
            var fertBtn = fertBtnGo.AddComponent<Button>();
            CreateText("Text", fertBtnGo.transform, new Vector2(230, 40), Vector2.zero, "⚡ Удобрить", 18, FontStyles.Bold, Color.white);

            // Модальное окно выбора семян
            var modal = CreateUI("SeedPickerModal", parent, new Vector2(520, 540), Vector2.zero, new Color(0.08f, 0.11f, 0.17f, 0.98f));
            var modalTitle = CreateText("ModalTitle", modal.transform, new Vector2(460, 45), new Vector2(0, 220), "Выбор семени для посадки", 22, FontStyles.Bold, Color.white);

            var closeBtnGo = CreateUI("Btn_Close", modal.transform, new Vector2(40, 40), new Vector2(220, 220), new Color(0.4f, 0.2f, 0.2f));
            var closeBtn = closeBtnGo.AddComponent<Button>();
            CreateText("X", closeBtnGo.transform, new Vector2(40, 40), Vector2.zero, "✕", 20, FontStyles.Bold, Color.white);

            var listContainer = CreateUI("ListContainer", modal.transform, new Vector2(460, 400), new Vector2(0, -25), Color.clear);
            var vlg = listContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Сериализуем GreenhouseController
            var soGh = new SerializedObject(controller);
            soGh.FindProperty("plantDatabase").objectReferenceValue = plantDb;

            var potsProp = soGh.FindProperty("pots");
            potsProp.arraySize = potsList.Count;
            for (int i = 0; i < potsList.Count; i++)
            {
                potsProp.GetArrayElementAtIndex(i).objectReferenceValue = potsList[i];
            }

            soGh.FindProperty("seedPickerModal").objectReferenceValue = modal;
            soGh.FindProperty("seedButtonsContainer").objectReferenceValue = listContainer.transform;
            soGh.FindProperty("closePickerButton").objectReferenceValue = closeBtn;
            soGh.FindProperty("seedPickerTitle").objectReferenceValue = modalTitle;
            soGh.FindProperty("waterAllButton").objectReferenceValue = waterBtn;
            soGh.FindProperty("fertilizeButton").objectReferenceValue = fertBtn;
            soGh.FindProperty("greenhouseHintLabel").objectReferenceValue = hint;
            soGh.ApplyModifiedProperties();
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
            var orderText = CreateText("OrderDetails", bubbleGo.transform, new Vector2(400, 60), new Vector2(0, -5), "«Хочу купить Маргаритку!»\nПлачу: 8 🪙", 17, FontStyles.Normal, Color.white);

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
            var sellBtnText = CreateText("Text", sellBtnGo.transform, new Vector2(350, 40), Vector2.zero, "Продать (+8 🪙)", 20, FontStyles.Bold, Color.white);

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
