using ProjectCook.Items;

namespace ProjectCook.Cooking
{
    /// <summary>ภาพนิ่งของวัตถุดิบหนึ่งชิ้นตอนถูกส่ง ระบบที่ให้คะแนนอ่านจากตรงนี้ จึงไม่ต้องมีวัตถุจริงในฉาก</summary>
    public readonly struct IngredientSnapshot
    {
        public readonly IngredientDataSO Data;
        public readonly CookingState State;

        /// <summary>ความสุก 0-1 ใช้ให้คะแนนชิ้นที่ยังทอดไม่เสร็จ</summary>
        public readonly float Progress;

        public IngredientSnapshot(IngredientDataSO data, CookingState state, float progress)
        {
            Data = data;
            State = state;
            Progress = progress;
        }

        /// <summary>ภาพนิ่งของวัตถุดิบที่เป็นข้อมูลอยู่ (ไม่มีวัตถุในฉาก) false ถ้าไอเทมนี้ไม่ใช่วัตถุดิบ</summary>
        public static bool TryFrom(ItemInstance item, out IngredientSnapshot snapshot)
        {
            if (!(item.Definition is IngredientDataSO data))
            {
                snapshot = default;
                return false;
            }

            var progress = new CookingProgress(data);
            if (item.TryGetState(IngredientInstance.CookTimesKey, out string times)) progress.Restore(times);
            snapshot = new IngredientSnapshot(data, progress.State, progress.Progress);
            return true;
        }
    }
}
