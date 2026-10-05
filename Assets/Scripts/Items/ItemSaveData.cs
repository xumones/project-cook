using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Items
{
    /// <summary>ไอเทมหนึ่งชิ้นในรูปที่เขียนลงไฟล์ save ได้ อ้างชนิดด้วยรหัส ไม่ใช่ตัว asset</summary>
    [Serializable]
    public class ItemSaveData
    {
        [Serializable]
        public struct State
        {
            public string key;
            public string data;
        }

        public string itemId;
        public int flags;
        public List<State> states = new List<State>();

        public static ItemSaveData From(ItemInstance instance)
        {
            var saved = new ItemSaveData { itemId = instance.Definition.ItemId, flags = (int)instance.Flags };
            foreach (KeyValuePair<string, string> state in instance.States)
            {
                saved.states.Add(new State { key = state.Key, data = state.Value });
            }
            return saved;
        }

        /// <summary>สร้างตัวตนกลับคืน null ถ้าไอเทมชนิดนี้ไม่มีในเกมแล้ว</summary>
        public ItemInstance ToInstance()
        {
            ItemDefinitionSO definition = ItemDatabaseSO.Instance != null ? ItemDatabaseSO.Instance.Find(itemId) : null;
            if (definition == null)
            {
                Debug.LogWarning($"[ItemSaveData] ไม่พบไอเทมรหัส '{itemId}' ใน ItemDatabase ข้ามชิ้นนี้");
                return null;
            }

            var instance = new ItemInstance(definition);
            instance.AddFlag((ItemFlags)flags);
            foreach (State state in states)
            {
                instance.SetState(state.key, state.data);
            }
            return instance;
        }
    }
}
