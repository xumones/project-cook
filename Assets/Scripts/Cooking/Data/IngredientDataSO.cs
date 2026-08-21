using System;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// ScriptableObject สำหรับเก็บข้อมูลและค่าคอนฟิกคงที่ของวัตถุดิบ (Data Container)
    /// </summary>
    [CreateAssetMenu(fileName = "IngredientData_", menuName = "ProjectCook/Cooking/Ingredient Data")]
    public class IngredientDataSO : ScriptableObject
    {
        /// <summary>
        /// Callback Event แจ้งเตือนเมื่อข้อมูลใน ScriptableObject มีการเปลี่ยนแปลง
        /// </summary>
        public event Action OnDataChanged;
        [Header("General Info")]
        [Tooltip("รหัสอ้างอิงวัตถุดิบ (เช่น pork_chop, french_fries)")]
        [SerializeField] private string ingredientId;

        [Tooltip("ชื่อวัตถุดิบที่แสดงใน UI")]
        [SerializeField] private string ingredientName;

        [Tooltip("รูปไอคอนสำหรับ UI")]
        [SerializeField] private Sprite icon;

        [Header("Cooking Mode")]
        [Tooltip("เปิดใช้งานโหมดทอดแยกด้าน (เช่น เนื้อสเต๊ก/เบอร์เกอร์ ต้องทอดสุกทั้งด้านหน้าและด้านหลัง)")]
        [SerializeField] private bool isTwoSidedCooking = false;

        [Header("Cooking Parameters")]
        [Tooltip("ระยะเวลาทอดจนสุก (วินาทีต่อด้าน หรือรวมทั้งชิ้น)")]
        [SerializeField] private float cookTime = 5f;

        [Tooltip("ระยะเวลาทอดสะสมเพิ่มเติมหลังสุกจนกระทั่งไหม้ (วินาที)")]
        [SerializeField] private float burnTime = 5f;

        [Header("Material References")]
        [Tooltip("Material ของวัตถุดิบตอนสด/ดิบ (เช่น rawbacon.mat)")]
        [SerializeField] private Material rawMaterial;

        [Tooltip("Material ของวัตถุดิบตอนสุกพอดี (เช่น cookbacon.mat)")]
        [SerializeField] private Material cookedMaterial;

        [Tooltip("Material ของวัตถุดิบตอนไหม้ (Burnt Material)")]
        [SerializeField] private Material burntMaterial;

        [Header("Visual Properties (Material Color)")]
        [Tooltip("ความเข้มของการย้อมสีไหม้ลงบน Texture เดิม (0 = แสดง Texture เดิม 100%, 1 = ย้อมสีเต็มที่)")]
        [Range(0f, 1f)]
        [SerializeField] private float tintIntensity = 0.5f;

        [Tooltip("สีของวัตถุดิบตอนไหม้ (Burnt Color)")]
        [SerializeField] private Color burntColor = new Color(0.35f, 0.3f, 0.3f, 1f);

        [Tooltip("ชื่อ Shader Property สำหรับเปลี่ยนสี (เช่น _Color หรือ _BaseColor)")]
        [SerializeField] private string colorPropertyName = "_Color";

        [Header("Audio SFX Settings")]
        [Tooltip("ความดังของเสียง SFX ทั้งหมดประจำวัตถุดิบนี้ (0.0 ถึง 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Tooltip("ระดับ Pitch โทนเสียงซู่ซ่าขณะทอด (1.0 = ปกติ, >1.0 = เสียงแหลม/ทอดเร็ว, <1.0 = เสียงทุ้ม)")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float sizzlePitch = 1f;

        [Tooltip("ความสุ่มของระดับ Pitch ตอนวัตถุดิบตกลงกระทะ (เพื่อความสมจริงไม่ให้เสียงซ้ำกันเดี่ยวๆ)")]
        [Range(0f, 0.3f)]
        [SerializeField] private float pitchRandomness = 0.05f;

        [Tooltip("เสียงเมื่อวัตถุดิบตกลงในกระทะ/น้ำมัน")]
        [SerializeField] private AudioClip dropSFX;

        [Tooltip("เสียงซู่ซ่าขณะทอดประจำวัตถุดิบนี้")]
        [SerializeField] private AudioClip sizzleSFX;

        [Tooltip("เสียงแจ้งเตือนเมื่อวัตถุดิบสุกพอดี")]
        [SerializeField] private AudioClip cookedDoneSFX;

        // Public Getters
        public string IngredientId => ingredientId;
        public string IngredientName => ingredientName;
        public Sprite Icon => icon;
        public bool IsTwoSidedCooking => isTwoSidedCooking;
        public float CookTime => cookTime;
        public float BurnTime => burnTime;
        public Material RawMaterial => rawMaterial;
        public Material CookedMaterial => cookedMaterial;
        public Material BurntMaterial => burntMaterial;
        public float TintIntensity => tintIntensity;
        public Color BurntColor => burntColor;
        public string ColorPropertyName => colorPropertyName;
        public float SFXVolume => sfxVolume;
        public float SizzlePitch => sizzlePitch;
        public float PitchRandomness => pitchRandomness;
        public AudioClip DropSFX => dropSFX;
        public AudioClip SizzleSFX => sizzleSFX;
        public AudioClip CookedDoneSFX => cookedDoneSFX;

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnDataChanged?.Invoke();
        }
#endif
    }
}
