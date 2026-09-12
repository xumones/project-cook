using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>กดปุ่มปฏิสัมพันธ์แล้วสั่ง IInteractable ที่ PlayerAiming เล็งอยู่</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference interactAction; // อ้างอิง Action จาก New Input System

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

            if (PlayerAiming.Instance == null)
            {
                Debug.LogError("[PlayerInteractor] ไม่พบ PlayerAiming ใน Scene ปุ่มปฏิสัมพันธ์จะไม่ทำงาน", this);
            }
        }

        private void Update()
        {
            if (!GameModeManager.GameplayInputAllowed) return;
            if (!WasInteractPressed()) return;

            // ค้นหา component เฉพาะตอนกดปุ่ม ไม่ใช่ทุกเฟรม จึงไม่ต้องแคชผลไว้เอง
            Collider target = PlayerAiming.Instance != null ? PlayerAiming.Instance.Target : null;
            if (target == null) return;

            target.GetComponentInParent<IInteractable>()?.Interact();
        }

        private bool WasInteractPressed()
        {
            InputAction action = interactAction != null ? interactAction.action : null;
            return action != null && action.enabled && action.WasPressedThisFrame();
        }
    }
}
