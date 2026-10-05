namespace ProjectCook.Core
{
    /// <summary>ส่วนหนึ่งของไฟล์ save ที่ระบบหนึ่งเป็นเจ้าของ ระบบจดและคืนข้อมูลของตัวเองเป็นข้อความ (JSON)</summary>
    public interface ISaveSection
    {
        /// <summary>รหัสคงที่ของส่วนนี้ ห้ามเปลี่ยนหลังมี save และห้ามซ้ำกับส่วนอื่น</summary>
        string SectionKey { get; }

        string Capture();

        void Restore(string data);
    }
}
