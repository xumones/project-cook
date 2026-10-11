namespace ProjectCook.Core
{
    /// <summary>โหมดที่เกมอยู่ตอนนี้ เป็นตัวกำหนดว่าระบบไหนรับอินพุตและกล้องทำตัวอย่างไร</summary>
    // โหมดใหม่ต่อท้ายเสมอ ค่าในฉาก (เช่นตารางของ InputContextSwitcher) เก็บเป็นตัวเลข
    public enum GameMode
    {
        Exploring,
        /// <summary>อยู่ในสถานีใดสถานีหนึ่ง กล้องล็อกกับสถานี</summary>
        Station,
        Dialogue,
        /// <summary>เปิดหน้าต่างกระเป๋า เมาส์ใช้จัดของ ผู้เล่นเดินและหันกล้องไม่ได้ แต่เวลาในเกมยังเดินต่อ</summary>
        Inventory,
        /// <summary>กำลังย้ายพื้นที่ ทุกระบบหยุดรับอินพุตและผู้เล่นไม่ขยับจนกว่าพื้นที่ใหม่จะพร้อม</summary>
        Loading,
        /// <summary>กำลังเล่น cutscene ผู้เล่นเดินไม่ได้ ส่วนโลกจะหยุดด้วยไหมขึ้นกับชนิดของ cutscene</summary>
        Cutscene
    }
}
