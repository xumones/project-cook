using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.NPC
{
    /// <summary>มองหาของในบ้านที่อยู่ในสายตา แล้วจดลงความจำว่าว่างหรือมีคนใช้ ของที่มองไม่เห็นจะเชื่อแบบเดิมต่อไป</summary>
    // เห็นแล้วก็ยังพลาดได้ (ไม่ทันสังเกต) ตามความไม่สมบูรณ์ ความเชื่อเดิมจึงค้างอยู่ทั้งที่ความจริงเปลี่ยนแล้ว
    public class SmartObjectSensor : Sensor
    {
        private const float SightRange = 10f;
        private const float EyeHeight = 1.6f;

        // มองไปที่ระดับตาเหนือจุดใช้ของ ซึ่งเป็นที่โล่งเสมอ ตัวของเองจึงไม่บังสายตา
        private const float LookHeight = 1.2f;

        private readonly Transform body;
        private readonly AgentMemory memory;
        private readonly ImperfectionRoller imperfection;
        private readonly System.Func<float> wellbeing;
        private readonly System.Func<SmartObject> currentTarget;
        private readonly int wallMask;

        public SmartObjectSensor(Transform body, AgentMemory memory, ImperfectionRoller imperfection,
            System.Func<float> wellbeing, System.Func<SmartObject> currentTarget) : base(0.5f)
        {
            this.body = body;
            this.memory = memory;
            this.imperfection = imperfection;
            this.wellbeing = wellbeing;
            this.currentTarget = currentTarget;
            wallMask = LayerMask.GetMask("Default");
        }

        /// <summary>true = เพิ่งรู้ว่าของที่กำลังจะไปใช้ไม่ว่าง</summary>
        protected override bool Sense(float now)
        {
            bool targetLost = false;
            Vector3 eye = body.position + Vector3.up * EyeHeight;
            foreach (SmartObject smartObject in SmartObjectRegistry.All)
            {
                Vector3 look = smartObject.UsePosition + Vector3.up * LookHeight;
                if ((look - eye).sqrMagnitude > SightRange * SightRange || Physics.Linecast(eye, look, wallMask, QueryTriggerInteraction.Ignore)) continue;

                bool free = !smartObject.IsOccupied;
                bool known = memory.TryRecall(smartObject, now, out bool believed, out _);
                if (known && believed != free && imperfection.RollMiss(wellbeing(), now)) continue;

                memory.Observe(smartObject, free, now);
                if (!free && (!known || believed) && smartObject == currentTarget()) targetLost = true;
            }
            return targetLost;
        }
    }
}
