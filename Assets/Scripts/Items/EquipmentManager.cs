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

        [Header("Place")]
        [Tooltip("ระยะวางของสูงสุด (เมตร) ต้องไม่เกินระยะเอื้อมที่ตั้งไว้ใน PlayerAiming")]
        [SerializeField] private float placeMaxDistance = 1f;

        private EquippableItem primaryItem;
        private EquippableItem secondaryItem;
        private HandRole lastEquippedRole = HandRole.Secondary;

        private EquippableItem Get(HandRole role) => role == HandRole.Primary ? primaryItem : secondaryItem;

        /// <summary>มีของอยู่ในมือข้างใดข้างหนึ่งหรือไม่</summary>
        public bool IsHoldingAnything => primaryItem != null || secondaryItem != null;

        /// <summary>หา component ชนิดที่ระบุจากของที่ถืออยู่ ไม่ว่าจะอยู่มือไหน</summary>
        public bool TryGetHeldComponent<T>(out T component) where T : class
        {
            component = primaryItem != null ? primaryItem.GetComponentInChildren<T>() : null;
            if (component == null && secondaryItem != null)
            {
                component = secondaryItem.GetComponentInChildren<T>();
            }
            return component != null;
        }

        /// <summary>แบบ static สำหรับระบบที่ทำงานได้แม้ไม่มี Manager ใน Scene</summary>
        public static bool TryGetHeld<T>(out T component) where T : class
        {
            if (Instance != null) return Instance.TryGetHeldComponent(out component);

            component = null;
            return false;
        }

        private void Start()
        {
            if (PlayerAiming.Instance == null)
            {
                Debug.LogError("[EquipmentManager] ไม่พบ PlayerAiming ใน Scene การหยิบและวางของจะไม่ทำงาน", this);
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
            Collider target = PlayerAiming.Instance != null ? PlayerAiming.Instance.Target : null;
            if (target == null) return false;

            EquippableItem equippable = target.GetComponentInParent<EquippableItem>();
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
            PlayerAiming aiming = PlayerAiming.Instance;
            if (aiming == null)
            {
                position = transform.position;
                rotation = transform.rotation;
                return;
            }

            Ray ray = aiming.AimRay;

            // คงมุมหันตามที่ผู้เล่นหันอยู่ ไม่ใช่ปล่อยให้ FromToRotation สุ่มมุมมาให้
            Vector3 facing = ray.direction;
            facing.y = 0f;
            rotation = facing.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                : Quaternion.identity;

            // ระยะวางสั้นกว่าระยะเอื้อม จึงต้องกรองเป้าที่ไกลเกินออกเอง
            if (aiming.Target != null && aiming.Hit.distance <= placeMaxDistance)
            {
                position = aiming.Hit.point;
                return;
            }

            // ไม่เจอพื้นผิว: ปล่อยไว้ข้างหน้าใกล้ ๆ แล้วให้ฟิสิกส์จัดการต่อ
            position = ray.GetPoint(placeMaxDistance * 0.6f);
        }

        private static bool WasPressed(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled && action.WasPressedThisFrame();
        }
    }
}
