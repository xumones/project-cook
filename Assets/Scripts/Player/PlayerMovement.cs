using System.Collections.Generic;
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

        // ใช้ชุดผู้ขอแทนตัวนับ กันเคสเรียกซ้ำหรือลืมคืนแล้วนับเพี้ยน
        private readonly HashSet<Object> movementLocks = new HashSet<Object>();

        /// <summary>กำลังกดวิ่งอยู่ ให้ระบบอื่น (เช่น HeadBob) อ่านแทนเดาจากความเร็ว</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>ขอหยุดเดินชั่วคราว (เช่น ระหว่างบิดลูกบิด) ต้องเรียก ReleaseMovementLock คู่กันเสมอ</summary>
        public void RequestMovementLock(Object requester)
        {
            if (requester != null && movementLocks.Add(requester)) StopMotion();
        }

        public void ReleaseMovementLock(Object requester)
        {
            if (requester != null) movementLocks.Remove(requester);
        }

        private void StopMotion()
        {
            velocity = Vector3.zero;
            IsSprinting = false;

            // CharacterController.velocity ค้างค่าจาก Move() ครั้งล่าสุด ต้อง Move(zero) ไม่งั้น HeadBob เข้าใจว่ายังเดินอยู่
            if (controller != null)
            {
                controller.Move(Vector3.zero);
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

        // เดินได้เฉพาะตอนเดินสำรวจ เข้าโหมดอื่นแล้วหยุดทันทีและลุกขึ้นยืน
        private void HandleModeChanged(GameMode mode)
        {
            isControlActive = mode == GameMode.Exploring;
            if (isControlActive) return;

            isCrouching = false;
            StopMotion();
        }

        private void Update()
        {
            // ปล่อยให้ลุกจนสุดได้แม้ตอนถูกปิดการควบคุม (เช่นเข้าบทสนทนาตอนย่ออยู่)
            UpdateStance();

            if (!isControlActive || movementLocks.Count > 0) return;

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
            // เพดาน = สิ่งที่ชนกับตัวผู้เล่นจริงตาม Collision Matrix ของหลวมเหนือหัวจึงไม่กันการลุก
            int ceilingMask = GameLayers.CollisionMaskOf(gameObject.layer);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius - 0.01f, ceilingHits, ceilingMask, QueryTriggerInteraction.Ignore);

            // layer ผู้เล่นชนกับตัวเองด้วย จึงกรอง collider ตัวเองออก
            for (int i = 0; i < count; i++)
            {
                if (ceilingHits[i] != controller) return false;
            }
            return true;
        }
    }
}
