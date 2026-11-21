using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Полоска времени под клеткой оранжереи (154x24 px).
    /// Отображает прогресс роста, оставшееся время (м:сс),
    /// индикатор полива (капля с пульсацией) или статус «Готово!».
    /// </summary>
    public class TimeBar : MonoBehaviour
    {
        [Header("UI элементы")]
        [SerializeField] private Image background;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Image waterDropIcon;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Цвета")]
        [SerializeField] private Color normalFillColor = new Color(0.31f, 0.76f, 0.97f, 1f); // #4FC3F7
        [SerializeField] private Color ripeFillColor = new Color(0.48f, 0.82f, 0.42f, 1f);   // #7BC66A
        [SerializeField] private Color waterFillColor = new Color(0.25f, 0.55f, 0.85f, 1f);  // #408CD8

        private bool isPulsing = false;
        private Vector3 initialDropScale = Vector3.one;

        private void Awake()
        {
            if (waterDropIcon != null)
            {
                initialDropScale = waterDropIcon.transform.localScale;
            }
        }

        private void Update()
        {
            if (isPulsing && waterDropIcon != null && waterDropIcon.gameObject.activeSelf)
            {
                float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 6f);
                waterDropIcon.transform.localScale = initialDropScale * pulse;
            }
        }

        /// <summary>
        /// Скрыть полоску (для пустых и заблокированных клеток).
        /// </summary>
        public void SetHidden()
        {
            isPulsing = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Режим активного роста: прогресс-бар и оставшееся время (м:сс).
        /// </summary>
        public void SetGrowing(float progress, float remainingSeconds)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            isPulsing = false;

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(progress);
                fillImage.color = normalFillColor;
            }

            if (timeLabel != null)
            {
                timeLabel.gameObject.SetActive(true);
                int totalSec = Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
                int min = totalSec / 60;
                int sec = totalSec % 60;
                timeLabel.text = $"{min}:{sec:D2}";
            }

            if (waterDropIcon != null)
            {
                waterDropIcon.gameObject.SetActive(false);
            }

            if (statusLabel != null)
            {
                statusLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Режим потребности в воде: полоска замирает, отображается пульсирующая капля.
        /// </summary>
        public void SetNeedsWater(float currentProgress)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            isPulsing = true;

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(currentProgress);
                fillImage.color = waterFillColor;
            }

            if (timeLabel != null)
            {
                timeLabel.gameObject.SetActive(false);
            }

            if (waterDropIcon != null)
            {
                waterDropIcon.gameObject.SetActive(true);
            }

            if (statusLabel != null)
            {
                statusLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Режим созревания: надпись «Готово!».
        /// </summary>
        public void SetRipe()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            isPulsing = false;

            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
                fillImage.color = ripeFillColor;
            }

            if (timeLabel != null)
            {
                timeLabel.gameObject.SetActive(false);
            }

            if (waterDropIcon != null)
            {
                waterDropIcon.gameObject.SetActive(false);
            }

            if (statusLabel != null)
            {
                statusLabel.gameObject.SetActive(true);
                statusLabel.text = "Готово!";
            }
        }
    }
}
