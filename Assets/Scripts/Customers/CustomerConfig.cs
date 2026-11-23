using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Конфигурация жизненного цикла и параметров покупателей (Этап 3).
    /// Все временные интервалы, скорости и точки вынесены из логики в ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerConfig", menuName = "GreenShop/Конфиг покупателей (CustomerConfig)")]
    public class CustomerConfig : ScriptableObject
    {
        [Header("Временные интервалы (секунды)")]
        [Tooltip("Задержка появления самого первого покупателя после старта.")]
        [SerializeField, Min(0.5f)] private float firstCustomerDelay = 8f;

        [Tooltip("Базовый интервал между покупателями (пока касса свободна).")]
        [SerializeField, Min(1f)] private float spawnInterval = 20f;

        [Tooltip("Время терпения покупателя у кассы.")]
        [SerializeField, Min(3f)] private float patienceDuration = 30f;

        [Header("Движение")]
        [Tooltip("Скорость ходьбы покупателя (юнитов в секунду).")]
        [SerializeField, Min(0.5f)] private float moveSpeed = 2.5f;

        [Tooltip("Частота покачивания при ходьбе.")]
        [SerializeField, Min(1f)] private float walkWobbleFrequency = 10f;

        [Tooltip("Угол покачивания при ходьбе (в градусах).")]
        [SerializeField, Min(0f)] private float walkWobbleAngle = 3.5f;

        [Header("Размеры и точки (мировые координаты)")]
        [Tooltip("Точка появления (у двери).")]
        [SerializeField] private Vector3 doorPoint = new Vector3(8.4f, -3.4f, 0f);

        [Tooltip("Точка ожидания у прилавка (между котом и звонком).")]
        [SerializeField] private Vector3 counterPoint = new Vector3(3.7f, -3.4f, 0f);

        [Tooltip("Точка ухода (через дверь).")]
        [SerializeField] private Vector3 exitPoint = new Vector3(8.4f, -3.4f, 0f);

        [Tooltip("Смещение пузыря заказа относительно центра покупателя.")]
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.3f, 0f);

        [Tooltip("Высота покупателя в мировых единицах (420 px ≈ 4.2 юнита).")]
        [SerializeField, Min(1f)] private float customerHeight = 4.2f;

        [Header("Модификаторы улучшений")]
        [Tooltip("Множитель интервала появления (улучшение «Приток покупателей» выставляет 0.5f).")]
        [SerializeField, Range(0.1f, 1f)] private float spawnIntervalMultiplier = 1f;

        public float FirstCustomerDelay => firstCustomerDelay;
        public float SpawnInterval => spawnInterval * spawnIntervalMultiplier;
        public float BaseSpawnInterval => spawnInterval;
        public float PatienceDuration => patienceDuration;
        public float MoveSpeed => moveSpeed;
        public float WalkWobbleFrequency => walkWobbleFrequency;
        public float WalkWobbleAngle => walkWobbleAngle;
        public Vector3 DoorPoint => doorPoint;
        public Vector3 CounterPoint => counterPoint;
        public Vector3 ExitPoint => exitPoint;
        public Vector3 BubbleOffset => bubbleOffset;
        public float CustomerHeight => customerHeight;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;

        public void SetSpawnIntervalMultiplier(float multiplier)
        {
            spawnIntervalMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        private void OnEnable()
        {
            // Сбрасываем множитель в 1 при запуске сессии
            spawnIntervalMultiplier = 1f;
        }

        private void OnValidate()
        {
            firstCustomerDelay = Mathf.Max(0.5f, firstCustomerDelay);
            spawnInterval = Mathf.Max(1f, spawnInterval);
            patienceDuration = Mathf.Max(3f, patienceDuration);
            moveSpeed = Mathf.Max(0.5f, moveSpeed);
            spawnIntervalMultiplier = Mathf.Clamp(spawnIntervalMultiplier, 0.1f, 1f);
        }
    }
}
