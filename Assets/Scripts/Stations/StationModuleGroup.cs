using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Stations
{
    /// <summary>รวบรวมโมดูลของเจ้าของหนึ่งราย (ตัวเองและลูกๆ) แล้วสั่งเปิด/ปิดพร้อมกัน</summary>
    public class StationModuleGroup
    {
        private readonly List<IStationModule> modules = new List<IStationModule>();

        public void Collect(MonoBehaviour owner)
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
