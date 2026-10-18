namespace ProjectCook.Inventory
{
    /// <summary>กฎของที่เก็บที่ไม่ให้ของเข้าออกอิสระ (เช่น ร้านค้า) ใช้ตอนของข้ามไปที่เก็บอื่นหรือลงฉาก ย้ายในที่เก็บเดิมไม่นับ</summary>
    public interface IStorageRules
    {
        bool CanRelease(InventoryItem item);

        bool CanAccept(InventoryItem item);

        /// <summary>รับของจากมือที่ปกติเก็บไม่ได้ (เช่นจาน) ด้วยไหม</summary>
        bool AcceptsHeldItems { get; }

        /// <summary>ราคาที่ต้องจ่ายถ้าเอาของชิ้นนี้ออก 0 = ฟรี</summary>
        int PriceOf(InventoryItem item);

        /// <summary>หลังของออกจากที่เก็บนี้ไปแล้ว</summary>
        void OnReleased(InventoryItem item);
    }
}
