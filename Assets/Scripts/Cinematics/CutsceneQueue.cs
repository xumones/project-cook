using System;
using System.Collections.Generic;

namespace ProjectCook.Cinematics
{
    /// <summary>บัญชี cutscene ของผู้เล่น: คำขอที่ค้างรอเล่นตามลำดับ และรายการที่ดูจบแล้ว ไม่รู้จักฉากหรือเวลาจึง test ได้ตรงๆ</summary>
    // คำขออยู่จนกว่าจะเล่นจบจริง เพราะตัวกระตุ้นส่วนใหญ่เกิดครั้งเดียว ถ้าทิ้งไปตอนถูกขัด cutscene นั้นจะไม่มีวันได้เล่น
    public class CutsceneQueue
    {
        [Serializable]
        public class SaveData
        {
            public List<string> pending = new List<string>();
            public List<string> seen = new List<string>();
        }

        private readonly List<string> pending = new List<string>();
        private readonly HashSet<string> seen = new HashSet<string>();

        /// <summary>รหัส cutscene ที่รอเล่น เรียงตามลำดับที่ถูกขอ</summary>
        public IReadOnlyList<string> Pending => pending;

        /// <summary>เปิดคำขอ ไม่เปิดซ้ำถ้ายังค้างอยู่ หรือเป็นแบบเล่นครั้งเดียวที่ดูไปแล้ว</summary>
        public bool Request(CutsceneSO cutscene)
        {
            string id = cutscene.CutsceneId;
            if (pending.Contains(id) || (cutscene.PlayOnce && seen.Contains(id))) return false;

            pending.Add(id);
            return true;
        }

        /// <summary>เล่นจบแล้ว (ดูจบหรือข้าม) ปิดคำขอและจดว่าดูแล้ว</summary>
        public void Complete(CutsceneSO cutscene)
        {
            pending.Remove(cutscene.CutsceneId);
            seen.Add(cutscene.CutsceneId);
        }

        public SaveData Capture() => new SaveData { pending = new List<string>(pending), seen = new List<string>(seen) };

        public void Restore(SaveData data)
        {
            pending.Clear();
            pending.AddRange(data.pending);
            seen.Clear();
            seen.UnionWith(data.seen);
        }
    }
}
