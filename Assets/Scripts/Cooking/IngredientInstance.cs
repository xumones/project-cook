using System;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์หลักควบคุมวัตถุดิบแต่ละชิ้นในฉาก (Runtime State & Main Facade Coordinator)
    /// ประสานงานระหว่าง Logic เวลาความสุก, การแสดงผล (IngredientVisuals) และระบบเสียง (IngredientAudio)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IngredientVisuals))]
    [RequireComponent(typeof(IngredientAudio))]
    public class IngredientInstance : MonoBehaviour
    {
        [Header("Data Reference")]
        [Tooltip("ScriptableObject ข้อมูลและค่าตั้งต้นของวัตถุดิบนี้")]
        [SerializeField] private IngredientDataSO data;

        // Sub Component References (Auto-initialized)
        private IngredientVisuals visualsComponent;
        private IngredientAudio audioComponent;

        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อสถานะความสุกเปลี่ยน (Raw -> Cooking -> Cooked -> Burnt)
        /// </summary>
        public event Action<CookingState> OnStateChanged;

        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อระดับความสุกเปลี่ยน (0.0 ถึง 1.0)
        /// </summary>
        public event Action<float> OnCookProgressChanged;

        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อ IngredientDataSO ถูกเปลี่ยน
        /// </summary>
        public event Action<IngredientDataSO> OnDataChanged;

        // Dynamic Runtime States
        private CookingState currentCookingState = CookingState.Raw;
        private CookingSide currentActiveSide = CookingSide.Omni;

        private float omniCookTime = 0f;
        private float sideACookTime = 0f;
        private float sideBCookTime = 0f;

        private bool isVisualsDirty = false;
        private float lastReportedCookProgress = -1f;

        // Getters
        public IngredientDataSO Data => data;
        public CookingState CurrentCookingState => currentCookingState;
        public CookingSide CurrentActiveSide => currentActiveSide;

        public float CookProgress => data != null && data.CookTime > 0 
            ? (data.IsTwoSidedCooking ? Mathf.Clamp01((sideACookTime + sideBCookTime) / (data.CookTime * 2f)) : Mathf.Clamp01(omniCookTime / data.CookTime)) 
            : 0f;

        public float SideAProgress => data != null && data.CookTime > 0 ? Mathf.Clamp01(sideACookTime / data.CookTime) : 0f;
        public float SideBProgress => data != null && data.CookTime > 0 ? Mathf.Clamp01(sideBCookTime / data.CookTime) : 0f;

        public bool IsFullyCooked => currentCookingState == CookingState.Cooked;
        public bool IsBurnt => currentCookingState == CookingState.Burnt;

        private void Awake()
        {
            EnsureSubComponents();

            visualsComponent.Init(GetComponent<Renderer>());
            audioComponent.Init();

            visualsComponent.UpdateColorPropertyId(data);
            visualsComponent.ApplyInitialMaterial(data);
            visualsComponent.ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
        }

        private void EnsureSubComponents()
        {
            if (visualsComponent == null)
            {
                visualsComponent = GetComponent<IngredientVisuals>();
                if (visualsComponent == null)
                {
                    visualsComponent = gameObject.AddComponent<IngredientVisuals>();
                }
            }

            if (audioComponent == null)
            {
                audioComponent = GetComponent<IngredientAudio>();
                if (audioComponent == null)
                {
                    audioComponent = gameObject.AddComponent<IngredientAudio>();
                }
            }
        }

        private void Update()
        {
            if (isVisualsDirty)
            {
                isVisualsDirty = false;
                visualsComponent.ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
            }
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
            visualsComponent.UpdateColorPropertyId(data);
            visualsComponent.ApplyInitialMaterial(data);
            visualsComponent.ResetAppliedStates();
            visualsComponent.ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
            OnDataChanged?.Invoke(data);
        }

        /// <summary>
        /// กำหนดข้อมูลวัตถุดิบใหม่แบบ Dynamic
        /// </summary>
        public void SetData(IngredientDataSO newData)
        {
            UnsubscribeFromDataEvents();
            data = newData;
            SubscribeToDataEvents();

            visualsComponent.UpdateColorPropertyId(data);
            visualsComponent.ApplyInitialMaterial(data);
            visualsComponent.ResetAppliedStates();
            visualsComponent.ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
            OnDataChanged?.Invoke(data);
        }

        /// <summary>
        /// กำหนดสภาวะการจัดการเสียง Sizzle รวมโดยกระทะ (PanFoodCarrier)
        /// </summary>
        public void SetManagedByPanCarrier(bool managed)
        {
            if (audioComponent != null)
            {
                audioComponent.SetManagedByPanCarrier(managed);
            }
        }

        /// <summary>
        /// รับความร้อนจากกระทะ/เตา และคำนวณการสะสมความสุก (พร้อมคำนวณทิศทางการพลิกด้าน)
        /// </summary>
        /// <param name="deltaHeatTime">ระยะเวลาได้รับความร้อน (วินาที)</param>
        /// <param name="panUpVector">ทิศทางหันขึ้นของกระทะ (สำหรับคำนวณ Vector Dot Product เช็กการพลิกด้าน)</param>
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
            audioComponent.UpdateSizzleAudio(true, currentCookingState, data);

            float currentProgress = CookProgress;
            if (Mathf.Abs(currentProgress - lastReportedCookProgress) >= 0.005f)
            {
                lastReportedCookProgress = currentProgress;
                OnCookProgressChanged?.Invoke(currentProgress);
            }
        }

        /// <summary>
        /// overload สำหรับการเรียกทอดแบบ Omni-directional โดยไม่ต้องระบุ Vector ทิศทาง
        /// </summary>
        public void ApplyHeat(float deltaHeatTime)
        {
            ApplyHeat(deltaHeatTime, Vector3.up);
        }

        /// <summary>
        /// ตรวจสอบการหันของวัตถุดิบเทียบกับกระทะด้วย Vector Dot Product (Zero Allocation)
        /// </summary>
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

        /// <summary>
        /// ประเมินสถานะความสุกสำหรับการทอดรอบทิศทาง (Omni)
        /// </summary>
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

        /// <summary>
        /// ประเมินสถานะความสุกสำหรับการทอดแบบสองด้าน (Two-Sided)
        /// </summary>
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

            if (currentCookingState == CookingState.Cooked)
            {
                audioComponent.PlayCookedSFX(data);
            }
            else if (currentCookingState == CookingState.Burnt)
            {
                audioComponent.UpdateSizzleAudio(false, currentCookingState, data);
            }
        }

        /// <summary>
        /// เล่นเสียงวัตถุดิบตกลงในกระทะ
        /// </summary>
        public void PlayDropSFX()
        {
            audioComponent.PlayDropSFX(data);
        }
    }
}
