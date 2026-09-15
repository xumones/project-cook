using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Cooking
{
    /// <summary>คีบ/ยก และลากวัตถุดิบด้วยแรงฟิสิกส์ จับทีละชิ้น ชิ้นอื่นของวัตถุดิบเดียวกันยังอิสระ</summary>
    public class IngredientDragController : MonoBehaviour
    {
        [Header("Layer Settings")]
        [Tooltip("LayerMask สำหรับตรวจจับชิ้นวัตถุดิบอาหาร")]
        [SerializeField] private LayerMask ingredientLayerMask;

        [Header("Drag Settings")]
        [Tooltip("ระยะยิง Raycast เล็งวัตถุดิบสูงสุด (เมตร) ปรับให้เป็นระยะเอื้อมมือทั่วไปเพราะตอนนี้ใช้ได้ทั้งเกม ไม่ใช่แค่มุมกล้องสถานีระยะประชิดอีกต่อไป")]
        [Range(0.5f, 8f)]
        [SerializeField] private float maxRaycastDistance = 3f;

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

        [Tooltip("ระยะใกล้สุดจากกล้อง (เมตร) ค่าเริ่มต้นปรับสำหรับระยะเอื้อมมือแบบเดินอิสระ ควร Test แล้วปรับอีกทีตามความเหมาะสม")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float minGripDistance = 0.4f;

        [Tooltip("ระยะไกลสุดจากกล้อง (เมตร) ค่าเริ่มต้นปรับสำหรับระยะเอื้อมมือแบบเดินอิสระ ควร Test แล้วปรับอีกทีตามความเหมาะสม")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float maxGripDistance = 1.2f;

        private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[16];
        private IngredientInstance currentGrippedIngredient = null;
        private Rigidbody grippedRigidbody = null;
        private Camera cachedCamera = null;
        private Vector3 localGripPoint = Vector3.zero;
        private float initialGripDistance = 0f;
        private float originalLinearDamping;
        private float originalAngularDamping;
        private RigidbodyInterpolation originalInterpolation;

        private Camera GetEffectiveCamera()
        {
            if (cachedCamera == null) cachedCamera = Camera.main;
            return cachedCamera;
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
            if (currentGrippedIngredient == null) return;

            Camera cam = GetEffectiveCamera();
            if (cam == null) return;

            Rigidbody rb = grippedRigidbody;
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

                Rigidbody body = hit.collider.attachedRigidbody;
                IngredientInstance ingredient = hit.collider.GetComponentInParent<IngredientInstance>();
                if (ingredient == null || body == null || hit.distance >= closestDistance) continue;

                closestDistance = hit.distance;
                closestIngredient = ingredient;
                closestHitPoint = hit.point;
                closestRb = body;
            }

            if (closestIngredient == null) return;

            currentGrippedIngredient = closestIngredient;
            grippedRigidbody = closestRb;
            currentGrippedIngredient.GrippedBody = grippedRigidbody;

            // ระหว่างคีบ crosshair จะสลับ hint ถี่ตามของที่กวาดผ่าน ซ่อนไว้จนกว่าจะปล่อย
            PlayerAiming.Instance?.SuppressHints(this);

            originalLinearDamping = grippedRigidbody.linearDamping;
            originalAngularDamping = grippedRigidbody.angularDamping;
            originalInterpolation = grippedRigidbody.interpolation;

            // ขับด้วยแรง (non-kinematic) ให้ยังหมุนและทิ้งตัวตามแรงโน้มถ่วงได้
            grippedRigidbody.isKinematic = false;
            grippedRigidbody.useGravity = true;
            grippedRigidbody.linearDamping = grippingDamping;
            grippedRigidbody.angularDamping = grippingAngularDamping;
            grippedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            localGripPoint = grippedRigidbody.transform.InverseTransformPoint(closestHitPoint);

            // Clamp ระยะทันทีตอนคีบ กันคีบของไกลสุด Raycast แล้วโดนลากติดมือข้ามระยะทางไกล
            initialGripDistance = Mathf.Clamp(closestDistance, minGripDistance, maxGripDistance);
        }

        private void ReleaseIngredient()
        {
            if (currentGrippedIngredient == null) return;

            currentGrippedIngredient.GrippedBody = null;
            PlayerAiming.Instance?.ReleaseHintSuppression(this);

            if (grippedRigidbody != null)
            {
                grippedRigidbody.linearDamping = originalLinearDamping;
                grippedRigidbody.angularDamping = originalAngularDamping;
                grippedRigidbody.interpolation = originalInterpolation;

                // จำกัดความเร็วขณะปล่อยเพื่อวางลงบนกระทะอย่างนุ่มนวล
                grippedRigidbody.linearVelocity = Vector3.ClampMagnitude(grippedRigidbody.linearVelocity, 2f);
                grippedRigidbody.angularVelocity = Vector3.ClampMagnitude(grippedRigidbody.angularVelocity, 2f);
            }

            currentGrippedIngredient = null;
            grippedRigidbody = null;
        }

        private static bool IsLeftClickPressedThisFrame()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private static bool IsLeftClickReleasedThisFrame()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
        }

        private static float GetNormalizedMouseScroll()
        {
            if (Mouse.current == null) return 0f;

            float value = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(value) > 0.01f ? Mathf.Sign(value) : 0f;
        }
    }
}
