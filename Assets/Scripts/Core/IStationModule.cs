using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>โมดูลที่ทำงานเฉพาะตอนผู้เล่นเข้าใช้งาน Station เปิด/ปิดให้อัตโนมัติ</summary>
    public interface IStationModule
    {
        void OnStationEnter(Camera stationCamera);
        void OnStationExit();
    }
}
