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
        [SerializeField] private float gravity = -9.81f;

        [Header("References")]
        [SerializeField] private Transform orientation;

        [Header("Input Settings")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;

        private CharacterController controller;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance คืน null ไปแล้ว แต่ยังต้องถอน event ให้ครบ
        private GameModeManager gameMode;
        private Vector3 velocity;
        private bool isControlActive = true;

        public bool IsControlActive => isControlActive;

        /// <summary>เปิด/ปิด การควบคุมการเคลื่อนที่ของผู้เล่น</summary>
        public void SetControlActive(bool active)
        {
            isControlActive = active;
            if (!active)
            {
                velocity = Vector3.zero;

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
            moveAction?.action?.Enable();
            sprintAction?.action?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action?.Disable();
            sprintAction?.action?.Disable();
        }

        private void Update()
        {
            if (!isControlActive) return;

            // 1. อ่านค่า Input การเดิน (Vector2: WASD / Left Stick)
            Vector2 input = Vector2.zero;
            if (moveAction?.action != null)
            {
                input = moveAction.action.ReadValue<Vector2>();
            }

            // 2. เช็คว่ากำลังกดปุ่มวิ่งเร็ว (Sprint: Shift / Left Stick Press) หรือไม่
            bool isSprinting = false;
            if (sprintAction?.action != null)
            {
                isSprinting = sprintAction.action.IsPressed();
            }

            // 3. เลือกความเร็วระหว่าง เดิน (walkSpeed) หรือ วิ่ง (sprintSpeed)
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            // 4. คำนวณทิศทางการเคลื่อนที่บนระนาบ XZ ตามทิศของ orientation หรือ transform (เพื่อไม่ให้การก้ม-เงยกล้องกระทบความเร็วเคลื่อนที่)
            Transform refTransform = orientation != null ? orientation : transform;
            Vector3 forward = Vector3.ProjectOnPlane(refTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(refTransform.right, Vector3.up).normalized;
            Vector3 moveDir = (forward * input.y) + (right * input.x);
            // 5. คำนวณแรงโน้มถ่วง (Gravity)
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // แรงกดพื้นเล็กน้อยเพื่อความเสถียร
            }
            velocity.y += gravity * Time.deltaTime;

            // 6. รวมการเคลื่อนที่แกน XZ และแกน Y เข้าด้วยกัน แล้วสั่ง Move เพียงครั้งเดียวในแต่ละเฟรม
            Vector3 horizontalMove = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized * currentSpeed : Vector3.zero;
            Vector3 finalVelocity = horizontalMove + new Vector3(0, velocity.y, 0);

            controller.Move(finalVelocity * Time.deltaTime);
        }
    }
}
