using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.UI
{
    /// <summary>หน้าต่างกระเป๋าที่เลื่อนเข้ามาจากซ้าย: เปิด/ปิด สลับโหมดเกม ปลดล็อกเมาส์ และวาดช่องมือกับตารางใหม่เมื่อของเปลี่ยน</summary>
    public class InventoryScreen : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Action เปิด/ปิดหน้าต่าง (Tab)")]
        [SerializeField] private InputActionReference toggleAction;

        [Tooltip("Action ปิดหน้าต่าง (Esc)")]
        [SerializeField] private InputActionReference closeAction;

        [Header("Panel")]
        [Tooltip("แผงที่เลื่อนเข้าออก ต้องยึดขอบซ้ายของจอ และห้ามเป็นตัวเดียวกับ GameObject นี้ เพราะแผงถูกปิดตอนซ่อน")]
        [SerializeField] private RectTransform panel;

        [SerializeField] private float slideDuration = 0.2f;

        [Header("Views")]
        [SerializeField] private ItemSlotView[] slots;
        [SerializeField] private StorageView bodyView;
        [SerializeField] private StorageView bagView;
        [SerializeField] private ItemDragController dragController;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private PlayerInventory inventory;
        private EquipmentManager equipment;

        private bool isOpen;
        private float openAmount;

        private void Start()
        {
            inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                inventory.Changed += RefreshIfOpen;
            }

            equipment = EquipmentManager.Instance;
            if (equipment != null)
            {
                equipment.HandsChanged += RefreshIfOpen;
            }

            // สร้างตารางไว้ตั้งแต่ต้น ตอนเปิดครั้งแรกเลย์เอาต์จะได้เข้าที่แล้ว
            Refresh();
            ApplySlide();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= RefreshIfOpen;
            if (equipment != null) equipment.HandsChanged -= RefreshIfOpen;
        }

        private void OnEnable()
        {
            toggleAction.EnableAction();
            closeAction.EnableAction();
        }

        private void OnDisable()
        {
            toggleAction.DisableAction();
            closeAction.DisableAction();
        }

        private void Update()
        {
            if (!isOpen)
            {
                if (GameModeManager.GameplayInputAllowed && toggleAction.WasPressedThisFrame()) Open();
            }
            else if (toggleAction.WasPressedThisFrame() || closeAction.WasPressedThisFrame())
            {
                Close();
            }

            float target = isOpen ? 1f : 0f;
            if (openAmount != target)
            {
                openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime / slideDuration);
                ApplySlide();
            }
        }

        private void Open()
        {
            isOpen = true;
            GameModeManager.Instance.SetMode(GameMode.Inventory);
            CursorManager.RequestUnlock(this);
            Refresh();
        }

        private void Close()
        {
            // แผงจะถูกปิดระหว่างลาก ทำให้ไม่มี EndDrag มาเก็บเงาให้
            dragController.Cancel();
            isOpen = false;
            CursorManager.ReleaseUnlock(this);
            GameModeManager.Instance.SetMode(GameMode.Exploring);
        }

        private void RefreshIfOpen()
        {
            if (isOpen) Refresh();
        }

        private void Refresh()
        {
            foreach (ItemSlotView slot in slots) slot.Refresh();

            if (inventory == null) return;
            bodyView.Show(inventory.BodyGrids);
            bagView.Show(inventory.BagGrids);
        }

        // ease-out: ออกตัวเร็วแล้วชะลอตอนใกล้ถึงที่
        private void ApplySlide()
        {
            float eased = 1f - (1f - openAmount) * (1f - openAmount);
            panel.anchoredPosition = new Vector2(Mathf.Lerp(-panel.rect.width, 0f, eased), panel.anchoredPosition.y);
            panel.gameObject.SetActive(openAmount > 0f);
        }
    }
}
