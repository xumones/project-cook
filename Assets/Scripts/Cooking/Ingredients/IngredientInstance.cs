using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Cooking
{
    /// <summary>ตัวตนของวัตถุดิบหนึ่งหน่วย (นับ/สุก/เข้าสูตร) ซึ่งมีชิ้นฟิสิกส์ได้หลายชิ้น เช่น ผักหั่นหรือเนื้อเต๋า</summary>
    // กติกา prefab: root มี IngredientInstance + IngredientVisuals, ชิ้น (Rigidbody + Collider) อยู่ที่ root เองหรือเป็นลูก
    // ห้ามมี Rigidbody ที่ root ถ้ามีชิ้นลูก เพราะ Rigidbody ซ้อนกันจะสู้กันเอง
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IngredientVisuals))]
    [RequireComponent(typeof(Item))]
    public class IngredientInstance : MonoBehaviour, ISavableState
    {
        // หันเกินมุมนี้จากแนวผิวกระทะถือว่าด้านนั้นแตะผิว น้อยกว่านี้ = ตะแคง ทอดสองด้านพร้อมกัน
        private const float SideContactDot = 0.35f;

        [Serializable]
        private struct SavedCookTimes
        {
            public float omni;
            public float sideA;
            public float sideB;
        }

        // มาจาก Definition ของ Item จะได้ตั้งข้อมูลวัตถุดิบไว้ที่เดียว
        private IngredientDataSO data;

        private IngredientVisuals visualsComponent;
        private Rigidbody[] bodies;
        private CookingProgress progress;
        private bool isVisualsDirty;

        /// <summary>ชิ้นฟิสิกส์ทั้งหมดของวัตถุดิบนี้ ระบบภายนอกต้องอ่านจากตรงนี้แทนการหา Rigidbody เอง</summary>
        public IReadOnlyList<Rigidbody> Bodies => bodies;

        /// <summary>สถานะความสุกเปลี่ยน (Raw -> Cooking -> Cooked -> Burnt)</summary>
        public event Action<CookingState> OnStateChanged;

        public IngredientDataSO Data => data;
        public CookingState CurrentCookingState => progress != null ? progress.State : CookingState.Raw;
        public float CookProgress => progress != null ? progress.Progress : 0f;
        public bool IsBurnt => CurrentCookingState == CookingState.Burnt;

        /// <summary>ชิ้นที่คีมกำลังจับอยู่ null = ไม่ได้ถูกจับ ชิ้นอื่นยังเป็นอิสระตามฟิสิกส์</summary>
        public Rigidbody GrippedBody { get; set; }

        private void Awake()
        {
            data = GetComponent<Item>().Definition as IngredientDataSO;
            if (data == null)
            {
                Debug.LogError($"[IngredientInstance] {name} ต้องตั้ง Definition ของ Item เป็น IngredientDataSO", this);
            }
            else
            {
                progress = new CookingProgress(data);
            }

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
            visualsComponent.Configure(data, 0f, 0f, 0f);
        }

        private void OnEnable()
        {
            if (data != null) data.OnDataChanged += HandleDataChanged;
        }

        private void OnDisable()
        {
            if (data != null) data.OnDataChanged -= HandleDataChanged;
        }

        private void HandleDataChanged()
        {
            visualsComponent.Configure(data, progress.SideATime, progress.SideBTime, progress.OmniTime);
        }

        /// <summary>อัปเดตการแสดงผลหากมีการเปลี่ยนแปลง ถูกเรียกจากภาชนะที่วัตถุดิบชิ้นนี้อยู่</summary>
        public void TickVisuals()
        {
            if (!isVisualsDirty) return;

            isVisualsDirty = false;
            visualsComponent.ApplyVisuals(data, progress.SideATime, progress.SideBTime, progress.OmniTime);
        }

        /// <summary>รับความร้อนจากกระทะ/เตา ด้านที่โดนทอดดูจากทิศของวัตถุดิบเทียบกับผิวกระทะ</summary>
        public void ApplyHeat(float deltaHeatTime, Vector3 panUpVector)
        {
            if (progress == null) return;

            if (progress.AddHeat(deltaHeatTime, ContactSide(panUpVector))) HandleStateChanged();
            isVisualsDirty = true;
        }

        private CookingSide ContactSide(Vector3 panUpVector)
        {
            float dot = Vector3.Dot(transform.up, panUpVector);
            if (dot > SideContactDot) return CookingSide.SideA;
            if (dot < -SideContactDot) return CookingSide.SideB;
            return CookingSide.Omni;
        }

        public string StateKey => "cook_times";

        // สถานะความสุกและภาพคำนวณจากเวลาทอดได้ทั้งหมด จึงจดแค่เวลา
        public string CaptureState() => progress == null ? string.Empty : JsonUtility.ToJson(new SavedCookTimes
        {
            omni = progress.OmniTime,
            sideA = progress.SideATime,
            sideB = progress.SideBTime
        });

        public void RestoreState(string state)
        {
            if (progress == null || string.IsNullOrEmpty(state)) return;

            SavedCookTimes saved = JsonUtility.FromJson<SavedCookTimes>(state);
            CookingState before = progress.State;
            progress.Restore(saved.omni, saved.sideA, saved.sideB);
            if (progress.State != before) HandleStateChanged();
            visualsComponent.ApplyVisuals(data, progress.SideATime, progress.SideBTime, progress.OmniTime);
        }

        private void HandleStateChanged()
        {
            visualsComponent.UpdateMaterialForState(progress.State, data);
            OnStateChanged?.Invoke(progress.State);
        }
    }
}
