namespace ProjectCook.Core
{
    /// <summary>สิ่งที่ผู้เล่นกดค้างแล้วลากเมาส์แนวนอนเพื่อหมุนปรับค่าได้ (ลูกบิด ก๊อก ฯลฯ)</summary>
    public interface ITurnable
    {
        /// <summary>ระยะเมาส์แนวนอนเฟรมนี้ (พิกเซล) ขวาเป็นบวก</summary>
        void Turn(float horizontalDelta);

        void Release();
    }
}
