using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Всплывающий пузырь заказа над головой покупателя (Этап 3).
    /// Показывает иконку желаемого цветка, индикатор терпения и пометку «нет в наличии».
    /// </summary>
    public class OrderBubble : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private Image plantIcon;
        [SerializeField] private Image patienceFill;
        [SerializeField] private TMP_Text outOfStockLabel;

        [Header("Палитра терпения")]
        [SerializeField] private Color patienceHigh = new Color(0.29f, 0.87f, 0.50f, 1f);   // #4ade80
        [SerializeField] private Color patienceMedium = new Color(0.98f, 0.80f, 0.08f, 1f); // #facc15
        [SerializeField] private Color patienceLow = new Color(0.94f, 0.27f, 0.27f, 1f);    // #ef4444

        private PlantData currentPlant;
        private float totalDuration = 30f;

        public bool IsVisible => bubbleRoot != null && bubbleRoot.activeSelf;
        public PlantData CurrentOrder => currentPlant;

        private void Awake()
        {
            if (bubbleRoot == null)
            {
                bubbleRoot = gameObject;
            }
        }

        public void Show(PlantData plant, float duration, bool inStock)
        {
            currentPlant = plant;
            totalDuration = Mathf.Max(0.1f, duration);

            if (plantIcon != null && plant != null)
            {
                if (plant.Icon != null)
                {
                    plantIcon.sprite = plant.Icon;
                    plantIcon.color = Color.white;
                }
                else
                {
                    plantIcon.sprite = null;
                    plantIcon.color = plant.PlantColor;
                }
            }

            UpdateProgress(totalDuration, totalDuration, inStock);

            if (bubbleRoot != null)
            {
                bubbleRoot.SetActive(true);
            }
        }

        public void UpdateProgress(float currentPatience, float maxPatience, bool inStock)
        {
            float progress = Mathf.Clamp01(currentPatience / maxPatience);

            if (patienceFill != null)
            {
                patienceFill.fillAmount = progress;

                // Плавная смена цвета: Зеленый -> Желтый -> Красный
                Color fillColor;
                if (progress > 0.5f)
                {
                    float t = (progress - 0.5f) * 2f;
                    fillColor = Color.Lerp(patienceMedium, patienceHigh, t);
                }
                else
                {
                    float t = progress * 2f;
                    fillColor = Color.Lerp(patienceLow, patienceMedium, t);
                }
                patienceFill.color = fillColor;
            }

            if (outOfStockLabel != null)
            {
                // Пометка «нет в наличии» видна только если цветка нет на витрине
                outOfStockLabel.gameObject.SetActive(!inStock);
            }
        }

        public void Hide()
        {
            currentPlant = null;
            if (bubbleRoot != null)
            {
                bubbleRoot.SetActive(false);
            }
        }
    }
}
