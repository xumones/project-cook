namespace ProjectCook.Items
{
    /// <summary>component ที่มีสถานะต้องติดไปกับไอเทม ตอนไอเทมกลายเป็นข้อมูล (เข้าที่เก็บ หรือ save)</summary>
    public interface ISavableState
    {
        /// <summary>รหัสคงที่ของสถานะนี้ ห้ามเปลี่ยนหลังมี save ไอเทมหนึ่งชิ้นห้ามมีรหัสซ้ำกัน</summary>
        string StateKey { get; }

        string CaptureState();

        /// <summary>ตั้งสถานะจากข้อความที่ CaptureState เคยจดไว้</summary>
        void RestoreState(string state);
    }
}
