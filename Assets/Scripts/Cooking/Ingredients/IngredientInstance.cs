using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>ตัวตนของวัตถุดิบหนึ่งหน่วย (นับ/สุก/เข้าสูตร) ซึ่งมีชิ้นฟิสิกส์ได้หลายชิ้น เช่น ผักหั่นหรือเนื้อเต๋า</summary>
    // กติกา prefab: root มี IngredientInstance + IngredientVisuals, ชิ้น (Rigidbody + Collider) อยู่ที่ root เองหรือเป็นลูก
    // ห้ามมี Rigidbody ที่ root ถ้ามีชิ้นลูก เพราะ Rigidbody ซ้อนกันจะสู้กันเอง
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IngredientVisuals))]
    public class IngredientInstance : MonoBehaviour
    {
        [Header("Data Reference")]
        [Tooltip("ScriptableObject ข้อมูลและค่าตั้งต้นของวัตถุดิบนี้")]
        [SerializeField] private IngredientDataSO data;

        private IngredientVisuals visualsComponent;
        private Rigidbody[] bodies;

        /// <summary>ชิ้นฟิสิกส์ทั้งหมดของวัตถุดิบนี้ ระบบภายนอกต้องอ่านจากตรงนี้แทนการหา Rigidbody เอง</summary>
        public IReadOnlyList<Rigidbody> Bodies => bodies;

        /// <summary>Callback Event แจ้งเตือนเมื่อสถานะความสุกเปลี่ยน (Raw -> Cooking -> Cooked -> Burnt)</summary>
        public event Action<CookingState> OnStateChanged;

        // Dynamic Runtime States
        private CookingState currentCookingState = CookingState.Raw;
        private CookingSide currentActiveSide = CookingSide.Omni;

        private float omniCookTime = 0f;
        private float sideACookTime = 0f;
        private float sideBCookTime = 0f;

        private bool isVisualsDirty = false;

        // Getters
        public IngredientDataSO Data => data;
        public CookingState CurrentCookingState => currentCookingState;

        public float CookProgress => data != null && data.CookTime > 0 
            ? (data.IsTwoSidedCooking ? Mathf.Clamp01((sideACookTime + sideBCookTime) / (data.CookTime * 2f)) : Mathf.Clamp01(omniCookTime / data.CookTime)) 
            : 0f;

        public bool IsBurnt => currentCookingState == CookingState.Burnt;

        private void Awake()
        {
            bodies = GetComponentsInChildren<Rigidbody>(true);
            if (bodies.Length == 0)
            {
                Debug.LogError($"[IngredientInstance] {name} ไม่มี Rigidbody เลย ภาชนะและคีมจะมองไม่เห็นวัตถุดิบนี้", this);
            }
            else if (bodies.Length > 1 && GetComponent<Rigidbody>() != null)
            {
                Debug.LogError($"[IngredientInstance] {name} มี Rigidbody ที่ root ซ้อนกับชิ้นลูก ต้องเอาของ root ออก", this);
            }

            visualsComponent = GetComponent<IngredientVisuals>();
            visualsComponent.Init();
            visualsComponent.Configure(data, sideACookTime, sideBCookTime, omniCookTime);
        }

        /// <summary>อัปเดตการแสดงผลหากมีการเปลี่ยนแปลง ถูกเรียกจากภาชนะที่วัตถุดิบชิ้นนี้อยู่</summary>
        public void TickVisuals()
        {
            if (!isVisualsDirty) return;

            isVisualsDirty = false;
            visualsComponent.ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
        }

        private void OnEnable()
        {
            SubscribeToDataEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromDataEvents();
        }

        private void SubscribeToDataEvents()
        {
            if (data != null)
            {
                data.OnDataChanged += HandleDataSOChanged;
            }
        }

        private void UnsubscribeFromDataEvents()
        {
            if (data != null)
            {
                data.OnDataChanged -= HandleDataSOChanged;
            }
        }

        private void HandleDataSOChanged()
        {
            visualsComponent.Configure(data, sideACookTime, sideBCookTime, omniCookTime);
        }

        /// <summary>รับความร้อนจากกระทะ/เตา และคำนวณการสะสมความสุก (พร้อมคำนวณทิศทางการพลิกด้าน)</summary>
        public void ApplyHeat(float deltaHeatTime, Vector3 panUpVector)
        {
            if (data == null || currentCookingState == CookingState.Burnt) return;

            if (data.IsTwoSidedCooking)
            {
                DetermineActiveSide(panUpVector);

                if (currentActiveSide == CookingSide.SideA)
                {
                    sideACookTime += deltaHeatTime;
                }
                else if (currentActiveSide == CookingSide.SideB)
                {
                    sideBCookTime += deltaHeatTime;
                }
                else
                {
                    sideACookTime += deltaHeatTime * 0.5f;
                    sideBCookTime += deltaHeatTime * 0.5f;
                }

                EvaluateTwoSidedCookingState();
            }
            else
            {
                currentActiveSide = CookingSide.Omni;
                omniCookTime += deltaHeatTime;
                EvaluateCookingState();
            }

            isVisualsDirty = true;
        }

        /// <summary>ตรวจสอบการหันของวัตถุดิบเทียบกับกระทะด้วย Vector Dot Product (Zero Allocation)</summary>
        private void DetermineActiveSide(Vector3 panUpVector)
        {
            float dot = Vector3.Dot(transform.up, panUpVector);
            if (dot > 0.35f)
            {
                currentActiveSide = CookingSide.SideA;
            }
            else if (dot < -0.35f)
            {
                currentActiveSide = CookingSide.SideB;
            }
            else
            {
                currentActiveSide = CookingSide.Omni;
            }
        }

        /// <summary>ประเมินสถานะความสุกสำหรับการทอดรอบทิศทาง (Omni)</summary>
        private void EvaluateCookingState()
        {
            float totalTargetTime = data.CookTime + data.BurnTime;

            if (omniCookTime >= totalTargetTime)
            {
                SetState(CookingState.Burnt);
            }
            else if (omniCookTime >= data.CookTime)
            {
                SetState(CookingState.Cooked);
            }
            else if (omniCookTime > 0)
            {
                SetState(CookingState.Cooking);
            }
        }

        /// <summary>ประเมินสถานะความสุกสำหรับการทอดแบบสองด้าน (Two-Sided)</summary>
        private void EvaluateTwoSidedCookingState()
        {
            float totalTargetTime = data.CookTime + data.BurnTime;

            if (sideACookTime >= totalTargetTime || sideBCookTime >= totalTargetTime)
            {
                SetState(CookingState.Burnt);
                return;
            }

            bool isSideACooked = sideACookTime >= data.CookTime;
            bool isSideBCooked = sideBCookTime >= data.CookTime;

            if (isSideACooked && isSideBCooked)
            {
                SetState(CookingState.Cooked);
            }
            else if (sideACookTime > 0 || sideBCookTime > 0)
            {
                SetState(CookingState.Cooking);
            }
        }

        private void SetState(CookingState newState)
        {
            if (currentCookingState == newState) return;

            currentCookingState = newState;
            visualsComponent.UpdateMaterialForState(newState, data);

            OnStateChanged?.Invoke(currentCookingState);
        }

        /// <summary>ชิ้นที่คีมกำลังจับอยู่ null = ไม่ได้ถูกจับ ชิ้นอื่นยังเป็นอิสระตามฟิสิกส์</summary>
        public Rigidbody GrippedBody { get; set; }
    }
}
