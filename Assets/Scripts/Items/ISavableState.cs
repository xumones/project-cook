namespace ProjectCook.Items
{
    /// <summary>component ที่มีสถานะต้องติดไปกับไอเทมตอนไอเทมถูกเก็บเป็นข้อมูล (เข้ากระเป๋า หรือ Save)</summary>
    public interface ISavableState
    {
        string CaptureState();

        /// <summary>ตั้งสถานะจากข้อความที่ CaptureState ของ component ชนิดเดียวกันเคยจดไว้</summary>
        void RestoreState(string state);
    }

    /// <summary>สถานะที่จดไว้ของ component หนึ่งตัว ชื่อชนิดใช้ตรวจว่าคืนให้ถูกตัว</summary>
    public readonly struct ComponentState
    {
        public string ComponentType { get; }
        public string Data { get; }

        public ComponentState(string componentType, string data)
        {
            ComponentType = componentType;
            Data = data;
        }
    }
}
