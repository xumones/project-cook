using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>ทำให้ไอเทมเก็บเข้ากระเป๋าได้ ตอนเก็บตัววัตถุถูกทำลาย เหลือแค่ชนิดกับสถานะที่จดจาก ISavableState</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class StorableItem : MonoBehaviour, IAimHintProvider
    {
        public ItemDefinitionSO Definition => GetComponent<Item>().Definition;

        public AimHint GetAimHint() => AimHint.PickUp;

        public List<ComponentState> CaptureStates()
        {
            var states = new List<ComponentState>();
            foreach (ISavableState savable in GetComponentsInChildren<ISavableState>(true))
            {
                states.Add(new ComponentState(savable.GetType().FullName, savable.CaptureState()));
            }
            return states;
        }

        /// <summary>คืนสถานะให้วัตถุที่เพิ่งสร้างจาก prefab เดียวกัน</summary>
        public void RestoreStates(IReadOnlyList<ComponentState> states)
        {
            // prefab เดียวกันให้ลำดับ component เดิมเสมอ จึงจับคู่ตามลำดับ แล้วใช้ชื่อชนิดกันคืนผิดตัวตอน prefab ถูกแก้
            ISavableState[] savables = GetComponentsInChildren<ISavableState>(true);
            if (savables.Length != states.Count)
            {
                Debug.LogWarning($"[StorableItem] {name} มี component ที่จดสถานะ {savables.Length} ตัว แต่สถานะที่จดไว้มี {states.Count} รายการ", this);
            }

            int count = Mathf.Min(savables.Length, states.Count);
            for (int i = 0; i < count; i++)
            {
                if (savables[i].GetType().FullName != states[i].ComponentType)
                {
                    Debug.LogWarning($"[StorableItem] {name} ข้ามสถานะของ {states[i].ComponentType} เพราะลำดับ component ไม่ตรงกับตอนจด", this);
                    continue;
                }

                savables[i].RestoreState(states[i].Data);
            }
        }
    }
}
