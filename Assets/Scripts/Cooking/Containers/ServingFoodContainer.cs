using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Cooking
{
    /// <summary>ภาชนะเสิร์ฟที่ไม่ให้ความร้อน ยึดของข้างในให้ติดไปด้วยตอนถูกยกขึ้นถือ (จาน ชาม ถาด)</summary>
    // ต้องรันหลัง HeadBob ที่ขยับหัวใน LateUpdate เพื่อให้เราเป็นฝ่ายเขียนตำแหน่งทีหลังสุดก่อนเรนเดอร์
    [DefaultExecutionOrder(100)]
    public class ServingFoodContainer : FoodContainer
    {
        [Header("Content Placement")]
        [Tooltip("จำนวนวัตถุดิบสูงสุดที่รับได้ นับรวมของที่ตกลงมาเองด้วย")]
        [SerializeField] private int capacity = 3;

        [Tooltip("จุดศูนย์กลางผิวที่วางของ เว้นว่าง = เหนือจุดศูนย์กลางภาชนะเล็กน้อย")]
        [SerializeField] private Transform contentAnchor;

        [Tooltip("รัศมีรอบจุดศูนย์กลางที่สุ่มวางของได้ (เมตร)")]
        [SerializeField] private float placementRadius = 0.10f;

        [Tooltip("ระยะห่างขั้นต่ำระหว่างจุดศูนย์กลางของแต่ละชิ้น (เมตร)")]
        [SerializeField] private float minSpacing = 0.07f;

        private const int PlacementAttempts = 10;

        private class FrozenState
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public bool SavedIsKinematic;
            public bool SavedUseGravity;
            public LayerSnapshot Layers;
        }

        private readonly Dictionary<Rigidbody, FrozenState> frozenStates = new Dictionary<Rigidbody, FrozenState>();
        private Collider triggerCollider;
        private bool isFrozen;

        /// <summary>รับของเพิ่มไม่ได้แล้ว</summary>
        public bool IsFull => foodItems.Count >= capacity;

        protected override void Awake()
        {
            base.Awake();

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].isTrigger)
                {
                    triggerCollider = colliders[i];
                    break;
                }
            }

            if (triggerCollider == null)
            {
                Debug.LogError($"[ServingFoodContainer] {name} ไม่มี Trigger Collider จะตรวจจับวัตถุดิบไม่ได้เลย", this);
            }
        }

        /// <summary>รับวัตถุดิบที่ระบบอื่นยื่นให้โดยตรง วางลงผิวทันทีไม่ต้องรอ trigger คืน false ถ้าเต็มหรือไม่ใช่วัตถุดิบ</summary>
        public bool TryReceive(GameObject item)
        {
            if (item == null || IsFull) return false;

            Rigidbody body = item.GetComponentInChildren<Rigidbody>();
            IngredientInstance ingredient = item.GetComponentInChildren<IngredientInstance>();
            if (body == null || ingredient == null) return false;

            if (!AddFoodItem(body, ingredient)) return false;

            PlaceOnSurface(body);
            if (isFrozen)
            {
                Freeze(body);
            }
            return true;
        }

        /// <summary>เรียกจาก EquippableItem ตอนภาชนะถูกยกขึ้นถือและตอนวางลง</summary>
        public override void SetFrozen(bool frozen)
        {
            if (isFrozen == frozen) return;
            isFrozen = frozen;

            if (frozen)
            {
                for (int i = 0; i < foodItems.Count; i++)
                {
                    Freeze(foodItems[i]?.Rigidbody);
                }
                return;
            }

            foreach (KeyValuePair<Rigidbody, FrozenState> entry in frozenStates)
            {
                Release(entry.Key, entry.Value);
            }
            frozenStates.Clear();

            DropItemsOutsideTrigger();
        }

        protected override void ProcessItem(FoodItemData item)
        {
            // ของที่ตกเข้ามาเองระหว่างถืออยู่ (ตักจากกระทะ) ต้องถูกจัดวางแล้วยึดทันที
            // ไม่งั้นมันจะเป็น dynamic กระเด้งอยู่บนภาชนะที่ขยับตามมือแล้วร่วง
            if (!isFrozen || frozenStates.ContainsKey(item.Rigidbody)) return;

            PlaceOnSurface(item.Rigidbody);
            Freeze(item.Rigidbody);
        }

        // เขียนตำแหน่งเองแทนการ parent เพราะ Rigidbody ซ้อนใน Rigidbody จะโดนฟิสิกส์เขียนทับกลับทุกสเต็ป
        private void LateUpdate()
        {
            if (!isFrozen) return;

            foreach (KeyValuePair<Rigidbody, FrozenState> entry in frozenStates)
            {
                if (entry.Key == null) continue;

                ApplyFrozenPose(entry.Key, entry.Value);
            }
        }

        private void ApplyFrozenPose(Rigidbody body, FrozenState state)
        {
            SetPose(body, transform.TransformPoint(state.LocalPosition), transform.rotation * state.LocalRotation);
        }

        // เขียน transform ให้ภาพอัปเดตทันเฟรมนี้ และเขียน Rigidbody ให้ pose ฝั่งฟิสิกส์ตรงกันด้วย
        // ถ้าเขียนแค่ transform ค่าจะยังไม่ถูก sync เข้า PhysX จนถึงสเต็ปถัดไป
        private static void SetPose(Rigidbody body, Vector3 position, Quaternion rotation)
        {
            body.transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
        }

        /// <summary>วางของลงบนผิวภาชนะที่จุดสุ่มซึ่งไม่ทับของเดิม หมุนสุ่ม และยกให้ก้นแตะผิวพอดี</summary>
        private void PlaceOnSurface(Rigidbody body)
        {
            Vector3 up = transform.up;
            Vector3 surfacePoint = transform.TransformPoint(PickFreeLocalSpot(body));
            Quaternion rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * Quaternion.FromToRotation(Vector3.up, up);

            SetPose(body, surfacePoint, rotation);

            // แต่ละชิ้นหนาไม่เท่ากัน ต้องวัดจากของจริงว่าก้นอยู่ต่ำกว่า pivot เท่าไหร่
            Renderer renderer = body.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                float belowPivot = Vector3.Dot(body.position - renderer.bounds.min, up);
                SetPose(body, surfacePoint + up * belowPivot, rotation);
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private Vector3 SurfaceCenterLocal =>
            contentAnchor != null ? transform.InverseTransformPoint(contentAnchor.position) : Vector3.up * 0.05f;

        // สุ่มจุดในวงกลมแล้วเช็คระยะจากของทุกชิ้นที่มีอยู่ ไม่ว่าจะเข้ามาทางไหน ถ้าหาจุดว่างไม่ได้ก็เอาจุดที่ห่างที่สุด
        private Vector3 PickFreeLocalSpot(Rigidbody placing)
        {
            Vector3 center = SurfaceCenterLocal;
            Vector3 best = center;
            float bestDistance = -1f;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * placementRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

                float nearest = NearestItemDistance(candidate, placing);
                if (nearest >= minSpacing) return candidate;

                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = candidate;
                }
            }
            return best;
        }

        private float NearestItemDistance(Vector3 localPoint, Rigidbody ignored)
        {
            float nearest = float.MaxValue;

            for (int i = 0; i < foodItems.Count; i++)
            {
                Rigidbody body = foodItems[i]?.Rigidbody;
                if (body == null || body == ignored) continue;

                Vector3 local = transform.InverseTransformPoint(body.position);
                local.y = localPoint.y;
                nearest = Mathf.Min(nearest, Vector3.Distance(local, localPoint));
            }
            return nearest;
        }

        private void Freeze(Rigidbody body)
        {
            if (body == null || frozenStates.ContainsKey(body)) return;

            Transform bodyTransform = body.transform;

            // Layer HeldItem ตัดออกจากการเล็งและไม่ชนกับผู้เล่นผ่าน Collision Matrix ไม่ต้องปิดการชนเอง
            frozenStates[body] = new FrozenState
            {
                LocalPosition = transform.InverseTransformPoint(bodyTransform.position),
                LocalRotation = Quaternion.Inverse(transform.rotation) * bodyTransform.rotation,
                SavedIsKinematic = body.isKinematic,
                SavedUseGravity = body.useGravity,
                Layers = LayerSnapshot.Apply(bodyTransform, GameLayers.HeldItem)
            };

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void Release(Rigidbody body, FrozenState state)
        {
            if (body == null) return;

            // ย้ายไปตำแหน่งสุดท้ายบนภาชนะก่อนคืนฟิสิกส์ เพราะ LateUpdate จะไม่ทำงานอีกแล้วหลังเลิกยึด
            ApplyFrozenPose(body, state);
            state.Layers.Restore();

            body.isKinematic = state.SavedIsKinematic;
            body.useGravity = state.SavedUseGravity;
        }

        // ตอนยึด การสลับ isKinematic ทำให้ Exit ถูกยิงออกมาแล้วเราเมินไป ถ้าของหลุดออกนอกภาชนะจริง
        // จะไม่มี Exit รอบสองมาแก้บัญชีให้อีก จึงต้องมาตรวจเองตอนปล่อย ไม่งั้นรายการจะเพี้ยนค้างยาว
        private void DropItemsOutsideTrigger()
        {
            if (triggerCollider == null) return;

            for (int i = foodItems.Count - 1; i >= 0; i--)
            {
                Rigidbody body = foodItems[i]?.Rigidbody;
                if (body == null) continue;

                Vector3 point = body.worldCenterOfMass;
                if ((triggerCollider.ClosestPoint(point) - point).sqrMagnitude > 1e-6f)
                {
                    RemoveFoodItemAt(i);
                }
            }
        }

        public override void OnTriggerExit(Collider other)
        {
            // การสลับ isKinematic ทั้งของภาชนะและของอาหาร ทำให้ PhysX สร้าง actor ใหม่แล้วยิง Exit หลอก
            // ถ้าปล่อยผ่าน ของที่ยึดอยู่จะถูกถอดออกจากรายการแล้วร่วงตอนยกจานพอดี
            Rigidbody body = other.attachedRigidbody;
            if (isFrozen && body != null && frozenStates.ContainsKey(body)) return;

            base.OnTriggerExit(other);
        }

        protected override void RemoveFoodItemAt(int index)
        {
            if (index >= 0 && index < foodItems.Count)
            {
                Rigidbody body = foodItems[index]?.Rigidbody;
                if (body != null && frozenStates.TryGetValue(body, out FrozenState state))
                {
                    Release(body, state);
                    frozenStates.Remove(body);
                }
            }

            base.RemoveFoodItemAt(index);
        }
    }
}
