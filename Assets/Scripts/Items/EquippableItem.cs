using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [Header("Contents")]
        [Tooltip("ของที่อยู่ในภาชนะนี้จะเป็นอย่างไรตอนถูกยกขึ้นถือ")]
        [SerializeField] private ContentsPolicy contentsPolicy = ContentsPolicy.Freeze;

        private Item item;
        private Rigidbody cachedRigidbody;
        private IItemContents contents;

        private HeldState heldState;

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

            heldState = HeldState.Capture(transform, cachedRigidbody);

            transform.SetParent(anchor, false);
            transform.localPosition = holdPose.localPosition;
            transform.localRotation = Quaternion.Euler(holdPose.localEulerAngles);

            // มือผู้เล่นอยู่ใน Core ของที่ถือจึงย้ายมาด้วย ของข้างในต้องตามมาไม่งั้นหายตอนเปลี่ยนพื้นที่
            contents?.MoveToScene(gameObject.scene);

            item.SetLocation(ItemLocation.Hand);
        }

        /// <summary>เรียกจาก EquipmentManager เท่านั้น: หลุด parent ไปวางที่จุดที่กำหนด + คืนฟิสิกส์และ Layer เดิม</summary>
        public void NotifyPlaced(Vector3 worldPosition, Quaternion worldRotation)
        {
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(worldPosition, worldRotation);

            // หลุดจากมือแล้วยังค้างอยู่ใน Core ต้องย้ายลงพื้นที่ปัจจุบัน ไม่งั้นจะตามผู้เล่นไปทุกพื้นที่
            item.MoveToScene(SceneManager.GetActiveScene());

            heldState.Restore();

            // ปล่อยของข้างในเป็นขั้นสุดท้าย ตอนภาชนะอยู่ตำแหน่งสุดท้ายแล้ว ของจะได้ตกลงถูกที่
            if (contentsPolicy == ContentsPolicy.Freeze)
            {
                contents?.SetFrozen(false);
            }

            item.SetLocation(ItemLocation.World);
        }
    }
}
