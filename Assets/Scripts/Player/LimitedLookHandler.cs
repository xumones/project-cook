using UnityEngine;
using Unity.Cinemachine;

namespace ProjectCook.Player
{
    /// <summary>จำกัดมุมกล้องผู้เล่นไว้รอบทิศที่เล็งอยู่ตอนเข้าโหมด แล้วคืนขอบเขตเดิมตอนออก ใช้กับโหมดที่ควรมองแค่สิ่งตรงหน้า (คุย, cutscene)</summary>
    public class LimitedLookHandler : ICameraModeHandler
    {
        private readonly CinemachineCamera playerCamera;
        private readonly float panRange;
        private readonly float tiltMin;
        private readonly float tiltMax;

        private Vector2 savedPanRange;
        private Vector2 savedTiltRange;
        private bool savedPanWrap;
        private bool rangeSaved;

        public LimitedLookHandler(CinemachineCamera playerCamera, float panRange, float tiltMin, float tiltMax)
        {
            this.playerCamera = playerCamera;
            this.panRange = panRange;
            this.tiltMin = tiltMin;
            this.tiltMax = tiltMax;
        }

        public void OnEnter()
        {
            if (playerCamera == null) return;

            var panTilt = playerCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt == null) return;

            // บันทึกขอบเขตเดิมไว้คืนตอนออกจากโหมด (กันเคสเผลอเรียกซ้ำโดยไม่ได้ OnExit ก่อน)
            if (!rangeSaved)
            {
                savedPanRange = panTilt.PanAxis.Range;
                savedTiltRange = panTilt.TiltAxis.Range;
                savedPanWrap = panTilt.PanAxis.Wrap;
                rangeSaved = true;
            }

            // ปิด Wrap ชั่วคราวแล้วจำกัดช่วงหมุนแนวนอนไว้รอบมุมที่กล้องกำลังเล็งอยู่ตอนนี้
            float currentPan = panTilt.PanAxis.Value;
            panTilt.PanAxis.Wrap = false;
            panTilt.PanAxis.Range = new Vector2(currentPan - panRange * 0.5f, currentPan + panRange * 0.5f);

            // จำกัดมุมก้ม/เงยไว้ในช่วงที่กำหนด
            panTilt.TiltAxis.Value = Mathf.Clamp(panTilt.TiltAxis.Value, tiltMin, tiltMax);
            panTilt.TiltAxis.Range = new Vector2(tiltMin, tiltMax);
        }

        public void OnExit()
        {
            if (playerCamera != null && rangeSaved)
            {
                var panTilt = playerCamera.GetComponent<CinemachinePanTilt>();
                if (panTilt != null)
                {
                    panTilt.PanAxis.Range = savedPanRange;
                    panTilt.PanAxis.Wrap = savedPanWrap;
                    panTilt.TiltAxis.Range = savedTiltRange;
                }
            }
            rangeSaved = false;
        }
    }
}
