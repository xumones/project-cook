using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Stations;

namespace ProjectCook.Cooking
{
    /// <summary>โมดูลรับอินพุตเพื่อเคลื่อนที่และเอียงกระทะบนเตาในโหมดทำอาหาร</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PanController : MonoBehaviour, IStationModule
    {
        [Header("Movement Settings")]
        [Tooltip("ความเร็วในการเคลื่อนที่สไลด์ของกระทะ")]
        [SerializeField] private float moveSpeed = 1.5f;

        [Tooltip("ความนุ่มนวลในการตอบสนองของการเคลื่อนที่สไลด์")]
        [SerializeField] private float smoothSpeed = 15f;

        [Tooltip("ขอบเขตการเคลื่อนที่สูงสุดจากจุดเริ่มต้นในแนว X และ Z (เมตร)")]
        [SerializeField] private Vector2 moveBounds = new Vector2(0.3f, 0.3f);

        [Header("Height Settings")]
        [Tooltip("ระยะยกความสูงเริ่มต้นของกระทะเมื่อเริ่มใช้งาน (เมตร)")]
        [SerializeField] private float startHeightOffset = 0.05f;

        [Header("Tilt Settings")]
        [Tooltip("มุมเอียงสูงสุดของกระทะในแต่ละทิศทาง (องศา)")]
        [SerializeField] private float maxTiltAngle = 12f;

        [Tooltip("ความนุ่มนวลในการตอบสนองของการเอียงกระทะ")]
        [SerializeField] private float tiltSmoothSpeed = 12f;

        [Tooltip("คืนค่ามุมเอียงเป็น 0 องศาเมื่อปล่อยปุ่ม WASD")]
        [SerializeField] private bool autoCenterRotationOnRelease = true;

        [Header("Input Settings")]
        [Tooltip("Action จาก New Input System สำหรับอ่านค่า Vector2 (WASD)")]
        [SerializeField] private InputActionReference moveAction;

        private Rigidbody panRigidbody;
        private Camera cachedMainCamera;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private Vector3 targetLocalPosition;
        private Quaternion targetLocalRotation;

        private bool isControllerActive = false;
        private bool isResetting = false;

        private Camera GetMainCamera()
        {
            if (cachedMainCamera == null) cachedMainCamera = Camera.main;
            return cachedMainCamera;
        }

        private void Awake()
        {
            panRigidbody = GetComponent<Rigidbody>();
            panRigidbody.isKinematic = true;
            panRigidbody.useGravity = false;
            panRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;

            targetLocalPosition = initialLocalPosition;
            targetLocalRotation = initialLocalRotation;
        }

        public void OnStationEnter()
        {
            SetControllerActive(true);
        }

        public void OnStationExit()
        {
            SetControllerActive(false);
        }

        private void OnEnable()
        {
            moveAction.EnableAction();
        }

        private void OnDisable()
        {
            moveAction.DisableAction();
        }

        private void SetControllerActive(bool active)
        {
            isControllerActive = active;
            if (!active)
            {
                ResetPanPosition();
            }
            else
            {
                isResetting = false;
                targetLocalPosition = initialLocalPosition + new Vector3(0f, startHeightOffset, 0f);
            }
        }

        /// <summary>รีเซ็ตตำแหน่งและมุมเอียงของกระทะให้สไลด์และหมุนกลับสู่จุดเริ่มต้นบนเตาอย่างนุ่มนวล</summary>
        private void ResetPanPosition()
        {
            targetLocalPosition = initialLocalPosition;
            targetLocalRotation = initialLocalRotation;
            isResetting = true;
        }

        private void Update()
        {
            if (isControllerActive)
            {
                Vector2 input = ReadMoveInput();

                if (input.sqrMagnitude > 0.001f)
                {
                    // ทิศอ้างอิง = กล้องที่มองอยู่ (Cinemachine ย้าย Camera.main มาที่กล้องสถานีแล้ว)
                    Camera mainCam = GetMainCamera();
                    Transform refTrans = mainCam != null ? mainCam.transform : transform;
                    Vector3 forward = Vector3.ProjectOnPlane(refTrans.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.ProjectOnPlane(refTrans.right, Vector3.up).normalized;

                    // สไลด์ตำแหน่งแบบ Camera-Relative
                    Vector3 worldMoveDir = (forward * input.y) + (right * input.x);

                    Vector3 localMoveDir = transform.parent != null
                        ? transform.parent.InverseTransformDirection(worldMoveDir)
                        : worldMoveDir;

                    Vector3 moveDelta = localMoveDir * (moveSpeed * Time.deltaTime);
                    targetLocalPosition += moveDelta;

                    // จำกัดขอบเขตการสไลด์ (Clamp)
                    targetLocalPosition.x = Mathf.Clamp(
                        targetLocalPosition.x,
                        initialLocalPosition.x - moveBounds.x,
                        initialLocalPosition.x + moveBounds.x
                    );
                    targetLocalPosition.y = initialLocalPosition.y + startHeightOffset;
                    targetLocalPosition.z = Mathf.Clamp(
                        targetLocalPosition.z,
                        initialLocalPosition.z - moveBounds.y,
                        initialLocalPosition.z + moveBounds.y
                    );

                    // เอียงกระทะแบบ Camera-Relative (Tilt: Pitch & Roll)
                    Quaternion tiltWorldRot = Quaternion.AngleAxis(input.y * maxTiltAngle, right) *
                                              Quaternion.AngleAxis(-input.x * maxTiltAngle, forward);

                    Transform parentTrans = transform.parent;
                    Quaternion initialWorldRot = parentTrans != null ? parentTrans.rotation * initialLocalRotation : initialLocalRotation;
                    Quaternion targetWorldRot = tiltWorldRot * initialWorldRot;

                    targetLocalRotation = parentTrans != null
                        ? Quaternion.Inverse(parentTrans.rotation) * targetWorldRot
                        : targetWorldRot;
                }
                else if (autoCenterRotationOnRelease)
                {
                    // ปล่อยปุ่ม WASD -> คืนค่าหมุนราบ 0 องศา
                    targetLocalRotation = initialLocalRotation;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isControllerActive)
            {
                // เลื่อนตำแหน่งกระทะอย่างนุ่มนวลผ่านทางฟิสิกส์
                Vector3 newLocalPos = Vector3.Lerp(
                    transform.localPosition,
                    targetLocalPosition,
                    Time.fixedDeltaTime * smoothSpeed
                );

                // หมุนเอียงองศากระทะอย่างนุ่มนวลผ่านทางฟิสิกส์
                Quaternion newLocalRot = Quaternion.Slerp(
                    transform.localRotation,
                    targetLocalRotation,
                    Time.fixedDeltaTime * tiltSmoothSpeed
                );

                ApplyPhysicsTransform(newLocalPos, newLocalRot);
            }
            else if (isResetting)
            {
                // สไลด์ตำแหน่งและหมุนคืนค่าตั้งต้นแบบนุ่มนวลเมื่อออกจากสถานี
                Vector3 newLocalPos = Vector3.Lerp(
                    transform.localPosition,
                    initialLocalPosition,
                    Time.fixedDeltaTime * smoothSpeed
                );

                Quaternion newLocalRot = Quaternion.Slerp(
                    transform.localRotation,
                    initialLocalRotation,
                    Time.fixedDeltaTime * tiltSmoothSpeed
                );

                ApplyPhysicsTransform(newLocalPos, newLocalRot);

                bool posReached = Vector3.SqrMagnitude(transform.localPosition - initialLocalPosition) < 0.00001f;
                bool rotReached = Quaternion.Angle(transform.localRotation, initialLocalRotation) < 0.1f;

                if (posReached && rotReached)
                {
                    ApplyPhysicsTransform(initialLocalPosition, initialLocalRotation);
                    isResetting = false;
                }
            }
        }

        /// <summary>ใช้ MovePosition และ MoveRotation เพื่อย้ายกระทะผ่าน PhysX engine สำหรับ Kinematic Rigidbody</summary>
        private void ApplyPhysicsTransform(Vector3 localPos, Quaternion localRot)
        {
            Transform parentTrans = transform.parent;
            Vector3 worldPos = parentTrans != null ? parentTrans.TransformPoint(localPos) : localPos;
            Quaternion worldRot = parentTrans != null ? parentTrans.rotation * localRot : localRot;

            panRigidbody.MovePosition(worldPos);
            panRigidbody.MoveRotation(worldRot);
        }

        private Vector2 ReadMoveInput() => moveAction.ReadValue<Vector2>();
    }
}
