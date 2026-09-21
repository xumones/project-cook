using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Cooking
{
    /// <summary>คีบ/ยก และลากวัตถุดิบด้วยแรงฟิสิกส์ จับทีละชิ้น ชิ้นอื่นของวัตถุดิบเดียวกันยังอิสระ เป้าที่คีบมาจาก PlayerAiming</summary>
    public class IngredientDragController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference gripAction;
        [Tooltip("Vector2 จาก scroll wheel ใช้แกน y ปรับระยะใกล้-ไกล")]
        [SerializeField] private InputActionReference distanceAction;

        [Header("Drag Settings")]
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

        private IngredientInstance currentGrippedIngredient;
        private Rigidbody grippedRigidbody;
        private Vector3 localGripPoint;
        private float gripDistance;
        private float originalLinearDamping;
        private float originalAngularDamping;
        private RigidbodyInterpolation originalInterpolation;

        private void OnEnable()
        {
            gripAction.EnableAction();
            distanceAction.EnableAction();
        }

        private void OnDisable()
        {
            gripAction.DisableAction();
            distanceAction.DisableAction();
            ReleaseIngredient();
        }

        private void Update()
        {
            // คีบได้ทั้งตอนเดินและในสถานี เข้าคุยกลางคันต้องปล่อยของที่คีบอยู่ ไม่ให้ค้างในมือ
            if (!GameModeManager.WorldInputAllowed)
            {
                ReleaseIngredient();
                return;
            }

            if (currentGrippedIngredient == null)
            {
                if (gripAction.WasPressedThisFrame()) TryGripAimed();
                return;
            }

            float scroll = distanceAction.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                gripDistance = Mathf.Clamp(gripDistance + Mathf.Sign(scroll) * scrollSensitivity, minGripDistance, maxGripDistance);
            }

            if (gripAction.WasReleasedThisFrame()) ReleaseIngredient();
        }

        private void FixedUpdate()
        {
            Rigidbody rb = grippedRigidbody;
            if (rb == null || PlayerAiming.Instance == null) return;

            // 1. ดึงจุดคีบไปยังจุดบนเส้นเล็งที่ระยะ gripDistance
            Ray ray = PlayerAiming.Instance.AimRay;
            Vector3 worldTargetPoint = ray.GetPoint(gripDistance);

            Vector3 currentWorldGripPoint = rb.transform.TransformPoint(localGripPoint);
            Vector3 displacement = worldTargetPoint - currentWorldGripPoint;
            Vector3 desiredVelocity = Vector3.ClampMagnitude(displacement * dragForceMultiplier, maxDragSpeed);

            Vector3 currentVelocityAtPoint = rb.GetPointVelocity(currentWorldGripPoint);
            Vector3 force = (desiredVelocity - currentVelocityAtPoint) * gripSpringGain;
            rb.AddForceAtPosition(force, currentWorldGripPoint, ForceMode.Acceleration);

            // 2. แรงบิดช่วยดึงลงแนวตั้ง
            ApplyHangingTorque(rb, ray.direction);
        }

        private void ApplyHangingTorque(Rigidbody rb, Vector3 aimDirection)
        {
            if (hangingTorqueStiffness <= 0.001f) return;

            // แขนคานจากจุดจับไปยังจุดศูนย์ถ่วง (Local Space)
            Vector3 localArm = rb.centerOfMass - localGripPoint;
            if (localArm.sqrMagnitude < 0.0001f)
            {
                localArm = -localGripPoint;
            }
            if (localArm.sqrMagnitude < 0.0001f) return;

            Vector3 currentWorldArm = rb.transform.TransformDirection(localArm.normalized);

            // เอียงทำมุม maxVerticalTiltAngle จากแนวดิ่ง ไม่ให้ตั้งตรง 90° จนเกินไป
            Vector3 forward = aimDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 tiltAxis = Vector3.Cross(Vector3.down, forward);
            if (tiltAxis.sqrMagnitude < 0.001f) tiltAxis = Vector3.right;
            Vector3 targetHangingDir = Quaternion.AngleAxis(maxVerticalTiltAngle, tiltAxis.normalized) * Vector3.down;

            Vector3 torqueAxis = Vector3.Cross(currentWorldArm, targetHangingDir);
            float sinAngle = torqueAxis.magnitude;

            if (sinAngle > 0.001f)
            {
                // Smooth deadzone (< 1.5 องศา) ขจัดอาการสั่น
                float deadzoneFactor = Mathf.Clamp01(sinAngle / 0.025f);
                Vector3 activeTorque = torqueAxis.normalized * (sinAngle * deadzoneFactor * hangingTorqueStiffness * rb.mass);
                rb.AddTorque(activeTorque, ForceMode.Force);
            }
        }

        // ใช้เป้าเดียวกับ crosshair ผู้เล่นจึงคีบชิ้นที่กำลังเห็นไอคอนอยู่เสมอ
        private void TryGripAimed()
        {
            PlayerAiming aiming = PlayerAiming.Instance;
            Collider target = aiming != null ? aiming.Target : null;
            if (target == null) return;

            Rigidbody body = target.attachedRigidbody;
            IngredientInstance ingredient = target.GetComponentInParent<IngredientInstance>();
            if (ingredient == null || body == null) return;

            currentGrippedIngredient = ingredient;
            grippedRigidbody = body;
            ingredient.GrippedBody = body;

            // ระหว่างคีบ crosshair จะสลับ hint ถี่ตามของที่กวาดผ่าน ซ่อนไว้จนกว่าจะปล่อย
            aiming.SuppressHints(this);

            originalLinearDamping = body.linearDamping;
            originalAngularDamping = body.angularDamping;
            originalInterpolation = body.interpolation;

            // ขับด้วยแรง (non-kinematic) ให้ยังหมุนและทิ้งตัวตามแรงโน้มถ่วงได้
            body.isKinematic = false;
            body.useGravity = true;
            body.linearDamping = grippingDamping;
            body.angularDamping = grippingAngularDamping;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            localGripPoint = body.transform.InverseTransformPoint(aiming.Hit.point);

            // Clamp ระยะทันทีตอนคีบ กันคีบของไกลแล้วโดนลากติดมือข้ามระยะ
            gripDistance = Mathf.Clamp(aiming.Hit.distance, minGripDistance, maxGripDistance);
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
    }
}
