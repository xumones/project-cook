using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Items;
using ProjectCook.Player;
using Unity.Cinemachine;

namespace ProjectCook.Cooking
{
    /// <summary>Abstract Base Class สำหรับสถานีทำอาหารทุกประเภทในเกม (กระทะ, หม้อ, เตาอบ ฯลฯ)</summary>
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

        protected virtual void Awake()
        {
            CollectModules();

            // ค่าที่ Save ไว้ใน Scene ของกล้องสถานีจะเป็น enabled เสมอ ปิด Input ไว้ก่อนตั้งแต่เริ่มเกม
            // กันไม่ให้มันแอบอ่าน mouse look อยู่เงียบๆ ทั้งที่ยังไม่เคยเข้าใช้งานสถานีนี้เลยสักครั้ง
            // (ปกติ CameraManager.SwitchToFirstPerson เป็นคนปิดให้ตอนออกจากสถานี แต่รอบแรกยังไม่เคยเข้าจึงไม่เคยถูกปิด)
            if (stationCamera != null)
            {
                var stationInput = stationCamera.GetComponent<CinemachineInputAxisController>();
                if (stationInput != null)
                {
                    stationInput.enabled = false;
                }
            }
        }

        /// <summary>รวบรวมโมดูลจากตัวเองและลูกๆ พร้อมกับโมดูลภายนอกที่ระบุไว้ใน Inspector</summary>
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

        /// <summary>สถานีที่ต้องการเงื่อนไขพิเศษก่อนเข้าใช้งานให้ Override เมธอดนี้</summary>
        public virtual void Interact()
        {
            // ของในมือใช้ในสถานีไม่ได้และบังกล้อง ต้องวางก่อน (โหมด Cooking ปิดการหยิบอยู่แล้ว จึงกันแค่ตอนเข้าพอ)
            EquipmentManager equipment = EquipmentManager.Instance;
            if (equipment != null && equipment.IsHoldingAnything)
            {
                Debug.Log($"[CookingStation] เข้า {name} ไม่ได้: ต้องวางของในมือก่อน");
                return;
            }

            EnterStation();
        }

        /// <summary>ดึงกล้องประจำสถานี (หากไม่ได้ตั้งค่าไว้จะใช้ Camera.main แทน)</summary>
        protected Camera GetStationCamera()
        {
            if (stationCamera != null)
            {
                Camera cam = stationCamera.GetComponent<Camera>();
                if (cam != null) return cam;
            }

            return Camera.main;
        }

        /// <summary>เข้าสู่โหมดทำอาหาร: สลับกล้องมายังสถานีนี้ แล้วสั่งเปิดโมดูลทั้งหมด</summary>
        public virtual void EnterStation()
        {
            if (isCooking) return;

            isCooking = true;
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.StationCamera = stationCamera;
            }

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetMode(GameMode.Cooking);
            }

            Camera stationCam = GetStationCamera();
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationEnter(stationCam);
            }
        }

        /// <summary>ออกจากโหมดทำอาหาร: สั่งปิดโมดูลทั้งหมด แล้วสลับกล้องกลับสภาวะ FirstPerson</summary>
        public virtual void ExitStation()
        {
            if (!isCooking) return;

            isCooking = false;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationExit();
            }

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetMode(GameMode.Exploring);
            }
        }

        protected virtual void Update()
        {
            if (isCooking && WasExitPressed())
            {
                ExitStation();
            }
        }

        /// <summary>ตรวจสอบการกดปุ่มออกผ่าน New Input System</summary>
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
