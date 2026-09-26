using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Player;

namespace ProjectCook.Stations
{
    /// <summary>สถานีย่อยภายใน MainStation (กระทะบนเตา แผ่นทอด ฯลฯ) มีมุมกล้องของตัวเอง และโมดูลลูกที่ทำงานเฉพาะตอนผู้เล่นเลือกเข้ามา</summary>
    public class SubStation : MonoBehaviour, IStationModuleOwner
    {
        [Tooltip("มุมกล้องตอนใช้งานสถานีย่อยนี้ ห้ามเป็นลูกของชิ้นที่ขยับได้ ไม่งั้นกล้องสั่นตาม")]
        [SerializeField] private CinemachineCamera viewCamera;

        private readonly StationModuleGroup modules = new StationModuleGroup();

        public CinemachineCamera ViewCamera => viewCamera;

        private void Awake()
        {
            modules.Collect(this);

            // กล้องที่ Save ไว้ใน Scene เปิดรับ mouse look อยู่ ปิดไว้จนกว่าจะถูกเลือก
            CameraManager.SetLookInput(viewCamera, false);
        }

        public void Enter() => modules.EnterAll();

        public void Exit() => modules.ExitAll();
    }
}
