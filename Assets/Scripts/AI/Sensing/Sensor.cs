namespace ProjectCook.AI
{
    /// <summary>ตัวสำรวจโลกหนึ่งเรื่อง ทำงานตามรอบเวลาของตัวเอง แล้วจดผลลงความจำหรือข้อเท็จจริง ตัววางแผนไม่ต้องทำงานหนักเอง</summary>
    // รอบ 0 = ทุกครั้งที่ถูกเรียก
    public abstract class Sensor
    {
        private readonly float interval;
        private float nextTime;

        protected Sensor(float interval)
        {
            this.interval = interval;
        }

        /// <summary>true = เห็นการเปลี่ยนแปลงสำคัญ สมองควรคิดใหม่</summary>
        public bool Update(float now)
        {
            if (now < nextTime) return false;

            nextTime = now + interval;
            return Sense(now);
        }

        protected abstract bool Sense(float now);
    }
}
