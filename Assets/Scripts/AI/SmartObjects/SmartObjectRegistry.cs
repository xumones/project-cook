using System.Collections.Generic;

namespace ProjectCook.AI
{
    /// <summary>SmartObject ทุกตัวที่ใช้ได้ตอนนี้ ตัวที่ฉากถูกซ่อนหรือถูกปิดจะหลุดออกไปเอง</summary>
    public static class SmartObjectRegistry
    {
        private static readonly List<SmartObject> all = new List<SmartObject>();

        public static IReadOnlyList<SmartObject> All => all;

        internal static void Register(SmartObject smartObject)
        {
            if (!all.Contains(smartObject)) all.Add(smartObject);
        }

        internal static void Unregister(SmartObject smartObject) => all.Remove(smartObject);
    }
}
