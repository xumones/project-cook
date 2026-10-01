using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ProjectCook.Inventory;

namespace ProjectCook.UI
{
    /// <summary>วาดตารางกระเป๋าหนึ่งตาราง และแปลงตำแหน่งเมาส์เป็นช่อง การลากและคลิกของส่งต่อให้ ItemDragController</summary>
    // ต้องมี Graphic ที่ Raycast Target เต็มพื้นที่ตาราง ไม่งั้นเมาส์ตรงช่องว่างระหว่างช่องจะไม่นับว่าอยู่บนตาราง
    [RequireComponent(typeof(RectTransform))]
    public class ItemGridView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Templates")]
        [Tooltip("ช่องพื้นหลังหนึ่งช่อง (ปิดไว้) ถูกคัดลอกตามจำนวนช่องของตาราง")]
        [SerializeField] private RectTransform cellTemplate;

        [Tooltip("กล่องของหนึ่งชิ้น (ปิดไว้) ต้องมีข้อความ TMP เป็นลูกไว้แสดงรหัสไอเทม")]
        [SerializeField] private RectTransform itemTemplate;

        [Header("Layout")]
        [SerializeField] private float cellSize = 64f;
        [SerializeField] private float spacing = 4f;

        private ItemGrid grid;
        private readonly List<RectTransform> itemViews = new List<RectTransform>();
        private ItemDragController dragController;

        public ItemGrid Grid => grid;

        /// <summary>ที่เก็บที่ตารางนี้เป็นส่วนหนึ่ง</summary>
        public ItemStorage Storage { get; private set; }

        /// <summary>ระยะจากช่องหนึ่งถึงช่องถัดไป (ขนาดช่องรวมช่องว่าง)</summary>
        public float Step => cellSize + spacing;

        private void Awake()
        {
            dragController = GetComponentInParent<ItemDragController>();
        }

        public void Bind(ItemStorage owner, ItemGrid target)
        {
            Storage = owner;
            grid = target;
            ((RectTransform)transform).sizeDelta = SpanOf(grid.Size);

            for (int y = 0; y < grid.Size.y; y++)
            {
                for (int x = 0; x < grid.Size.x; x++)
                {
                    RectTransform cell = Instantiate(cellTemplate, transform);
                    cell.gameObject.SetActive(true);
                    Place(cell, new Vector2Int(x, y), Vector2Int.one);
                }
            }

            Refresh();
        }

        public void Refresh()
        {
            if (grid == null) return;

            IReadOnlyList<InventoryItem> items = grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform view = GetItemView(i);
                view.gameObject.SetActive(true);
                Place(view, items[i].Position, items[i].Size);
                view.GetComponentInChildren<TMP_Text>().text = items[i].Definition.ItemId;
            }

            for (int i = items.Count; i < itemViews.Count; i++)
            {
                itemViews[i].gameObject.SetActive(false);
            }
        }

        // กล่องของถูกสร้างหลังช่องพื้นหลังเสมอ จึงวาดทับช่องโดยไม่ต้องจัดลำดับเอง
        private RectTransform GetItemView(int index)
        {
            if (index < itemViews.Count) return itemViews[index];

            RectTransform view = Instantiate(itemTemplate, transform);
            itemViews.Add(view);
            return view;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!TryGetGridPoint(eventData.position, eventData.pressEventCamera, out Vector2 point)) return;

            InventoryItem item = grid.GetItemAt(Vector2Int.FloorToInt(point / Step));
            if (item == null) return;

            dragController.BeginDrag(this, item, point - (Vector2)item.Position * Step, eventData);
        }

        public void OnDrag(PointerEventData eventData) => dragController.Drag(eventData);

        public void OnEndDrag(PointerEventData eventData) => dragController.EndDrag(eventData);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!TryGetCell(eventData.position, eventData.pressEventCamera, out Vector2Int cell)) return;

            InventoryItem item = grid.GetItemAt(cell);
            if (item != null) dragController.TryQuickMove(Storage, item);
        }

        public bool TryGetCell(Vector2 screenPosition, Camera eventCamera, out Vector2Int cell)
        {
            bool inside = TryGetGridPoint(screenPosition, eventCamera, out Vector2 point);
            cell = Vector2Int.FloorToInt(point / Step);
            return inside;
        }

        /// <summary>ตำแหน่งมุมซ้ายบนของช่องในพิกัดโลก ใช้วางของที่ไม่ได้เป็นลูกของตารางนี้ให้ตรงช่อง</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            Rect rect = ((RectTransform)transform).rect;
            return transform.TransformPoint(new Vector3(rect.xMin + cell.x * Step, rect.yMax - cell.y * Step, 0f));
        }

        public Vector2 SpanOf(Vector2Int cells) =>
            new Vector2(cells.x * Step - spacing, cells.y * Step - spacing);

        // จุดบนตารางวัดจากมุมซ้ายบน x ไปทางขวา y ลงล่าง ให้ตรงกับพิกัดช่องของ ItemGrid
        private bool TryGetGridPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 point)
        {
            var rectTransform = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, eventCamera, out Vector2 local);

            Rect rect = rectTransform.rect;
            point = new Vector2(local.x - rect.xMin, rect.yMax - local.y);
            return point.x >= 0f && point.y >= 0f && point.x < rect.width && point.y < rect.height;
        }

        private void Place(RectTransform target, Vector2Int cell, Vector2Int size)
        {
            target.anchorMin = target.anchorMax = target.pivot = new Vector2(0f, 1f);
            target.anchoredPosition = new Vector2(cell.x * Step, -cell.y * Step);
            target.sizeDelta = SpanOf(size);
        }
    }
}
