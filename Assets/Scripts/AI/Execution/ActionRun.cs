using UnityEngine;

namespace ProjectCook.AI
{
    public enum ActionStatus
    {
        Running,
        Succeeded,
        Failed
    }

    /// <summary>การกระทำหนึ่งครั้งที่กำลังทำอยู่ ถูกเรียกทุกเฟรมจนสำเร็จหรือล้มเหลว</summary>
    public abstract class ActionRun
    {
        private const float TurnDegreesPerSecond = 360f;

        public abstract ActionStatus Tick(float deltaTime);

        /// <summary>ถูกหยุดกลางคัน (หยุดคุยกับผู้เล่น ลืม หรือเปลี่ยนใจ) ต้องปล่อยทุกอย่างที่จับไว้</summary>
        public virtual void Stop() { }

        /// <summary>บอกสั้น ๆ ว่ากำลังทำอะไร เช่น "Walking to Sofa"</summary>
        public abstract string Describe();

        /// <summary>หันตัวเข้าหาทิศเป้าหมายเฉพาะแนวนอน true = หันครบแล้ว</summary>
        protected static bool TurnToward(Transform body, Quaternion rotation, float deltaTime)
        {
            Quaternion flat = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            body.rotation = Quaternion.RotateTowards(body.rotation, flat, TurnDegreesPerSecond * deltaTime);
            return body.rotation == flat;
        }
    }
}
