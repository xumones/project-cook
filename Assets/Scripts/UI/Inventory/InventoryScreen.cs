using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ProjectCook.Core;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.UI
{
    /// <summary>หน้าต่างกระเป๋า (แผงซ้าย) และที่เก็บที่เปิดคู่กัน (แผงขวา): เปิด/ปิด เข้าโหมดกระเป๋า ปลดล็อกเมาส์ วาดใหม่เมื่อของเปลี่ยน และปุ่มยืนยันของที่เก็บที่ขอมา</summary>
    public class InventoryScreen : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Action เปิดหน้าต่าง (World/OpenInventory)")]
        [SerializeField] private InputActionReference openAction;

        [Tooltip("Action ปิดหน้าต่าง (Menu/Close)")]
        [SerializeField] private InputActionReference closeAction;

        [Tooltip("ตัวเลื่อนแผงซ้าย/ขวาเข้าออก")]
        [SerializeField] private InventoryPanelSlider slider;

        [Header("Views")]
        [SerializeField] private ItemSlotView[] slots;
        [SerializeField] private StorageView bodyView;
        [SerializeField] private StorageView bagView;
        [SerializeField] private StorageView containerView;
        [SerializeField] private TMP_Text containerTitle;

        [Tooltip("ปุ่มยืนยันในแผงขวา แสดงเฉพาะที่เก็บที่ขอปุ่มนี้ (กล่องให้ของ)")]
        [SerializeField] private Button confirmButton;

        [SerializeField] private ItemDragController dragController;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private PlayerInventory inventory;
        private EquipmentManager equipment;

        // ที่เก็บที่เปิดคู่กันรอบล่าสุด null = เปิดแค่กระเป๋า ยังเก็บไว้ระหว่างแผงเลื่อนออก
        private ItemStorage container;

        // คำขอที่เปิดหน้าต่างอยู่ ล้างตอนปิด
        private StorageWindow window;

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

            ItemStorage.OpenRequested += Open;
            confirmButton.onClick.AddListener(Confirm);

            // สร้างตารางไว้ตั้งแต่ต้น ตอนเปิดครั้งแรกเลย์เอาต์จะได้เข้าที่แล้ว
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Storage.Changed -= RefreshIfOpen;
            if (equipment != null) equipment.HandsChanged -= RefreshIfOpen;
            ItemStorage.OpenRequested -= Open;
            confirmButton.onClick.RemoveListener(Confirm);
            SetContainer(null);
        }

        private void Update()
        {
            // ปุ่มเปิดอยู่ในชุด World (เดินและในสถานี) ปุ่มปิด (Tab/Esc) อยู่ในชุด Menu ที่เปิดเฉพาะตอนหน้าต่างอยู่บนสุด
            if (!isOpen && openAction.WasPressedThisFrame()) Open(default);
            else if (isOpen && closeAction.WasPressedThisFrame()) Close();
        }

        private void Open(StorageWindow request)
        {
            window = request;
            SetContainer(request.Storage);
            containerTitle.text = request.Title;
            confirmButton.gameObject.SetActive(request.Confirm != null);
            isOpen = true;
            GameModeManager.Instance.Enter(this, GameMode.Inventory);
            CursorManager.RequestUnlock(this);
            Refresh();
            slider.Open(request.Storage != null);
        }

        private void Confirm()
        {
            if (!isOpen) return;

            window.Confirm?.Invoke();
            Close();
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
            GameModeManager.Instance.Exit(this);

            // แจ้งหลังออกจากโหมดกระเป๋าแล้ว เจ้าของหน้าต่างจะได้เริ่มอย่างอื่นต่อได้ทันที (เช่น กลับไปคุย)
            Action closed = window.Closed;
            window = default;
            closed?.Invoke();
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
