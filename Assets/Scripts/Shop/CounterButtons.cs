using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Кнопки обслуживания у прилавка (Этап 3).
    /// Столбик из двух кнопок: «Продать» (зелёная, активна при наличии) и «Отказать» (нейтральная).
    /// Защита от клик-спама блокирует повторные нажатия во время ухода покупателя.
    /// </summary>
    public class CounterButtons : MonoBehaviour
    {
        [Header("UI Кнопки")]
        [SerializeField] private GameObject buttonsRoot;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button refuseButton;
        [SerializeField] private TMP_Text sellButtonText;
        [SerializeField] private TMP_Text refuseButtonText;
        [SerializeField] private Image sellButtonBg;
        [SerializeField] private Image refuseButtonBg;

        [Header("Палитра состояний кнопки Продать")]
        [SerializeField] private Color sellActiveColor = new Color(0.18f, 0.65f, 0.32f, 1f);   // Зеленая
        [SerializeField] private Color sellInactiveColor = new Color(0.40f, 0.45f, 0.50f, 0.7f); // Серая

        public event Action SellClicked;
        public event Action RefuseClicked;

        private bool isBusy = false;

        public bool IsVisible => buttonsRoot != null && buttonsRoot.activeSelf;

        private void Awake()
        {
            InitListeners();
        }

        private void InitListeners()
        {
            if (buttonsRoot == null)
            {
                buttonsRoot = gameObject;
            }

            if (sellButton != null)
            {
                sellButton.onClick.RemoveListener(HandleSellClick);
                sellButton.onClick.AddListener(HandleSellClick);
            }

            if (refuseButton != null)
            {
                refuseButton.onClick.RemoveListener(HandleRefuseClick);
                refuseButton.onClick.AddListener(HandleRefuseClick);
            }
        }

        public void Show(PlantData plant, bool inStock)
        {
            InitListeners();
            isBusy = false;
            UpdateState(plant, inStock);

            if (buttonsRoot != null)
            {
                buttonsRoot.SetActive(true);
            }
        }

        public void UpdateState(PlantData plant, bool inStock)
        {
            if (isBusy) return;

            if (sellButton != null)
            {
                sellButton.interactable = inStock;
            }

            if (sellButtonBg != null)
            {
                sellButtonBg.color = inStock ? sellActiveColor : sellInactiveColor;
            }

            if (sellButtonText != null)
            {
                int price = plant != null ? plant.SellPrice : 0;
                sellButtonText.text = inStock ? $"Продать (+{price} 🪙)" : "Нет на витрине";
            }

            if (refuseButton != null)
            {
                refuseButton.interactable = true;
            }
        }

        public void Hide()
        {
            isBusy = false;
            if (buttonsRoot != null)
            {
                buttonsRoot.SetActive(false);
            }
        }

        private void HandleSellClick()
        {
            if (isBusy) return;
            isBusy = true;
            SetInteractable(false);
            SellClicked?.Invoke();
        }

        private void HandleRefuseClick()
        {
            if (isBusy) return;
            isBusy = true;
            SetInteractable(false);
            RefuseClicked?.Invoke();
        }

        public void SetInteractable(bool interactable)
        {
            if (sellButton != null) sellButton.interactable = interactable;
            if (refuseButton != null) refuseButton.interactable = interactable;
        }
    }
}
