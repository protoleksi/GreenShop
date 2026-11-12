using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Затемнение экрана для плавных переходов между сценами.
    /// Может создать собственный полноэкранный чёрный Canvas автоматически.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float defaultDuration = 0.35f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            Apply(0f);
        }

        public IEnumerator FadeOut(float duration = -1f)
        {
            return FadeTo(1f, duration);
        }

        public IEnumerator FadeIn(float duration = -1f)
        {
            return FadeTo(0f, duration);
        }

        public IEnumerator FadeTo(float targetAlpha, float duration = -1f)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (duration < 0f)
            {
                duration = defaultDuration;
            }

            float startAlpha = canvasGroup.alpha;
            if (duration <= 0f)
            {
                Apply(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            Apply(targetAlpha);
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            Apply(alpha);
        }

        private void Apply(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            canvasGroup.alpha = alpha;

            bool blocksInput = alpha > 0.001f;
            canvasGroup.blocksRaycasts = blocksInput;
            canvasGroup.interactable = blocksInput;
        }

        /// <summary>Создать затемнение в рантайме (если в сцене нет готового UI).</summary>
        public static ScreenFade CreateRuntime(Transform parent)
        {
            GameObject root = new GameObject(
                "ScreenFade",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(ScreenFade));

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                root.layer = uiLayer;
            }

            root.transform.SetParent(parent, false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(RawImage));
            imageObject.layer = root.layer;
            imageObject.transform.SetParent(root.transform, false);

            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = Color.black;
            image.raycastTarget = true;

            return root.GetComponent<ScreenFade>();
        }
    }
}
