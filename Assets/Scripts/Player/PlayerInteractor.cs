using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>สคริปต์จัดการ Raycast ตลอดเวลา (Continuous) และตรวจจับการกดปุ่มปฏิสัมพันธ์ของผู้เล่น</summary>
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
            if (interactAction == null || interactAction.action == null)
            {
                Debug.LogError("[PlayerInteractor] ยังไม่ได้ตั้ง Interact Action ปุ่มปฏิสัมพันธ์จะไม่ทำงาน", this);
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (interactableLayerMask.value == 0)
            {
                int defaultLayer = LayerMask.NameToLayer("Interactable");
                if (defaultLayer != -1)
                {
                    interactableLayerMask = 1 << defaultLayer;
                }
                else
                {
                    interactableLayerMask = Physics.DefaultRaycastLayers;
                }
            }
        }

        private void Update()
        {
            // 1. ยิง Raycast ตรวจจับวัตถุตรงหน้าทุกเฟรม
            PerformRaycastDetection();

            if (!GameModeManager.GameplayInputAllowed) return;

            if (WasInteractPressed() && currentTarget != null)
            {
                currentTarget.Interact();
            }
        }

        private void PerformRaycastDetection()
        {
            Transform rayOrigin = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : transform);
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
            InputAction action = interactAction != null ? interactAction.action : null;
            return action != null && action.enabled && action.WasPressedThisFrame();
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
