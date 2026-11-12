using System.Collections;
using TMPro;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Всплывающий текст (+1 монета). Живёт в Canvas, поднимается вверх и плавно исчезает.
    /// Вешается на префаб с TMP_Text и CanvasGroup.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Анимация")]
        [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 70f;
        [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1f, 0f));

        private RectTransform rectTransform;
        private Coroutine animationRoutine;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                Debug.LogError("[FloatingText] Компонент должен висеть на UI-объекте с RectTransform.");
                enabled = false;
                return;
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>Запустить анимацию с новым текстом и цветом.</summary>
        public void Play(string text, Color color)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            animationRoutine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            Vector3 startPosition = rectTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lifetime);

                rectTransform.localPosition = startPosition + Vector3.up * (riseDistance * riseCurve.Evaluate(progress));

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = fadeCurve.Evaluate(progress);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
