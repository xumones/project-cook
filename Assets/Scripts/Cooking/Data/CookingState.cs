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
}
