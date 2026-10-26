using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectCook.AI;
using ProjectCook.Core;

namespace ProjectCook.NPC
{
    /// <summary>save ชีวิตของ NPC: มาตรวัด ความมั่นคง และตำแหน่ง แผนที่ทำค้างไม่ save โหลดแล้วคิดใหม่</summary>
    [RequireComponent(typeof(NPCCharacter))]
    [RequireComponent(typeof(AgentVitals))]
    [RequireComponent(typeof(AgentMover))]
    public class NPCLifeSave : MonoBehaviour, ISaveSection
    {
        // เปลี่ยนรูปแบบข้อมูลเมื่อไหร่ให้เพิ่มเลขนี้ แล้วแปลงข้อมูลเก่าใน Restore
        private const int Version = 1;

        [Serializable]
        private class SavedLife
        {
            public int version;
            public List<SavedMeter> meters;
            public float wellbeing;
            public Vector3 position;
            public float yaw;
        }

        private NPCCharacter character;
        private AgentVitals vitals;
        private AgentMover mover;

        private void Awake()
        {
            character = GetComponent<NPCCharacter>();
            vitals = GetComponent<AgentVitals>();
            mover = GetComponent<AgentMover>();
        }

        private void Start() => SaveService.Instance?.Register(this);

        private void OnDestroy() => SaveService.Instance?.Unregister(this);

        public string SectionKey => "npc-life:" + character.Profile.NpcId;

        public string Capture() => JsonUtility.ToJson(new SavedLife
        {
            version = Version,
            meters = vitals.Meters.Capture(),
            wellbeing = vitals.Wellbeing.Value,
            position = transform.position,
            yaw = transform.eulerAngles.y
        });

        public void Restore(string data)
        {
            var saved = JsonUtility.FromJson<SavedLife>(data);
            if (saved.meters != null) vitals.Meters.Restore(saved.meters);
            vitals.Wellbeing.Restore(saved.wellbeing);

            if (!mover.Warp(saved.position, Quaternion.Euler(0f, saved.yaw, 0f)))
            {
                Debug.LogWarning($"[NPCLifeSave] วาง {name} กลับที่ {saved.position} ไม่ได้ เพราะไม่อยู่บน NavMesh จึงอยู่ที่เดิม", this);
            }
        }
    }
}
