using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Interaction;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// โมดูลรับอินพุตเพื่อเคลื่อนที่และเอียงกระทะบนเตาในโหมดทำอาหาร
    /// (Hybrid: WASD Position + Dynamic Tilting via Kinematic Physics)
    ///
    /// รับผิดชอบเฉพาะ "อินพุต → ทรานส์ฟอร์มของกระทะ" เท่านั้น
    /// ส่วนแรงฟิสิกส์ที่กระทำต่ออาหารในกระทะถูกแยกไปอยู่ที่ ContainerContentPhysics แล้ว
    /// </summary>
    public class PanController : MonoBehaviour, IStationModule
    {
        [Header("Pan References")]
        [Tooltip("Transform ของตัวกระทะ (หากไม่ใส่จะใช้ Transform ของ GameObject นี้)")]
        [SerializeField] private Transform panTransform;

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

        [Header("Reference Transform Settings")]
        [Tooltip("Transform สำหรับอ้างอิงทิศทาง (เช่น กล้อง stationCamera) หากไม่ใส่จะอ้างอิงจาก Camera.main หรือเตา")]
        [SerializeField] private Transform referenceTransform;

        private Rigidbody panRigidbody;
        private Camera cachedMainCamera;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private Vector3 targetLocalPosition;
        private Quaternion targetLocalRotation;

        private bool isControllerActive = false;
        private bool isResetting = false;

        public bool IsControllerActive => isControllerActive;

        private Camera GetMainCamera()
        {
            if (cachedMainCamera == null) cachedMainCamera = Camera.main;
            return cachedMainCamera;
        }

        private void Awake()
        {
            if (panTransform == null)
            {
                panTransform = transform;
            }

            panRigidbody = panTransform.GetComponent<Rigidbody>();
            if (panRigidbody == null)
            {
                panRigidbody = panTransform.gameObject.AddComponent<Rigidbody>();
            }

            panRigidbody.isKinematic = true;
            panRigidbody.useGravity = false;
            panRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            initialLocalPosition = panTransform.localPosition;
            initialLocalRotation = panTransform.localRotation;

            targetLocalPosition = initialLocalPosition;
            targetLocalRotation = initialLocalRotation;
        }

        /// <summary>
        /// เข้าใช้งานสถานี: อ้างอิงทิศทางจากกล้องประจำสถานีแล้วเริ่มรับอินพุต (IStationModule)
        /// </summary>
        public void OnStationEnter(Camera stationCamera)
        {
            referenceTransform = stationCamera != null ? stationCamera.transform : null;
            SetControllerActive(true);
        }

        /// <summary>
        /// ออกจากสถานี: หยุดรับอินพุตและสไลด์กระทะกลับตำแหน่งเริ่มต้น (IStationModule)
        /// </summary>
        public void OnStationExit()
        {
            SetControllerActive(false);
        }

        private void OnEnable()
        {
            moveAction?.action?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action?.Disable();
        }

        /// <summary>
        /// กำหนด Transform อ้างอิงทิศทางการเคลื่อนที่ (เช่น กล้องทำอาหาร)
        /// </summary>
        public void SetReferenceTransform(Transform refTrans)
        {
            referenceTransform = refTrans;
        }

        /// <summary>
        /// เปิด/ปิด การใช้งานระบบควบคุมกระทะ
        /// </summary>
        public void SetControllerActive(bool active)
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

        /// <summary>
        /// รีเซ็ตตำแหน่งและมุมเอียงของกระทะให้สไลด์และหมุนกลับสู่จุดเริ่มต้นบนเตาอย่างนุ่มนวล
        /// </summary>
        public void ResetPanPosition()
        {
            targetLocalPosition = initialLocalPosition;
            targetLocalRotation = initialLocalRotation;
            isResetting = true;
        }

        private void Update()
        {
            if (panTransform == null) return;

            if (isControllerActive)
            {
                Vector2 input = ReadMoveInput();

                if (input.sqrMagnitude > 0.001f)
                {
                    // 1. หา Transform ที่ใช้อ้างอิงทิศทาง (กล้องทำอาหาร หรือ Camera.main)
                    Camera mainCam = GetMainCamera();
                    Transform refTrans = referenceTransform != null
                        ? referenceTransform
                        : (mainCam != null ? mainCam.transform : (panTransform.parent != null ? panTransform.parent : transform));

                    Vector3 forward = Vector3.forward;
                    Vector3 right = Vector3.right;

                    if (refTrans != null)
                    {
                        forward = Vector3.ProjectOnPlane(refTrans.forward, Vector3.up).normalized;
                        right = Vector3.ProjectOnPlane(refTrans.right, Vector3.up).normalized;
                    }

                    // 2. คำนวณทิศทางการสไลด์ตำแหน่งแบบ Camera-Relative
                    Vector3 worldMoveDir = (forward * input.y) + (right * input.x);

                    Vector3 localMoveDir = panTransform.parent != null
                        ? panTransform.parent.InverseTransformDirection(worldMoveDir)
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

                    // 3. คำนวณการเอียงกระทะแบบ Camera-Relative (Tilt: Pitch & Roll)
                    Quaternion tiltWorldRot = Quaternion.AngleAxis(input.y * maxTiltAngle, right) *
                                              Quaternion.AngleAxis(-input.x * maxTiltAngle, forward);

                    Transform parentTrans = panTransform.parent;
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
            if (panTransform == null) return;

            if (isControllerActive)
            {
                // เลื่อนตำแหน่งกระทะอย่างนุ่มนวลผ่านทางฟิสิกส์
                Vector3 newLocalPos = Vector3.Lerp(
                    panTransform.localPosition,
                    targetLocalPosition,
                    Time.fixedDeltaTime * smoothSpeed
                );

                // หมุนเอียงองศากระทะอย่างนุ่มนวลผ่านทางฟิสิกส์
                Quaternion newLocalRot = Quaternion.Slerp(
                    panTransform.localRotation,
                    targetLocalRotation,
                    Time.fixedDeltaTime * tiltSmoothSpeed
                );

                ApplyPhysicsTransform(newLocalPos, newLocalRot);
            }
            else if (isResetting)
            {
                // สไลด์ตำแหน่งและหมุนคืนค่าตั้งต้นแบบนุ่มนวลเมื่อออกจากสถานี
                Vector3 newLocalPos = Vector3.Lerp(
                    panTransform.localPosition,
                    initialLocalPosition,
                    Time.fixedDeltaTime * smoothSpeed
                );

                Quaternion newLocalRot = Quaternion.Slerp(
                    panTransform.localRotation,
                    initialLocalRotation,
                    Time.fixedDeltaTime * tiltSmoothSpeed
                );

                ApplyPhysicsTransform(newLocalPos, newLocalRot);

                bool posReached = Vector3.SqrMagnitude(panTransform.localPosition - initialLocalPosition) < 0.00001f;
                bool rotReached = Quaternion.Angle(panTransform.localRotation, initialLocalRotation) < 0.1f;

                if (posReached && rotReached)
                {
                    ApplyPhysicsTransform(initialLocalPosition, initialLocalRotation);
                    isResetting = false;
                }
            }
        }

        /// <summary>
        /// ใช้ MovePosition และ MoveRotation เพื่อย้ายกระทะผ่าน PhysX engine สำหรับ Kinematic Rigidbody
        /// </summary>
        private void ApplyPhysicsTransform(Vector3 localPos, Quaternion localRot)
        {
            Transform parentTrans = panTransform.parent;
            Vector3 worldPos = parentTrans != null ? parentTrans.TransformPoint(localPos) : localPos;
            Quaternion worldRot = parentTrans != null ? parentTrans.rotation * localRot : localRot;

            if (panRigidbody != null && panRigidbody.isKinematic)
            {
                panRigidbody.MovePosition(worldPos);
                panRigidbody.MoveRotation(worldRot);
            }
            else
            {
                panTransform.localPosition = localPos;
                panTransform.localRotation = localRot;
            }
        }

        /// <summary>
        /// อ่านค่า Input จาก New Input System หรือ Keyboard Fallback
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            if (moveAction?.action != null)
            {
                Vector2 val = moveAction.action.ReadValue<Vector2>();
                if (val.sqrMagnitude > 0.001f)
                {
                    return val;
                }
            }

            // Fallback อ่านค่าจาก Keyboard โดยตรง
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

                return new Vector2(x, y).normalized;
            }

            return Vector2.zero;
        }
    }
}
