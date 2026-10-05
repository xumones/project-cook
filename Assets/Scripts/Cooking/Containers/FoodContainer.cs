using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectCook.Items;

namespace ProjectCook.Cooking
{
    /// <summary>ฐานของภาชนะทุกชนิด ติดตามว่าวัตถุดิบตัวไหนอยู่ข้างในโดยนับจากชิ้นฟิสิกส์ที่อยู่ใน trigger</summary>
    public abstract class FoodContainer : MonoBehaviour, IItemContents
    {
        public event System.Action<IngredientDataSO> OnIngredientDropped;
        public event System.Action<IngredientDataSO> OnIngredientCooked;

        /// <summary>วัตถุดิบหนึ่งตัวตนที่อยู่ในภาชนะ วัตถุดิบหั่นจะมีหลายชิ้นแต่เป็นรายการเดียว</summary>
        public class ContainedIngredient
        {
            public IngredientInstance Ingredient;

            /// <summary>ชิ้นที่ยังอยู่ใน trigger ตอนนี้ ว่างเมื่อไหร่ถือว่าวัตถุดิบออกจากภาชนะแล้ว</summary>
            public readonly HashSet<Rigidbody> BodiesInside = new HashSet<Rigidbody>();

            // เก็บ delegate ไว้เพื่อถอน subscription ได้ตอนวัตถุดิบออกจากภาชนะ
            public System.Action<CookingState> StateChangedHandler;
        }

        protected readonly List<ContainedIngredient> contents = new List<ContainedIngredient>();

        /// <summary>Trigger ที่ใช้นับของเข้า/ออก หาจาก collider ตัวแรกที่เป็น trigger ในตัวหรือลูก</summary>
        protected Collider TriggerCollider { get; private set; }

        /// <summary>ความร้อนของภาชนะตอนนี้ 0-1 (0 = เย็น)</summary>
        public virtual float Heat => 0f;

        public IReadOnlyList<ContainedIngredient> Contents => contents;

        public bool HasContents => contents.Count > 0;

        /// <summary>เติมรายชื่อวัตถุดิบข้างในลง list ที่ผู้เรียกเตรียมมา</summary>
        public void GetIngredients(List<IngredientInstance> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < contents.Count; i++)
            {
                buffer.Add(contents[i].Ingredient);
            }
        }

        /// <summary>เอาวัตถุดิบออกจากภาชนะทั้งหมดแล้วทำลายทิ้ง ใช้ตอนส่งมอบ</summary>
        public void ConsumeAll()
        {
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                IngredientInstance ingredient = contents[i].Ingredient;

                // ถอดออกจากรายการก่อน (คลาสลูกจะได้คืนสถานะที่ยึดไว้) แล้วค่อยทำลาย
                RemoveAt(i);
                if (ingredient != null)
                {
                    Destroy(ingredient.gameObject);
                }
            }
        }

        /// <summary>ยึดวัตถุดิบข้างในให้ติดไปกับภาชนะ ภาชนะที่ให้ความร้อนไม่รองรับเพราะอาหารต้องไถลได้</summary>
        public virtual void SetFrozen(bool frozen)
        {
        }

        public void MoveToScene(Scene scene)
        {
            for (int i = 0; i < contents.Count; i++)
            {
                GameObject ingredient = contents[i].Ingredient != null ? contents[i].Ingredient.gameObject : null;

                // ย้ายฉากได้เฉพาะ root วัตถุดิบที่ถูก parent ไว้กับอย่างอื่นจะไปตามพ่อของมันเอง
                if (ingredient == null || ingredient.transform.parent != null || ingredient.scene == scene) continue;

                SceneManager.MoveGameObjectToScene(ingredient, scene);
            }
        }

        protected virtual void Awake()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger) continue;

                TriggerCollider = colliders[i];
                break;
            }

            if (TriggerCollider == null)
            {
                Debug.LogError($"[FoodContainer] {name} ไม่มี Trigger Collider จะตรวจจับวัตถุดิบไม่ได้เลย", this);
            }
        }

        /// <summary>จุดศูนย์ถ่วงของชิ้นอยู่ในปริมาตร trigger หรือไม่ ใช้แทน event ตอนที่ PhysX ไม่ยิงให้ (ของถูกวางเข้ามาตรง ๆ หรือสลับ kinematic)</summary>
        protected bool IsInsideTrigger(Rigidbody body)
        {
            if (TriggerCollider == null || body == null) return false;

            Vector3 point = body.worldCenterOfMass;
            return (TriggerCollider.ClosestPoint(point) - point).sqrMagnitude <= 1e-6f;
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (ingredient == null) return;

            ContainedIngredient entry = Find(ingredient) ?? Register(ingredient);
            entry.BodiesInside.Add(body);
        }

        public virtual void OnTriggerExit(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null) return;

            for (int i = contents.Count - 1; i >= 0; i--)
            {
                ContainedIngredient entry = contents[i];
                if (!entry.BodiesInside.Remove(body)) continue;

                if (entry.BodiesInside.Count == 0)
                {
                    RemoveAt(i);
                }
                return;
            }
        }

        protected ContainedIngredient Find(IngredientInstance ingredient)
        {
            for (int i = 0; i < contents.Count; i++)
            {
                if (contents[i].Ingredient == ingredient) return contents[i];
            }
            return null;
        }

        /// <summary>ลงทะเบียนวัตถุดิบเข้าภาชนะ ผู้เรียกต้องเช็คก่อนว่ายังไม่มี</summary>
        protected ContainedIngredient Register(IngredientInstance ingredient)
        {
            var entry = new ContainedIngredient { Ingredient = ingredient };
            entry.StateChangedHandler = state => HandleIngredientStateChanged(ingredient, state);
            ingredient.OnStateChanged += entry.StateChangedHandler;
            contents.Add(entry);

            if (ingredient.Data != null)
            {
                OnIngredientDropped?.Invoke(ingredient.Data);
            }
            return entry;
        }

        // ใช้ตำแหน่งจริงนับใหม่ เพราะการสลับ isKinematic ทำให้บัญชี Enter/Exit จาก PhysX เชื่อไม่ได้
        protected void RecountBodiesInside(ContainedIngredient entry)
        {
            entry.BodiesInside.Clear();

            IReadOnlyList<Rigidbody> bodies = entry.Ingredient.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                if (IsInsideTrigger(bodies[i]))
                {
                    entry.BodiesInside.Add(bodies[i]);
                }
            }
        }

        /// <summary>ถอด subscription ของวัตถุดิบออกก่อนแล้วจึงลบออกจากรายการ</summary>
        protected virtual void RemoveAt(int index)
        {
            if (index < 0 || index >= contents.Count) return;

            ContainedIngredient entry = contents[index];
            if (entry.Ingredient != null && entry.StateChangedHandler != null)
            {
                entry.Ingredient.OnStateChanged -= entry.StateChangedHandler;
            }

            contents.RemoveAt(index);
        }

        protected virtual void OnDestroy()
        {
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }

        protected virtual void HandleIngredientStateChanged(IngredientInstance ingredient, CookingState newState)
        {
            if (newState == CookingState.Cooked && ingredient != null && ingredient.Data != null)
            {
                OnIngredientCooked?.Invoke(ingredient.Data);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (contents.Count == 0) return;

            // วนย้อนกลับรอบเดียว ทั้งล้างรายการที่ถูกทำลายไปแล้วและประมวลผลวัตถุดิบ
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                ContainedIngredient entry = contents[i];

                if (entry.Ingredient == null || !entry.Ingredient.gameObject.activeInHierarchy)
                {
                    RemoveAt(i);
                    continue;
                }

                ProcessIngredient(entry);

                // ภาชนะเป็นคนสั่งอัปเดตภาพ วัตถุดิบจึงไม่ต้องมี Update() ของตัวเอง
                entry.Ingredient.TickVisuals();
            }
        }

        /// <summary>Hook ให้ภาชนะแต่ละชนิดประมวลผลวัตถุดิบตามความสามารถของตัวเอง</summary>
        protected virtual void ProcessIngredient(ContainedIngredient entry)
        {
        }

        /// <summary>วัตถุดิบนี้กำลังได้รับความร้อนจริงอยู่หรือไม่ ใช้ตัดสินว่าควรมีเสียงทอด</summary>
        public virtual bool IsReceivingHeat(ContainedIngredient entry)
        {
            return true;
        }

        /// <summary>หา FoodContainer จากตัวเอง ลูก แล้วจึงพ่อ ตามลำดับ ใช้แทนการเขียน fallback chain นี้ซ้ำในแต่ละ component ที่ต้องพ่วงกับภาชนะ</summary>
        public static FoodContainer FindFor(Component host)
        {
            if (host == null) return null;

            FoodContainer container = host.GetComponent<FoodContainer>();
            if (container == null) container = host.GetComponentInChildren<FoodContainer>();
            if (container == null) container = host.GetComponentInParent<FoodContainer>();
            return container;
        }
    }
}
