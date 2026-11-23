using System;
using UnityEngine;

namespace GreenShop
{
    public enum CustomerState
    {
        Inactive,
        WalkingToCounter,
        WaitingAtCounter,
        Leaving
    }

    /// <summary>
    /// Компонент покупателя (Зона 0 — Торговый зал).
    /// Управляет движением, анимацией покачивания, направлением взгляда и состояниями.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        [Header("Визуализация")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private UnityEngine.UI.Image customerImage;
        [SerializeField] private Transform visualRoot;

        [Header("Настройки")]
        [SerializeField] private CustomerConfig config;

        private CustomerState state = CustomerState.Inactive;
        private Vector3 targetPosition;
        private Action onArrivalCallback;
        private Action onExitCallback;
        private float moveSpeed = 2.5f;
        private float wobbleFreq = 10f;
        private float wobbleAngle = 3.5f;
        private bool isHappy = false;

        public CustomerState State => state;
        public bool IsAtCounter => state == CustomerState.WaitingAtCounter;
        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void Initialize(CustomerConfig customerConfig)
        {
            config = customerConfig;
            if (config != null)
            {
                moveSpeed = config.MoveSpeed;
                wobbleFreq = config.WalkWobbleFrequency;
                wobbleAngle = config.WalkWobbleAngle;
            }
        }

        public void SetVisualColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
            if (customerImage != null)
            {
                customerImage.color = color;
            }
        }

        /// <summary>Появление у двери и движение к прилавку.</summary>
        public void WalkToCounter(Vector3 start, Vector3 counter, Action onArrived)
        {
            transform.position = start;
            targetPosition = counter;
            onArrivalCallback = onArrived;
            state = CustomerState.WalkingToCounter;
            gameObject.SetActive(true);
            UpdateFacing(targetPosition.x < transform.position.x);
        }

        /// <summary>Уход покупателя от прилавка к двери.</summary>
        public void Leave(Vector3 exit, bool happy, Action onExited)
        {
            targetPosition = exit;
            isHappy = happy;
            onExitCallback = onExited;
            state = CustomerState.Leaving;
            UpdateFacing(targetPosition.x < transform.position.x);
        }

        private void Update()
        {
            if (state == CustomerState.WalkingToCounter || state == CustomerState.Leaving)
            {
                float step = moveSpeed * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

                // Покачивание при ходьбе
                float wobble = Mathf.Sin(Time.time * wobbleFreq) * wobbleAngle;
                if (visualRoot != null)
                {
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, wobble);
                }

                if (Vector3.Distance(transform.position, targetPosition) < 0.02f)
                {
                    transform.position = targetPosition;
                    if (visualRoot != null)
                    {
                        visualRoot.localRotation = Quaternion.identity;
                    }

                    if (state == CustomerState.WalkingToCounter)
                    {
                        state = CustomerState.WaitingAtCounter;
                        onArrivalCallback?.Invoke();
                    }
                    else if (state == CustomerState.Leaving)
                    {
                        state = CustomerState.Inactive;
                        onExitCallback?.Invoke();
                    }
                }
            }
        }

        private void UpdateFacing(bool facingLeft)
        {
            // Если идет влево (от двери 8.4 к прилавку 3.7), scale.x = 1
            // Если идет вправо (к двери 8.4), scale.x = -1
            Vector3 scale = transform.localScale;
            scale.x = facingLeft ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
