using UnityEditor;
using UnityEngine;

namespace GreenShop.EditorTools
{
    /// <summary>Служебные пункты меню для тестирования сохранений.</summary>
    public static class GreenShopEditorMenu
    {
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
