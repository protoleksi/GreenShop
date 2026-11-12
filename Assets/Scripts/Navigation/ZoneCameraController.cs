using System;
using System.Collections;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Навигация между зонами в одной сцене: плавно перевозит камеру к точкам обзора.
    /// Точки — пустые GameObject'ы с нужной позицией/поворотом камеры (3 шт.).
    /// Кнопки зон в UI вызывают GoToZone(int), GoToNextZone() или GoToPreviousZone().
    /// </summary>
    public class ZoneCameraController : MonoBehaviour
    {
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
        [SerializeField] private int startZoneIndex;

        [Header("Анимация")]
        [SerializeField, Min(0.01f)] private float moveDuration = 0.8f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private bool useUnscaledTime;

        [Header("Прогресс")]
        [SerializeField] private bool unlockVisitedZone = true;

        private Coroutine moveRoutine;

        public event Action<int> ZoneChanged;

        public int CurrentZoneIndex { get; private set; } = -1;
        public int ZoneCount => zones != null ? zones.Length : 0;
        public bool IsMoving { get; private set; }

        public string CurrentZoneName =>
            IsValidZone(CurrentZoneIndex) ? zones[CurrentZoneIndex].name : string.Empty;

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

            SnapTo(Mathf.Clamp(startZoneIndex, 0, ZoneCount - 1));
        }

        private void OnValidate()
        {
            moveDuration = Mathf.Max(0.01f, moveDuration);
        }

        /// <summary>Плавно переехать к зоне по индексу.</summary>
        public void GoToZone(int index)
        {
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

            if (index == CurrentZoneIndex && !IsMoving)
            {
                Unlock(index);
                ZoneChanged?.Invoke(index);
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
            if (ZoneCount == 0)
            {
                return;
            }

            GoToZone((CurrentZoneIndex + 1) % ZoneCount);
        }

        public void GoToPreviousZone()
        {
            if (ZoneCount == 0)
            {
                return;
            }

            GoToZone((CurrentZoneIndex - 1 + ZoneCount) % ZoneCount);
        }

        private IEnumerator MoveToZone(int index)
        {
            IsMoving = true;

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
        }

        private void SnapTo(int index)
        {
            Transform destination = zones[index].viewPoint;
            targetCamera.transform.SetPositionAndRotation(destination.position, destination.rotation);
            CurrentZoneIndex = index;
            Unlock(index);
            ZoneChanged?.Invoke(index);
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
