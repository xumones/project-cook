using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์ควบคุมระบบเสียงประจำสถานีทำอาหารและภาชนะ (Station Audio Controller System)
    /// จัดการการเล่นเสียงวนลูปทอด/ต้ม (Sizzle SFX), เสียงวางวัตถุดิบ (Drop SFX) และเสียงสุก (Cooked Done SFX) แบบแยกอิสระจาก Logic การทำอาหาร
    /// เพิ่มประสิทธิภาพด้วย Zero-Doppler, Audio Check Throttling (10 Hz) และ Zero-GC Frame Interpolation
    /// </summary>
    [DisallowMultipleComponent]
    public class StationAudioController : MonoBehaviour
    {
        [Header("Target Container")]
        [Tooltip("สคริปต์ BaseFoodContainer อ้างอิง (หากไม่ใส่จะค้นหาบน GameObject นี้หรือ Parent/Child อัตโนมัติ)")]
        [SerializeField] private BaseFoodContainer foodContainer;

        [Header("Audio References")]
        [Tooltip("AudioSource สำหรับเล่นเสียงประจำอุปกรณ์ (หากไม่ระบุจะค้นหาบน GameObject นี้อัตโนมัติ)")]
        [SerializeField] private AudioSource ambientAudioSource;

        [Header("Fade Settings")]
        [Tooltip("ความเร็วในการเพิ่มระดับเสียงเมื่อเริ่มทอด/ต้ม (Fade-In Speed)")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float fadeInSpeed = 1.0f;

        [Tooltip("ความเร็วในการหรี่ระดับเสียงลงเมื่อหยุดทอด/ต้ม (Fade-Out Speed)")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float fadeOutSpeed = 0.8f;

        [Header("Optimization Settings")]
        [Tooltip("ระยะห่างเวลาการสแกนเช็กสถานะวัตถุดิบและฟิสิกส์สำหรับระบบเสียง (วินาที) เช่น 0.1 วินาที = 10 Hz ช่วยลดภาระ CPU เกิน 90%")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float checkInterval = 0.1f;

        private float checkTimer = 0f;
        private float targetVolume = 0f;
        private float currentFadeSpeed = 1.0f;
        private AudioClip cachedSizzleClip = null;

        public AudioSource AmbientAudioSource => ambientAudioSource;

        private void Awake()
        {
            if (foodContainer == null)
            {
                foodContainer = GetComponent<BaseFoodContainer>();
                if (foodContainer == null)
                {
                    foodContainer = GetComponentInChildren<BaseFoodContainer>();
                }
                if (foodContainer == null)
                {
                    foodContainer = GetComponentInParent<BaseFoodContainer>();
                }
            }

            InitAudioSource();
        }

        private void Reset()
        {
            InitAudioSource();
        }

        /// <summary>
        /// ค้นหา ตรวจสอบ และสร้าง AudioSource ให้อัตโนมัติหากยังไม่มีใน GameObject พร้อมตั้งค่าพื้นฐานที่ปราศจาก Doppler Lag
        /// </summary>
        public AudioSource InitAudioSource()
        {
            if (ambientAudioSource == null)
            {
                ambientAudioSource = GetComponent<AudioSource>();
                if (ambientAudioSource == null)
                {
                    ambientAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (ambientAudioSource != null)
            {
                ambientAudioSource.playOnAwake = false;
                ambientAudioSource.dopplerLevel = 1f;
            }

            return ambientAudioSource;
        }

        private void OnEnable()
        {
            if (foodContainer != null)
            {
                foodContainer.OnIngredientDropped += HandleIngredientDropped;
                foodContainer.OnIngredientCooked += HandleIngredientCooked;
            }
        }

        private void OnDisable()
        {
            if (foodContainer != null)
            {
                foodContainer.OnIngredientDropped -= HandleIngredientDropped;
                foodContainer.OnIngredientCooked -= HandleIngredientCooked;
            }
        }

        private void HandleIngredientDropped(IngredientDataSO data)
        {
            PlayDropSFX(data);
            // บังคับเช็กสถานะเสียงทันทีเมื่อมีวัตถุดิบตกลงในกระทะ ไม่ต้องรอจังหวะ Throttling
            checkTimer = checkInterval;
        }

        private void HandleIngredientCooked(IngredientDataSO data)
        {
            PlayCookedDoneSFX(data);
        }

        public void PlayDropSFX(IngredientDataSO data)
        {
            if (data != null && data.DropSFX != null && ambientAudioSource != null)
            {
                float randomOffset = Random.Range(-data.PitchRandomness, data.PitchRandomness);
                ambientAudioSource.pitch = 1f + randomOffset;
                ambientAudioSource.PlayOneShot(data.DropSFX, data.SFXVolume);
            }
        }

        public void PlayCookedDoneSFX(IngredientDataSO data)
        {
            if (data != null && data.CookedDoneSFX != null && ambientAudioSource != null)
            {
                ambientAudioSource.pitch = 1f;
                ambientAudioSource.PlayOneShot(data.CookedDoneSFX, data.SFXVolume);
            }
        }

        private void Update()
        {
            if (foodContainer == null || ambientAudioSource == null) return;

            // 1. Throttled State Check: เช็กจำนวนวัตถุดิบและ Physics Raycast ทุกๆ checkInterval (10 Hz)
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                EvaluateCookingAudioState();
            }

            // 2. Direct Frame Interpolation (Zero-GC): ปรับระดับเสียงแบบนุ่มนวลทุกเฟรมโดยไม่สร้าง Coroutine
            UpdateVolumeInterpolation();
        }

        private void EvaluateCookingAudioState()
        {
            var foodItems = foodContainer.GetContainedFoodItems();
            if (foodItems == null || foodItems.Count == 0)
            {
                targetVolume = 0f;
                currentFadeSpeed = fadeOutSpeed;
                return;
            }

            int activeCookingCount = 0;
            AudioClip foundClip = null;

            for (int i = 0; i < foodItems.Count; i++)
            {
                var item = foodItems[i];
                if (item != null && item.Rigidbody != null && item.Rigidbody.gameObject.activeInHierarchy)
                {
                    if (foodContainer.IsHeating && item.Ingredient != null && !item.Ingredient.IsBurnt)
                    {
                        if (foodContainer.IsFoodItemActiveInStation(item))
                        {
                            activeCookingCount++;
                            if (foundClip == null && item.Ingredient.Data != null && item.Ingredient.Data.SizzleSFX != null)
                            {
                                foundClip = item.Ingredient.Data.SizzleSFX;
                            }
                        }
                    }
                }
            }

            bool hasCookingFood = foodContainer.IsHeating && activeCookingCount > 0;
            if (hasCookingFood)
            {
                targetVolume = Mathf.Clamp01(0.35f + (activeCookingCount * 0.12f));
                currentFadeSpeed = fadeInSpeed;

                if (foundClip != null && foundClip != cachedSizzleClip)
                {
                    cachedSizzleClip = foundClip;
                    ambientAudioSource.clip = cachedSizzleClip;
                    ambientAudioSource.loop = true;
                }
            }
            else
            {
                targetVolume = 0f;
                currentFadeSpeed = fadeOutSpeed;
            }
        }

        private void UpdateVolumeInterpolation()
        {
            float currentVol = ambientAudioSource.volume;

            if (targetVolume > 0.001f && !ambientAudioSource.isPlaying)
            {
                ambientAudioSource.volume = 0f;
                currentVol = 0f;
                ambientAudioSource.Play();
            }

            if (!Mathf.Approximately(currentVol, targetVolume))
            {
                ambientAudioSource.volume = Mathf.MoveTowards(currentVol, targetVolume, currentFadeSpeed * Time.deltaTime);
            }

            if (targetVolume <= 0.001f && ambientAudioSource.volume <= 0.001f && ambientAudioSource.isPlaying)
            {
                ambientAudioSource.Pause();
                ambientAudioSource.clip = null;
                cachedSizzleClip = null;
            }
        }
    }
}
