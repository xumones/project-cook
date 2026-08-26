using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Interaction;
using ProjectCook.CameraSystem;
using Unity.Cinemachine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// Abstract Base Class สำหรับสถานีทำอาหารทุกประเภทในเกม (กระทะ, หม้อ, เตาอบ ฯลฯ)
    /// จัดการการเข้า/ออกจากสถานี, สั่งเปลี่ยน State กล้องผ่าน CameraManager
    /// และขับเคลื่อนโมดูลประจำสถานี (IStationModule) ทั้งหมดให้อัตโนมัติ
    ///
    /// สถานีชนิดใหม่เพียงสืบทอดคลาสนี้แล้วแปะโมดูลที่ต้องการลงบน GameObject
    /// โดยไม่ต้องเขียนโค้ดเปิด/ปิดโมดูลเองอีก
    /// </summary>
    public abstract class CookingStation : MonoBehaviour, IInteractable
    {
        [Header("Camera Settings")]
        [Tooltip("Cinemachine Virtual Camera ประจำสถานีทำอาหารนี้")]
        [SerializeField] protected CinemachineCamera stationCamera;

        [Header("Input Settings")]
        [Tooltip("Action จาก New Input System สำหรับกดออกจากสถานี (เช่น Cancel หรือ Escape)")]
        [SerializeField] private InputActionReference exitAction;

        [Header("Station Modules")]
        [Tooltip("โมดูลที่อยู่นอก GameObject นี้และลูกๆ (ต้อง Implement IStationModule) โดยปกติเว้นว่างไว้ได้")]
        [SerializeField] private MonoBehaviour[] externalModules;

        // โมดูลทั้งหมดที่สถานีนี้ต้องสั่งเปิด/ปิด (รวบรวมอัตโนมัติตอน Awake)
        private readonly List<IStationModule> modules = new List<IStationModule>();

        protected bool isCooking = false;
        public bool IsCooking => isCooking;

        protected virtual void Awake()
        {
            CollectModules();
        }

        /// <summary>
        /// รวบรวมโมดูลจากตัวเองและลูกๆ พร้อมกับโมดูลภายนอกที่ระบุไว้ใน Inspector
        /// </summary>
        private void CollectModules()
        {
            modules.Clear();

            IStationModule[] localModules = GetComponentsInChildren<IStationModule>(true);
            for (int i = 0; i < localModules.Length; i++)
            {
                if (localModules[i] != null && !modules.Contains(localModules[i]))
                {
                    modules.Add(localModules[i]);
                }
            }

            if (externalModules == null) return;

            for (int i = 0; i < externalModules.Length; i++)
            {
                if (externalModules[i] == null) continue;

                if (externalModules[i] is IStationModule module)
                {
                    if (!modules.Contains(module))
                    {
                        modules.Add(module);
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] '{externalModules[i].GetType().Name}' ไม่ได้ Implement IStationModule จึงถูกข้ามไป", this);
                }
            }
        }

        protected virtual void OnEnable()
        {
            exitAction?.action?.Enable();
        }

        protected virtual void OnDisable()
        {
            exitAction?.action?.Disable();
        }

        /// <summary>
        /// พฤติกรรมเริ่มต้นเมื่อผู้เล่นกด Interact คือเข้าใช้งานสถานี
        /// สถานีที่ต้องการเงื่อนไขพิเศษ (เช่น ต้องมีวัตถุดิบก่อน) ให้ Override เมธอดนี้
        /// </summary>
        public virtual void Interact(PlayerInteractor interactor)
        {
            EnterStation(interactor);
        }

        /// <summary>
        /// ดึงกล้องประจำสถานี (หากไม่ได้ตั้งค่าไว้จะใช้ Camera.main แทน)
        /// </summary>
        protected Camera GetStationCamera()
        {
            if (stationCamera != null)
            {
                Camera cam = stationCamera.GetComponent<Camera>();
                if (cam != null) return cam;
            }

            return Camera.main;
        }

        /// <summary>
        /// เข้าสู่โหมดทำอาหาร: สลับกล้องมายังสถานีนี้ แล้วสั่งเปิดโมดูลทั้งหมด
        /// </summary>
        public virtual void EnterStation(PlayerInteractor interactor)
        {
            if (isCooking) return;

            isCooking = true;
            if (stationCamera != null && CameraManager.Instance != null)
            {
                CameraManager.Instance.SwitchToCooking(stationCamera);
            }

            Camera stationCam = GetStationCamera();
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationEnter(stationCam);
            }
        }

        /// <summary>
        /// ออกจากโหมดทำอาหาร: สั่งปิดโมดูลทั้งหมด แล้วสลับกล้องกลับสภาวะ FirstPerson
        /// </summary>
        public virtual void ExitStation()
        {
            if (!isCooking) return;

            isCooking = false;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationExit();
            }

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
