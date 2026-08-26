using UnityEngine;

namespace ProjectCook.Interaction
{
    /// <summary>
    /// Interface สำหรับโมดูลที่ทำงานเฉพาะตอนผู้เล่นเข้าใช้งานสถานี (Station Module)
    ///
    /// CookingStation จะรวบรวมโมดูลทั้งหมดที่อยู่บนตัวเองและลูกๆ แล้วสั่งเปิด/ปิดให้อัตโนมัติ
    /// สถานีใหม่จึงไม่ต้องเขียนโค้ดต่อสายโมดูลเองอีกต่อไป เพียงแค่แปะโมดูลที่ต้องการลงไป
    /// </summary>
    public interface IStationModule
    {
        /// <summary>
        /// ถูกเรียกเมื่อผู้เล่นเข้าใช้งานสถานี พร้อมส่งกล้องประจำสถานีให้ใช้อ้างอิงทิศทาง
        /// </summary>
        void OnStationEnter(Camera stationCamera);

        /// <summary>
        /// ถูกเรียกเมื่อผู้เล่นออกจากสถานี ให้โมดูลหยุดทำงานและคืนสถานะทุกอย่าง
        /// </summary>
        void OnStationExit();
    }
}
