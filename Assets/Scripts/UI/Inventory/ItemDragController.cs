using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Inventory;
using ProjectCook.Items;

namespace ProjectCook.UI
{
    /// <summary>คุมการย้ายของด้วยเมาส์ในหน้าต่างกระเป๋า: ลาก (เงาตามเมาส์ หมุน ย้าย วางลงช่องติดตัว วางลงฉาก ย้ายของในมือลงที่เก็บ) และกดค้างคลิกเพื่อย้ายไปอีกฝั่ง</summary>
    public class ItemDragController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Action หมุนของระหว่างลาก (R)")]
        [SerializeField] private InputActionReference rotateAction;

        [Tooltip("Action ที่กดค้างแล้วคลิกของ เพื่อย้ายไปที่เก็บอีกฝั่งทันที (Shift)")]
        [SerializeField] private InputActionReference quickMoveAction;

        [Header("Views")]
        [Tooltip("พื้นที่ที่ปล่อยของแล้วไม่วางลงฉาก: แผงทุกแผงและแถบขอบจอที่ใช้สลับแผง")]
        [SerializeField] private RectTransform[] panels;

        [Tooltip("เงาของที่ลากอยู่ (ปิดไว้) ต้องปิด Raycast Target ไม่งั้นบังตารางที่อยู่ข้างใต้")]
        [SerializeField] private UnityEngine.UI.Image ghost;

        [SerializeField] private TMP_Text ghostLabel;

        [Header("Colors")]
        [SerializeField] private Color placeableColor = new Color(0.3f, 0.9f, 0.4f, 0.6f);
        [SerializeField] private Color blockedColor = new Color(0.95f, 0.3f, 0.3f, 0.6f);
        [Tooltip("สีตอนเมาส์อยู่นอกหน้าต่าง ปล่อยแล้วของจะถูกวางลงฉาก")]
        [SerializeField] private Color dropColor = new Color(1f, 1f, 1f, 0.5f);

        // ที่เก็บที่เปิดคู่กับผู้เล่นอยู่ null = เปิดแค่กระเป๋า
        private ItemStorage openContainer;

        // null = ของที่ลากไม่ได้มาจากตาราง (มาจากช่องติดตัว)
        private ItemGridView source;

        // มือที่ของที่ลากอยู่มาจาก null = ไม่ได้ลากจากมือ
        private HandSide? heldSide;
        private Vector2 heldSlotSize;

        // null = ของที่ลากวางลงตารางไม่ได้ (เช่น กระเป๋าที่สวมอยู่) ปล่อยได้แค่นอกหน้าต่าง
        private InventoryItem item;
        private bool rotated;

        // สิ่งที่เกิดตอนปล่อยนอกหน้าต่าง ไม่เป็น null ตลอดที่กำลังลาก
        private Action dropOutside;

        // ระยะจากมุมซ้ายบนของของถึงจุดที่จับ (x ขวา y ลง) เงาจะได้ไม่กระโดดตอนเริ่มลาก
        private Vector2 grabOffset;

        private ItemGridView target;

        // ช่องของติดตัวที่เมาส์ชี้อยู่ ใช้เมื่อไม่ได้ชี้ตาราง
        private ItemSlotView slotTarget;
        private Vector2Int targetCell;
        private bool canPlace;
        private Vector2 pointer;
        private Camera eventCamera;

        private void OnDisable()
        {
            Cancel();
        }

        public void SetOpenContainer(ItemStorage container)
        {
            openContainer = container;
        }

        /// <summary>ถ้ากดปุ่มย้ายด่วนค้างอยู่ ย้ายของไปที่ว่างแรกของที่เก็บอีกฝั่ง (ผู้เล่น ↔ ที่เก็บที่เปิดอยู่)</summary>
        public void TryQuickMove(ItemStorage from, InventoryItem clicked)
        {
            if (openContainer == null || !quickMoveAction.IsPressed()) return;

            ItemStorage player = PlayerInventory.Instance.Storage;
            ItemStorage.TryTransfer(clicked, from, from == player ? openContainer : player);
        }

        public void BeginDrag(ItemGridView from, InventoryItem dragged, Vector2 grab, PointerEventData eventData)
        {
            source = from;
            item = dragged;
            rotated = dragged.IsRotated;
            grabOffset = grab;
            dropOutside = () => PlayerItemTransfer.Instance.DropFromStorage(from.Storage, dragged);
            ShowGhost(source.SpanOf(item.Size), item.Definition.GetDisplayName(), eventData);
        }

        /// <summary>ลากของในมือ ปล่อยลงตารางของที่เก็บที่รับได้ หรือนอกหน้าต่างเพื่อวางลงฉาก</summary>
        public void BeginHeldDrag(HandSide side, InventoryItem held, Vector2 slotSize, PointerEventData eventData)
        {
            heldSide = side;
            heldSlotSize = slotSize;
            item = held;
            rotated = false;
            grabOffset = slotSize * 0.5f;
            dropOutside = () => PlayerItemTransfer.Instance.DropFromHand(side);
            ShowGhost(slotSize, held.Definition.GetDisplayName(), eventData);
        }

        /// <summary>ลากของที่วางลงตารางไม่ได้ ปล่อยนอกหน้าต่างแล้วเรียก drop</summary>
        public void BeginDropOnlyDrag(string label, Vector2 size, Action drop, PointerEventData eventData)
        {
            grabOffset = size * 0.5f;
            dropOutside = drop;
            ShowGhost(size, label, eventData);
        }

        public void Drag(PointerEventData eventData)
        {
            if (dropOutside == null) return;

            pointer = eventData.position;
            eventCamera = eventData.pressEventCamera;
            GameObject hovered = eventData.pointerCurrentRaycast.gameObject;
            target = item != null && hovered != null ? hovered.GetComponentInParent<ItemGridView>() : null;
            slotTarget = source != null && hovered != null && target == null ? hovered.GetComponentInParent<ItemSlotView>() : null;
            UpdateGhost();
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (dropOutside == null) return;

            Drag(eventData);
            if (target != null)
            {
                if (canPlace && heldSide.HasValue) PlayerItemTransfer.Instance.StoreFromHand(heldSide.Value, target.Storage, target.Grid, targetCell, rotated);
                else if (canPlace) ItemStorage.TryMove(item, source.Storage, target.Storage, target.Grid, targetCell, rotated);
            }
            else if (slotTarget != null)
            {
                slotTarget.TryTake(source.Storage, item);
            }
            else if (IsOutsidePanel)
            {
                dropOutside();
            }
            Cancel();
        }

        public void Cancel()
        {
            item = null;
            source = null;
            heldSide = null;
            target = null;
            slotTarget = null;
            dropOutside = null;
            if (ghost != null) ghost.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (item != null && rotateAction.WasPressedThisFrame()) Rotate();
        }

        private void Rotate()
        {
            rotated = !rotated;
            grabOffset = new Vector2(grabOffset.y, grabOffset.x);
            if (source != null) ghost.rectTransform.sizeDelta = source.SpanOf(item.SizeWhen(rotated));
            UpdateGhost();
        }

        // ของจากมือเข้าได้เฉพาะที่เก็บที่รับของถือ ส่วนของจากตารางต้องผ่านกฎของทั้งสองที่เก็บ
        private bool CanMoveTo(ItemStorage to) =>
            heldSide.HasValue ? to.CanAcceptFromHand(item) : ItemStorage.CanMove(item, source.Storage, to);

        private void ShowGhost(Vector2 size, string label, PointerEventData eventData)
        {
            ghost.rectTransform.sizeDelta = size;
            ghostLabel.text = label;
            ghost.gameObject.SetActive(true);
            Drag(eventData);
        }

        // พื้นที่ที่ปิดอยู่ (เช่น แผงตู้เย็นตอนเปิดแค่กระเป๋า แถบขอบที่ใช้ไม่ได้) ไม่นับ
        private bool IsOutsidePanel
        {
            get
            {
                foreach (RectTransform panel in panels)
                {
                    if (panel.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(panel, pointer, eventCamera)) return false;
                }
                return true;
            }
        }

        private void UpdateGhost()
        {
            RectTransform ghostRect = ghost.rectTransform;

            // ของจากมือไม่มีตารางต้นทาง เงาจึงเปลี่ยนขนาดตามตารางที่ชี้อยู่ และจับที่กลางเงาเสมอ
            if (heldSide.HasValue)
            {
                ghostRect.sizeDelta = target != null ? target.SpanOf(item.SizeWhen(rotated)) : heldSlotSize;
                grabOffset = ghostRect.sizeDelta * 0.5f;
            }

            if (target != null && target.TryGetCell(pointer, eventCamera, out Vector2Int pointerCell))
            {
                targetCell = pointerCell - Vector2Int.FloorToInt(grabOffset / target.Step);
                canPlace = target.Grid.CanPlace(item, targetCell, rotated) && CanMoveTo(target.Storage);
                ghostRect.position = target.CellToWorld(targetCell);
                ghost.color = canPlace ? placeableColor : blockedColor;
                return;
            }

            target = null;
            canPlace = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghostRect.parent, pointer, eventCamera, out Vector2 local);
            ghostRect.localPosition = local + new Vector2(-grabOffset.x, grabOffset.y);

            if (slotTarget != null)
            {
                ghost.color = slotTarget.CanTake(source.Storage, item) ? placeableColor : blockedColor;
                return;
            }

            // ของที่ไม่ได้มาจากตารางไม่มีกฎที่เก็บมาบังคับ
            bool canDrop = IsOutsidePanel && (source == null || source.Storage.CanRelease(item));
            ghost.color = canDrop ? dropColor : blockedColor;
        }
    }
}
