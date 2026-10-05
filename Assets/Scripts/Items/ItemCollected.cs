namespace ProjectCook.Items
{
    /// <summary>ข่าว: ผู้เล่นหยิบไอเทมชิ้นนี้สำเร็จ (เก็บเข้าที่เก็บ สวม หรือถือ)</summary>
    public readonly struct ItemCollected
    {
        public readonly ItemInstance Item;

        public ItemCollected(ItemInstance item)
        {
            Item = item;
        }
    }
}
