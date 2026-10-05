using System;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>มือทั้งสองของผู้เล่น: ยกไอเทมเข้ามือ และปล่อยไอเทมออกจากมือไปวางที่จุดที่กำหนด</summary>
    public class EquipmentManager : SceneSingleton<EquipmentManager>, ISaveSection
    {
        [Serializable]
        private class HandsSaveData
        {
            public ItemSaveData left;
            public ItemSaveData right;
        }

        [Header("Hand Anchors")]
        [Tooltip("Transform ที่ไอเทมจะ parent เข้าตอนถืออยู่มือซ้าย")]
        [SerializeField] private Transform leftHandAnchor;

        [Tooltip("Transform ที่ไอเทมจะ parent เข้าตอนถืออยู่มือขวา")]
        [SerializeField] private Transform rightHandAnchor;

        private EquippableItem leftItem;
        private EquippableItem rightItem;

        /// <summary>ของในมือข้างใดข้างหนึ่งเปลี่ยน (หยิบหรือปล่อย)</summary>
        public event Action HandsChanged;

        public EquippableItem GetHeld(HandSide side) => side == HandSide.Left ? leftItem : rightItem;

        private void Start() => SaveService.Instance?.Register(this);

        protected override void OnDestroy()
        {
            SaveService.Instance?.Unregister(this);
            base.OnDestroy();
        }

        public string SectionKey => "hands";

        public string Capture() => JsonUtility.ToJson(new HandsSaveData { left = SaveHand(leftItem), right = SaveHand(rightItem) });

        public void Restore(string data)
        {
            var saved = JsonUtility.FromJson<HandsSaveData>(data);
            RestoreHand(saved.left, HandSide.Left);
            RestoreHand(saved.right, HandSide.Right);
        }

        private static ItemSaveData SaveHand(EquippableItem held) =>
            held != null ? ItemSaveData.From(held.GetComponent<Item>().Capture()) : null;

        private void RestoreHand(ItemSaveData saved, HandSide side)
        {
            // JsonUtility เขียนช่องว่างเป็น object เปล่า ไม่ใช่ null
            ItemInstance instance = saved != null && !string.IsNullOrEmpty(saved.itemId) ? saved.ToInstance() : null;
            if (instance == null) return;

            Item spawned = Item.Spawn(instance, AnchorOf(side).position, AnchorOf(side).rotation);
            if (spawned != null) TryEquip(spawned.GetComponent<EquippableItem>(), side);
        }

        /// <summary>มีของอยู่ในมือข้างใดข้างหนึ่งหรือไม่</summary>
        public bool IsHoldingAnything => leftItem != null || rightItem != null;

        /// <summary>หา component ชนิดที่ระบุจากของที่ถืออยู่ ไม่ว่าจะอยู่มือไหน</summary>
        public bool TryGetHeldComponent<T>(out T component) where T : class
        {
            component = leftItem != null ? leftItem.GetComponentInChildren<T>() : null;
            if (component == null && rightItem != null)
            {
                component = rightItem.GetComponentInChildren<T>();
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

        /// <summary>สวมไอเทมเข้ามือที่ว่าง โดยลองมือซ้ายก่อน</summary>
        public bool TryEquip(EquippableItem equippable) =>
            TryEquip(equippable, leftItem == null ? HandSide.Left : HandSide.Right);

        private bool TryEquip(EquippableItem equippable, HandSide side)
        {
            if (equippable == null) return false;

            if (!equippable.CanBePickedUp(out string reason))
            {
                Debug.Log($"[EquipmentManager] ยก {equippable.name} ไม่ได้: {reason}");
                return false;
            }

            if (GetHeld(side) != null)
            {
                Debug.Log($"[EquipmentManager] มือไม่ว่างทั้งสองข้าง ยก {equippable.name} ไม่ได้");
                return false;
            }

            Transform anchor = AnchorOf(side);
            if (anchor == null)
            {
                Debug.LogWarning($"[EquipmentManager] ยังไม่ได้ตั้ง Hand Anchor ของมือ {side}", this);
                return false;
            }

            SetSlot(side, equippable);
            equippable.NotifyEquipped(anchor);
            return true;
        }

        /// <summary>ปล่อยไอเทมที่ถืออยู่ไปวางที่จุดที่กำหนด คืน false ถ้าไม่ได้ถือชิ้นนั้น</summary>
        public bool Release(EquippableItem equippable, Vector3 position, Quaternion rotation)
        {
            if (equippable == null) return false;

            if (leftItem == equippable)
            {
                SetSlot(HandSide.Left, null);
            }
            else if (rightItem == equippable)
            {
                SetSlot(HandSide.Right, null);
            }
            else
            {
                return false;
            }

            equippable.NotifyPlaced(position, rotation);
            return true;
        }

        private void SetSlot(HandSide side, EquippableItem equippable)
        {
            if (side == HandSide.Left)
            {
                leftItem = equippable;
            }
            else
            {
                rightItem = equippable;
            }

            HandsChanged?.Invoke();
        }

        private Transform AnchorOf(HandSide side) => side == HandSide.Left ? leftHandAnchor : rightHandAnchor;
    }
}
