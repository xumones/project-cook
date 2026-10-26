using UnityEditor;
using UnityEngine;
using ProjectCook.NPC;

namespace ProjectCook.Editor
{
    /// <summary>ตอนเล่น แสดงบรรทัดสรุปว่า NPC กำลังทำอะไรเพราะอะไร ใต้ช่องตั้งค่าของ NPCBrain</summary>
    [CustomEditor(typeof(NPCBrain))]
    public class NPCBrainEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(((NPCBrain)target).Summary, MessageType.None);
        }
    }
}
