using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>สคริปต์ควบคุมระบบเสียงประจำสถานีทำอาหารและภาชนะ (Container Audio Controller System)</summary>
    [DisallowMultipleComponent]
    public class ContainerAudioController : MonoBehaviour
    {
        [Header("Target Container")]
        [Tooltip("สคริปต์ FoodContainer อ้างอิง (หากไม่ใส่จะค้นหาบน GameObject นี้หรือ Parent/Child อัตโนมัติ)")]
        [SerializeField] private FoodContainer foodContainer;

        [Header("Audio References")]
        [Tooltip("AudioSource แยกต่างหากสำหรับเสียง One-Shot (ตกกระทะ/สุก) เพื่อไม่ให้ไปแย่ง Pitch กับเสียงซู่ซ่าที่กำลังลูปอยู่ (หากไม่ระบุจะสร้างให้อัตโนมัติ)")]
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Fade Settings")]
        [Tooltip("ความเร็วในการเพิ่มระดับเสียงเมื่อเริ่มทอด/ต้ม (Fade-In Speed)")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float fadeInSpeed = 1.0f;

        [Tooltip("ความเร็วในการหรี่ระดับเสียงลงเมื่อหยุดทอด/ต้ม (Fade-Out Speed)")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float fadeOutSpeed = 1.0f;

        [Header("Optimization Settings")]
        [Tooltip("ระยะห่างเวลาการสแกนเช็กสถานะวัตถุดิบและฟิสิกส์สำหรับระบบเสียง (วินาที) เช่น 0.1 วินาที = 10 Hz")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float checkInterval = 0.5f;

        [Tooltip("จำนวนเสียงซู่ซ่าสูงสุดที่เล่นพร้อมกันได้ในเวลาเดียวกัน หากมีวัตถุดิบที่ให้เสียงต่างกันมากกว่านี้ จะเลือกเล่นเฉพาะกลุ่มที่มีจำนวนชิ้นเยอะที่สุด (ดังที่สุด) เรียงลงมาตามจำนวนนี้ ที่เหลือจะไม่เล่น")]
        [Range(1, 4)]
        [SerializeField] private int maxConcurrentVoices = 3;

        // นับตามชนิดวัตถุดิบ ไม่ใช่ตาม clip เพราะ pitch ต่างกันได้แม้ clip เดียวกัน
        private struct SizzleGroup
        {
            public IngredientDataSO Data;
            public int Count;
        }

        private class SizzleVoice
        {
            public AudioSource Source;
            public IngredientDataSO CurrentData;
            public float TargetVolume;
            public float FadeSpeed;
        }

        private static readonly Comparison<SizzleGroup> DescendingByCount = (a, b) => b.Count.CompareTo(a.Count);

        private readonly List<SizzleGroup> groups = new List<SizzleGroup>(8);
        private SizzleVoice[] voices;
        private bool[] voiceMatchedBuffer;
        private bool[] selectedConsumedBuffer;

        private float checkTimer = 0f;

        private void Awake()
        {
            if (foodContainer == null)
            {
                foodContainer = FoodContainer.FindFor(this);
            }

            InitAudioSources();
        }

        /// <summary>ค้นหา ตรวจสอบ และสร้าง AudioSource ทั้งหมดให้อัตโนมัติหากยังไม่มี</summary>
        private void InitAudioSources()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null)
                {
                    sfxAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (sfxAudioSource != null)
            {
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.loop = false;
                sfxAudioSource.dopplerLevel = 0f;
                sfxAudioSource.spatialBlend = 1f;
            }

            if (voices == null || voices.Length != maxConcurrentVoices)
            {
                voices = new SizzleVoice[maxConcurrentVoices];
                voiceMatchedBuffer = new bool[maxConcurrentVoices];
                selectedConsumedBuffer = new bool[maxConcurrentVoices];

                for (int i = 0; i < maxConcurrentVoices; i++)
                {
                    var source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = true;
                    source.dopplerLevel = 0f;
                    source.spatialBlend = 1f;
                    voices[i] = new SizzleVoice { Source = source };
                }
            }
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

        private void PlayDropSFX(IngredientDataSO data)
        {
            if (data != null && data.DropSFX != null && sfxAudioSource != null)
            {
                float randomOffset = UnityEngine.Random.Range(-data.PitchRandomness, data.PitchRandomness);
                sfxAudioSource.pitch = 1f + randomOffset;
                sfxAudioSource.PlayOneShot(data.DropSFX, data.SFXVolume);
            }
        }

        private void PlayCookedDoneSFX(IngredientDataSO data)
        {
            if (data != null && data.CookedDoneSFX != null && sfxAudioSource != null)
            {
                sfxAudioSource.pitch = 1f;
                sfxAudioSource.PlayOneShot(data.CookedDoneSFX, data.SFXVolume);
            }
        }

        private void Update()
        {
            if (foodContainer == null || voices == null) return;

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
            var items = foodContainer.Contents;
            groups.Clear();

            float heat = foodContainer.Heat;
            if (heat > 0f)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var entry = items[i];
                    IngredientInstance ingredient = entry.Ingredient;
                    if (ingredient == null || !ingredient.gameObject.activeInHierarchy || ingredient.IsBurnt) continue;

                    var data = ingredient.Data;
                    // นับเฉพาะชิ้นที่มีเสียงซู่ซ่าให้เล่นจริง วัตถุดิบที่ยังไม่ได้ตั้งค่า SizzleSFX จะไม่ถูกนับ
                    if (data == null || data.SizzleSFX == null) continue;

                    if (!foodContainer.IsReceivingHeat(entry)) continue;

                    AddToGroup(data);
                }
            }

            // เรียงคลิปตามจำนวนชิ้นที่ใช้เสียงเดียวกัน (มาก -> น้อย) แล้วเลือกมาเล่นพร้อมกันได้สูงสุด maxConcurrentVoices คลิป ที่เหลือไม่เล่น
            groups.Sort(DescendingByCount);
            int selectedCount = Mathf.Min(groups.Count, voices.Length);

            // เสียงซู่ซ่าหลายชนิดพร้อมกันจะรวมกันดังเกินไป จึงลดความดังต่อเสียงลงตามจำนวนเสียงที่ active พร้อมกัน แล้วคูณความร้อนของภาชนะ
            float loudnessScale = (selectedCount > 1 ? 1f / Mathf.Sqrt(selectedCount) : 1f) * heat;

            Array.Clear(voiceMatchedBuffer, 0, voiceMatchedBuffer.Length);
            Array.Clear(selectedConsumedBuffer, 0, selectedConsumedBuffer.Length);

            // Pass 1: voice ที่กำลังเล่น clip ซึ่งยังติด top-N อยู่ ให้เล่นต่อเนื่อง ไม่ตัดจบกลางคัน
            for (int v = 0; v < voices.Length; v++)
            {
                var voice = voices[v];
                if (voice.CurrentData == null) continue;

                int selIdx = -1;
                for (int s = 0; s < selectedCount; s++)
                {
                    if (!selectedConsumedBuffer[s] && groups[s].Data == voice.CurrentData)
                    {
                        selIdx = s;
                        break;
                    }
                }

                if (selIdx >= 0)
                {
                    selectedConsumedBuffer[selIdx] = true;
                    voiceMatchedBuffer[v] = true;
                    voice.TargetVolume = ComputeClipVolume(groups[selIdx].Count, loudnessScale);
                    voice.FadeSpeed = fadeInSpeed;
                }
                else
                {
                    voice.TargetVolume = 0f;
                    voice.FadeSpeed = fadeOutSpeed;
                }
            }

            // Pass 2: clip ที่เพิ่งติด top-N แต่ยังไม่มีเสียงเล่น ให้ยึด voice ที่ไม่ถูกจับคู่ใน Pass 1
            // (voice เหล่านี้คือตัวที่ว่างอยู่ หรือกำลังเฟดออกเพราะ clip เดิมหลุด top-N ไปแล้ว)
            int nextSelIdx = 0;
            for (int v = 0; v < voices.Length; v++)
            {
                if (voiceMatchedBuffer[v]) continue;

                while (nextSelIdx < selectedCount && selectedConsumedBuffer[nextSelIdx]) nextSelIdx++;
                if (nextSelIdx >= selectedCount) break;

                var voice = voices[v];
                var cc = groups[nextSelIdx];

                voice.Source.Stop();
                voice.Source.clip = cc.Data.SizzleSFX;
                voice.Source.pitch = cc.Data.SizzlePitch;
                voice.CurrentData = cc.Data;
                voice.TargetVolume = ComputeClipVolume(cc.Count, loudnessScale);
                voice.FadeSpeed = fadeInSpeed;

                selectedConsumedBuffer[nextSelIdx] = true;
                nextSelIdx++;
            }
        }

        private void AddToGroup(IngredientDataSO data)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Data == data)
                {
                    var group = groups[i];
                    group.Count++;
                    groups[i] = group;
                    return;
                }
            }
            groups.Add(new SizzleGroup { Data = data, Count = 1 });
        }

        private static float ComputeClipVolume(int count, float loudnessScale)
        {
            return Mathf.Clamp01((0.35f + count * 0.12f) * loudnessScale);
        }

        private void UpdateVolumeInterpolation()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = voices[i];
                var source = voice.Source;
                float currentVol = source.volume;

                // ยิง Play() ได้ก็ต่อเมื่อมีคลิปจริงเท่านั้น (source.clip == null ทำให้ isPlaying ไม่มีวันเป็น true
                // ถ้าไม่กันไว้ เงื่อนไขนี้จะเป็นจริงทุกเฟรมและเรียก Play() รัวๆ ไม่มีวันหยุด)
                if (voice.TargetVolume > 0.001f && source.clip != null && !source.isPlaying)
                {
                    source.volume = 0f;
                    currentVol = 0f;
                    source.Play();
                }

                if (!Mathf.Approximately(currentVol, voice.TargetVolume))
                {
                    source.volume = Mathf.MoveTowards(currentVol, voice.TargetVolume, voice.FadeSpeed * Time.deltaTime);
                }

                if (voice.TargetVolume <= 0.001f && source.volume <= 0.001f && source.isPlaying)
                {
                    // ไม่ล้าง clip ที่นี่ (เดิมเซ็ต null) เพราะถ้าสถานะกะพริบ (เช่นคีบวัตถุดิบขึ้น-ลง) จะเกิดการโหลด/ปล่อยคลิปรัวๆ โดยไม่จำเป็น
                    source.Pause();
                }
            }
        }
    }
}
