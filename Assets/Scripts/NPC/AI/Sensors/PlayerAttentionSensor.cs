using ProjectCook.AI;
using ProjectCook.Core;

namespace ProjectCook.NPC
{
    /// <summary>ผู้เล่นเล็งมาที่ NPC ในระยะคุยอยู่ไหม NPC จะได้หยุดรอให้กดคุยง่าย ๆ ไม่ต้องไล่ตาม</summary>
    // รอได้นานสุด MaxHoldSeconds ผู้เล่นที่ยืนจ้องเฉย ๆ จึงไม่ทำให้ NPC ค้าง ต้องเล็งออกก่อนถึงจะหยุดรอได้อีกรอบ
    public class PlayerAttentionSensor : Sensor
    {
        // เลิกเล็งแล้วยังรออีกนิด ผู้เล่นที่เล็งหลุดแป๊บเดียวจะได้ไม่เห็น NPC ออกเดินทันที
        private const float GraceSeconds = 1f;
        private const float MaxHoldSeconds = 5f;

        private readonly NPCCharacter self;
        private float aimedSince = float.NegativeInfinity;
        private float heldUntil;
        private bool aiming;

        public PlayerAttentionSensor(NPCCharacter self) : base(0.1f)
        {
            this.self = self;
        }

        public bool IsHolding(float now) => now < heldUntil;

        protected override bool Sense(float now)
        {
            PlayerAiming playerAiming = PlayerAiming.Instance;
            GameModeManager gameMode = GameModeManager.Instance;
            bool aimed = playerAiming != null && playerAiming.Target != null && gameMode != null && gameMode.CurrentMode == GameMode.Exploring
                && playerAiming.Target.GetComponentInParent<NPCCharacter>() == self;

            if (aimed && !aiming) aimedSince = now;
            aiming = aimed;
            if (aimed && now - aimedSince < MaxHoldSeconds) heldUntil = now + GraceSeconds;
            return false;
        }
    }
}
