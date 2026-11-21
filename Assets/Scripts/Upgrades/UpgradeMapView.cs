using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// Контроллер интерактивной карты Дерева улучшений (UpgradeMapView).
    /// Управляет узлами, связями, всплывающим тултипом, счётчиком монет
    /// и навигацией по карте (панорамирование мышью и зум колесом).
    /// </summary>
    public class UpgradeMapView : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        public static UpgradeMapView Instance { get; private set; }

        [Header("Контейнер карты (Pan & Zoom)")]
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private float minZoom = 0.65f;
        [SerializeField] private float maxZoom = 1.45f;
        [SerializeField] private float zoomSpeed = 0.08f;
        [SerializeField] private Vector2 panLimits = new Vector2(400f, 250f);

        [Header("Стили и оформление")]
        [SerializeField] private TreeStyle style;
        [SerializeField] private UpgradeTooltip tooltip;
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text floatingFeedbackLabel;

        [Header("Коллекция узлов и связей")]
        [SerializeField] private List<UpgradeNodeView> nodeViews = new List<UpgradeNodeView>();
        [SerializeField] private List<UpgradeConnectionLine> connectionLines = new List<UpgradeConnectionLine>();

        private float currentZoom = 1.0f;
        private Vector2 defaultContentPos;
        private Coroutine feedbackRoutine;
        private readonly Dictionary<string, UpgradeNodeView> nodesById = new Dictionary<string, UpgradeNodeView>();

        public IReadOnlyList<UpgradeNodeView> NodeViews => nodeViews;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (contentRect != null)
            {
                defaultContentPos = contentRect.anchoredPosition;
            }

            if (nodeViews == null || nodeViews.Count == 0)
            {
                nodeViews = new List<UpgradeNodeView>(GetComponentsInChildren<UpgradeNodeView>(true));
            }
            if (connectionLines == null || connectionLines.Count == 0)
            {
                connectionLines = new List<UpgradeConnectionLine>(GetComponentsInChildren<UpgradeConnectionLine>(true));
            }
        }

        private void Start()
        {
            RebuildNodeDictionary();
            UpdateCoinsLabel();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CoinsChanged += _ => UpdateCoinsLabel();
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.UpgradePurchased += _ => RefreshAllNodes();
            }

            RefreshAllNodes();
        }

        public void RegisterNode(UpgradeNodeView node)
        {
            if (node != null && !nodeViews.Contains(node))
            {
                nodeViews.Add(node);
                if (node.Data != null && !string.IsNullOrEmpty(node.Data.id))
                {
                    nodesById[node.Data.id] = node;
                }
            }
        }

        public void RegisterLine(UpgradeConnectionLine line)
        {
            if (line != null && !connectionLines.Contains(line))
            {
                connectionLines.Add(line);
            }
        }

        public void ClearRegistrations()
        {
            nodeViews.Clear();
            connectionLines.Clear();
            nodesById.Clear();
        }

        private void RebuildNodeDictionary()
        {
            nodesById.Clear();
            foreach (var nv in nodeViews)
            {
                if (nv != null && nv.Data != null && !string.IsNullOrEmpty(nv.Data.id))
                {
                    nodesById[nv.Data.id] = nv;
                }
            }
        }

        public void RefreshAllNodes()
        {
            RebuildNodeDictionary();

            var mgr = UpgradeManager.Instance;
            foreach (var node in nodeViews)
            {
                if (node == null || node.Data == null) continue;

                var data = node.Data;

                if (data.id == "root")
                {
                    node.SetState(UpgradeNodeState.Purchased);
                    continue;
                }

                bool isPurchased = mgr != null && mgr.IsPurchased(data.id);
                if (isPurchased)
                {
                    node.SetState(UpgradeNodeState.Purchased);
                }
                else
                {
                    // Проверяем родителей
                    bool hasPurchasedParent = false;
                    if (data.prerequisiteIds == null || data.prerequisiteIds.Count == 0)
                    {
                        hasPurchasedParent = true;
                    }
                    else
                    {
                        foreach (var parentId in data.prerequisiteIds)
                        {
                            if (parentId == "root" || (mgr != null && mgr.IsPurchased(parentId)))
                            {
                                hasPurchasedParent = true;
                                break;
                            }
                        }
                    }

                    if (hasPurchasedParent)
                    {
                        node.SetState(UpgradeNodeState.Available);
                    }
                    else
                    {
                        node.SetState(UpgradeNodeState.Locked);
                    }
                }
            }

            // Обновляем подсветку линий
            foreach (var line in connectionLines)
            {
                if (line != null)
                {
                    line.UpdateVisuals();
                }
            }

            UpdateCoinsLabel();
        }

        public bool TryBuyNode(UpgradeNodeView node)
        {
            if (node == null || node.Data == null) return false;

            var data = node.Data;
            if (UpgradeManager.Instance == null) return false;

            bool bought = UpgradeManager.Instance.TryBuyUpgrade(data.id);
            if (bought)
            {
                RefreshAllNodes();
                if (tooltip != null)
                {
                    tooltip.RefreshIfCurrent(node);
                }
                return true;
            }

            return false;
        }

        public void ShowTooltip(UpgradeNodeView node)
        {
            if (tooltip != null)
            {
                tooltip.Show(node);
            }
        }

        public void HideTooltip()
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }

        public void ShowFloatingFeedback(string message)
        {
            if (floatingFeedbackLabel == null) return;

            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message));
        }

        private IEnumerator ShowFeedbackRoutine(string message)
        {
            floatingFeedbackLabel.text = message;
            floatingFeedbackLabel.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.8f);

            floatingFeedbackLabel.gameObject.SetActive(false);
            feedbackRoutine = null;
        }

        public void UpdateCoinsLabel()
        {
            if (coinsLabel != null && GameManager.Instance != null)
            {
                coinsLabel.text = $"{GameManager.Instance.Coins:N0}";
            }
        }

        // B5: Pan & Zoom навигация по карте
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Начинаем перетаскивание карты
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (contentRect == null) return;

            // Блокировка при переходе камеры
            var camCtrl = ZoneCameraController.Instance;
            if (camCtrl != null && camCtrl.IsMoving) return;

            Vector2 newPos = contentRect.anchoredPosition + eventData.delta;
            newPos.x = Mathf.Clamp(newPos.x, defaultContentPos.x - panLimits.x, defaultContentPos.x + panLimits.x);
            newPos.y = Mathf.Clamp(newPos.y, defaultContentPos.y - panLimits.y, defaultContentPos.y + panLimits.y);
            contentRect.anchoredPosition = newPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (contentRect == null) return;

            var camCtrl = ZoneCameraController.Instance;
            if (camCtrl != null && camCtrl.IsMoving) return;

            float scroll = eventData.scrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom = Mathf.Clamp(currentZoom + scroll * zoomSpeed, minZoom, maxZoom);
                contentRect.localScale = new Vector3(currentZoom, currentZoom, 1f);
            }
        }
    }
}
