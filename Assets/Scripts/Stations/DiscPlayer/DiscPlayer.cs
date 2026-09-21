using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Stations
{
    /// <summary>เครื่องเล่นแผ่นในฉาก เป็นเจ้าของ BGM: เล่น/หยุด/สลับแผ่น และเปลี่ยนเสียงเป็น 2D ระหว่างผู้เล่นใช้งานอยู่</summary>
    [RequireComponent(typeof(AudioSource))]
    public class DiscPlayer : MonoBehaviour, IStationModule
    {
        [Header("Discs")]
        [Tooltip("แผ่นทั้งหมดที่เครื่องนี้มี ใบแรกเล่นตอนเริ่มเกม")]
        [SerializeField] private List<DiscSO> discs = new List<DiscSO>();
        [SerializeField] private bool playOnStart = true;

        [Header("Spatial Blend")]
        [Tooltip("ระยะเวลาไล่เสียงจาก 3D เป็น 2D ตอนเข้าใช้งานและกลับตอนออก (วินาที)")]
        [SerializeField] private float blendDuration = 0.3f;

        private AudioSource source;
        private int currentIndex;
        private bool isPaused;

        // ค่า 3D เดิมจาก Inspector ใช้เป็นปลายทางตอนออกจากสถานี
        private float worldSpatialBlend;
        private float targetSpatialBlend;

        /// <summary>แจ้งเมื่อแผ่นเปลี่ยนหรือสถานะเล่น/หยุดเปลี่ยน</summary>
        public event Action StateChanged;

        public DiscSO CurrentDisc => discs.Count > 0 ? discs[currentIndex] : null;
        public int DiscCount => discs.Count;
        public bool IsPlaying => source.isPlaying;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;

            worldSpatialBlend = source.spatialBlend;
            targetSpatialBlend = worldSpatialBlend;
        }

        private void Start()
        {
            if (discs.Count == 0)
            {
                Debug.LogWarning($"[DiscPlayer] {name} ไม่มีแผ่นเลย จะไม่มีเพลงเล่น", this);
                return;
            }

            LoadDisc(0);
            if (playOnStart)
            {
                source.Play();
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(source.spatialBlend, targetSpatialBlend)) return;

            float step = blendDuration > 0f ? Time.deltaTime / blendDuration : 1f;
            source.spatialBlend = Mathf.MoveTowards(source.spatialBlend, targetSpatialBlend, step);
        }

        public void TogglePlayPause()
        {
            if (CurrentDisc == null) return;

            if (source.isPlaying)
            {
                source.Pause();
                isPaused = true;
            }
            else if (isPaused)
            {
                source.UnPause();
                isPaused = false;
            }
            else
            {
                source.Play();
            }

            StateChanged?.Invoke();
        }

        public void Next() => Skip(1);

        public void Previous() => Skip(-1);

        private void Skip(int direction)
        {
            if (discs.Count < 2) return;

            bool wasPlaying = source.isPlaying;
            LoadDisc((currentIndex + direction + discs.Count) % discs.Count);
            if (wasPlaying)
            {
                source.Play();
            }

            StateChanged?.Invoke();
        }

        private void LoadDisc(int index)
        {
            currentIndex = index;
            isPaused = false;
            source.Stop();
            source.clip = discs[index] != null ? discs[index].Clip : null;
        }

        public void OnStationEnter()
        {
            targetSpatialBlend = 0f;
        }

        public void OnStationExit()
        {
            targetSpatialBlend = worldSpatialBlend;
        }
    }
}
