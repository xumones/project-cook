using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Inventory
{
    /// <summary>ที่เก็บของที่ตั้งอยู่ในฉาก (ตู้เย็น ลัง ตู้) กดใช้งานแล้วเปิดหน้าต่างคู่กับกระเป๋าผู้เล่น</summary>
    [DisallowMultipleComponent]
    public class StorageContainer : MonoBehaviour, IInteractable, IAimHintProvider
    {
        [Tooltip("ชื่อบนหัวหน้าต่าง")]
        [SerializeField] private string title = "STORAGE";

        [Tooltip("ช่องย่อยของที่เก็บนี้ ของที่ใส่แบบหาที่ว่างเองจะไหลลงตามลำดับในรายการ")]
        [SerializeField] private CompartmentLayout[] compartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 4) }
        };

        private ItemStorage storage;

        private void Awake()
        {
            storage = new ItemStorage(ItemGrid.FromLayouts(compartments));
        }

        public void Interact() => storage.RequestOpen(title);

        public AimHint GetAimHint() => AimHint.Use;
    }
}
