using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GreenShop.EditorTools
{
    /// <summary>Служебные пункты меню для проекта GreenShop.</summary>
    public static class GreenShopEditorMenu
    {
        [MenuItem("Tools/GreenShop/Навигация/Настроить 4 кнопки навигации в сцене")]
        public static void SetupNavigationButtons()
        {
            var controller = Object.FindAnyObjectByType<ZoneCameraController>();
            if (controller == null)
            {
                Debug.LogError("[GreenShop] ZoneCameraController не найден в активной сцене!");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[GreenShop] Canvas не найден в активной сцене!");
                return;
            }

            // Находим или создаём контейнер для кнопок навигации
            Transform navPanel = canvas.transform.Find("NavigationPanel");
            if (navPanel == null)
            {
                var navGo = new GameObject("NavigationPanel", typeof(RectTransform));
                navGo.transform.SetParent(canvas.transform, false);
                var rt = navGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                navPanel = navGo.transform;
            }

            // Создаём или находим 4 кнопки
            var btnToGreenhouse = GetOrCreateNavButton(navPanel, "Btn_ToGreenhouse", "< Оранжерея",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(170f, 55f));

            var btnToUpgrades = GetOrCreateNavButton(navPanel, "Btn_ToUpgrades", "v Улучшения",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(190f, 55f));

            var btnGreenhouseToShop = GetOrCreateNavButton(navPanel, "Btn_GreenhouseToShop", "Магазин >",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(170f, 55f));

            var btnUpgradesToShop = GetOrCreateNavButton(navPanel, "Btn_UpgradesToShop", "^ Магазин",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(190f, 55f));

            // Скрываем старые прототипные кнопки (Btn_Shop, Btn_Greenhouse, Btn_Upgrades), если они есть на Canvas
            string[] legacyNames = { "Btn_Shop", "Btn_Greenhouse", "Btn_Upgrades" };
            foreach (var legacyName in legacyNames)
            {
                var legacy = canvas.transform.Find(legacyName);
                if (legacy != null)
                {
                    legacy.gameObject.SetActive(false);
                }
            }

            // Привязываем кнопки к сериализованным полям ZoneCameraController
            var so = new SerializedObject(controller);
            so.FindProperty("buttonToGreenhouse").objectReferenceValue = btnToGreenhouse;
            so.FindProperty("buttonToUpgrades").objectReferenceValue = btnToUpgrades;
            so.FindProperty("buttonGreenhouseToShop").objectReferenceValue = btnGreenhouseToShop;
            so.FindProperty("buttonUpgradesToShop").objectReferenceValue = btnUpgradesToShop;
            so.ApplyModifiedProperties();

            // Обновляем видимость для текущей зоны
            controller.UpdateButtonVisibility(ZoneCameraController.ZoneShop);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);

            Debug.Log("[GreenShop] 4 кнопки навигации успешно настроены и привязаны к ZoneCameraController!");
        }

        private static Button GetOrCreateNavButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject btnGo;
            if (existing != null)
            {
                btnGo = existing.gameObject;
            }
            else
            {
                btnGo = new GameObject(name, typeof(RectTransform));
                btnGo.transform.SetParent(parent, false);
            }

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);

            var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.16f, 0.24f, 0.92f);
            colors.highlightedColor = new Color(0.20f, 0.28f, 0.40f, 1f);
            colors.pressedColor = new Color(0.08f, 0.10f, 0.16f, 1f);
            btn.colors = colors;

            // Текст внутри кнопки
            Transform textTrans = btnGo.transform.Find("Text (TMP)");
            GameObject textGo = (textTrans != null) ? textTrans.gameObject : new GameObject("Text (TMP)", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);

            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>() ?? textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }

        [MenuItem("Tools/GreenShop/Сохранение/Показать путь сохранения")]
        public static void LogSavePath()
        {
            Debug.Log($"[GreenShop] Сохранение: {SaveSystem.SavePath}\nРезервная копия: {SaveSystem.BackupPath}");
        }

        [MenuItem("Tools/GreenShop/Сохранение/Открыть папку сохранений")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Tools/GreenShop/Сохранение/Удалить сохранение")]
        public static void DeleteSave()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Удалить сохранение?",
                "Будут удалены save.json, резервная копия и запись в PlayerPrefs.\nЭто действие нельзя отменить.",
                "Удалить",
                "Отмена");

            if (!confirmed)
            {
                return;
            }

            SaveSystem.DeleteSave();
            Debug.Log("[GreenShop] Сохранение удалено.");
        }
    }
}
