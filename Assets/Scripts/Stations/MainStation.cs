using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Player;

namespace ProjectCook.Stations
{
    /// <summary>สถานีหลักที่มีหลายสถานีย่อย (เช่น เตา) เข้าแล้วเห็นมุมรวม คลิกสถานีย่อยเพื่อเข้าไปใช้งาน Esc ถอยกลับมุมรวมก่อนออก</summary>
    public class MainStation : Station
    {
        [Header("Sub Stations")]
        [Tooltip("ปุ่มคลิกเลือกสถานีย่อยในมุมรวม")]
        [SerializeField] private InputActionReference selectAction;

        private SubStation current;

        protected override void OnEnable()
        {
            base.OnEnable();
            selectAction.EnableAction();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            selectAction.DisableAction();
        }

        public override void ExitStation()
        {
            if (!isActive) return;

            ExitSubStation();
            base.ExitStation();
        }

        protected override void Update()
        {
            base.Update();

            if (isActive && current == null && selectAction.WasPressedThisFrame()) TryEnterAimedSubStation();
        }

        protected override void OnExitPressed()
        {
            if (current == null)
            {
                ExitStation();
                return;
            }

            ExitSubStation();
            if (CameraManager.Instance != null) CameraManager.Instance.SetStationCamera(stationCamera);
        }

        private void TryEnterAimedSubStation()
        {
            Collider target = PlayerAiming.Instance != null ? PlayerAiming.Instance.Target : null;
            SubStation subStation = target != null ? target.GetComponentInParent<SubStation>() : null;
            if (subStation == null || subStation.ViewCamera == null) return;

            current = subStation;
            if (CameraManager.Instance != null) CameraManager.Instance.SetStationCamera(subStation.ViewCamera);

            // สถานีย่อยเล็งกลางจอ เคอร์เซอร์ใช้เฉพาะมุมรวม
            if (unlockCursor) CursorManager.ReleaseUnlock(this);
            subStation.Enter();
        }

        private void ExitSubStation()
        {
            if (current == null) return;

            current.Exit();
            current = null;
            if (unlockCursor) CursorManager.RequestUnlock(this);
        }
    }
}
