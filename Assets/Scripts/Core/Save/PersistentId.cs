using System;
using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>รหัสคงที่ของวัตถุที่วางไว้ในฉาก ให้ save จำได้ว่าข้อมูลเป็นของชิ้นไหนแม้ฉากถูกโหลดใหม่</summary>
    [DisallowMultipleComponent]
    public class PersistentId : MonoBehaviour
    {
        [Tooltip("สร้างให้อัตโนมัติ ห้ามแก้หลังมี save")]
        [SerializeField] private string id;

        public string Id => id;

#if UNITY_EDITOR
        // วัตถุที่ duplicate มาได้รหัสซ้ำกับต้นฉบับ จึงต้องสร้างใหม่เมื่อเจอรหัสซ้ำในฉากเดียวกัน
        private void OnValidate()
        {
            if (Application.isPlaying || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
            if (!string.IsNullOrEmpty(id) && !HasDuplicateInScene()) return;

            id = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private bool HasDuplicateInScene()
        {
            foreach (PersistentId other in FindObjectsByType<PersistentId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != this && other.id == id && other.gameObject.scene == gameObject.scene) return true;
            }
            return false;
        }
#endif
    }
}
