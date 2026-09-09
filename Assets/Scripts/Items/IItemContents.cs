namespace ProjectCook.Items
{
    /// <summary>ไอเทมที่ "มีของอยู่ข้างในได้"</summary>
    public interface IItemContents
    {
        /// <summary>ตอนนี้มีของอยู่ข้างในหรือไม่</summary>
        bool HasContents { get; }
    }
}
