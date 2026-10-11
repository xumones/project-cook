using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ProjectCook.Dialogue;
using ProjectCook.NPC;

namespace ProjectCook.Cinematics
{
    /// <summary>บทพูดหนึ่งบรรทัดบนแถวบทพูด: ขึ้นตอน clip เริ่ม และถ้าติ๊กรอ Timeline จะค้างตอน clip จบจนผู้เล่นกดไปต่อ</summary>
    public class DialogueLineClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("ผู้พูด ว่าง = ไม่แสดงชื่อและรูป")]
        [SerializeField] private NPCProfileSO speaker;

        [SerializeField] private LocalizedString line;

        [Tooltip("ติ๊ก = ถ้าผู้เล่นยังไม่กดไปต่อตอน clip จบ Timeline จะค้างรอ ไม่ติ๊ก = บรรทัดหายเองตอน clip จบ")]
        [SerializeField] private bool waitForAdvance = true;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DialogueLineBehaviour>.Create(graph);
            DialogueLineBehaviour behaviour = playable.GetBehaviour();
            behaviour.Speaker = speaker;
            behaviour.Line = line;
            behaviour.WaitForAdvance = waitForAdvance;
            return playable;
        }
    }

    /// <summary>ตัวทำงานของ DialogueLineClip ส่งต่อจังหวะเริ่มและจบของ clip ให้ CutscenePlayer</summary>
    public class DialogueLineBehaviour : PlayableBehaviour
    {
        public ISpeakerIdentity Speaker;
        public LocalizedString Line;
        public bool WaitForAdvance;

        // Timeline เรียก OnBehaviourPause ตอนเริ่มเล่นด้วยแม้ clip ยังไม่ถึง จึงจบเฉพาะบรรทัดที่ขึ้นไปแล้ว
        private bool shown;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying || shown || CutscenePlayer.Instance == null) return;

            shown = true;
            CutscenePlayer.Instance.ShowLine(this);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!shown) return;

            shown = false;
            CutscenePlayer.Instance?.EndLine(this);
        }
    }
}
