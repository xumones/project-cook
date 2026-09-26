using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Stations
{
    /// <summary>รวบรวมโมดูลของเจ้าของหนึ่งราย (จากตัวเองและลูกๆ บวกโมดูลภายนอก) แล้วสั่งเปิด/ปิดพร้อมกัน</summary>
    public class StationModuleGroup
    {
        private readonly List<IStationModule> modules = new List<IStationModule>();

        public void Collect(MonoBehaviour owner, MonoBehaviour[] externalModules = null)
        {
            modules.Clear();

            // ข้ามโมดูลที่อยู่ใต้เจ้าของอีกราย (เช่น PanController ของกระทะที่วางอยู่บนเตา)
            IStationModule[] localModules = owner.GetComponentsInChildren<IStationModule>(true);
            for (int i = 0; i < localModules.Length; i++)
            {
                var component = (Component)localModules[i];
                if (!ReferenceEquals(component.GetComponentInParent<IStationModuleOwner>(true), owner)) continue;

                Add(localModules[i]);
            }

            if (externalModules == null) return;

            for (int i = 0; i < externalModules.Length; i++)
            {
                if (externalModules[i] == null) continue;

                if (externalModules[i] is IStationModule module)
                {
                    Add(module);
                }
                else
                {
                    Debug.LogWarning($"[{owner.GetType().Name}] '{externalModules[i].GetType().Name}' ไม่ได้ Implement IStationModule จึงถูกข้ามไป", owner);
                }
            }
        }

        public void EnterAll()
        {
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationEnter();
            }
        }

        public void ExitAll()
        {
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationExit();
            }
        }

        private void Add(IStationModule module)
        {
            if (!modules.Contains(module)) modules.Add(module);
        }
    }
}
