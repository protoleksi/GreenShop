using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Навигация между зонами в одной сцене: плавно перевозит камеру к точкам обзора.
    /// Архитектура локаций (крест):
    /// - Зона 0: Shop (Торговый зал — центральный хаб)
    /// - Зона 1: Greenhouse (Оранжерея — находится слева от Торгового зала)
    /// - Зона 2: Upgrades (Дерево улучшений — находится снизу от Торгового зала)
    /// Переходы разрешены ТОЛЬКО через центральную зону Shop (0):
    /// 0 <-> 1, 0 <-> 2. Прямые переходы 1 <-> 2 строго заблокированы.
    /// </summary>
    public class ZoneCameraController : MonoBehaviour
    {
        public const int ZoneShop = 0;
        public const int ZoneGreenhouse = 1;
        public const int ZoneUpgrades = 2;

        public static ZoneCameraController Instance { get; private set; }

        [Serializable]
        public class CameraZone
        {
            [Tooltip("Название зоны для UI и сохранения.")]
            public string name = "Зона";

            [Tooltip("Точка, куда должна приехать камера (позиция и поворот).")]
            public Transform viewPoint;
        }

        [Header("Камера")]
        [SerializeField] private Camera targetCamera;

        [Header("Зоны")]
        [SerializeField] private CameraZone[] zones = new CameraZone[0];
        [SerializeField] private int startZoneIndex = ZoneShop;

        [Header("Анимация")]
        [SerializeField, Min(0.01f)] private float moveDuration = 0.8f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool useUnscaledTime;

        [Header("Прогресс")]
        [SerializeField] private bool unlockVisitedZone = true;

        [Header("UI Кнопки навигации по краям экрана")]
        [Tooltip("Кнопка перехода в Оранжерею (левый край экрана). Активна только в Торговом зале (0).")]
        [SerializeField] private Button buttonToGreenhouse;

        [Tooltip("Кнопка перехода в Улучшения (нижний край экрана). Активна только в Торговом зале (0).")]
        [SerializeField] private Button buttonToUpgrades;

        [Tooltip("Кнопка возврата в Торговый зал из Оранжереи (правый край экрана). Активна только в Оранжерее (1).")]
        [SerializeField] private Button buttonGreenhouseToShop;

        [Tooltip("Кнопка возврата в Торговый зал из Улучшений (верхний край экрана). Активна только в Улучшениях (2).")]
        [SerializeField] private Button buttonUpgradesToShop;

        [Tooltip("Глобальный счётчик монет на экранном канвасе (активен только в Торговом зале, так как в Оранжерее и Улучшениях свои HUD).")]
        [SerializeField] private GameObject screenSpaceCoins;

        private Coroutine moveRoutine;

        public event Action<int> ZoneChanged;

        public int CurrentZoneIndex { get; private set; } = -1;
        public int ZoneCount => zones != null ? zones.Length : 0;
        public bool IsMoving { get; private set; }

        public string CurrentZoneName =>
            IsValidZone(CurrentZoneIndex) ? zones[CurrentZoneIndex].name : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Автоматическая подписка на клики кнопок (если они заданы в инспекторе)
            if (buttonToGreenhouse != null)
                buttonToGreenhouse.onClick.AddListener(GoToGreenhouse);

            if (buttonToUpgrades != null)
                buttonToUpgrades.onClick.AddListener(GoToUpgrades);

            if (buttonGreenhouseToShop != null)
                buttonGreenhouseToShop.onClick.AddListener(GoToShop);

            if (buttonUpgradesToShop != null)
                buttonUpgradesToShop.onClick.AddListener(GoToShop);
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                Debug.LogError("[ZoneCameraController] Не найдена камера.");
                return;
            }

            if (ZoneCount == 0)
            {
                Debug.LogWarning("[ZoneCameraController] Не настроены зоны.");
                return;
            }

            int initialIndex = Mathf.Clamp(startZoneIndex, 0, ZoneCount - 1);
            SnapTo(initialIndex);
        }

        private void OnDestroy()
        {
            if (buttonToGreenhouse != null)
                buttonToGreenhouse.onClick.RemoveListener(GoToGreenhouse);

            if (buttonToUpgrades != null)
                buttonToUpgrades.onClick.RemoveListener(GoToUpgrades);

            if (buttonGreenhouseToShop != null)
                buttonGreenhouseToShop.onClick.RemoveListener(GoToShop);

            if (buttonUpgradesToShop != null)
                buttonUpgradesToShop.onClick.RemoveListener(GoToShop);
        }

        private void OnValidate()
        {
            moveDuration = Mathf.Max(0.01f, moveDuration);
        }

        /// <summary>Перейти в центральный Торговый зал (0).</summary>
        public void GoToShop() => GoToZone(ZoneShop);

        /// <summary>Перейти в Оранжерею (1, слева от магазина).</summary>
        public void GoToGreenhouse() => GoToZone(ZoneGreenhouse);

        /// <summary>Перейти в Дерево улучшений (2, снизу от магазина).</summary>
        public void GoToUpgrades() => GoToZone(ZoneUpgrades);

        /// <summary>
        /// Проверка допустимости перехода между зонами по схеме «крест».
        /// Все перемещения осуществляются строго через центральный хаб — Shop (0).
        /// </summary>
        public bool CanTransition(int from, int to)
        {
            if (from == to)
                return false;

            if (!IsValidZone(from) || !IsValidZone(to))
                return false;

            // Из центрального Shop (0) разрешён переход в Greenhouse (1) и Upgrades (2)
            if (from == ZoneShop && (to == ZoneGreenhouse || to == ZoneUpgrades))
                return true;

            // Из Greenhouse (1) и Upgrades (2) разрешён переход ТОЛЬКО обратно в Shop (0)
            if ((from == ZoneGreenhouse || from == ZoneUpgrades) && to == ZoneShop)
                return true;

            // Все прочие переходы (включая прямой 1 <-> 2) запрещены
            return false;
        }

        /// <summary>Плавно переехать к зоне по индексу.</summary>
        public void GoToZone(int index)
        {
            // Защита от спама кликов: пока камера движется, новые команды игнорируются
            if (IsMoving)
            {
                Debug.LogWarning("[ZoneCameraController] Переход заблокирован: камера уже находится в движении.");
                return;
            }

            if (!IsValidZone(index))
            {
                Debug.LogWarning($"[ZoneCameraController] Некорректная зона: {index}.");
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (index == CurrentZoneIndex)
            {
                Unlock(index);
                ZoneChanged?.Invoke(index);
                UpdateButtonVisibility(index);
                return;
            }

            // Защита от недопустимых переходов (например, Оранжерея <-> Улучшения напрямую)
            if (CurrentZoneIndex >= 0 && !CanTransition(CurrentZoneIndex, index))
            {
                Debug.LogWarning($"[ZoneCameraController] Прямой переход из зоны {CurrentZoneIndex} ({CurrentZoneName}) в зону {index} запрещён! Переход возможен только через центральный Shop ({ZoneShop}).");
                return;
            }

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveToZone(index));
        }

        public void GoToNextZone()
        {
            if (ZoneCount == 0 || IsMoving)
            {
                return;
            }

            int next = (CurrentZoneIndex + 1) % ZoneCount;
            if (CanTransition(CurrentZoneIndex, next))
            {
                GoToZone(next);
            }
            else
            {
                Debug.LogWarning($"[ZoneCameraController] GoToNextZone: переход в зону {next} запрещён из текущей зоны {CurrentZoneIndex}.");
            }
        }

        public void GoToPreviousZone()
        {
            if (ZoneCount == 0 || IsMoving)
            {
                return;
            }

            int prev = (CurrentZoneIndex - 1 + ZoneCount) % ZoneCount;
            if (CanTransition(CurrentZoneIndex, prev))
            {
                GoToZone(prev);
            }
            else
            {
                Debug.LogWarning($"[ZoneCameraController] GoToPreviousZone: переход в зону {prev} запрещён из текущей зоны {CurrentZoneIndex}.");
            }
        }

        private IEnumerator MoveToZone(int index)
        {
            IsMoving = true;

            // Во время анимации скрываем кнопки навигации, чтобы исключить паразитные клики
            UpdateButtonVisibility(-1);

            Transform destination = zones[index].viewPoint;
            Transform cameraTransform = targetCamera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / moveDuration);
                float eased = easeCurve.Evaluate(progress);

                cameraTransform.position = Vector3.LerpUnclamped(startPosition, destination.position, eased);
                cameraTransform.rotation = Quaternion.SlerpUnclamped(startRotation, destination.rotation, eased);

                yield return null;
            }

            cameraTransform.position = destination.position;
            cameraTransform.rotation = destination.rotation;

            CurrentZoneIndex = index;
            IsMoving = false;
            moveRoutine = null;

            Unlock(index);
            ZoneChanged?.Invoke(index);

            // Активируем кнопки для зоны, в которую только что приехали
            UpdateButtonVisibility(CurrentZoneIndex);
        }

        private void SnapTo(int index)
        {
            Transform destination = zones[index].viewPoint;
            targetCamera.transform.SetPositionAndRotation(destination.position, destination.rotation);
            CurrentZoneIndex = index;
            Unlock(index);
            ZoneChanged?.Invoke(index);
            UpdateButtonVisibility(CurrentZoneIndex);
        }

        /// <summary>
        /// Обновляет видимость 4-х краевых кнопок навигации в зависимости от активной зоны.
        /// </summary>
        public void UpdateButtonVisibility(int zoneIndex)
        {
            SetButtonActive(buttonToGreenhouse, zoneIndex == ZoneShop);
            SetButtonActive(buttonToUpgrades, zoneIndex == ZoneShop);
            SetButtonActive(buttonGreenhouseToShop, zoneIndex == ZoneGreenhouse);
            SetButtonActive(buttonUpgradesToShop, zoneIndex == ZoneUpgrades);
            if (screenSpaceCoins != null)
            {
                screenSpaceCoins.SetActive(zoneIndex == ZoneShop);
            }
        }

        public void UpdateButtonVisibility() => UpdateButtonVisibility(CurrentZoneIndex);

        /// <summary>Алиас для удобства и обратной совместимости.</summary>
        public void UpdateNavigationButtonsVisibility(int zoneIndex) => UpdateButtonVisibility(zoneIndex);
        public void UpdateNavigationButtonsVisibility() => UpdateButtonVisibility(CurrentZoneIndex);

        private void SetButtonActive(Button button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }

        private bool IsValidZone(int index)
        {
            return zones != null &&
                   index >= 0 &&
                   index < zones.Length &&
                   zones[index] != null &&
                   zones[index].viewPoint != null;
        }

        private void Unlock(int index)
        {
            if (!unlockVisitedZone || GameManager.Instance == null || !IsValidZone(index))
            {
                return;
            }

            GameManager.Instance.UnlockZone(zones[index].name);
        }
    }
}
