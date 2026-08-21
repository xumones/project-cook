using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์จัดการฟิสิกส์และการทอดอาหารในกระทะ (Hybrid Pan Food Carrier System)
    /// ช่วยให้อาหารตอบสนองต่อการเอียง การสไลด์ การเหวี่ยงกระทะ และส่งผ่านความร้อนทอดใส่วัตถุดิบ
    /// </summary>
    public class PanFoodCarrier : MonoBehaviour
    {
        [Header("Pan References")]
        [Tooltip("Transform ของกระทะ (หากไม่ใส่จะอ้างอิงจาก Transform ตัวเองหรือ Parent)")]
        [SerializeField] private Transform panTransform;

        [Header("Physics Multipliers")]
        [Tooltip("ตัวคูณแรงเหวี่ยง (Inertia Force) เมื่อสไลด์กระทะ WASD")]
        [SerializeField] private float momentumMultiplier = 5f;

        [Tooltip("ตัวคูณแรงสไลด์ตามความเอียงของกระทะ (Slope Gravity Assist)")]
        [SerializeField] private float slopeForceMultiplier = 8f;

        [Tooltip("ตัวคูณแรงหมุน/พลิกตัวของวัตถุ (Rolling Torque)")]
        [SerializeField] private float rollTorqueMultiplier = 3f;

        [Tooltip("แรงดึงประคองเข้าหาก้นกระทะเบาๆ ป้องกันอาหารกระเด็นหลุดกระทะง่ายเกินไป")]
        [SerializeField] private float bowlAttractionForce = 2f;

        [Header("Filtering Settings")]
        [Tooltip("เปิดใช้งานการกรองเฉพาะวัตถุที่มี Tag ที่กำหนด (หากปิดจะส่งแรงให้ Rigidbody ทุกชิ้นที่อยู่ในกระทะ)")]
        [SerializeField] private bool useTagFilter = false;

        [Tooltip("Tag ของวัตถุดิบอาหาร เช่น 'Food' หรือ 'Ingredient'")]
        [SerializeField] private string foodTag = "Food";

        [Header("Cooking Heat Settings")]
        [Tooltip("เปิดใช้งานการแผ่ความร้อนทอดใส่อาหารในกระทะ (เช่น เมื่อกระทะวางอยู่บนเตาที่เปิดอยู่)")]
        [SerializeField] private bool isHeating = true;

        [Tooltip("ตัวคูณความเร็วการสะสมความร้อนทอด")]
        [SerializeField] private float heatRateMultiplier = 1.0f;

        [Header("Audio Settings")]
        [Tooltip("AudioSource สำหรับเล่นเสียงซู่ซ่ารวมของกระทะเมื่อมีอาหารกำลังทอดอยู่")]
        [SerializeField] private AudioSource ambientSizzleAudioSource;

        private class FoodItemData
        {
            public Rigidbody Rigidbody;
            public IngredientInstance Ingredient;
        }

        private readonly List<FoodItemData> foodItems = new List<FoodItemData>();
        private readonly List<Rigidbody> foodRigidbodiesForPublicAccess = new List<Rigidbody>();
        private Vector3 previousPanPosition;

        public bool IsHeating => isHeating;

        public void SetHeatingActive(bool active)
        {
            isHeating = active;
        }

        private void Awake()
        {
            if (panTransform == null)
            {
                panTransform = transform;
            }

            previousPanPosition = panTransform.position;

            if (ambientSizzleAudioSource == null)
            {
                ambientSizzleAudioSource = GetComponent<AudioSource>();
                if (ambientSizzleAudioSource == null)
                {
                    ambientSizzleAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            ambientSizzleAudioSource.playOnAwake = false;
            ambientSizzleAudioSource.spatialBlend = 1f; // 3D Spatial Audio

            // ค้นหา Trigger Collider ทั้งหมดในก้นกระทะ/TriggerZone และติดตั้ง Relay รับส่ง Trigger อัตโนมัติ
            Collider[] childColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in childColliders)
            {
                if (col != null && col.isTrigger)
                {
                    PanTriggerZone zone = col.gameObject.GetComponent<PanTriggerZone>();
                    if (zone == null)
                    {
                        zone = col.gameObject.AddComponent<PanTriggerZone>();
                    }
                    zone.Init(this);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            OnFoodTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnFoodTriggerExit(other);
        }

        public void OnFoodTriggerEnter(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;

            if (useTagFilter && !other.CompareTag(foodTag)) return;

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
                IngredientInstance ingredient = other.GetComponentInParent<IngredientInstance>();
                if (ingredient == null && rb != null)
                {
                    ingredient = rb.GetComponent<IngredientInstance>();
                }

                foodItems.Add(new FoodItemData { Rigidbody = rb, Ingredient = ingredient });

                if (ingredient != null)
                {
                    ingredient.SetManagedByPanCarrier(true);
                    ingredient.OnStateChanged += state => HandleIngredientStateChanged(ingredient, state);

                    // เล่นเสียงวัตถุดิบตกลงในกระทะผ่าน AudioSource ของกระทะเป็นศูนย์กลาง
                    PlayPanDropSFX(ingredient.Data);
                }
            }
        }

        private void HandleIngredientStateChanged(IngredientInstance ingredient, CookingState newState)
        {
            if (newState == CookingState.Cooked && ingredient != null && ingredient.Data != null)
            {
                if (ambientSizzleAudioSource != null && ingredient.Data.CookedDoneSFX != null)
                {
                    ambientSizzleAudioSource.pitch = 1f;
                    ambientSizzleAudioSource.PlayOneShot(ingredient.Data.CookedDoneSFX, ingredient.Data.SFXVolume);
                }
            }
        }

        private void PlayPanDropSFX(IngredientDataSO data)
        {
            if (data != null && data.DropSFX != null && ambientSizzleAudioSource != null)
            {
                ambientSizzleAudioSource.PlayOneShot(data.DropSFX, data.SFXVolume);
            }
        }

        public void OnFoodTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                for (int i = foodItems.Count - 1; i >= 0; i--)
                {
                    if (foodItems[i] == null || foodItems[i].Rigidbody == rb)
                    {
                        if (foodItems[i] != null && foodItems[i].Ingredient != null)
                        {
                            foodItems[i].Ingredient.SetManagedByPanCarrier(false);
                        }
                        foodItems.RemoveAt(i);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (panTransform == null) return;

            // คำนวณความเร็วการขยับของกระทะใน Physics Step
            Vector3 panDeltaPos = panTransform.position - previousPanPosition;
            Vector3 panVelocity = panDeltaPos / Time.fixedDeltaTime;
            previousPanPosition = panTransform.position;

            // ลบ null rigidbody หรือวัตถุที่ไม่ active ไประหว่างเล่น แบบ Zero-GC (For Loop ถอยหลัง)
            int activeCookingCount = 0;
            for (int i = foodItems.Count - 1; i >= 0; i--)
            {
                FoodItemData item = foodItems[i];
                if (item == null || item.Rigidbody == null || !item.Rigidbody.gameObject.activeInHierarchy)
                {
                    if (item != null && item.Ingredient != null)
                    {
                        item.Ingredient.SetManagedByPanCarrier(false);
                    }
                    foodItems.RemoveAt(i);
                }
                else if (isHeating && item.Ingredient != null && !item.Ingredient.IsBurnt)
                {
                    activeCookingCount++;
                }
            }

            UpdateAmbientSizzleAudio(activeCookingCount);

            if (foodItems.Count == 0) return;

            Vector3 panUp = panTransform.up;
            Vector3 panCenter = panTransform.position;

            // คำนวณทิศทางความเอียงของกระทะ (Slope Vector)
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Physics.gravity, panUp);

            for (int i = 0; i < foodItems.Count; i++)
            {
                FoodItemData item = foodItems[i];
                Rigidbody foodRb = item.Rigidbody;
                if (foodRb == null || foodRb.isKinematic) continue;

                // --- 1. Physics Movement Logic (Batching Forces) ---
                // รวมแรงเหวี่ยงสไลด์ + แรงสไลด์ความเอียง + แรงดึงก้นกระทะ เป็นเวกเตอร์เดียวเพื่อลด PhysX calls
                Vector3 inertiaForce = -panVelocity * momentumMultiplier;
                Vector3 slopeForce = slopeDirection * slopeForceMultiplier;
                Vector3 toCenterDir = (panCenter - foodRb.position);
                toCenterDir.y = 0f; // เน้นดึงในแนวราบก้นกระทะ
                Vector3 attractionForce = toCenterDir * bowlAttractionForce;

                Vector3 combinedForce = inertiaForce + slopeForce + attractionForce;
                
                // สั่ง AddForce เฉพาะเมื่อมีความขยับหรือความเอียงมากกว่า threshold เล็กน้อย เพื่อให้ PhysX หลับได้เมื่อกระทะนิ่ง
                if (combinedForce.sqrMagnitude > 0.0001f)
                {
                    foodRb.AddForce(combinedForce, ForceMode.Acceleration);
                }

                // แรงหมุนตัว/กลิ้งของวัตถุดิบ (Rolling & Tumbling Torque)
                Vector3 foodVel = foodRb.linearVelocity;
                if (foodVel.sqrMagnitude > 0.01f)
                {
                    Vector3 rollAxis = Vector3.Cross(panUp, foodVel.normalized);
                    foodRb.AddTorque(rollAxis * (foodVel.magnitude * rollTorqueMultiplier), ForceMode.Acceleration);
                }

                // --- 2. Cooking Heat Logic ---
                if (isHeating && item.Ingredient != null)
                {
                    item.Ingredient.ApplyHeat(Time.fixedDeltaTime * heatRateMultiplier, panUp);
                }
            }
        }

        private void UpdateAmbientSizzleAudio(int activeCookingCount)
        {
            if (ambientSizzleAudioSource == null) return;

            bool hasCookingFood = isHeating && activeCookingCount > 0;
            if (hasCookingFood)
            {
                // ดึง SizzleSFX จากวัตถุดิบที่กำลังทอดชิ้นแรกขึ้นมาใส่เป็นคลิปเสียงซู่ซ่าของกระทะแบบอัตโนมัติ
                if (ambientSizzleAudioSource.clip == null)
                {
                    for (int i = 0; i < foodItems.Count; i++)
                    {
                        if (foodItems[i] != null && foodItems[i].Ingredient != null && foodItems[i].Ingredient.Data != null && foodItems[i].Ingredient.Data.SizzleSFX != null)
                        {
                            ambientSizzleAudioSource.clip = foodItems[i].Ingredient.Data.SizzleSFX;
                            ambientSizzleAudioSource.loop = true;
                            break;
                        }
                    }
                }

                if (ambientSizzleAudioSource.clip != null && !ambientSizzleAudioSource.isPlaying)
                {
                    ambientSizzleAudioSource.Play();
                }

                // ปรับระดับความดังของเสียงซู่ซ่ารวมกระทะอย่างนุ่มนวลตามจำนวนชิ้นวัตถุดิบที่กำลังทอด
                float targetVolume = Mathf.Clamp01(0.35f + (activeCookingCount * 0.12f));
                ambientSizzleAudioSource.volume = Mathf.Lerp(ambientSizzleAudioSource.volume, targetVolume, Time.fixedDeltaTime * 5f);
            }
            else
            {
                if (ambientSizzleAudioSource.isPlaying)
                {
                    ambientSizzleAudioSource.Pause();
                }
            }
        }

        /// <summary>
        /// ดึงรายการ Rigidbody ของอาหารที่อยู่ในกระทะในปัจจุบัน (Zero-GC)
        /// </summary>
        public IReadOnlyList<Rigidbody> GetFoodInPan()
        {
            foodRigidbodiesForPublicAccess.Clear();
            for (int i = 0; i < foodItems.Count; i++)
            {
                var item = foodItems[i];
                if (item != null && item.Rigidbody != null)
                {
                    foodRigidbodiesForPublicAccess.Add(item.Rigidbody);
                }
            }
            return foodRigidbodiesForPublicAccess;
        }
    }

    /// <summary>
    /// สคริปต์ Helper สำหรับส่งต่อ Trigger callback จาก Child Trigger Colliders (เช่น TriggerZone) ไปยัง PanFoodCarrier
    /// </summary>
    public class PanTriggerZone : MonoBehaviour
    {
        private PanFoodCarrier carrier;

        public void Init(PanFoodCarrier carrier)
        {
            this.carrier = carrier;
        }

        private void OnTriggerEnter(Collider other)
        {
            carrier?.OnFoodTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            carrier?.OnFoodTriggerExit(other);
        }
    }
}
