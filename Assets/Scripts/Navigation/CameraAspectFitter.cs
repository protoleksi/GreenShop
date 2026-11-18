using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Автоматически подгоняет Viewport Rect камеры под соотношение 16:9 (1920x1080),
    /// чтобы при любом разрешении экрана (или свободной пропорции Game View)
    /// камера не выходила за границы фона комнат (19.2 x 10.8) и не заглядывала в соседние зоны.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        [Header("Целевое соотношение сторон")]
        [SerializeField] private float targetAspectWidth = 16f;
        [SerializeField] private float targetAspectHeight = 9f;

        [Header("Ортографический размер (1080p / 100 PPU / 2 = 5.4)")]
        [SerializeField] private float fixedOrthoSize = 5.4f;
        [SerializeField] private bool lockOrthoSize = true;

        private Camera targetCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public float TargetAspect => targetAspectWidth / targetAspectHeight;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            ApplyAspect();
        }

        private void OnEnable()
        {
            ApplyAspect();
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ApplyAspect();
            }
        }

        public void ApplyAspect()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            if (lockOrthoSize && targetCamera.orthographic)
            {
                targetCamera.orthographicSize = fixedOrthoSize;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            if (lastScreenWidth <= 0 || lastScreenHeight <= 0)
            {
                return;
            }

            float currentAspect = (float)lastScreenWidth / lastScreenHeight;
            float targetAspect = TargetAspect;
            float scaleHeight = currentAspect / targetAspect;

            if (scaleHeight < 1.0f)
            {
                // Экран уже, чем 16:9 (Letterbox: полосы сверху и снизу)
                Rect rect = new Rect(0f, (1.0f - scaleHeight) * 0.5f, 1.0f, scaleHeight);
                targetCamera.rect = rect;
            }
            else
            {
                // Экран шире, чем 16:9 (Pillarbox: полосы по бокам слева и справа)
                float scaleWidth = 1.0f / scaleHeight;
                Rect rect = new Rect((1.0f - scaleWidth) * 0.5f, 0f, scaleWidth, 1.0f);
                targetCamera.rect = rect;
            }
        }

        private void OnValidate()
        {
            targetAspectWidth = Mathf.Max(1f, targetAspectWidth);
            targetAspectHeight = Mathf.Max(1f, targetAspectHeight);
            fixedOrthoSize = Mathf.Max(0.1f, fixedOrthoSize);
            ApplyAspect();
        }
    }
}
