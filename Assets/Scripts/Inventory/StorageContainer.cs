using System;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Inventory
{
    /// <summary>ที่เก็บของที่ตั้งอยู่ในฉาก (ตู้เย็น ลัง ตู้) กดใช้งานแล้วเปิดหน้าต่างคู่กับกระเป๋าผู้เล่น</summary>
    [DisallowMultipleComponent]
    public class StorageContainer : MonoBehaviour, IInteractable, IAimHintProvider
    {
        [Tooltip("ช่องย่อยของที่เก็บนี้ ของที่ใส่แบบหาที่ว่างเองจะไหลลงตามลำดับในรายการ")]
        [SerializeField] private CompartmentLayout[] compartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 4) }
        };

        public ItemStorage Storage { get; private set; }

        /// <summary>ผู้เล่นกดใช้งานที่เก็บนี้ UI เป็นคนเปิดหน้าต่าง</summary>
        public static event Action<StorageContainer> OpenRequested;

        private void Awake()
        {
            Storage = new ItemStorage(ItemGrid.FromLayouts(compartments));
        }

        public void Interact() => OpenRequested?.Invoke(this);

        public AimHint GetAimHint() => AimHint.Use;
    }
}
