using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ผู้ตัดสินว่ากดหยิบใส่เป้าที่เล็งอยู่แล้วเกิดอะไร ใช้เมื่อเป้าไม่ใช่ของที่ใช้งานได้ (IInteractable)</summary>
    public interface IPickUpHandler
    {
        bool TryPickUp(Collider target);
    }
}
