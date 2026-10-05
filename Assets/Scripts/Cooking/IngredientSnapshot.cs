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
    }
}
