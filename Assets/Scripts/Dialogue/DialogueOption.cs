using System;
using UnityEngine.Localization;

namespace ProjectCook.Dialogue
{
    /// <summary>ตัวเลือกหนึ่งข้อในเมนูสนทนา ความสามารถของ NPC เป็นคนสร้างขึ้นมาตามสถานะตอนนั้น</summary>
    public class DialogueOption
    {
        public LocalizedString Label;

        /// <summary>คำตอบของ NPC หลังเลือก ปล่อย null ถ้าไม่ต้องตอบ อ่านหลัง OnSelected จึงตั้งค่าจากในนั้นได้</summary>
        public LocalizedString Response;

        public Action OnSelected;

        /// <summary>true = จบการคุยหลังตอบ แทนที่จะกลับไปเมนู</summary>
        public bool EndsDialogue;
    }
}
