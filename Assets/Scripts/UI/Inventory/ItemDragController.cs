using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Inventory;

namespace ProjectCook.UI
{
    /// <summary>คุมการย้ายของด้วยเมาส์ในหน้าต่างกระเป๋า: ลาก (เงาตามเมาส์ หมุน ย้าย วางลงฉาก) และกดค้างคลิกเพื่อย้ายไปอีกฝั่ง</summary>
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

        private ItemGridView source;

        // null = ของที่ลากวางลงตารางไม่ได้ (เช่น กระเป๋าที่สวมอยู่) ปล่อยได้แค่นอกหน้าต่าง
        private InventoryItem item;
        private bool rotated;

        // สิ่งที่เกิดตอนปล่อยนอกหน้าต่าง ไม่เป็น null ตลอดที่กำลังลาก
        private Action dropOutside;

        // ระยะจากมุมซ้ายบนของของถึงจุดที่จับ (x ขวา y ลง) เงาจะได้ไม่กระโดดตอนเริ่มลาก
        private Vector2 grabOffset;

        private ItemGridView target;
        private Vector2Int targetCell;
        private bool canPlace;
        private Vector2 pointer;
        private Camera eventCamera;

        private void OnEnable()
        {
            rotateAction.EnableAction();
            quickMoveAction.EnableAction();
        }

        private void OnDisable()
        {
            rotateAction.DisableAction();
            quickMoveAction.DisableAction();
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
            ShowGhost(source.SpanOf(item.Size), item.Definition.ItemId, eventData);
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
            UpdateGhost();
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (dropOutside == null) return;

            Drag(eventData);
            if (target != null)
            {
                if (canPlace) ItemStorage.TryMove(item, source.Storage, target.Storage, target.Grid, targetCell, rotated);
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
            target = null;
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
            ghost.rectTransform.sizeDelta = source.SpanOf(item.SizeWhen(rotated));
            UpdateGhost();
        }

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

            if (target != null && target.TryGetCell(pointer, eventCamera, out Vector2Int pointerCell))
            {
                targetCell = pointerCell - Vector2Int.FloorToInt(grabOffset / target.Step);
                canPlace = target.Grid.CanPlace(item, targetCell, rotated);
                ghostRect.position = target.CellToWorld(targetCell);
                ghost.color = canPlace ? placeableColor : blockedColor;
                return;
            }

            target = null;
            canPlace = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)ghostRect.parent, pointer, eventCamera, out Vector2 local);
            ghostRect.localPosition = local + new Vector2(-grabOffset.x, grabOffset.y);
            ghost.color = IsOutsidePanel ? dropColor : blockedColor;
        }
    }
}
