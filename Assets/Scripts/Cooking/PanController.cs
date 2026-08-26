using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์จัดการการเคลื่อนที่และการเอียงกระทะบนเตาในโหมดทำอาหาร (Hybrid: WASD Position + Dynamic Tilting via Kinematic Physics)
    /// </summary>
    public class PanController : MonoBehaviour
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

        [Header("Food Physics Settings")]
        [Tooltip("ตัวคูณแรงเหวี่ยง (Inertia Force) เมื่อสไลด์กระทะ WASD")]
        [SerializeField] private float momentumMultiplier = 5f;

        [Tooltip("ตัวคูณแรงสไลด์ตามความเอียงของกระทะ (Slope Gravity Assist)")]
        [SerializeField] private float slopeForceMultiplier = 8f;

        [Tooltip("ตัวคูณแรงหมุน/พลิกตัวของวัตถุ (Rolling Torque)")]
        [SerializeField] private float rollTorqueMultiplier = 3f;

        [Tooltip("แรงดึงประคองเข้าหาก้นกระทะเบาๆ ป้องกันอาหารกระเด็นหลุดกระทะง่ายเกินไป")]
        [SerializeField] private float bowlAttractionForce = 2f;

        [Header("Input Settings")]
        [Tooltip("Action จาก New Input System สำหรับอ่านค่า Vector2 (WASD)")]
        [SerializeField] private InputActionReference moveAction;

        [Header("Reference Transform Settings")]
        [Tooltip("Transform สำหรับอ้างอิงทิศทาง (เช่น กล้อง stationCamera) หากไม่ใส่จะอ้างอิงจาก Camera.main หรือเตา")]
        [SerializeField] private Transform referenceTransform;

        [Header("Food Carrier Settings")]
        [Tooltip("สคริปต์จัดการภาชนะและฟิสิกส์อาหารในกระทะ (หากไม่ใส่จะค้นหาใน GameObject นี้หรือ Child)")]
        [SerializeField] private PanFoodContainer foodContainer;

        private Rigidbody panRigidbody;
        private Camera cachedMainCamera;

        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private Vector3 targetLocalPosition;
        private Quaternion targetLocalRotation;
        private Vector3 previousPanPosition;

        private bool isControllerActive = false;
        private bool isResetting = false;

        public bool IsControllerActive => isControllerActive;
        public PanFoodContainer FoodContainer => foodContainer;

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

            if (foodContainer == null)
            {
                foodContainer = panTransform.GetComponentInChildren<PanFoodContainer>();
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
            previousPanPosition = panTransform.position;
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

            // คำนวณและส่งแรงฟิสิกส์สไลด์/เหวี่ยงใส่อาหารที่อยู่ในกระทะ
            ApplyFoodCarrierPhysics();
        }

        /// <summary>
        /// คำนวณความเร็วกระทะ และส่งแรงฟิสิกส์ดันอาหารให้สไลด์และกลิ้งตามการเคลื่อนที่และการเอียงของกระทะ
        /// </summary>
        private void ApplyFoodCarrierPhysics()
        {
            if (foodContainer == null || panTransform == null) return;

            var items = foodContainer.GetContainedFoodItems();
            if (items == null || items.Count == 0) return;

            Vector3 panDeltaPos = panTransform.position - previousPanPosition;
            Vector3 panVelocity = panDeltaPos / Time.fixedDeltaTime;
            previousPanPosition = panTransform.position;

            Vector3 panUp = panTransform.up;
            Vector3 panCenter = panTransform.position;
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Physics.gravity, panUp);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.Rigidbody == null || item.Rigidbody.isKinematic) continue;

                // หากถูกคีบจับอยู่ ให้ข้ามแรงลากเคลื่อนย้ายฟิสิกส์ของกระทะ (เพื่อไม่ให้แย่งแรงกับ IngredientDragController)
                if (item.Ingredient != null && item.Ingredient.IsGripped) continue;

                // --- 1. Physics Movement Logic (Batching Forces) ---
                Vector3 inertiaForce = -panVelocity * momentumMultiplier;
                Vector3 slopeForce = slopeDirection * slopeForceMultiplier;
                Vector3 toCenterDir = (panCenter - item.Rigidbody.position);
                toCenterDir.y = 0f;
                Vector3 attractionForce = toCenterDir * bowlAttractionForce;

                Vector3 combinedForce = inertiaForce + slopeForce + attractionForce;
                if (combinedForce.sqrMagnitude > 0.0001f)
                {
                    item.Rigidbody.AddForce(combinedForce, ForceMode.Acceleration);
                }

                // แรงหมุนตัว/กลิ้งของวัตถุดิบ (Rolling & Tumbling Torque)
                Vector3 foodVel = item.Rigidbody.linearVelocity;
                if (foodVel.sqrMagnitude > 0.01f)
                {
                    Vector3 rollAxis = Vector3.Cross(panUp, foodVel.normalized);
                    item.Rigidbody.AddTorque(rollAxis * (foodVel.magnitude * rollTorqueMultiplier), ForceMode.Acceleration);
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
