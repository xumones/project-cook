using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Core;
using ProjectCook.Dialogue;
using ProjectCook.Player;

namespace ProjectCook.Cinematics
{
    /// <summary>คุมมุมกล้องของบทสนทนาปกติ: สายตาผู้เล่นเป็นมุมหลัก ตอนผู้พูดตอบใช้มุมที่ตัวเลือกนั้นขอ เมนูค้างมุมเดิม จบคุยกลับสายตาผู้เล่น</summary>
    // ระบบบทสนทนาแค่ประกาศจังหวะและมุมที่เนื้อหาขอ ไม่รู้จักกล้อง ส่วน cutscene คุมกล้องเองผ่าน Timeline ตัวนี้จึงไม่ยุ่ง
    public class DialogueShotDirector : SceneSingleton<DialogueShotDirector>
    {
        [Tooltip("อยู่ในมุมหนึ่งอย่างน้อยกี่วินาทีก่อนตัดอีก กันภาพกะพริบเวลาผู้เล่นกดข้ามเร็ว")]
        [Min(0f)]
        [SerializeField] private float minShotTime = 1.5f;

        [Tooltip("Priority ของมุมที่ใช้อยู่ ต้องสูงกว่ากล้องผู้เล่น")]
        [SerializeField] private int shotPriority = 10;

        private readonly List<DialogueShotRig> rigs = new List<DialogueShotRig>();

        // มุมบนจอ (null = สายตาผู้เล่น) / มุมที่รอเวลาตัด
        private CinemachineCamera live;
        private CinemachineCamera pending;
        private bool hasPending;
        private float lastCutTime = float.NegativeInfinity;

        public void Register(DialogueShotRig rig) => rigs.Add(rig);

        public void Unregister(DialogueShotRig rig) => rigs.Remove(rig);

        private void OnEnable()
        {
            MessageBus<DialogueBeatStarted>.Subscribe(HandleBeat);
            MessageBus<DialogueEnded>.Subscribe(HandleEnded);
        }

        private void OnDisable()
        {
            MessageBus<DialogueBeatStarted>.Unsubscribe(HandleBeat);
            MessageBus<DialogueEnded>.Unsubscribe(HandleEnded);
        }

        private void Update()
        {
            if (hasPending && Time.unscaledTime - lastCutTime >= minShotTime) Cut(pending);
        }

        // ทักทายใช้มุมหลัก (สายตาผู้เล่น) อยู่แล้ว ส่วนเมนูค้างมุมเดิมให้เห็นหน้า NPC ระหว่างเลือก มุมจึงเปลี่ยนเฉพาะตอนผู้พูดตอบ
        private void HandleBeat(DialogueBeatStarted message)
        {
            if (message.Beat != DialogueBeat.Response) return;

            DialogueShotRig rig = rigs.Find(r => r.isActiveAndEnabled && r.UseInDialogue && r.SpeakerId == message.SpeakerId);
            Request(rig == null ? null : message.Shot switch
            {
                DialogueShot.CloseUp => rig.CloseUp,
                DialogueShot.Medium => rig.Medium,
                _ => null
            });
        }

        // จบคุยกลับสายตาผู้เล่นทันที ไม่รอเวลาขั้นต่ำ
        private void HandleEnded(DialogueEnded message)
        {
            if (live == null && !hasPending) return;

            Cut(null);
        }

        private void Request(CinemachineCamera shot)
        {
            if (shot == live)
            {
                hasPending = false;
                return;
            }

            // ออกจากสายตาผู้เล่นได้ทันที ส่วนการตัดออกจากมุมของ NPC ต้องรอให้ครบเวลาขั้นต่ำ
            if (live != null && Time.unscaledTime - lastCutTime < minShotTime)
            {
                pending = shot;
                hasPending = true;
                return;
            }
            Cut(shot);
        }

        private void Cut(CinemachineCamera shot)
        {
            hasPending = false;
            if (live != null) live.Priority = 0;
            else if (shot != null) CameraManager.Instance.RequestLookLock(this);

            live = shot;
            lastCutTime = Time.unscaledTime;
            if (shot == null)
            {
                // กลับสายตาผู้เล่น ปลดล็อกการหันที่ใส่ไว้ตอนออกจากสายตาผู้เล่น
                CameraManager.Instance.ReleaseLookLock(this);
                return;
            }

            shot.Priority = shotPriority;
        }
    }
}
