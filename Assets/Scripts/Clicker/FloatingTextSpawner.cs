using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Создаёт всплывающий текст в Canvas по экранной позиции клика.
    /// Вешается на объект Canvas (или его ребёнка). В поле Prefab нужно указать префаб FloatingText.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        public static FloatingTextSpawner Instance { get; private set; }

        [SerializeField] private FloatingText prefab;
        [SerializeField] private Color defaultColor = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField, Min(1)] private int maxAlive = 30;

        private readonly Queue<FloatingText> alive = new Queue<FloatingText>();
        private Canvas canvas;
        private RectTransform canvasRect;

        private void Awake()
        {
            Instance = this;

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[FloatingTextSpawner] Нужен родительский Canvas.");
                enabled = false;
                return;
            }

            canvasRect = (RectTransform)canvas.transform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public FloatingText Spawn(Vector2 screenPosition, string text)
        {
            return Spawn(screenPosition, text, defaultColor);
        }

        public FloatingText Spawn(Vector2 screenPosition, string text, Color color)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[FloatingTextSpawner] Не назначен префаб всплывающего текста.");
                return null;
            }

            FloatingText instance = Instantiate(prefab, canvasRect);
            instance.transform.SetAsLastSibling();

            Camera cameraForCanvas = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cameraForCanvas, out Vector2 localPoint))
            {
                ((RectTransform)instance.transform).localPosition = localPoint;
            }

            instance.Play(text, color);

            alive.Enqueue(instance);
            while (alive.Count > maxAlive)
            {
                FloatingText oldest = alive.Dequeue();
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }

            return instance;
        }
    }
}
