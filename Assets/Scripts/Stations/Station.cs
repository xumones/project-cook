using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Player;
using Unity.Cinemachine;

namespace ProjectCook.Stations
{
    /// <summary>จุดที่ผู้เล่นเข้าไป "ใช้งาน" แล้วกล้องล็อกอยู่กับที่ (สถานีทำอาหาร เครื่องเล่นแผ่น ฯลฯ) จัดการกล้อง โหมดเกม ปุ่มออก และเปิด/ปิดโมดูล</summary>
    public abstract class Station : MonoBehaviour, IInteractable, IAimHintProvider
    {
        [Header("Camera Settings")]
        [Tooltip("Cinemachine Virtual Camera ประจำสถานีนี้ ไม่ต้องมี PanTilt/InputAxisController ถ้าไม่อยากให้ผู้เล่นหันกล้องระหว่างใช้งาน")]
        [SerializeField] protected CinemachineCamera stationCamera;

        [Header("Input Settings")]
        [Tooltip("Action สำหรับกดออกจากสถานี (เช่น Cancel หรือ Escape)")]
        [SerializeField] private InputActionReference exitAction;

        [Header("Station Modules")]
        [Tooltip("โมดูลที่อยู่นอก GameObject นี้และลูกๆ (ต้อง Implement IStationModule) โดยปกติเว้นว่างไว้ได้")]
        [SerializeField] private MonoBehaviour[] externalModules;

        // โมดูลทั้งหมดที่สถานีนี้ต้องสั่งเปิด/ปิด (รวบรวมอัตโนมัติตอน Awake)
        private readonly List<IStationModule> modules = new List<IStationModule>();

        protected bool isActive;

        protected virtual void Awake()
        {
            CollectModules();

            // ค่าที่ Save ไว้ใน Scene ของกล้องสถานีจะเป็น enabled เสมอ ปิด Input ไว้ก่อนตั้งแต่เริ่มเกม
            // กันไม่ให้มันแอบอ่าน mouse look ทั้งที่ยังไม่เคยเข้าใช้งานสถานีนี้ (CameraManager ปิดให้เฉพาะตอนออก)
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

        public virtual AimHint GetAimHint() => AimHint.Use;

        /// <summary>สถานีที่มีเงื่อนไขก่อนเข้าใช้งานให้ Override แล้วเรียก EnterStation เอง</summary>
        public virtual void Interact()
        {
            EnterStation();
        }

        /// <summary>กล้องที่โมดูลควรอ้างอิงระหว่างอยู่ในสถานี ไม่มีกล้องประจำ = กล้องหลักของผู้เล่น</summary>
        protected Camera GetStationCamera()
        {
            if (stationCamera != null)
            {
                Camera cam = stationCamera.GetComponent<Camera>();
                if (cam != null) return cam;
            }

            return Camera.main;
        }

        /// <summary>สลับกล้องมายังสถานีนี้ เปลี่ยนโหมดเกม แล้วสั่งเปิดโมดูลทั้งหมด</summary>
        public virtual void EnterStation()
        {
            if (isActive) return;

            isActive = true;
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.StationCamera = stationCamera;
            }

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.SetMode(GameMode.Station);
            }

            Camera stationCam = GetStationCamera();
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i]?.OnStationEnter(stationCam);
            }
        }

        /// <summary>สั่งปิดโมดูลทั้งหมด แล้วคืนโหมดเกมให้กล้องกลับมุมมองผู้เล่น</summary>
        public virtual void ExitStation()
        {
            if (!isActive) return;

            isActive = false;

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
            if (isActive && WasExitPressed())
            {
                ExitStation();
            }
        }

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
