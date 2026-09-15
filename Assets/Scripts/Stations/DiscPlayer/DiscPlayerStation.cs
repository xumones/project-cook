using ProjectCook.Core;

namespace ProjectCook.Stations
{
    /// <summary>สถานีเครื่องเล่นแผ่น ใช้เมาส์กดปุ่มจึงปลดล็อกเคอร์เซอร์ตลอดที่อยู่ในสถานี</summary>
    public class DiscPlayerStation : Station
    {
        public override void EnterStation()
        {
            if (isActive) return;

            base.EnterStation();
            CursorManager.RequestUnlock(this);
        }

        public override void ExitStation()
        {
            if (!isActive) return;

            CursorManager.ReleaseUnlock(this);
            base.ExitStation();
        }
    }
}
