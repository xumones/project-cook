namespace ProjectCook.Core
{
    /// <summary>โหมดที่เกมอยู่ตอนนี้ เป็นตัวกำหนดว่าระบบไหนรับอินพุตและกล้องทำตัวอย่างไร</summary>
    public enum GameMode
    {
        Exploring,
        /// <summary>อยู่ในสถานีใดสถานีหนึ่ง กล้องล็อกกับสถานี</summary>
        Station,
        Dialogue
    }
}
