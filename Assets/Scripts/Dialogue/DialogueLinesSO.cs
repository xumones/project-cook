using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>ชุดคำพูดประจำตัวละคร: key → ประโยคที่ใช้แทน {key} ในบทสนทนา ทำให้ JSON โครงเดียวพูดต่างกันได้ตามผู้พูด</summary>
    [CreateAssetMenu(fileName = "Lines_", menuName = "ProjectCook/Dialogue/Dialogue Lines")]
    public class DialogueLinesSO : ScriptableObject
    {
        [System.Serializable]
        public class Line
        {
            [Tooltip("ชื่อ placeholder ไม่ต้องใส่ปีกกา เช่น greeting")]
            public string key;

            [TextArea(1, 3)]
            [Tooltip("ประโยคให้สุ่ม ใส่หลายอันจะไม่พูดซ้ำอันเดิมติดกัน")]
            public List<string> variants = new List<string>();

            [System.NonSerialized] public int lastIndex = -1;
        }

        [Tooltip("ชุดที่ใช้แทนเมื่อไม่มี key ในชุดนี้ ปกติชี้ไป Lines_Default")]
        [SerializeField] private DialogueLinesSO fallback;

        [SerializeField] private List<Line> lines = new List<Line>();

        private Dictionary<string, Line> lookup;
        private readonly StringBuilder builder = new StringBuilder();

        // แก้ใน Inspector ระหว่างเล่นแล้วให้ผลทันที
        private void OnValidate()
        {
            lookup = null;
        }

        /// <summary>แทน {key} ทุกตัวที่รู้จัก ตัวที่ไม่รู้จักคงไว้ให้ provider อื่นจัดการ</summary>
        public string ResolveText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;

            builder.Clear();
            int cursor = 0;
            while (cursor < text.Length)
            {
                int open = text.IndexOf('{', cursor);
                int close = open >= 0 ? text.IndexOf('}', open) : -1;
                if (close < 0)
                {
                    builder.Append(text, cursor, text.Length - cursor);
                    break;
                }

                builder.Append(text, cursor, open - cursor);
                string key = text.Substring(open + 1, close - open - 1);
                builder.Append(TryGetLine(key, out string line) ? line : text.Substring(open, close - open + 1));
                cursor = close + 1;
            }
            return builder.ToString();
        }

        private bool TryGetLine(string key, out string text)
        {
            if (lookup == null) BuildLookup();

            if (lookup.TryGetValue(key, out Line line) && line.variants.Count > 0)
            {
                text = PickVariant(line);
                return true;
            }

            if (fallback != null && fallback != this) return fallback.TryGetLine(key, out text);

            text = null;
            return false;
        }

        private static string PickVariant(Line line)
        {
            int count = line.variants.Count;
            int index = Random.Range(0, count);
            if (count > 1 && index == line.lastIndex) index = (index + 1) % count;

            line.lastIndex = index;
            return line.variants[index];
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<string, Line>(lines.Count);
            foreach (Line line in lines)
            {
                if (line == null || string.IsNullOrEmpty(line.key)) continue;

                if (!lookup.TryAdd(line.key, line))
                {
                    Debug.LogWarning($"[DialogueLinesSO] key ซ้ำ '{line.key}' ใน {name}", this);
                }
            }
        }
    }
}
