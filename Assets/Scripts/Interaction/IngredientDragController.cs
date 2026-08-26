using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Cooking;

namespace ProjectCook.Interaction
{
    /// <summary>
    /// สคริปต์ควบคุมการจับ คีบ/ยก และลากเคลื่อนย้ายอาหารด้วยระบบแรงฟิสิกส์ (Physics Velocity-Driven Dragging)
    /// ช่วยให้อาหารเคลื่อนที่ตามเมาส์โดยไม่ทะลุขอบกระทะและวัตถุอื่นในฉาก (PhysX Collision-Safe)
    ///
    /// อยู่ในโฟลเดอร์ Interaction เพราะเป็นเครื่องมือของผู้เล่นสำหรับหยิบจับวัตถุ
    /// ไม่ได้ผูกกับสถานีทำอาหารชนิดใดชนิดหนึ่ง สถานีใดก็นำไปใช้เป็นโมดูลได้
    /// </summary>
    public class IngredientDragController : MonoBehaviour, IStationModule
    {
        [Header("Layer Settings")]
        [Tooltip("LayerMask สำหรับตรวจจับชิ้นวัตถุดิบอาหาร")]
        [SerializeField] private LayerMask ingredientLayerMask;

        [Header("Drag Settings")]
        [Tooltip("ระยะยิง Raycast เล็งวัตถุดิบสูงสุด (เมตร)")]
        [SerializeField] private float maxRaycastDistance = 10f;

        [Tooltip("ตัวคูณความเร็วในการลากเคลื่อนย้ายฟิสิกส์ตามเมาส์")]
        [SerializeField] private float dragForceMultiplier = 25f;

        [Tooltip("ความเร็วสูงสุดในการลากเพื่อป้องกันอาหารกระเด็นหลุดกระทะ (เมตร/วินาที)")]
        [SerializeField] private float maxDragSpeed = 6f;

        [Tooltip("ตัวคูณแรงต้านทานความเร็วขณะคีบเพื่อความนุ่มนวลในการควบคุม (Linear Damping)")]
        [SerializeField] private float grippingDamping = 1.0f;

        [Tooltip("ตัวคูณแรงต้านทานความเร็วหมุนขณะคีบเพื่อลดการแกว่งโคลงเคลง (Angular Damping)")]
        [SerializeField] private float grippingAngularDamping = 1.0f;

        [Tooltip("ตัวคูณความแน่นหนาของแรงยึดจุดหมุน (Grip Stiffness Gain)")]
        [SerializeField] private float gripSpringGain = 1.0f;

        [Header("Hanging Torque & Tilt Settings")]
        [Tooltip("ความเร็วแรงบิดในการดึงหมุนวัตถุดิบลงแนวตั้ง (Hanging Torque Stiffness)")]
        [SerializeField] private float hangingTorqueStiffness = 0.25f;

        [Tooltip("มุมเอียงสูงสุดจากแนวตั้งดิ่ง เพื่อไม่ให้อาหารตั้งฉาก 90° และแตะพื้นกระทะราบเสมอ (องศา)")]
        [Range(1f, 45f)]
        [SerializeField] private float maxVerticalTiltAngle = 25f;

        [Header("Scroll Distance Settings")]
        [Tooltip("ความไวในการปรับระยะใกล้-ไกลด้วย Scroll Wheel (เมตรต่อคลิก)")]
        [Range(0.01f, 2.0f)]
        [SerializeField] private float scrollSensitivity = 0.5f;

        [Tooltip("ระยะใกล้สุดจากกล้อง (เมตร)")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float minGripDistance = 0.2f;

        [Tooltip("ระยะไกลสุดจากกล้อง (เมตร)")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float maxGripDistance = 0.8f;

        [Header("Camera Reference")]
        [Tooltip("กล้องอ้างอิงระนาบการมองเห็น (หากไม่ใส่จะใช้ Camera.main)")]
        [SerializeField] private Camera targetCamera;

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[16];
        private bool isControllerActive = false;
        private IngredientInstance currentGrippedIngredient = null;
        private Rigidbody grippedRigidbody = null;
        private Camera cachedCamera = null;
        private Vector3 localGripPoint = Vector3.zero;
        private float initialGripDistance = 0f;
        private float originalLinearDamping = 0.05f;
        private float originalAngularDamping = 0.05f;
        private RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;
        private RigidbodyConstraints originalConstraints = RigidbodyConstraints.None;

        public bool IsControllerActive => isControllerActive;
        public IngredientInstance CurrentGrippedIngredient => currentGrippedIngredient;

        public void SetTargetCamera(Camera cam)
        {
            targetCamera = cam;
        }

        /// <summary>
        /// เข้าใช้งานสถานี: ใช้กล้องประจำสถานีเป็นระนาบอ้างอิงแล้วเริ่มรับอินพุต (IStationModule)
        /// </summary>
        public void OnStationEnter(Camera stationCamera)
        {
            SetTargetCamera(stationCamera);
            SetControllerActive(true);
        }

        /// <summary>
        /// ออกจากสถานี: หยุดรับอินพุตและปล่อยวัตถุดิบที่คีบค้างอยู่ (IStationModule)
        /// </summary>
        public void OnStationExit()
        {
            SetControllerActive(false);
        }

        private Camera GetEffectiveCamera()
        {
            if (targetCamera != null) return targetCamera;
            if (cachedCamera == null) cachedCamera = Camera.main;
            return cachedCamera;
        }

        /// <summary>
        /// เปิด/ปิด ระบบคีบลากวัตถุดิบ (ถูกสั่งจาก PanStation ตอนเข้า/ออกสถานีทำอาหาร)
        /// หมายเหตุ: ไม่ต้องสั่งล็อกเคอร์เซอร์เอง เพราะสถานะเริ่มต้นของ CursorManager คือล็อกอยู่แล้ว
        /// </summary>
        public void SetControllerActive(bool active)
        {
            isControllerActive = active;
            if (!active && currentGrippedIngredient != null)
            {
                ReleaseIngredient();
            }
        }

        private void Awake()
        {
            if (ingredientLayerMask.value == 0)
            {
                ingredientLayerMask = Physics.DefaultRaycastLayers;
            }
        }

        private void Update()
        {
            if (!isControllerActive) return;

            Camera cam = GetEffectiveCamera();
            if (cam == null) return;

            bool isLMBPressed = IsLeftClickPressedThisFrame();
            bool isLMBReleased = IsLeftClickReleasedThisFrame();

            // 1. เมื่อเริ่มกดคลิกซ้าย: ยิง Raycast จากกึ่งกลางหน้าจอ (Viewport 0.5, 0.5) เพื่อเริ่มจับ
            if (isLMBPressed && currentGrippedIngredient == null)
            {
                TryGripIngredient(cam);
            }

            // 2. ขณะจับวัตถุดิบอยู่: อ่านค่า Scroll Wheel เพื่อปรับระยะใกล้-ไกลตามแนวสายตา
            if (currentGrippedIngredient != null)
            {
                float scrollDir = GetNormalizedMouseScroll();
                if (Mathf.Abs(scrollDir) > 0.01f)
                {
                    initialGripDistance += scrollDir * scrollSensitivity;
                    initialGripDistance = Mathf.Clamp(initialGripDistance, minGripDistance, maxGripDistance);
                }
            }

            // 3. เมื่อปล่อยคลิกซ้าย: ปล่อยให้อาหารตกลงตามฟิสิกส์
            if (isLMBReleased && currentGrippedIngredient != null)
            {
                ReleaseIngredient();
            }
        }

        private void FixedUpdate()
        {
            if (!isControllerActive || currentGrippedIngredient == null) return;

            Camera cam = GetEffectiveCamera();
            if (cam == null) return;

            Rigidbody rb = grippedRigidbody != null ? grippedRigidbody : currentGrippedIngredient.GetComponent<Rigidbody>();
            if (rb == null) return;

            // 1. คำนวณตำแหน่งเป้าหมายบน Ray และส่งแรงดึงจุดคีบ (Grip Point Drive)
            Ray ray = GetCenterScreenRay(cam);
            Vector3 worldTargetPoint = ray.GetPoint(initialGripDistance);

            Vector3 currentWorldGripPoint = rb.transform.TransformPoint(localGripPoint);
            Vector3 displacement = worldTargetPoint - currentWorldGripPoint;
            Vector3 desiredVelocity = displacement * dragForceMultiplier;
            desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxDragSpeed);

            Vector3 currentVelocityAtPoint = rb.GetPointVelocity(currentWorldGripPoint);
            Vector3 force = (desiredVelocity - currentVelocityAtPoint) * gripSpringGain;
            rb.AddForceAtPosition(force, currentWorldGripPoint, ForceMode.Acceleration);

            // 2. ออกแรงบิดช่วยดึงลงแนวตั้งตามค่าใน Inspector (หาก hangingTorqueStiffness > 0)
            ApplyFastHangingTorque(rb);
        }

        private void ApplyFastHangingTorque(Rigidbody rb)
        {
            if (hangingTorqueStiffness <= 0.001f) return;

            // คำนวณ Vector แขนคานหมุนจากจุดจับ ➔ ไปยังจุดศูนย์ถ่วง (Local Space)
            Vector3 localArm = rb.centerOfMass - localGripPoint;
            if (localArm.sqrMagnitude < 0.0001f)
            {
                localArm = -localGripPoint;
            }
            if (localArm.sqrMagnitude < 0.0001f) return;

            // แปลง Vector แขนไปยัง World Space
            Vector3 currentWorldArm = rb.transform.TransformDirection(localArm.normalized);

            // คำนวณทิศทางเป้าหมายโดยเอียงทำมุม maxVerticalTiltAngle จากแนวตั้งดิ่ง เพื่อไม่ให้ตั้งตรง 90° จนเกินไป
            Camera cam = GetEffectiveCamera();
            Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.001f) camForward = Vector3.forward;
            camForward.Normalize();

            Vector3 tiltAxis = Vector3.Cross(Vector3.down, camForward);
            if (tiltAxis.sqrMagnitude < 0.001f) tiltAxis = Vector3.right;
            Vector3 targetHangingDir = Quaternion.AngleAxis(maxVerticalTiltAngle, tiltAxis.normalized) * Vector3.down;

            // คำนวณแรงบิดบิดแขน (Grip ➔ CoM) ให้ชี้เข้าหา targetHangingDir
            Vector3 torqueAxis = Vector3.Cross(currentWorldArm, targetHangingDir);
            float sinAngle = torqueAxis.magnitude;

            if (sinAngle > 0.001f)
            {
                // Smooth Deadzone Fade (< 1.5 องศา) ขจัดอาการสั่นสะกิด
                float deadzoneFactor = Mathf.Clamp01(sinAngle / 0.025f);
                Vector3 activeTorque = torqueAxis.normalized * (sinAngle * deadzoneFactor * hangingTorqueStiffness * rb.mass);
                rb.AddTorque(activeTorque, ForceMode.Force);
            }
        }

        private Ray GetCenterScreenRay(Camera cam)
        {
            // ยิง Raycast จากเป้ากึ่งกลางหน้าจอพอดีเป๊ะ (Center Crosshair)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        private void TryGripIngredient(Camera cam)
        {
            Ray ray = GetCenterScreenRay(cam);
            int hitCount = Physics.RaycastNonAlloc(ray, raycastHitBuffer, maxRaycastDistance, ingredientLayerMask);
            if (hitCount <= 0) return;

            IngredientInstance closestIngredient = null;
            Rigidbody closestRb = null;
            Vector3 closestHitPoint = Vector3.zero;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = raycastHitBuffer[i];
                if (hit.collider == null) continue;

                IngredientInstance ingredient = hit.collider.GetComponentInParent<IngredientInstance>();
                if (ingredient == null)
                {
                    ingredient = hit.collider.GetComponent<IngredientInstance>();
                }

                if (ingredient != null && hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestIngredient = ingredient;
                    closestHitPoint = hit.point;
                    closestRb = ingredient.GetComponent<Rigidbody>();
                }
            }

            if (closestIngredient != null)
            {
                currentGrippedIngredient = closestIngredient;
                currentGrippedIngredient.IsGripped = true;
                grippedRigidbody = closestRb;

                if (grippedRigidbody != null)
                {
                    originalLinearDamping = grippedRigidbody.linearDamping;
                    originalAngularDamping = grippedRigidbody.angularDamping;
                    originalInterpolation = grippedRigidbody.interpolation;
                    originalConstraints = grippedRigidbody.constraints;

                    // เปิดใช้ Physics Velocity Drive (non-kinematic) โดยอนุญาตให้หมุนและทิ้งตัวตามแรงโน้มถ่วง
                    grippedRigidbody.isKinematic = false;
                    grippedRigidbody.useGravity = true;
                    grippedRigidbody.linearDamping = grippingDamping;
                    grippedRigidbody.angularDamping = grippingAngularDamping;
                    grippedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    grippedRigidbody.constraints = originalConstraints;

                    // บันทึกตำแหน่งจุดที่คลิกโดนใน Local Space ของ Rigidbody
                    localGripPoint = grippedRigidbody.transform.InverseTransformPoint(closestHitPoint);
                }

                // บันทึกระยะห่างความลึกคงที่ (Fixed Distance) ระหว่างกล้องกับจุดที่คลิกโดน
                initialGripDistance = closestDistance;
            }
        }

        private void ReleaseIngredient()
        {
            if (currentGrippedIngredient != null)
            {
                currentGrippedIngredient.IsGripped = false;

                Rigidbody rb = grippedRigidbody != null ? grippedRigidbody : currentGrippedIngredient.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.linearDamping = originalLinearDamping;
                    rb.angularDamping = originalAngularDamping;
                    rb.interpolation = originalInterpolation;
                    rb.constraints = originalConstraints;

                    // จำกัดความเร็วขณะปล่อยเพื่อวางลงบนกระทะอย่างนุ่มนวล
                    rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, 2f);
                    rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 2f);
                }

                currentGrippedIngredient = null;
                grippedRigidbody = null;
            }
        }

        // --- Input Helper Methods ---
        private bool IsLeftClickPressedThisFrame()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasPressedThisFrame;
            }
            return Input.GetMouseButtonDown(0);
        }

        private bool IsLeftClickReleasedThisFrame()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.leftButton.wasReleasedThisFrame;
            }
            return Input.GetMouseButtonUp(0);
        }

        private float GetNormalizedMouseScroll()
        {
            float val = 0f;
            if (Mouse.current != null)
            {
                val = Mouse.current.scroll.ReadValue().y;
            }
            else
            {
                val = Input.mouseScrollDelta.y;
            }

            if (Mathf.Abs(val) > 0.01f)
            {
                return Mathf.Sign(val);
            }
            return 0f;
        }
    }
}
