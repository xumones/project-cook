using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Items;
using ProjectCook.Player;
using Unity.Cinemachine;

namespace ProjectCook.Stations
{
    /// <summary>จุดที่ผู้เล่นเข้าไป "ใช้งาน" แล้วกล้องล็อกอยู่กับที่ (สถานีทำอาหาร เครื่องเล่นแผ่น ฯลฯ) จัดการกล้อง โหมดเกม เคอร์เซอร์ ปุ่มออก และเปิด/ปิดโมดูล</summary>
    public class Station : MonoBehaviour, IInteractable, IAimHintProvider, IStationModuleOwner
    {
        [Header("Camera Settings")]
        [Tooltip("Cinemachine Virtual Camera ประจำสถานีนี้ ไม่ต้องมี PanTilt/InputAxisController ถ้าไม่อยากให้ผู้เล่นหันกล้องระหว่างใช้งาน")]
        [SerializeField] protected CinemachineCamera stationCamera;

        [Header("Input Settings")]
        [Tooltip("Action สำหรับกดออกจากสถานี (เช่น Cancel หรือ Escape)")]
        [SerializeField] private InputActionReference exitAction;

        [Tooltip("สถานีที่ใช้เมาส์กดปุ่ม UI ให้ปลดล็อกเคอร์เซอร์ตลอดที่อยู่ในสถานี")]
        [SerializeField] protected bool unlockCursor;

        [Tooltip("ต้องวางของในมือก่อนถึงจะเข้าได้")]
        [SerializeField] private bool requireEmptyHands;

        private readonly StationModuleGroup modules = new StationModuleGroup();

        protected bool isActive;

        protected virtual void Awake()
        {
            modules.Collect(this);

            // ค่าที่ Save ไว้ใน Scene ของกล้องสถานีจะเป็น enabled เสมอ ปิดไว้ก่อนกันมันแอบอ่าน mouse look ทั้งที่ยังไม่ได้เข้าสถานี
            CameraManager.SetLookInput(stationCamera, false);
        }

        // เงื่อนไขเดียวกับ Interact: เข้าไม่ได้ก็ไม่ต้องบอกว่าใช้ได้
        public virtual AimHint GetAimHint() => IsBlockedByHeldItem ? AimHint.None : AimHint.Use;

        /// <summary>สถานีที่มีเงื่อนไขก่อนเข้าใช้งานให้ Override แล้วเรียก EnterStation เอง</summary>
        public virtual void Interact()
        {
            // โหมด Station ปิดการหยิบอยู่แล้ว จึงกันแค่ตอนเข้าพอ
            if (IsBlockedByHeldItem)
            {
                Debug.Log($"[{GetType().Name}] เข้า {name} ไม่ได้: ต้องวางของในมือก่อน");
                return;
            }

            EnterStation();
        }

        private bool IsBlockedByHeldItem
        {
            get
            {
                EquipmentManager equipment = EquipmentManager.Instance;
                return requireEmptyHands && equipment != null && equipment.IsHoldingAnything;
            }
        }

        /// <summary>สลับกล้องมายังสถานีนี้ เปลี่ยนโหมดเกม แล้วสั่งเปิดโมดูลทั้งหมด</summary>
        public virtual void EnterStation()
        {
            if (isActive) return;

            isActive = true;
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SetStationCamera(stationCamera);
            }
            GameModeManager.Instance.Enter(this, GameMode.Station);
            if (unlockCursor) CursorManager.RequestUnlock(this);

            modules.EnterAll();
        }

        /// <summary>สั่งปิดโมดูลทั้งหมด แล้วออกจากโหมดสถานีให้กล้องกลับมุมมองผู้เล่น</summary>
        public virtual void ExitStation()
        {
            if (!isActive) return;

            isActive = false;

            modules.ExitAll();

            if (unlockCursor) CursorManager.ReleaseUnlock(this);
            GameModeManager.Instance.Exit(this);
        }

        protected virtual void Update()
        {
            // หน้าต่างที่เปิดทับ (กระเป๋า) ได้ Esc ก่อน
            if (isActive && GameModeManager.InputAllowedIn(GameMode.Station) && exitAction.WasPressedThisFrame())
            {
                OnExitPressed();
            }
        }

        /// <summary>สถานีที่มีหลายชั้น (เช่น เตาที่เลือกภาชนะอยู่) Override เพื่อถอยทีละชั้น</summary>
        protected virtual void OnExitPressed()
        {
            ExitStation();
        }
    }
}
