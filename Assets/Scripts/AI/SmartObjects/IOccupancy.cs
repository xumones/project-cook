namespace ProjectCook.AI
{
    /// <summary>ของที่มีคนอื่น (เช่น ผู้เล่น) ใช้อยู่ได้ SmartObject บน GameObject เดียวกันหรือลูกจะถือว่าไม่ว่างตอนที่มีคนใช้</summary>
    public interface IOccupancy
    {
        bool IsOccupied { get; }
    }
}
