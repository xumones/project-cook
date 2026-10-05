using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using ProjectCook.Core;

namespace ProjectCook.Stations
{
    /// <summary>เปิดแกนหมุนกล้อง (Look Orbit X/Y ของ CinemachineOrbitalFollow) และซ่อนเคอร์เซอร์ เฉพาะตอนคลิกขวาค้างลาก เหมือนโปรแกรม 3D ส่วนซูม (Orbit Scale) ปล่อยทำงานตลอดเวลาที่สถานีเปิดอยู่ และคืนมุมกล้องกลับจุดเริ่มต้นทุกครั้งที่เข้าสถานี</summary>
    [RequireComponent(typeof(CinemachineInputAxisController))]
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class PrepStationOrbitInput : MonoBehaviour, IStationModule
    {
        private const string HorizontalAxisName = "Look Orbit X";
        private const string VerticalAxisName = "Look Orbit Y";

        [Header("Input Settings")]
        [Tooltip("Action สำหรับกดคลิกขวาค้างเพื่อหมุนกล้อง")]
        [SerializeField] private InputActionReference orbitHoldAction;

        private CinemachineInputAxisController axisController;
        private CinemachineOrbitalFollow orbitalFollow;
        private bool isActive;

        // มุมเริ่มต้นตามที่ตั้งไว้ใน Inspector เก็บไว้ตอน Awake เพื่อคืนค่ากลับทุกครั้งที่เข้าสถานี
        private float initialHorizontal;
        private float initialVertical;

        private void Awake()
        {
            axisController = GetComponent<CinemachineInputAxisController>();
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

            initialHorizontal = orbitalFollow.HorizontalAxis.Value;
            initialVertical = orbitalFollow.VerticalAxis.Value;
        }

        private void OnDisable()
        {
            CursorManager.ReleaseHide(this);
        }

        public void OnStationEnter()
        {
            isActive = true;

            // คืนมุมตอนเข้าแทนตอนออก ไม่งั้นกล้องกระโดดกลับจุดเริ่มต้นก่อนค่อยๆ blend ออก
            orbitalFollow.HorizontalAxis.Value = initialHorizontal;
            orbitalFollow.VerticalAxis.Value = initialVertical;
        }

        public void OnStationExit()
        {
            isActive = false;
            SetOrbiting(false);
        }

        private void Update()
        {
            if (!isActive) return;

            SetOrbiting(orbitHoldAction.IsPressed());
        }

        /// <summary>เปิดแกนหมุนกล้องพร้อมซ่อนเคอร์เซอร์ระหว่างลาก ให้รู้สึกเหมือนหมุนด้วยมือ</summary>
        private void SetOrbiting(bool enabled)
        {
            if (enabled) CursorManager.RequestHide(this);
            else CursorManager.ReleaseHide(this);

            var horizontal = axisController.GetController(HorizontalAxisName);
            if (horizontal != null) horizontal.Enabled = enabled;

            var vertical = axisController.GetController(VerticalAxisName);
            if (vertical != null) vertical.Enabled = enabled;
        }
    }
}
