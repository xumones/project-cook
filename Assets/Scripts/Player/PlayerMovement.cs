using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float sprintSpeed = 3f;
        [SerializeField] private float crouchSpeed = 1.2f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Crouch Settings")]
        [SerializeField] private float crouchHeight = 1.0f;
        [Tooltip("ความเร็วการย่อ/ลุก (สูง = เร็ว)")]
        [SerializeField] private float crouchTransitionSpeed = 10f;
        [Tooltip("ชั้นที่ถือว่าเป็นเพดานตอนเช็คก่อนลุก")]
        [SerializeField] private LayerMask ceilingMask = ~0;

        [Header("References")]
        [SerializeField] private Transform orientation;
        [Tooltip("Transform แม่ของ Head และ Hand Anchor ที่จะเลื่อนลงตอนย่อ")]
        [SerializeField] private Transform mainBody;

        [Header("Input Settings")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference crouchAction;

        private CharacterController controller;
        private float standHeight;
        private Vector3 mainBodyDefaultPos;
        private bool isCrouching;
        private readonly Collider[] ceilingHits = new Collider[8];

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance คืน null ไปแล้ว แต่ยังต้องถอน event ให้ครบ
        private GameModeManager gameMode;
        private Vector3 velocity;
        private bool isControlActive = true;

        /// <summary>กำลังกดวิ่งอยู่ ให้ระบบอื่น (เช่น HeadBob) อ่านแทนเดาจากความเร็ว</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>เปิด/ปิด การควบคุมการเคลื่อนที่ของผู้เล่น</summary>
        public void SetControlActive(bool active)
        {
            isControlActive = active;
            if (!active)
            {
                velocity = Vector3.zero;
                isCrouching = false;
                IsSprinting = false;

                // CharacterController.velocity สะท้อนการขยับจากการเรียก Move() ครั้งล่าสุดเท่านั้น
                // ถ้าหยุดเรียก Move() กลางคันตอนผู้เล่นกำลังเดินอยู่ ค่านี้จะค้างไม่เป็นศูนย์ไปเรื่อยๆ
                // (ทำให้ระบบอื่นที่อ่านค่านี้ เช่น HeadBob เข้าใจผิดว่ายังเดินอยู่) เลยต้อง Move(zero) เพื่อรีเซ็ตค่าทันที
                if (controller != null)
                {
                    controller.Move(Vector3.zero);
                }
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standHeight = controller.height;
            if (mainBody != null)
            {
                mainBodyDefaultPos = mainBody.localPosition;
            }
        }

        private void Start()
        {
            // สร้าง CursorManager และบังคับสถานะเริ่มต้น (ล็อกเคอร์เซอร์กลางจอสำหรับมุมมอง FPS)
            CursorManager.EnsureInitialized();

            gameMode = GameModeManager.Instance;
            if (gameMode != null)
            {
                gameMode.ModeChanged += HandleModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (gameMode != null)
            {
                gameMode.ModeChanged -= HandleModeChanged;
            }
        }

        private void HandleModeChanged(GameMode mode)
        {
            SetControlActive(mode == GameMode.Exploring);
        }

        private void OnEnable()
        {
            moveAction.EnableAction();
            sprintAction.EnableAction();
            crouchAction.EnableAction();
        }

        private void OnDisable()
        {
            moveAction.DisableAction();
            sprintAction.DisableAction();
            crouchAction.DisableAction();
        }

        private void Update()
        {
            // ปล่อยให้ลุกจนสุดได้แม้ตอนถูกปิดการควบคุม (เช่นเข้าบทสนทนาตอนย่ออยู่)
            UpdateStance();

            if (!isControlActive) return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            IsSprinting = sprintAction.IsPressed();

            // ย่อชนะวิ่งเสมอ
            bool wantCrouch = crouchAction.IsPressed();
            if (wantCrouch || CanStandUp())
            {
                isCrouching = wantCrouch;
            }
            float currentSpeed = isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

            // คำนวณทิศทางการเคลื่อนที่บนระนาบ XZ ตามทิศของ orientation หรือ transform (เพื่อไม่ให้การก้ม-เงยกล้องกระทบความเร็วเคลื่อนที่)
            Transform refTransform = orientation != null ? orientation : transform;
            Vector3 forward = Vector3.ProjectOnPlane(refTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(refTransform.right, Vector3.up).normalized;
            Vector3 moveDir = (forward * input.y) + (right * input.x);
            // แรงโน้มถ่วง
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // แรงกดพื้นเล็กน้อยเพื่อความเสถียร
            }
            velocity.y += gravity * Time.deltaTime;

            // รวมการเคลื่อนที่แกน XZ และแกน Y เข้าด้วยกัน แล้วสั่ง Move เพียงครั้งเดียวในแต่ละเฟรม
            Vector3 horizontalMove = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized * currentSpeed : Vector3.zero;
            Vector3 finalVelocity = horizontalMove + new Vector3(0, velocity.y, 0);

            controller.Move(finalVelocity * Time.deltaTime);
        }

        /// <summary>ค่อยๆ ปรับความสูง CharacterController และเลื่อน mainBody ลงตามสัดส่วนที่ย่อ</summary>
        private void UpdateStance()
        {
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            if (controller.height == targetHeight) return;

            float t = 1f - Mathf.Exp(-crouchTransitionSpeed * Time.deltaTime);
            float height = Mathf.Lerp(controller.height, targetHeight, t);
            if (Mathf.Abs(height - targetHeight) < 0.001f) height = targetHeight;

            // center = height/2 เพื่อให้เท้าอยู่ที่เดิม
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);

            if (mainBody != null)
            {
                mainBody.localPosition = mainBodyDefaultPos + Vector3.down * (standHeight - height);
            }
        }

        /// <summary>เช็คว่ามีอะไรขวางเหนือหัวในช่วงที่จะยืดขึ้นไปหรือไม่</summary>
        private bool CanStandUp()
        {
            float radius = controller.radius;
            Vector3 bottom = transform.position + Vector3.up * (controller.height - radius);
            Vector3 top = transform.position + Vector3.up * (standHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius - 0.01f, ceilingHits, ceilingMask, QueryTriggerInteraction.Ignore);

            // ผู้เล่นอยู่ layer เดียวกับฉาก จึงกรอง collider ตัวเองออกแทนการใช้ mask
            for (int i = 0; i < count; i++)
            {
                if (ceilingHits[i] != controller) return false;
            }
            return true;
        }
    }
}
