using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Interaction;
using ProjectCook.CameraControl;
using Unity.Cinemachine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// Abstract Base Class สำหรับสถานีทำอาหารทุกประเภทในเกม (กระทะ, หม้อ, เตาอบ ฯลฯ)
    /// จัดการการเข้า/ออกจากสถานีทำอาหารและสั่งเปลี่ยน State กล้องผ่าน CameraManager (New Input System)
    /// </summary>
    public abstract class CookingStation : MonoBehaviour, IInteractable
    {
        [Header("Camera Settings")]
        [Tooltip("Cinemachine Virtual Camera ประจำสถานีทำอาหารนี้")]
        [SerializeField] protected CinemachineCamera stationCamera;

        [Header("Input Settings")]
        [Tooltip("Action จาก New Input System สำหรับกดออกจากสถานี (เช่น Cancel หรือ Escape)")]
        [SerializeField] private InputActionReference exitAction;

        protected bool isCooking = false;
        public bool IsCooking => isCooking;

        protected virtual void OnEnable()
        {
            exitAction?.action?.Enable();
        }

        protected virtual void OnDisable()
        {
            exitAction?.action?.Disable();
        }

        public abstract void Interact(PlayerInteractor interactor);

        /// <summary>
        /// เข้าสู่โหมดทำอาหาร: สั่ง CameraManager ให้สลับกล้องมายังสถานีนี้
        /// </summary>
        public virtual void EnterStation(PlayerInteractor interactor)
        {
            if (isCooking) return;

            isCooking = true;
            if (stationCamera != null && CameraManager.Instance != null)
            {
                CameraManager.Instance.SwitchToCooking(stationCamera);
            }
        }

        /// <summary>
        /// ออกจากโหมดทำอาหาร: สั่ง CameraManager ให้สลับกล้องกลับสภาวะ FirstPerson
        /// </summary>
        public virtual void ExitStation()
        {
            if (!isCooking) return;

            isCooking = false;
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SwitchToFirstPerson();
            }
        }

        protected virtual void Update()
        {
            if (isCooking && WasExitPressed())
            {
                ExitStation();
            }
        }

        /// <summary>
        /// ตรวจสอบการกดปุ่มออกผ่าน New Input System
        /// </summary>
        private bool WasExitPressed()
        {
            if (exitAction?.action != null)
            {
                return exitAction.action.WasPressedThisFrame();
            }
            if (Keyboard.current != null)
            {
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            return false;
        }
    }
}
