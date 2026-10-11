using UnityEngine.Timeline;

namespace ProjectCook.Cinematics
{
    /// <summary>แถวบทพูดใน Timeline ไม่ต้องผูกกับอะไร บทพูดแสดงผ่านหน้าต่างคุยใน Core ตอนเล่น</summary>
    [TrackColor(0.35f, 0.55f, 0.85f)]
    [TrackClipType(typeof(DialogueLineClip))]
    public class DialogueLineTrack : TrackAsset
    {
    }
}
