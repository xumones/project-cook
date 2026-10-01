using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectCook.Inventory;

namespace ProjectCook.UI
{
    /// <summary>วาดตารางย่อยทั้งชุดของที่เก็บของหนึ่งชิ้น (ตัวผู้เล่น กระเป๋า ตู้เย็น) ตามตำแหน่งในข้อมูล และซ่อนตัวเองเมื่อไม่มีตาราง</summary>
    [RequireComponent(typeof(RectTransform))]
    public class StorageView : MonoBehaviour
    {
        [Tooltip("ต้นแบบตาราง (ปิดไว้) ถูกคัดลอกหนึ่งชุดต่อหนึ่งตาราง")]
        [SerializeField] private ItemGridView gridTemplate;

        [Tooltip("หัวข้อของส่วนนี้ ซ่อนพร้อมกันตอนไม่มีตาราง")]
        [SerializeField] private GameObject title;

        private readonly List<ItemGridView> views = new List<ItemGridView>();
        private IReadOnlyList<ItemGrid> boundGrids;

        /// <summary>สร้างตารางใหม่เฉพาะตอนได้ชุดตารางใหม่ (สวมหรือถอดกระเป๋า เปิดที่เก็บอื่น) ไม่งั้นแค่วาดของใหม่</summary>
        public void Show(ItemStorage owner, IReadOnlyList<ItemGrid> grids)
        {
            if (grids != boundGrids)
            {
                Rebuild(owner, grids);
            }

            foreach (ItemGridView view in views)
            {
                view.Refresh();
            }
        }

        private void Rebuild(ItemStorage owner, IReadOnlyList<ItemGrid> grids)
        {
            boundGrids = grids;
            foreach (ItemGridView view in views)
            {
                Destroy(view.gameObject);
            }
            views.Clear();

            // เปิดก่อนสร้างตาราง Awake ของตารางใหม่จะได้ทำงานทันที
            bool visible = grids.Count > 0;
            gameObject.SetActive(visible);
            title.SetActive(visible);

            Vector2 extent = Vector2.zero;
            foreach (ItemGrid grid in grids)
            {
                ItemGridView view = Instantiate(gridTemplate, transform);
                view.gameObject.SetActive(true);
                view.Bind(owner, grid);

                var rect = (RectTransform)view.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(grid.LayoutPosition.x, -grid.LayoutPosition.y) * view.Step;
                extent = Vector2.Max(extent, new Vector2(rect.anchoredPosition.x, -rect.anchoredPosition.y) + rect.sizeDelta);
                views.Add(view);
            }

            ((RectTransform)transform).sizeDelta = extent;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform.parent);
        }
    }
}
