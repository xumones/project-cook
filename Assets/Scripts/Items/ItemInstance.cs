using System;
using System.Collections.Generic;

namespace ProjectCook.Items
{
    /// <summary>ที่มาของไอเทมชิ้นหนึ่ง ใช้เป็นเงื่อนไขของเควสได้ เช่น ต้องเป็นวัตถุดิบที่หามาเอง</summary>
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        /// <summary>หยิบได้จากพื้นที่ข้างนอกบ้าน</summary>
        FoundOutside = 1 << 0
    }

    /// <summary>ไอเทมหนึ่งชิ้น ตัวตนเดียวที่ติดตัวไปทั้งตอนอยู่ในโลก ในมือ และในที่เก็บ</summary>
    public class ItemInstance
    {
        // สถานะของ component แต่ละตัว อ้างด้วย StateKey ที่ไม่เปลี่ยนตามชื่อ class
        private readonly Dictionary<string, string> states = new Dictionary<string, string>();

        public ItemDefinitionSO Definition { get; }

        public ItemFlags Flags { get; private set; }

        public ItemInstance(ItemDefinitionSO definition)
        {
            Definition = definition;
        }

        public bool HasFlag(ItemFlags flag) => (Flags & flag) == flag;

        public void AddFlag(ItemFlags flag) => Flags |= flag;

        public IReadOnlyDictionary<string, string> States => states;

        public bool TryGetState(string key, out string data) => states.TryGetValue(key, out data);

        public void SetState(string key, string data) => states[key] = data;
    }
}
