using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectCook.Interaction
{
    /// <summary>
    /// สคริปต์จัดการ Raycast ตลอดเวลา (Continuous) และตรวจจับการกดปุ่มปฏิสัมพันธ์ของผู้เล่น
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float maxInteractionDistance = 2.5f; // ระยะเอื้อมมือของผู้เล่น (เมตร)
        
        [Tooltip("Interactable Layer")]
        [SerializeField] private LayerMask interactableLayerMask; // LayerMask กรองเฉพาะวัตถุที่กดได้

        [Header("Input Settings")]
        [SerializeField] private InputActionReference interactAction; // อ้างอิง Action จาก New Input System

        private IInteractable currentTarget; // วัตถุที่ผู้เล่นกำลังเอาเป้าเล็งอยู่นะปัจจุบัน
        private Collider lastHitCollider;
        private IInteractable cachedInteractable;

        /// <summary>
        /// Property สำหรับให้ระบบ UI หรือระบบอื่นแอบเข้ามาเช็คว่าตอนนี้ผู้เล่นกำลังมองอะไรอยู่
        /// </summary>
        public IInteractable CurrentTarget => currentTarget;

        private void OnEnable()
        {
            interactAction?.action?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.action?.Disable();
        }

        private void Start()
        {

        }

        private void Update()
        {
            // 1. ยิง Raycast ตรวจจับวัตถุตรงหน้าทุกเฟรม
            PerformRaycastDetection();

            // 2. ถ้ามีวัตถุที่มองอยู่ และผู้เล่นกดปุ่ม E ให้เรียกคำสั่ง Interact()
            if (WasInteractPressed() && currentTarget != null)
            {
                currentTarget.Interact(this);
            }
        }

        private void PerformRaycastDetection()
        {
            Transform rayOrigin = cameraTransform != null ? cameraTransform : transform;
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

            // ยิง Raycast ออกไปทุกเฟรม โดยกรองเฉพาะ Layer 'Interactable'
            if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance, interactableLayerMask))
            {
                // แคชค่าเพื่อไม่ต้องเรียก GetComponentInParent ค้นหา Hierarchy ทุกเฟรมถ้ายังเล็งโดน Collider ตัวเดิมอยู่
                if (hit.collider == lastHitCollider)
                {
                    currentTarget = cachedInteractable;
                    return;
                }

                lastHitCollider = hit.collider;
                cachedInteractable = hit.collider.GetComponentInParent<IInteractable>();
                currentTarget = cachedInteractable;
                return;
            }

            // ถ้าไม่ได้เล็งวัตถุใดๆ ให้ล้างค่าเป้าหมายเป็น null
            lastHitCollider = null;
            cachedInteractable = null;
            currentTarget = null;
        }

        private bool WasInteractPressed()
        {
            if (interactAction?.action != null && interactAction.action.enabled)
            {
                if (interactAction.action.WasPressedThisFrame()) return true;
            }
            if (Keyboard.current != null)
            {
                return Keyboard.current.eKey.wasPressedThisFrame;
            }
            return false;
        }

        // แสดงเส้น Raycast ใน Scene View (เปลี่ยนเป็นสีแดงถ้าเล็งโดนวัตถุ / สีเขียวถ้าเล็งที่ว่าง)
        private void OnDrawGizmosSelected()
        {
            Transform rayOrigin = cameraTransform != null ? cameraTransform : transform;
            Gizmos.color = currentTarget != null ? Color.red : Color.green;
            Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * maxInteractionDistance);
        }
    }
}
