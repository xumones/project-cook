using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>ความอยากที่ยังไม่มีใครเติมให้ทำให้หงุดหงิด ได้สิ่งที่อยากแล้วหายอยากและหงุดหงิดลดลง</summary>
    // ความอยากที่ค้างเป็น "พื้น" ของความหงุดหงิดที่ค่อย ๆ สูงขึ้นจนถึงเพดาน ความหงุดหงิดจางลงไม่ต่ำกว่าพื้นนี้
    // (ถ้าบวกเพิ่มทีละนิด อารมณ์ที่จางเร็วกว่าจะลบทิ้งทุกเฟรม) หายอยากแล้วพื้นกลับเป็น 0
    public class WantFrustration
    {
        private readonly FeelingSO frustration;
        private readonly float perMinute;
        private readonly float cap;
        private readonly float relief;
        private float floor;

        public WantFrustration(FeelingSO frustration, float perMinute, float cap, float relief)
        {
            this.frustration = frustration;
            this.perMinute = perMinute;
            this.cap = cap;
            this.relief = relief;
        }

        /// <summary>มีความอยากที่ถึงเกณฑ์แล้วอย่างน้อยหนึ่งอย่าง</summary>
        public static bool IsWanting(MeterSet meters)
        {
            foreach (MeterSO meter in meters.Meters)
            {
                if (meter is WantSO want && want.IsActive(meters.Get(want))) return true;
            }
            return false;
        }

        public void Advance(MeterSet meters, float minutes)
        {
            if (frustration == null) return;

            floor = IsWanting(meters) ? Mathf.Min(cap, floor + perMinute * minutes) : 0f;
            if (meters.Get(frustration) < floor) meters.Set(frustration, floor);
        }

        /// <summary>มีคนเติมสิ่งที่อยากให้แล้ว (เช่น ผู้เล่นส่งอาหารที่ขอ)</summary>
        public void Fulfill(MeterSet meters)
        {
            foreach (MeterSO meter in meters.Meters)
            {
                if (meter is WantSO) meters.Set(meter, 0f);
            }
            floor = 0f;
            if (frustration != null) meters.Change(frustration, -relief);
        }
    }
}
