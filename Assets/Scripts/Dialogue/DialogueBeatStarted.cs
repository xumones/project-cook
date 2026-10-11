namespace ProjectCook.Dialogue
{
    /// <summary>จังหวะของบทสนทนาปกติ ใช้ตัดสินมุมกล้อง</summary>
    public enum DialogueBeat
    {
        /// <summary>ผู้พูดทักทายตอนเริ่มคุย</summary>
        Greeting,
        /// <summary>ผู้พูดตอบตัวเลือกที่ผู้เล่นเลือก</summary>
        Response,
        /// <summary>ผู้เล่นกำลังเลือกตัวเลือก</summary>
        Menu
    }

    /// <summary>มุมกล้องที่เนื้อหาขอ Player = สายตาผู้เล่น (มุมหลัก หันกล้องได้) มุมอื่นต้องมีชุดกล้องที่ตัวผู้พูด</summary>
    public enum DialogueShot
    {
        Player,
        CloseUp,
        Medium
    }

    /// <summary>ข่าว: บทสนทนากับผู้พูดคนนี้เข้าจังหวะใหม่ ไม่ส่งสำหรับบทพูดใน cutscene</summary>
    public readonly struct DialogueBeatStarted
    {
        public readonly string SpeakerId;
        public readonly DialogueBeat Beat;

        /// <summary>มุมที่เนื้อหาของจังหวะนี้ขอ</summary>
        public readonly DialogueShot Shot;

        public DialogueBeatStarted(string speakerId, DialogueBeat beat, DialogueShot shot)
        {
            SpeakerId = speakerId;
            Beat = beat;
            Shot = shot;
        }
    }
}
