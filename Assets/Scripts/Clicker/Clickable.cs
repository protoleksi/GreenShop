using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GreenShop
{
    /// <summary>
    /// Кликабельный объект. Даёт монеты за клик, показывает всплывающий текст и анимацию "пружины".
    /// Для кликов по спрайту нужны Collider2D и Physics2DRaycaster на камере + EventSystem в сцене.
    /// </summary>
    public class Clickable : MonoBehaviour, IPointerClickHandler
    {
        [Header("Награда")]
        [SerializeField] private double coinsPerClick = 1.0;

        [Header("Всплывающий текст")]
        [SerializeField] private bool showPopup = true;
        [SerializeField] private Transform popupAnchor;
        [SerializeField] private Vector3 popupWorldOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private Color popupColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Обратная связь")]
        [SerializeField, Min(0f)] private float punchScale = 0.08f;
        [SerializeField, Min(0f)] private float punchDuration = 0.12f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;

        private Vector3 baseScale;
        private Coroutine punchRoutine;
        private Camera worldCamera;

        private void Awake()
        {
            baseScale = transform.localScale;
            worldCamera = Camera.main;
        }

        private void OnDisable()
        {
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
                punchRoutine = null;
            }

            transform.localScale = baseScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Click();
        }

        /// <summary>Можно вызывать и из UI-кнопки (Button.onClick).</summary>
        public void Click()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[Clickable] В сцене нет GameManager — клик не засчитан.");
                return;
            }

            GameManager.Instance.AddCoins(coinsPerClick);
            GameManager.Instance.RegisterClick();

            if (showPopup)
            {
                ShowPopup();
            }

            PlayPunch();

            if (audioSource != null && clickClip != null)
            {
                audioSource.PlayOneShot(clickClip);
            }
        }

        private void ShowPopup()
        {
            if (FloatingTextSpawner.Instance == null)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            Vector3 worldPosition = (popupAnchor != null ? popupAnchor.position : transform.position) + popupWorldOffset;
            Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
            FloatingTextSpawner.Instance.Spawn(screenPosition, "+" + NumberFormat.Compact(coinsPerClick), popupColor);
        }

        private void PlayPunch()
        {
            if (punchScale <= 0f || punchDuration <= 0f)
            {
                return;
            }

            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
            }

            transform.localScale = baseScale;
            punchRoutine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float elapsed = 0f;
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / punchDuration);
                float multiplier = 1f + punchScale * Mathf.Sin(progress * Mathf.PI);
                transform.localScale = baseScale * multiplier;
                yield return null;
            }

            transform.localScale = baseScale;
            punchRoutine = null;
        }
    }
}
