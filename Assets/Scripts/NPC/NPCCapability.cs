using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>ความสามารถหนึ่งอย่างของ NPC ใส่ component นี้บน NPC ตัวไหน ตัวนั้นก็ทำได้</summary>
    [RequireComponent(typeof(NPCCharacter))]
    public abstract class NPCCapability : MonoBehaviour
    {
        protected NPCCharacter Npc { get; private set; }

        protected virtual void Awake()
        {
            Npc = GetComponent<NPCCharacter>();
        }

        /// <summary>เติมตัวเลือกที่ทำได้ "ตอนนี้" ลงเมนู ถูกเรียกใหม่ทุกครั้งที่เมนูแสดง</summary>
        public abstract void BuildOptions(List<DialogueOption> options);
    }
}
