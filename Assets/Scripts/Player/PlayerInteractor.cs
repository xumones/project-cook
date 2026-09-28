using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Player
{
    /// <summary>ผู้ฟังปุ่มปฏิสัมพันธ์ตัวเดียว: เป้าที่ใช้งานได้ (IInteractable) ใช้งานก่อน ไม่งั้นส่งให้ผู้ตัดสินการหยิบ</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference interactAction; // อ้างอิง Action จาก New Input System

        [Tooltip("ผู้ตัดสินการหยิบของ (ต้อง Implement IPickUpHandler)")]
        [SerializeField] private MonoBehaviour pickUpHandler;

        private IPickUpHandler pickUp;

        private void Awake()
        {
            pickUp = pickUpHandler as IPickUpHandler;
            if (pickUpHandler != null && pickUp == null)
            {
                Debug.LogError($"[PlayerInteractor] {pickUpHandler.name} ไม่ได้ Implement IPickUpHandler หยิบของจะไม่ทำงาน", this);
            }
        }

        private void OnEnable()
        {
            interactAction.EnableAction();
        }

        private void OnDisable()
        {
            interactAction.DisableAction();
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
            if (!interactAction.WasPressedThisFrame()) return;

            // ค้นหา component เฉพาะตอนกดปุ่ม ไม่ใช่ทุกเฟรม จึงไม่ต้องแคชผลไว้เอง
            Collider target = PlayerAiming.Instance != null ? PlayerAiming.Instance.Target : null;
            if (target == null) return;

            // ใช้งานมาก่อนหยิบ ของชิ้นเดียวที่ทำได้ทั้งสองอย่างจะถูกใช้งาน
            IInteractable interactable = target.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }

            pickUp?.TryPickUp(target);
        }
    }
}
