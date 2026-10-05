using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Items
{
    /// <summary>รายชื่อไอเทมทุกชนิดในเกม ใช้หาชนิดจากรหัสตอนโหลด save อยู่ใน Resources จึงหาได้จากทุกระบบโดยไม่ต้องต่อสาย</summary>
    // Resources โหลดทุกอย่างที่อ้างถึงตั้งแต่เริ่มเกม พอไอเทมเยอะจนกินหน่วยความจำค่อยย้ายไป Addressables
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "ProjectCook/Items/Item Database")]
    public class ItemDatabaseSO : ScriptableObject
    {
        private const string ResourcePath = "ItemDatabase";

        [Tooltip("เลือก Refresh จากเมนู ⋮ มุมขวาบนของ Inspector เพื่อเก็บไอเทมทุกชนิดในโปรเจคให้อัตโนมัติ")]
        [SerializeField] private List<ItemDefinitionSO> items = new List<ItemDefinitionSO>();

        private static ItemDatabaseSO loaded;
        private Dictionary<string, ItemDefinitionSO> byId;

        public static ItemDatabaseSO Instance => loaded != null ? loaded : loaded = Resources.Load<ItemDatabaseSO>(ResourcePath);

        public ItemDefinitionSO Find(string itemId)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, ItemDefinitionSO>();
                foreach (ItemDefinitionSO item in items)
                {
                    if (item != null) byId[item.ItemId] = item;
                }
            }
            return itemId != null && byId.TryGetValue(itemId, out ItemDefinitionSO found) ? found : null;
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh")]
        private void Refresh()
        {
            items.Clear();
            var ids = new HashSet<string>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ItemDefinitionSO"))
            {
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.ItemId) || !ids.Add(item.ItemId))
                {
                    Debug.LogError($"[ItemDatabase] {item.name} รหัสว่างหรือซ้ำกับไอเทมอื่น ('{item.ItemId}') save จะหาชิ้นนี้ไม่เจอ", item);
                }
                if (item.Prefab == null)
                {
                    Debug.LogWarning($"[ItemDatabase] {item.name} ไม่มี Prefab โหลด save หรือวางลงฉากจะสร้างชิ้นนี้ไม่ได้", item);
                }
                items.Add(item);
            }
            byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
