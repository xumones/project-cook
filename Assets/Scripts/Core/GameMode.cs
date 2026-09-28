namespace ProjectCook.Core
{
    /// <summary>โหมดที่เกมอยู่ตอนนี้ เป็นตัวกำหนดว่าระบบไหนรับอินพุตและกล้องทำตัวอย่างไร</summary>
    public enum GameMode
    {
        Exploring,
        /// <summary>อยู่ในสถานีใดสถานีหนึ่ง กล้องล็อกกับสถานี</summary>
        Station,
        Dialogue,
        /// <summary>เปิดหน้าต่างกระเป๋า เมาส์ใช้จัดของ ผู้เล่นเดินและหันกล้องไม่ได้ แต่เวลาในเกมยังเดินต่อ</summary>
        Inventory
    }
}
