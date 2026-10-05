namespace ProjectCook.Cooking
{
    /// <summary>ความสุกของวัตถุดิบหนึ่งชิ้น: สะสมเวลาทอดแต่ละด้านแล้วตัดสินสถานะ ไม่รู้จักวัตถุในฉาก</summary>
    // อ่านเวลาจาก data ทุกครั้ง ปรับค่าใน Inspector ระหว่างกด Play แล้วมีผลทันที
    public class CookingProgress
    {
        private readonly IngredientDataSO data;

        public CookingState State { get; private set; } = CookingState.Raw;

        public float OmniTime { get; private set; }
        public float SideATime { get; private set; }
        public float SideBTime { get; private set; }

        public CookingProgress(IngredientDataSO data)
        {
            this.data = data;
        }

        /// <summary>ความสุก 0-1 ไหม้แล้วยังเป็น 1</summary>
        public float Progress
        {
            get
            {
                if (data.CookTime <= 0f) return 0f;
                float done = data.IsTwoSidedCooking ? (SideATime + SideBTime) / (data.CookTime * 2f) : OmniTime / data.CookTime;
                return done < 0f ? 0f : done > 1f ? 1f : done;
            }
        }

        /// <summary>เพิ่มความร้อนให้ด้านที่แตะผิว ชิ้นสองด้านที่ตะแคงอยู่ (Omni) แบ่งให้สองด้านเท่ากัน คืน true ถ้าสถานะเปลี่ยน</summary>
        public bool AddHeat(float amount, CookingSide side)
        {
            if (State == CookingState.Burnt || amount <= 0f) return false;

            if (!data.IsTwoSidedCooking) OmniTime += amount;
            else if (side == CookingSide.SideA) SideATime += amount;
            else if (side == CookingSide.SideB) SideBTime += amount;
            else
            {
                SideATime += amount * 0.5f;
                SideBTime += amount * 0.5f;
            }

            return Evaluate();
        }

        /// <summary>คืนเวลาทอดจาก save สถานะคำนวณจากเวลาได้ทั้งหมด</summary>
        public void Restore(float omni, float sideA, float sideB)
        {
            OmniTime = omni;
            SideATime = sideA;
            SideBTime = sideB;
            State = CookingState.Raw;
            Evaluate();
        }

        private bool Evaluate()
        {
            CookingState next = data.IsTwoSidedCooking ? TwoSidedState() : OmniState();
            if (next == State) return false;

            State = next;
            return true;
        }

        private CookingState OmniState()
        {
            if (OmniTime >= data.CookTime + data.BurnTime) return CookingState.Burnt;
            if (OmniTime >= data.CookTime) return CookingState.Cooked;
            return OmniTime > 0f ? CookingState.Cooking : State;
        }

        private CookingState TwoSidedState()
        {
            float burnAt = data.CookTime + data.BurnTime;
            if (SideATime >= burnAt || SideBTime >= burnAt) return CookingState.Burnt;
            if (SideATime >= data.CookTime && SideBTime >= data.CookTime) return CookingState.Cooked;
            return SideATime > 0f || SideBTime > 0f ? CookingState.Cooking : State;
        }
    }
}
