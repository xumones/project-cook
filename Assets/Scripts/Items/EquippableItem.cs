using System;
using UnityEngine;

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

        /// <summary>ยกได้ โดยแช่ของข้างในให้นิ่งติดภาชนะระหว่างถือ (จาน)</summary>
        Freeze,

        /// <summary>ยกได้ โดยปล่อยของข้างในเดินฟิสิกส์ต่อ (ยังไม่มีของชิ้นไหนใช้)</summary>
        Simulate
    }

    /// <summary>ทำให้ไอเทมถือเข้ามือได้ — เป็นทั้ง "สิทธิ์" และ "กลไก" ในตัวเดียวกัน</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Item))]
    public class EquippableItem : MonoBehaviour
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

        /// <summary>Layer ที่ไอเทมย้ายไปอยู่ระหว่างถือ</summary>
        public const string HeldLayerName = "HeldItem";

        private Item item;
        private Rigidbody cachedRigidbody;
        private Collider[] cachedColliders;
        private Transform[] cachedTransforms;
        private int[] savedLayers;
        private IItemContents contents;

        private bool savedIsKinematic;
        private bool savedUseGravity;
        private bool layersApplied;

        /// <summary>มือที่ไอเทมชิ้นนี้จะไปอยู่</summary>
        public HandRole HandRole => overrideHandRole ? handRoleOverride : item.PreferredHandRole;

        private void Awake()
        {
            item = GetComponent<Item>();
            cachedRigidbody = GetComponent<Rigidbody>();
            cachedColliders = GetComponentsInChildren<Collider>(true);
            cachedTransforms = GetComponentsInChildren<Transform>(true);
            savedLayers = new int[cachedTransforms.Length];
            contents = GetComponentInChildren<IItemContents>(true);
        }

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
            if (cachedRigidbody != null)
            {
                savedIsKinematic = cachedRigidbody.isKinematic;
                savedUseGravity = cachedRigidbody.useGravity;
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.useGravity = false;
            }

            ApplyHeldLayer();

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

            RestoreLayers();

            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = savedIsKinematic;
                cachedRigidbody.useGravity = savedUseGravity;
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            item.SetLocation(ItemLocation.World);
        }

        /// <summary>เปิด/ปิดการชนกันระหว่างไอเทมชิ้นนี้กับตัวผู้เล่น</summary>
        public void SetIgnoreCollisionWith(Collider[] others, bool ignore)
        {
            if (others == null || cachedColliders == null) return;

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                Collider mine = cachedColliders[i];
                if (mine == null) continue;

                for (int j = 0; j < others.Length; j++)
                {
                    if (others[j] == null) continue;
                    Physics.IgnoreCollision(mine, others[j], ignore);
                }
            }
        }

        private void ApplyHeldLayer()
        {
            int heldLayer = LayerMask.NameToLayer(HeldLayerName);
            if (heldLayer < 0)
            {
                Debug.LogWarning($"[EquippableItem] ไม่พบ Layer '{HeldLayerName}' ไอเทมจะคง Layer เดิมไว้ระหว่างถือ", this);
                return;
            }

            for (int i = 0; i < cachedTransforms.Length; i++)
            {
                if (cachedTransforms[i] == null) continue;

                savedLayers[i] = cachedTransforms[i].gameObject.layer;
                cachedTransforms[i].gameObject.layer = heldLayer;
            }

            layersApplied = true;
        }

        private void RestoreLayers()
        {
            if (!layersApplied) return;

            for (int i = 0; i < cachedTransforms.Length; i++)
            {
                if (cachedTransforms[i] == null) continue;

                cachedTransforms[i].gameObject.layer = savedLayers[i];
            }

            layersApplied = false;
        }
    }
}
