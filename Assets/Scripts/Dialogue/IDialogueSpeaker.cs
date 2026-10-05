using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.Dialogue
{
    /// <summary>คู่สนทนาที่ DialogueRunner คุยด้วย ให้ระบบบทสนทนาไม่ต้องรู้จัก NPC โดยตรง</summary>
    public interface IDialogueSpeaker
    {
        /// <summary>รหัสคงที่ของผู้พูด ระบบอื่นใช้อ้างถึง เช่น เควส "ไปคุยกับ"</summary>
        string SpeakerId { get; }

        LocalizedString DisplayName { get; }
        TMP_FontAsset Font { get; }
        Sprite Portrait { get; }

        /// <summary>ประโยคทักทายตอนเริ่มคุย คืน false ถ้าไม่มีให้ขึ้นเมนูเลย</summary>
        bool TryGetGreeting(out LocalizedString greeting);

        /// <summary>เติมตัวเลือกที่ทำได้ "ตอนนี้" ถูกเรียกใหม่ทุกครั้งที่กลับมาเมนู</summary>
        void CollectOptions(List<DialogueOption> options);
    }
}
