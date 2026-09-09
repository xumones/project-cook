using UnityEngine;
using UnityEngine.InputSystem;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>ด้านถนัดของผู้เล่น ตั้งค่าที่ EquipmentManager เพียงจุดเดียวในเกม</summary>
    public enum Handedness
    {
        RightHanded,
        LeftHanded
    }

    /// <summary>จัดการมือทั้งสองของผู้เล่น: ยกไอเทมเข้ามือ และวางกลับลงฉาก</summary>
    public class EquipmentManager : SceneSingleton<EquipmentManager>
    {
        [Header("Hand Anchors")]
        [Tooltip("Transform ที่ไอเทมจะ parent เข้าตอนถืออยู่มือซ้าย")]
        [SerializeField] private Transform leftHandAnchor;

        [Tooltip("Transform ที่ไอเทมจะ parent เข้าตอนถืออยู่มือขวา")]
        [SerializeField] private Transform rightHandAnchor;

        [Tooltip("ด้านถนัดของผู้เล่น เปลี่ยนค่านี้ค่าเดียวก็สลับมือทั้งเกม")]
        [SerializeField] private Handedness handedness = Handedness.RightHanded;

        [Header("Input")]
        [Tooltip("Action สำหรับยกไอเทมขึ้นถือ (ตอนนี้ผูกกับปุ่มเดียวกับ Interact ไปก่อน)")]
        [SerializeField] private InputActionReference pickUpAction;

        [Tooltip("Action สำหรับวางไอเทมที่ถืออยู่ลง")]
        [SerializeField] private InputActionReference placeAction;

        [Header("Aiming")]
        [Tooltip("จุดเริ่ม Raycast ทั้งการยกและการวาง เว้นว่าง = กล้องหลัก")]
        [SerializeField] private Transform aimOrigin;

        [Tooltip("ระยะเอื้อมหยิบไอเทมสูงสุด (เมตร)")]
        [SerializeField] private float pickUpMaxDistance = 2.5f;

        [Tooltip("Layer ที่นับว่าเป็นไอเทมหยิบได้")]
        [SerializeField] private LayerMask pickUpMask = ~0;

        [Header("Place")]
        [Tooltip("ระยะ Raycast หาที่วางสูงสุด (เมตร)")]
        [SerializeField] private float placeMaxDistance = 2f;

        [Tooltip("Layer ที่นับว่าเป็นพื้นผิววางของได้")]
        [SerializeField] private LayerMask placeMask = ~0;

        [Header("Player Body")]
        [Tooltip("Collider ของตัวผู้เล่น ใช้ปิดการชนกับของที่ถืออยู่ เว้นว่าง = ค้นหาจาก Root ของ GameObject นี้")]
        [SerializeField] private Collider[] playerColliders;

        private EquippableItem primaryItem;
        private EquippableItem secondaryItem;
        private HandRole lastEquippedRole = HandRole.Secondary;
        private Camera cachedCamera;

        // Layer ของที่ถืออยู่ ต้องตัดออกจาก Raycast ทั้งสองเส้นเสมอ ไม่งั้นการวางจะยิงโดน
        // ของที่ถืออยู่ในมือตัวเองก่อนถึงโต๊ะ (มันอยู่ห่างจากกล้องแค่ครึ่งเมตร)
        private int heldLayerExclusion;

        private EquippableItem Get(HandRole role) => role == HandRole.Primary ? primaryItem : secondaryItem;

        protected override void Awake()
        {
            base.Awake();

            if (playerColliders == null || playerColliders.Length == 0)
            {
                playerColliders = transform.root.GetComponentsInChildren<Collider>(true);
            }

            int heldLayer = LayerMask.NameToLayer(EquippableItem.HeldLayerName);
            heldLayerExclusion = heldLayer >= 0 ? ~(1 << heldLayer) : ~0;

            // Layer ของในมือถูกตัดออกจาก Raycast เสมอ ถ้า Mask ที่ตั้งไว้มีแต่ Layer นั้น
            // ผลลัพธ์จะกลายเป็นศูนย์และไม่เล็งโดนอะไรเลย ซึ่งเงียบมากถ้าไม่บอก
            if ((pickUpMask.value & heldLayerExclusion) == 0)
            {
                Debug.LogError($"[EquipmentManager] Pick Up Mask ไม่เหลือ Layer ให้เล็งเลย " +
                               $"(ตั้งไว้เป็น '{EquippableItem.HeldLayerName}' ซึ่งเป็น Layer ชั่วคราวตอนถืออยู่ ไม่ใช่ Layer ที่ของวางอยู่ในฉาก) " +
                               $"ให้ตั้งเป็น Layer ของไอเทมที่วางอยู่ในโลกแทน", this);
            }
        }

        private void OnEnable()
        {
            pickUpAction?.action?.Enable();
            placeAction?.action?.Enable();
        }

        private void OnDisable()
        {
            pickUpAction?.action?.Disable();
            placeAction?.action?.Disable();
        }

        private void Update()
        {
            if (!GameModeManager.GameplayInputAllowed) return;

            if (WasPressed(pickUpAction))
            {
                TryPickUpAimed();
            }

            if (WasPressed(placeAction))
            {
                TryPlaceHeld();
            }
        }

        /// <summary>ยกไอเทมที่กำลังเล็งอยู่ขึ้นถือ</summary>
        public bool TryPickUpAimed()
        {
            Transform origin = GetAimOrigin();
            if (origin == null) return false;

            Ray ray = new Ray(origin.position, origin.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, pickUpMaxDistance, pickUpMask & heldLayerExclusion, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            EquippableItem equippable = hit.collider.GetComponentInParent<EquippableItem>();
            if (equippable == null) return false;

            return TryEquip(equippable);
        }

        /// <summary>สวมไอเทมเข้ามือตามบทบาทของมัน</summary>
        public bool TryEquip(EquippableItem equippable)
        {
            if (equippable == null) return false;

            if (!equippable.CanBePickedUp(out string reason))
            {
                Debug.Log($"[EquipmentManager] ยก {equippable.name} ไม่ได้: {reason}");
                return false;
            }

            HandRole role = equippable.HandRole;
            if (Get(role) != null)
            {
                Debug.Log($"[EquipmentManager] มือ {role} ไม่ว่าง ยก {equippable.name} ไม่ได้");
                return false;
            }

            Transform anchor = AnchorOf(role);
            if (anchor == null)
            {
                Debug.LogWarning($"[EquipmentManager] ยังไม่ได้ตั้ง Hand Anchor ของมือ {role}", this);
                return false;
            }

            SetSlot(role, equippable);
            lastEquippedRole = role;

            equippable.SetIgnoreCollisionWith(playerColliders, true);
            equippable.NotifyEquipped(anchor);
            return true;
        }

        /// <summary>วางไอเทมที่ถืออยู่ลง โดยเลือกมือที่หยิบล่าสุดก่อน ถ้ามือนั้นว่างแล้วจึงใช้อีกมือ</summary>
        public bool TryPlaceHeld()
        {
            if (Get(lastEquippedRole) != null)
            {
                return TryPlace(lastEquippedRole);
            }

            HandRole other = lastEquippedRole == HandRole.Primary ? HandRole.Secondary : HandRole.Primary;
            return Get(other) != null && TryPlace(other);
        }

        /// <summary>วางไอเทมจากมือที่ระบุลงตรงจุดที่เล็ง</summary>
        public bool TryPlace(HandRole role)
        {
            EquippableItem equippable = Get(role);
            if (equippable == null) return false;

            GetPlaceTarget(out Vector3 position, out Quaternion rotation);

            SetSlot(role, null);
            equippable.NotifyPlaced(position, rotation);
            equippable.SetIgnoreCollisionWith(playerColliders, false);
            return true;
        }

        private void SetSlot(HandRole role, EquippableItem equippable)
        {
            if (role == HandRole.Primary)
            {
                primaryItem = equippable;
            }
            else
            {
                secondaryItem = equippable;
            }
        }

        /// <summary>แปลงบทบาทมือเป็น Hand Anchor กายภาพตามด้านถนัดที่ตั้งไว้</summary>
        private Transform AnchorOf(HandRole role)
        {
            bool useRight = (handedness == Handedness.RightHanded) == (role == HandRole.Primary);
            return useRight ? rightHandAnchor : leftHandAnchor;
        }

        /// <summary>หาจุดและมุมสำหรับวางของ โดยคงมุมหันตามที่ผู้เล่นหันอยู่เสมอ</summary>
        private void GetPlaceTarget(out Vector3 position, out Quaternion rotation)
        {
            Transform origin = GetAimOrigin();
            if (origin == null)
            {
                position = transform.position;
                rotation = transform.rotation;
                return;
            }

            // คงมุมหันตามที่ผู้เล่นหันอยู่ ไม่ใช่ปล่อยให้ FromToRotation สุ่มมุมมาให้
            Vector3 facing = origin.forward;
            facing.y = 0f;
            rotation = facing.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                : Quaternion.identity;

            Ray ray = new Ray(origin.position, origin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, placeMaxDistance, placeMask & heldLayerExclusion, QueryTriggerInteraction.Ignore))
            {
                position = hit.point;
                return;
            }

            // ไม่เจอพื้นผิว: ปล่อยไว้ข้างหน้าใกล้ ๆ แล้วให้ฟิสิกส์จัดการต่อ
            position = ray.GetPoint(placeMaxDistance * 0.6f);
        }

        private Transform GetAimOrigin()
        {
            if (aimOrigin != null) return aimOrigin;

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }
            return cachedCamera != null ? cachedCamera.transform : null;
        }

        private static bool WasPressed(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.WasPressedThisFrame();
        }
    }
}
