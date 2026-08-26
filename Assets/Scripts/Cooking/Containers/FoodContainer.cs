using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// Abstract Class แม่สำหรับอุปกรณ์ทำอาหาร/ภาชนะเก็บวัตถุดิบ (Base Food Container System)
    /// ควบคุมการติดตามวัตถุดิบในภาชนะ, การคัดกรองวัตถุดิบ, ระบบความร้อนพื้นฐาน และระบบเสียงศูนย์กลางแบบ Coroutine
    /// </summary>
    public abstract class FoodContainer : MonoBehaviour
    {
        [Header("Filtering Settings")]
        [Tooltip("เปิดใช้งานการกรองเฉพาะวัตถุที่มี Tag ที่กำหนด (หากปิดจะรับ Rigidbody ทุกชิ้นที่เป็นอาหาร)")]
        [SerializeField] protected bool useTagFilter = false;

        [Tooltip("Tag ของวัตถุดิบอาหาร เช่น 'Food' หรือ 'Ingredient'")]
        [SerializeField] protected string foodTag = "Food";

        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อมีวัตถุดิบวางลงในภาชนะ
        /// </summary>
        public event System.Action<IngredientDataSO> OnIngredientDropped;

        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อวัตถุดิบในภาชนะสุกได้ที่ (Cooked)
        /// </summary>
        public event System.Action<IngredientDataSO> OnIngredientCooked;

        public class FoodItemData
        {
            public Rigidbody Rigidbody;
            public IngredientInstance Ingredient;
            public int LastRaycastFrame = -1;
            public bool CachedIsTouchingSurface = false;

            // เก็บ Delegate ที่ Subscribe ไว้ เพื่อให้ถอนออกได้ตอนวัตถุดิบออกจากภาชนะ
            // (หากไม่เก็บไว้จะถอนไม่ได้ ทำให้ Event ค้างสะสมและยิงซ้ำหลายรอบ)
            public System.Action<CookingState> StateChangedHandler;
        }

        protected readonly List<FoodItemData> foodItems = new List<FoodItemData>();

        /// <summary>
        /// ภาชนะนี้กำลังแผ่ความร้อนอยู่หรือไม่ (ภาชนะที่ไม่มีความร้อน เช่น จานเสิร์ฟ จะเป็น false เสมอ)
        /// </summary>
        public virtual bool IsHeating => false;

        public IReadOnlyList<FoodItemData> GetContainedFoodItems() => foodItems;

        protected virtual void Awake()
        {
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;

            if (useTagFilter && !other.CompareTag(foodTag)) return;

            IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
            if (ingredient == null && rb != null)
            {
                ingredient = rb.GetComponent<IngredientInstance>();
            }

            if (ingredient == null) return;

            bool alreadyExists = false;
            for (int i = 0; i < foodItems.Count; i++)
            {
                if (foodItems[i] != null && foodItems[i].Rigidbody == rb)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                FoodItemData newItem = new FoodItemData { Rigidbody = rb, Ingredient = ingredient };

                // เก็บ Delegate ไว้ใน FoodItemData เพื่อให้ถอน Subscription ได้ตอนวัตถุดิบออกจากภาชนะ
                newItem.StateChangedHandler = state => HandleIngredientStateChanged(ingredient, state);
                ingredient.OnStateChanged += newItem.StateChangedHandler;

                foodItems.Add(newItem);

                if (ingredient.Data != null)
                {
                    OnIngredientDropped?.Invoke(ingredient.Data);
                }
            }
        }

        public virtual void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                for (int i = foodItems.Count - 1; i >= 0; i--)
                {
                    if (foodItems[i] == null || foodItems[i].Rigidbody == rb)
                    {
                        RemoveFoodItemAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// ถอด Subscription ของวัตถุดิบออกก่อนแล้วจึงลบออกจากรายการ
        /// (ต้องถอนทุกครั้งเพื่อไม่ให้ Event สะสมและยิงซ้ำเมื่อวัตถุดิบชิ้นเดิมถูกใส่กลับเข้ามาใหม่)
        /// </summary>
        protected void RemoveFoodItemAt(int index)
        {
            if (index < 0 || index >= foodItems.Count) return;

            FoodItemData item = foodItems[index];
            if (item != null && item.Ingredient != null && item.StateChangedHandler != null)
            {
                item.Ingredient.OnStateChanged -= item.StateChangedHandler;
            }

            foodItems.RemoveAt(index);
        }

        protected virtual void OnDestroy()
        {
            // ถอน Subscription ที่ค้างอยู่ทั้งหมดเมื่อภาชนะถูกทำลาย
            for (int i = foodItems.Count - 1; i >= 0; i--)
            {
                RemoveFoodItemAt(i);
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
            if (foodItems.Count == 0) return;

            // วนรอบเดียวแบบย้อนกลับ ทำทั้งการล้างรายการที่หมดอายุและการประมวลผลวัตถุดิบในคราวเดียว
            for (int i = foodItems.Count - 1; i >= 0; i--)
            {
                FoodItemData item = foodItems[i];

                if (item == null || item.Rigidbody == null || !item.Rigidbody.gameObject.activeInHierarchy)
                {
                    RemoveFoodItemAt(i);
                    continue;
                }

                if (item.Ingredient == null) continue;

                ProcessItem(item);

                // สั่งอัปเดตการแสดงผลจากตรงนี้แทนการให้วัตถุดิบแต่ละชิ้นมี Update() ของตัวเอง
                // ลดจำนวนการเรียก Update() ของ Unity ลงเหลือศูนย์เมื่อมีวัตถุดิบหลายชิ้นพร้อมกัน
                item.Ingredient.TickVisuals();
            }
        }

        /// <summary>
        /// Hook สำหรับให้ภาชนะแต่ละชนิดประมวลผลวัตถุดิบตามความสามารถของตัวเอง
        /// (ภาชนะที่ให้ความร้อนจะ Override เพื่อทอด/ต้ม ส่วนจานเสิร์ฟไม่ต้องทำอะไร)
        /// </summary>
        protected virtual void ProcessItem(FoodItemData item)
        {
        }

        /// <summary>
        /// Hook สำหรับการตรวจสอบว่าวัตถุดิบกำลังอยู่ในสภาวะทอด/ต้มจริงในภาชนะหรือไม่
        /// </summary>
        public virtual bool IsItemBeingProcessed(FoodItemData item)
        {
            return true;
        }

    }
}
