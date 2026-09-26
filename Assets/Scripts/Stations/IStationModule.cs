namespace ProjectCook.Stations
{
    /// <summary>โมดูลที่ทำงานเฉพาะตอนผู้เล่นเข้าใช้งาน Station เปิด/ปิดให้อัตโนมัติ กล้องระหว่างอยู่ในสถานีคือ Camera.main เสมอ</summary>
    public interface IStationModule
    {
        void OnStationEnter();
        void OnStationExit();
    }

    /// <summary>เจ้าของชุดโมดูล (สถานี, ภาชนะบนเตา) โมดูลเป็นของเจ้าของที่ใกล้ที่สุดเหนือมันเท่านั้น</summary>
    public interface IStationModuleOwner
    {
    }
}
