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
            public HeldState Held;
        }

        // ยึดเป็นรายชิ้น เพราะแต่ละชิ้นมี pose ของตัวเอง แต่จะยึด/ปล่อยพร้อมกันทั้งวัตถุดิบเสมอ
        private readonly Dictionary<Rigidbody, FrozenState> frozenStates = new Dictionary<Rigidbody, FrozenState>();
        private readonly List<Rigidbody> placementBuffer = new List<Rigidbody>();
        private bool isFrozen;

        /// <summary>รับของเพิ่มไม่ได้แล้ว</summary>
        public bool IsFull => contents.Count >= capacity;

        /// <summary>รับวัตถุดิบที่ระบบอื่นยื่นให้โดยตรง วางลงผิวทันทีไม่ต้องรอ trigger คืน false ถ้าเต็มหรือไม่ใช่วัตถุดิบ</summary>
        public bool TryReceive(GameObject item)
        {
            if (item == null || IsFull) return false;

            IngredientInstance ingredient = item.GetComponentInChildren<IngredientInstance>();
            if (ingredient == null || ingredient.Bodies.Count == 0) return false;

            ContainedIngredient entry = RegisterWhole(ingredient);
            if (entry == null) return false;

            placementBuffer.Clear();
            placementBuffer.AddRange(ingredient.Bodies);
            PlaceOnSurface(placementBuffer);

            RecountBodiesInside(entry);
            if (isFrozen)
            {
                FreezeBodiesInside(entry);
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
                for (int i = 0; i < contents.Count; i++)
                {
                    FreezeBodiesInside(contents[i]);
                }
                return;
            }

            foreach (KeyValuePair<Rigidbody, FrozenState> entry in frozenStates)
            {
                Release(entry.Key, entry.Value);
            }
            frozenStates.Clear();

            DropIngredientsOutsideTrigger();
        }

        protected override void ProcessIngredient(ContainedIngredient entry)
        {
            if (!isFrozen) return;

            // ชิ้นที่ตกเข้ามาเองระหว่างถืออยู่ (ตักจากกระทะ) ต้องถูกจัดวางแล้วยึดทันที
            // ไม่งั้นมันจะเป็น dynamic กระเด้งอยู่บนภาชนะที่ขยับตามมือแล้วร่วง
            placementBuffer.Clear();
            foreach (Rigidbody body in entry.BodiesInside)
            {
                if (!frozenStates.ContainsKey(body)) placementBuffer.Add(body);
            }
            if (placementBuffer.Count == 0) return;

            PlaceOnSurface(placementBuffer);
            for (int i = 0; i < placementBuffer.Count; i++)
            {
                Freeze(placementBuffer[i]);
            }
        }

        // ยึดเฉพาะชิ้นที่อยู่ในภาชนะจริง ชิ้นของวัตถุดิบเดียวกันที่อยู่ข้างนอกต้องไม่ถูกลากติดมา
        private void FreezeBodiesInside(ContainedIngredient entry)
        {
            foreach (Rigidbody body in entry.BodiesInside)
            {
                Freeze(body);
            }
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

        /// <summary>วางกลุ่มชิ้นลงผิวภาชนะที่จุดว่าง หมุนสุ่ม คงรูปทรงการกระจายของชิ้นไว้ และยกให้ทุกชิ้นแตะผิวพอดี</summary>
        private void PlaceOnSurface(List<Rigidbody> bodies)
        {
            if (bodies.Count == 0) return;

            Vector3 up = transform.up;
            Vector3 surfacePoint = transform.TransformPoint(PickFreeLocalSpot(bodies));
            Quaternion targetRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * Quaternion.FromToRotation(Vector3.up, up);

            // ใช้ชิ้นแรกเป็นกรอบอ้างอิง จะได้ย้ายทั้งกลุ่มโดยไม่เสียการจัดวางเดิมของโมเดล
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < bodies.Count; i++)
            {
                centroid += bodies[i].position;
            }
            centroid /= bodies.Count;
            Quaternion inverseReference = Quaternion.Inverse(bodies[0].rotation);

            for (int i = 0; i < bodies.Count; i++)
            {
                Rigidbody body = bodies[i];
                Vector3 offset = inverseReference * (body.position - centroid);
                offset.y = 0f;
                Quaternion rotation = targetRotation * (inverseReference * body.rotation);
                Vector3 position = surfacePoint + targetRotation * offset;

                SetPose(body, position, rotation);

                // แต่ละชิ้นหนาไม่เท่ากัน ต้องวัดจากของจริงว่าก้นอยู่ต่ำกว่า pivot เท่าไหร่
                Renderer renderer = body.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    float belowPivot = Vector3.Dot(body.position - renderer.bounds.min, up);
                    SetPose(body, position + up * belowPivot, rotation);
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private Vector3 SurfaceCenterLocal =>
            contentAnchor != null ? transform.InverseTransformPoint(contentAnchor.position) : Vector3.up * 0.05f;

        // สุ่มจุดในวงกลมแล้วเช็คระยะจากชิ้นอื่นที่อยู่ในภาชนะ ถ้าหาจุดว่างไม่ได้ก็เอาจุดที่ห่างที่สุด
        private Vector3 PickFreeLocalSpot(List<Rigidbody> placing)
        {
            Vector3 center = SurfaceCenterLocal;
            Vector3 best = center;
            float bestDistance = -1f;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * placementRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

                float nearest = NearestBodyDistance(candidate, placing);
                if (nearest >= minSpacing) return candidate;

                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = candidate;
                }
            }
            return best;
        }

        private float NearestBodyDistance(Vector3 localPoint, List<Rigidbody> ignored)
        {
            float nearest = float.MaxValue;

            for (int i = 0; i < contents.Count; i++)
            {
                foreach (Rigidbody body in contents[i].BodiesInside)
                {
                    if (ignored.Contains(body)) continue;

                    Vector3 local = transform.InverseTransformPoint(body.position);
                    local.y = localPoint.y;
                    nearest = Mathf.Min(nearest, Vector3.Distance(local, localPoint));
                }
            }
            return nearest;
        }

        private void Freeze(Rigidbody body)
        {
            if (body == null || frozenStates.ContainsKey(body)) return;

            Transform bodyTransform = body.transform;
            frozenStates[body] = new FrozenState
            {
                LocalPosition = transform.InverseTransformPoint(bodyTransform.position),
                LocalRotation = Quaternion.Inverse(transform.rotation) * bodyTransform.rotation,
                Held = HeldState.Capture(bodyTransform, body)
            };
        }

        private void Release(Rigidbody body, FrozenState state)
        {
            if (body == null) return;

            // ย้ายไปตำแหน่งสุดท้ายบนภาชนะก่อนคืนฟิสิกส์ เพราะ LateUpdate จะไม่ทำงานอีกแล้วหลังเลิกยึด
            ApplyFrozenPose(body, state);
            state.Held.Restore();
        }

        // ตอนยึด การสลับ isKinematic ทำให้ Exit ถูกยิงออกมาแล้วเราเมินไป ถ้าชิ้นหลุดออกนอกภาชนะจริง
        // จะไม่มี Exit รอบสองมาแก้บัญชีให้อีก จึงต้องมานับใหม่เองตอนปล่อย
        private void DropIngredientsOutsideTrigger()
        {
            if (TriggerCollider == null) return;

            for (int i = contents.Count - 1; i >= 0; i--)
            {
                ContainedIngredient entry = contents[i];
                if (entry.Ingredient == null) continue;

                RecountBodiesInside(entry);
                if (entry.BodiesInside.Count == 0)
                {
                    RemoveAt(i);
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

        protected override void RemoveAt(int index)
        {
            if (index >= 0 && index < contents.Count && contents[index].Ingredient != null)
            {
                IReadOnlyList<Rigidbody> bodies = contents[index].Ingredient.Bodies;
                for (int b = 0; b < bodies.Count; b++)
                {
                    if (frozenStates.TryGetValue(bodies[b], out FrozenState state))
                    {
                        Release(bodies[b], state);
                        frozenStates.Remove(bodies[b]);
                    }
                }
            }

            base.RemoveAt(index);
        }
    }
}
