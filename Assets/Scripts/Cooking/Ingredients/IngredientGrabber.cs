using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;
using ProjectCook.Stations;

namespace ProjectCook.Cooking
{
    /// <summary>โมดูลสถานีสำหรับคีบ ลาก และหมุนวัตถุดิบด้วยแรงฟิสิกส์ จับทีละชิ้น เป้าที่คีบมาจาก PlayerAiming</summary>
    public class IngredientGrabber : MonoBehaviour, IStationModule
    {
        [Header("Input")]
        [SerializeField] private InputActionReference gripAction;
        [Tooltip("Vector2 จาก scroll wheel ใช้แกน y ปรับระยะใกล้-ไกล")]
        [SerializeField] private InputActionReference distanceAction;
        [Tooltip("กดค้างระหว่างคีบเพื่อหมุนวัตถุดิบตามเมาส์")]
        [SerializeField] private InputActionReference rotateHoldAction;
        [Tooltip("Vector2 ระยะที่เมาส์ขยับต่อเฟรม ใช้หมุนวัตถุดิบ")]
        [SerializeField] private InputActionReference rotateDeltaAction;

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

        [Header("Rotate Settings")]
        [Tooltip("สถานีนี้ให้กด Rotate Hold ค้างเพื่อหมุนวัตถุดิบได้หรือไม่")]
        [SerializeField] private bool allowRotation;

        [Tooltip("องศาที่หมุนต่อการขยับเมาส์ 1 พิกเซล")]
        [SerializeField] private float rotateSensitivity = 0.3f;

        [Tooltip("ความไวในการไล่ตามมุมเป้าหมาย สูง = ตามทันเร็ว")]
        [SerializeField] private float rotateStiffness = 15f;

        [Tooltip("ความเร็วหมุนสูงสุด (เรเดียน/วินาที) ไม่ควรเกิน Max Angular Velocity ของ Rigidbody (ค่าเริ่มต้น 7)")]
        [SerializeField] private float maxRotateSpeed = 6f;

        [Header("Scroll Distance Settings")]
        [Tooltip("ความไวในการปรับระยะใกล้-ไกลด้วย Scroll Wheel (เมตรต่อคลิก)")]
        [Range(0.01f, 2.0f)]
        [SerializeField] private float scrollSensitivity = 0.5f;

        [Tooltip("ระยะใกล้สุดจากกล้อง (เมตร)")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float minGripDistance = 0.4f;

        [Tooltip("ระยะไกลสุดจากกล้อง (เมตร)")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float maxGripDistance = 1.2f;

        private IngredientInstance currentGrippedIngredient;
        private Rigidbody grippedRigidbody;
        private Vector3 localGripPoint;
        private float gripDistance;
        private float originalLinearDamping;
        private float originalAngularDamping;
        private RigidbodyInterpolation originalInterpolation;

        private bool isActive;
        private bool isRotating;
        // หมุนเองแล้วครั้งหนึ่ง คงมุมที่ผู้เล่นตั้งไว้แทนการห้อยลง จนกว่าจะปล่อย
        private bool holdsPlayerRotation;
        private Quaternion targetRotation;
        // จุดศูนย์ถ่วงที่ตรึงไว้ระหว่างหมุน อาหารจึงหมุนอยู่กับที่ ไม่โดนแรงลากดึงสู้
        private Vector3 rotatePivot;

        private void OnDisable()
        {
            ReleaseIngredient();
        }

        public void OnStationEnter()
        {
            isActive = true;
        }

        public void OnStationExit()
        {
            isActive = false;
            ReleaseIngredient();
        }

        private void Update()
        {
            if (!isActive) return;

            // เปิดหน้าต่างทับสถานีอยู่ คลิกเป็นของหน้าต่าง ปล่อยของที่คีบไว้
            if (GameModeManager.Instance.CurrentMode != GameMode.Station)
            {
                ReleaseIngredient();
                return;
            }

            if (currentGrippedIngredient == null)
            {
                if (gripAction.WasPressedThisFrame()) TryGripAimed();
                return;
            }

            if (gripAction.WasReleasedThisFrame())
            {
                ReleaseIngredient();
                return;
            }

            float scroll = distanceAction.ReadValue<Vector2>().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                gripDistance = Mathf.Clamp(gripDistance + Mathf.Sign(scroll) * scrollSensitivity, minGripDistance, maxGripDistance);
            }

            UpdateRotateInput();
        }

        private void UpdateRotateInput()
        {
            bool wantRotate = allowRotation && rotateHoldAction.IsPressed();

            // เฟรมที่เพิ่งสลับโหมด เคอร์เซอร์ถูกล็อก/วาร์ป delta จึงเพี้ยน ข้ามไปหนึ่งเฟรม
            if (wantRotate != isRotating)
            {
                SetRotating(wantRotate);
                return;
            }
            if (!isRotating) return;

            Vector2 delta = rotateDeltaAction.ReadValue<Vector2>();
            if (delta.sqrMagnitude < 0.0001f) return;

            // หมุนตามจอ: ซ้าย-ขวารอบแกนตั้ง ขึ้น-ลงรอบแกนขวาของกล้อง ด้านที่หันหาผู้เล่นขยับตามเมาส์
            Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Quaternion yaw = Quaternion.AngleAxis(-delta.x * rotateSensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(delta.y * rotateSensitivity, cameraRight);
            targetRotation = yaw * pitch * targetRotation;
        }

        private void SetRotating(bool enabled)
        {
            if (isRotating == enabled) return;
            isRotating = enabled;

            if (enabled)
            {
                // เริ่มจากมุมจริง ไม่ใช่มุมเป้าเดิม เผื่ออาหารเคยชนจนมุมคลาดไป
                targetRotation = grippedRigidbody.rotation;
                holdsPlayerRotation = true;

                rotatePivot = grippedRigidbody.worldCenterOfMass;
                grippedRigidbody.useGravity = false;

                // ซ่อนเคอร์เซอร์แล้ว PlayerAiming จะเล็งค้างจุดเดิม เส้นเล็งจึงไม่ขยับตามเมาส์
                CursorManager.RequestHide(this);
            }
            else
            {
                CursorManager.ReleaseHide(this);
                if (grippedRigidbody == null) return;

                grippedRigidbody.useGravity = true;

                // จุดคีบเดิมหมุนหนีไปแล้ว ย้ายมาไว้ใต้เคอร์เซอร์ ไม่งั้นแรงลากจะกระชากกลับ
                if (PlayerAiming.Instance != null)
                {
                    Vector3 aimPoint = PlayerAiming.Instance.AimRay.GetPoint(gripDistance);
                    localGripPoint = grippedRigidbody.transform.InverseTransformPoint(aimPoint);
                }
            }
        }

        private void FixedUpdate()
        {
            Rigidbody rb = grippedRigidbody;
            if (rb == null || PlayerAiming.Instance == null) return;

            // ระหว่างกด Ctrl หมุนอย่างเดียว ปิดแรงลากทั้งหมด
            if (isRotating)
            {
                Vector3 holdVelocity = Vector3.ClampMagnitude((rotatePivot - rb.worldCenterOfMass) * dragForceMultiplier, maxDragSpeed);
                rb.AddForce(holdVelocity - rb.linearVelocity, ForceMode.VelocityChange);
                ApplyRotationDrive(rb);
                return;
            }

            // 1. ดึงจุดคีบไปยังจุดบนเส้นเล็งที่ระยะ gripDistance
            Ray ray = PlayerAiming.Instance.AimRay;
            Vector3 worldTargetPoint = ray.GetPoint(gripDistance);

            Vector3 currentWorldGripPoint = rb.transform.TransformPoint(localGripPoint);
            Vector3 displacement = worldTargetPoint - currentWorldGripPoint;
            Vector3 desiredVelocity = Vector3.ClampMagnitude(displacement * dragForceMultiplier, maxDragSpeed);

            Vector3 currentVelocityAtPoint = rb.GetPointVelocity(currentWorldGripPoint);
            Vector3 force = (desiredVelocity - currentVelocityAtPoint) * gripSpringGain;

            // 2. คุมมุม: ถ้าผู้เล่นหมุนเองแล้วให้ดันแค่ตำแหน่ง ไม่ให้แรงที่จุดคีบบิดสู้กับมุมที่ตั้งไว้
            if (holdsPlayerRotation)
            {
                rb.AddForce(force, ForceMode.Acceleration);
                ApplyRotationDrive(rb);
            }
            else
            {
                rb.AddForceAtPosition(force, currentWorldGripPoint, ForceMode.Acceleration);
                ApplyHangingTorque(rb, ray.direction);
            }
        }

        // ใช้แรงบิดไล่ตามมุมเป้า แทนการตั้ง rotation ตรงๆ เพื่อให้ยังชนกระทะได้จริง
        private void ApplyRotationDrive(Rigidbody rb)
        {
            Quaternion offset = targetRotation * Quaternion.Inverse(rb.rotation);
            offset.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Vector3 desiredAngularVelocity = float.IsFinite(axis.x) && Mathf.Abs(angle) > 0.01f
                ? Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * rotateStiffness), maxRotateSpeed)
                : Vector3.zero;

            rb.AddTorque(desiredAngularVelocity - rb.angularVelocity, ForceMode.VelocityChange);
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

            SetRotating(false);
            holdsPlayerRotation = false;
            currentGrippedIngredient.GrippedBody = null;

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
