using UnityEditor;
using UnityEngine;
using ProjectCook.AI;

namespace ProjectCook.Editor
{
    /// <summary>ตอนเล่น แสดงบรรทัดสรุปว่าตัวละครกำลังทำอะไรเพราะอะไร ใต้ช่องตั้งค่าของ AgentBrain</summary>
    [CustomEditor(typeof(AgentBrain))]
    public class AgentBrainEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(((AgentBrain)target).Summary, MessageType.None);
        }
    }
}
