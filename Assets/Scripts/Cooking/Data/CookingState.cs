namespace ProjectCook.Cooking
{
    /// <summary>
    /// สถานะความสุกของวัตถุดิบ
    /// </summary>
    public enum CookingState
    {
        Raw,        // ดิบ/สด
        Cooking,    // กำลังทอด
        Cooked,     // สุกพอดี
        Burnt       // ไหม้
    }

    /// <summary>
    /// ด้านของวัตถุดิบที่สัมผัสกับกระทะ/เตา
    /// </summary>
    public enum CookingSide
    {
        Omni,   // รอบทิศทาง (สำหรับของชิ้นเล็ก เช่น นักเก็ต, เฟรนช์ฟรายส์)
        SideA,  // ด้านล่าง (Bottom / Front side)
        SideB   // ด้านบน (Top / Back side)
    }
}
