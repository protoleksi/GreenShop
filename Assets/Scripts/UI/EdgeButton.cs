using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Краевая навигационная полоса-кнопка (EdgeButton).
    /// Используется для возврата в Торговый зал из Оранжереи (правый край, ▶)
    /// и из Дерева улучшений (верхний край, ▲).
    /// Вся площадь полосы кликабельна, наведение подсвечивает полосу.
    /// Защищена от спам-кликов через проверку ZoneCameraController.IsMoving.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class EdgeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Иконка стрелки")]
        [SerializeField] private Image arrowIcon;

        [Header("Цвета полосы")]
        [SerializeField] private Color normalColor = new Color(0.12f, 0.16f, 0.22f, 0.85f);
        [SerializeField] private Color hoverColor = new Color(0.24f, 0.32f, 0.44f, 0.95f);
        [SerializeField] private Color pressedColor = new Color(0.18f, 0.26f, 0.36f, 1f);

        [Header("Навигация")]
        [SerializeField] private ZoneCameraController cameraController;

        private Image backgroundImage;
        public bool IsHovered { get; private set; }

        public event Action Clicked;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }

            if (cameraController == null)
            {
                cameraController = FindAnyObjectByType<ZoneCameraController>();
            }
        }

        private void OnEnable()
        {
            IsHovered = false;
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }

        public void SetCameraController(ZoneCameraController controller)
        {
            cameraController = controller;
        }

        public void SetArrowSprite(Sprite sprite)
        {
            if (arrowIcon != null)
            {
                arrowIcon.sprite = sprite;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
            if (backgroundImage != null)
            {
                backgroundImage.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // Защита от клик-спама во время анимации камеры
            if (cameraController != null && cameraController.IsMoving)
            {
                return;
            }

            Clicked?.Invoke();

            if (cameraController != null)
            {
                cameraController.GoToShop();
            }
        }
    }
}
