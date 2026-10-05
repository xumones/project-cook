using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace ProjectCook.NPC
{
    /// <summary>สุ่มประโยคจากชุดประโยคโดยไม่พูดอันเดิมติดกัน จำอันล่าสุดแยกต่อชุด เป็นของ NPC แต่ละตัว ไม่เก็บใน asset</summary>
    public class LinePicker
    {
        private readonly Dictionary<NPCProfileSO.LineSet, int> lastPicked = new Dictionary<NPCProfileSO.LineSet, int>();

        public bool TryPick(NPCProfileSO.LineSet lines, out LocalizedString line)
        {
            int count = lines != null ? lines.Count : 0;
            if (count == 0)
            {
                line = null;
                return false;
            }

            int index = Random.Range(0, count);
            if (count > 1 && lastPicked.TryGetValue(lines, out int last) && index == last) index = (index + 1) % count;

            lastPicked[lines] = index;
            line = lines[index];
            return true;
        }
    }
}
