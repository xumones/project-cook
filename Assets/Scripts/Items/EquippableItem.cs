using System;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Items
{
    /// <summary>ท่าถือของไอเทมตอนอยู่ในมือ (ตำแหน่ง/มุม เทียบกับ Hand Anchor)</summary>
    [Serializable]
    public struct HoldPose
    {
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
    }

    /// <summary>กติกาว่า "ของที่อยู่ในภาชนะจะเป็นอย่างไรตอนภาชนะถูกยกขึ้นถือ"</summary>
    public enum ContentsPolicy
    {
        /// <summary>มีของอยู่ข้างใน = ยกไม่ได้ ต้องเทออกก่อน (กระทะ)</summary>
        BlockPickup,

        /// <summary>ยกได้ โดยยึดของข้างในให้ติดภาชนะระหว่างถือ (จาน)</summary>
        Freeze
    }

    /// <summary>ทำให้ไอเทมถือเข้ามือได้ — เป็นทั้ง "สิทธิ์" และ "กลไก" ในตัวเดียวกัน</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class EquippableItem : MonoBehaviour, IAimHintProvider
    {
        [Header("Hold")]
        [Tooltip("ท่าถือเทียบกับ Hand Anchor")]
        [SerializeField] private HoldPose holdPose;

        [Header("Hand Role")]
        [Tooltip("ตั้งมือที่จะไปอยู่เองแทนการอนุมานจากประเภทของไอเทม")]
        [SerializeField] private bool overrideHandRole = false;

        [SerializeField] private HandRole handRoleOverride = HandRole.Secondary;

        [Header("Contents")]
        [Tooltip("ของที่อยู่ในภาชนะนี้จะเป็นอย่างไรตอนถูกยกขึ้นถือ")]
        [SerializeField] private ContentsPolicy contentsPolicy = ContentsPolicy.Freeze;

        private Item item;
        private Rigidbody cachedRigidbody;
        private IItemContents contents;

        private LayerSnapshot heldLayers;
        private bool savedIsKinematic;
        private bool savedUseGravity;

        /// <summary>มือที่ไอเทมชิ้นนี้จะไปอยู่</summary>
        public HandRole HandRole => overrideHandRole ? handRoleOverride : item.PreferredHandRole;

        private void Awake()
        {
            item = GetComponent<Item>();
            cachedRigidbody = GetComponent<Rigidbody>();
            contents = GetComponentInChildren<IItemContents>(true);
        }

        public AimHint GetAimHint() => CanBePickedUp(out _) ? AimHint.PickUp : AimHint.None;

        /// <summary>ยกไอเทมชิ้นนี้ได้ตอนนี้หรือไม่ พร้อมเหตุผลไว้ log ตอนยกไม่ได้</summary>
        public bool CanBePickedUp(out string reason)
        {
            if (item.Location != ItemLocation.World)
            {
                reason = "ไม่ได้วางอยู่ในฉาก";
                return false;
            }

            if (contentsPolicy == ContentsPolicy.BlockPickup && contents != null && contents.HasContents)
            {
                reason = "มีของอยู่ข้างใน";
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>เรียกจาก EquipmentManager เท่านั้น: ปิดฟิสิกส์ + ย้าย Layer + parent เข้า anchor + จัดท่าถือ</summary>
        public void NotifyEquipped(Transform anchor)
        {
            if (contentsPolicy == ContentsPolicy.Freeze)
            {
                contents?.SetFrozen(true);
            }

            if (cachedRigidbody != null)
            {
                savedIsKinematic = cachedRigidbody.isKinematic;
                savedUseGravity = cachedRigidbody.useGravity;
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.useGravity = false;
            }

            // Layer HeldItem ถูกตัดออกจากการเล็งและไม่ชนกับผู้เล่นผ่าน Collision Matrix ไม่ต้องจัดการการชนเองอีก
            heldLayers = LayerSnapshot.Apply(transform, GameLayers.HeldItem);

            transform.SetParent(anchor, false);
            transform.localPosition = holdPose.localPosition;
            transform.localRotation = Quaternion.Euler(holdPose.localEulerAngles);

            item.SetLocation(ItemLocation.Hand);
        }

        /// <summary>เรียกจาก EquipmentManager เท่านั้น: หลุด parent ไปวางที่จุดที่กำหนด + คืนฟิสิกส์และ Layer เดิม</summary>
        public void NotifyPlaced(Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);

            heldLayers.Restore();

            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = savedIsKinematic;
                cachedRigidbody.useGravity = savedUseGravity;
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            // ปล่อยของข้างในเป็นขั้นสุดท้าย ตอนภาชนะอยู่ตำแหน่งสุดท้ายแล้ว ของจะได้ตกลงถูกที่
            if (contentsPolicy == ContentsPolicy.Freeze)
            {
                contents?.SetFrozen(false);
            }

            item.SetLocation(ItemLocation.World);
        }
    }
}
