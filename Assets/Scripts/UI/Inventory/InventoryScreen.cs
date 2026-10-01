using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.UI
{
    /// <summary>หน้าต่างกระเป๋า (แผงซ้าย) และที่เก็บที่เปิดคู่กัน (แผงขวา): เปิด/ปิด สลับโหมดเกม ปลดล็อกเมาส์ และวาดใหม่เมื่อของเปลี่ยน</summary>
    public class InventoryScreen : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Action เปิด/ปิดหน้าต่าง (Tab)")]
        [SerializeField] private InputActionReference toggleAction;

        [Tooltip("Action ปิดหน้าต่าง (Esc)")]
        [SerializeField] private InputActionReference closeAction;

        [Tooltip("ตัวเลื่อนแผงซ้าย/ขวาเข้าออก")]
        [SerializeField] private InventoryPanelSlider slider;

        [Header("Views")]
        [SerializeField] private ItemSlotView[] slots;
        [SerializeField] private StorageView bodyView;
        [SerializeField] private StorageView bagView;
        [SerializeField] private StorageView containerView;
        [SerializeField] private ItemDragController dragController;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private PlayerInventory inventory;
        private EquipmentManager equipment;

        // ที่เก็บที่เปิดคู่กันรอบล่าสุด null = เปิดแค่กระเป๋า ยังเก็บไว้ระหว่างแผงเลื่อนออก
        private ItemStorage container;

        private bool isOpen;

        private void Start()
        {
            inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                inventory.Storage.Changed += RefreshIfOpen;
            }

            equipment = EquipmentManager.Instance;
            if (equipment != null)
            {
                equipment.HandsChanged += RefreshIfOpen;
            }

            StorageContainer.OpenRequested += OpenWith;

            // สร้างตารางไว้ตั้งแต่ต้น ตอนเปิดครั้งแรกเลย์เอาต์จะได้เข้าที่แล้ว
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Storage.Changed -= RefreshIfOpen;
            if (equipment != null) equipment.HandsChanged -= RefreshIfOpen;
            StorageContainer.OpenRequested -= OpenWith;
            SetContainer(null);
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
                if (GameModeManager.GameplayInputAllowed && toggleAction.WasPressedThisFrame()) Open(null);
            }
            else if (toggleAction.WasPressedThisFrame() || closeAction.WasPressedThisFrame())
            {
                Close();
            }
        }

        private void OpenWith(StorageContainer opened) => Open(opened.Storage);

        private void Open(ItemStorage with)
        {
            SetContainer(with);
            isOpen = true;
            GameModeManager.Instance.SetMode(GameMode.Inventory);
            CursorManager.RequestUnlock(this);
            Refresh();
            slider.Open(with != null);
        }

        private void SetContainer(ItemStorage next)
        {
            if (container != null) container.Changed -= RefreshIfOpen;
            container = next;
            if (container != null) container.Changed += RefreshIfOpen;
            dragController.SetOpenContainer(container);
        }

        private void Close()
        {
            // แผงจะถูกปิดระหว่างลาก ทำให้ไม่มี EndDrag มาเก็บเงาให้
            dragController.Cancel();
            isOpen = false;
            slider.Close();
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

            if (container != null) containerView.Show(container, container.Grids);

            if (inventory == null) return;
            bodyView.Show(inventory.Storage, inventory.BodyGrids);
            bagView.Show(inventory.Storage, inventory.BagGrids);
        }
    }
}
