namespace ProjectCook.NPC
{
    /// <summary>
    /// Interface สำหรับส่วนประกอบใดๆ ที่สามารถรับคำสั่งฟังก์ชันพิเศษ (Action ID) จากตัวเลือกบทสนทนาได้
    /// </summary>
    public interface INPCActionHandler
    {
        /// <summary>
        /// ประมวลผลและเรียกใช้งานคำสั่งฟังก์ชันพิเศษตาม actionID ที่ได้รับ
        /// </summary>
        /// <param name="actionID">ไอดีคำสั่ง (เช่น 'OPEN_SHOP', 'GIVE_RECIPE', 'CLOSE')</param>
        void HandleAction(string actionID);
    }
}
