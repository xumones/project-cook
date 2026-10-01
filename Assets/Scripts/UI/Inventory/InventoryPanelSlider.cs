using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.UI
{
    /// <summary>เลื่อนแผงกระเป๋า (ซ้าย) กับแผงที่เก็บ (ขวา) เข้าออกทีละฝั่ง และสลับฝั่งเมื่อเมาส์ค้างที่แถบขอบจอ</summary>
    public class InventoryPanelSlider : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Action ตำแหน่งเมาส์บนจอ (UI/Point)")]
        [SerializeField] private InputActionReference pointAction;

        [Header("Panels")]
        [Tooltip("แผงกระเป๋า ต้องยึดขอบซ้ายของจอ pivot x = 0")]
        [SerializeField] private CanvasGroup playerPanel;

        [Tooltip("แผงที่เก็บ (ตู้เย็น ลัง) ต้องยึดขอบขวาของจอ pivot x = 1")]
        [SerializeField] private CanvasGroup containerPanel;

        [Header("Edges")]
        [Tooltip("แถบขอบซ้าย ค้างเมาส์ไว้แล้วสลับไปแผงกระเป๋า ต้องปิด Raycast Target")]
        [SerializeField] private RectTransform leftEdge;

        [Tooltip("แถบขอบขวา ค้างเมาส์ไว้แล้วสลับไปแผงที่เก็บ ต้องปิด Raycast Target")]
        [SerializeField] private RectTransform rightEdge;

        [Tooltip("เวลาที่ต้องค้างเมาส์ที่ขอบก่อนสลับ (วินาที) กันเผลอสลับ")]
        [SerializeField] private float edgeDwell = 0.3f;

        [SerializeField] private float slideDuration = 0.2f;

        private bool isOpen;
        private bool hasContainer;
        private bool showingContainer;

        private float playerAmount;
        private float containerAmount;
        private float edgeTimer;

        private void OnEnable()
        {
            pointAction.EnableAction();
        }

        private void OnDisable()
        {
            pointAction.DisableAction();
        }

        private void Start()
        {
            Apply();
        }

        /// <summary>เปิดหน้าต่าง ถ้ามีที่เก็บคู่กันจะแสดงฝั่งที่เก็บก่อน</summary>
        public void Open(bool withContainer)
        {
            isOpen = true;
            hasContainer = withContainer;
            showingContainer = withContainer;
            edgeTimer = 0f;
            Apply();
        }

        public void Close()
        {
            isOpen = false;
            Apply();
        }

        private void Update()
        {
            if (isOpen && hasContainer) UpdateEdgeHover();

            float playerTarget = isOpen && !showingContainer ? 1f : 0f;
            float containerTarget = isOpen && showingContainer ? 1f : 0f;
            if (playerAmount == playerTarget && containerAmount == containerTarget) return;

            float step = Time.unscaledDeltaTime / slideDuration;
            playerAmount = Mathf.MoveTowards(playerAmount, playerTarget, step);
            containerAmount = Mathf.MoveTowards(containerAmount, containerTarget, step);
            Apply();
        }

        // ขอบที่ใช้ได้มีแค่ฝั่งของแผงที่ซ่อนอยู่ เมื่อสลับแล้วเมาส์ที่ค้างอยู่จึงไม่ทำให้สลับกลับ
        private void UpdateEdgeHover()
        {
            RectTransform edge = showingContainer ? leftEdge : rightEdge;
            if (!RectTransformUtility.RectangleContainsScreenPoint(edge, pointAction.ReadValue<Vector2>(), null))
            {
                edgeTimer = 0f;
                return;
            }

            edgeTimer += Time.unscaledDeltaTime;
            if (edgeTimer < edgeDwell) return;

            edgeTimer = 0f;
            showingContainer = !showingContainer;
            Apply();
        }

        private void Apply()
        {
            // แผงที่ซ่อนระหว่างเปิดหน้าต่างยังต้องเปิดอยู่ ไม่งั้นของที่ลากมาจากแผงนั้นจะไม่ได้ EndDrag
            ApplyPanel(playerPanel, playerAmount, -1f, isOpen, isOpen && !showingContainer);
            ApplyPanel(containerPanel, containerAmount, 1f, isOpen && hasContainer, isOpen && showingContainer);

            leftEdge.gameObject.SetActive(isOpen && hasContainer && showingContainer);
            rightEdge.gameObject.SetActive(isOpen && hasContainer && !showingContainer);
        }

        // ease-out: ออกตัวเร็วแล้วชะลอตอนใกล้ถึงที่ hiddenSide -1 = ซ่อนทางซ้าย 1 = ซ่อนทางขวา
        private static void ApplyPanel(CanvasGroup group, float amount, float hiddenSide, bool keepActive, bool interactable)
        {
            var rect = (RectTransform)group.transform;
            float eased = 1f - (1f - amount) * (1f - amount);
            rect.anchoredPosition = new Vector2(Mathf.Lerp(hiddenSide * rect.rect.width, 0f, eased), rect.anchoredPosition.y);

            group.blocksRaycasts = interactable;
            group.gameObject.SetActive(keepActive || amount > 0f);
        }
    }
}
