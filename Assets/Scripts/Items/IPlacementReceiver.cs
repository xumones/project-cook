using UnityEngine;

namespace ProjectCook.Items
{
    /// <summary>ไอเทมที่อยากรู้ว่าถูกวางชนผิวอะไร เช่น โน้ตที่แปะติดกระดาน</summary>
    public interface IPlacementReceiver
    {
        /// <summary>เรียกหลังวางลงฉาก surface.collider เป็น null ถ้าวางลอยไม่ชนอะไร</summary>
        void OnPlaced(RaycastHit surface);
    }
}
