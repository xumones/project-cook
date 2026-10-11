using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;

namespace ProjectCook.Cinematics
{
    /// <summary>จุดตั้งฉากของ cutscene หนึ่งอันในฉากพื้นที่: Timeline กล้อง และนักแสดงของฉากนั้น</summary>
    // Brain อยู่ในฉาก Core ซึ่ง Timeline อ้างล่วงหน้าไม่ได้ จึงผูกแถวกล้องตอนเริ่มเล่นแทน
    [RequireComponent(typeof(PlayableDirector))]
    public class CutsceneStage : MonoBehaviour
    {
        private const double EdgeTolerance = 0.0001;

        [SerializeField] private CutsceneSO cutscene;

        [Tooltip("ย้ายผู้เล่นมาที่นี่ตอนจบ ว่าง = อยู่ที่เดิม ถ้าตั้งไว้ ตอนออกจะเป็นจอดำเสมอ")]
        [SerializeField] private Transform playerEndPoint;

        // แคชไว้ตอนลงทะเบียน จะได้ถอนได้แม้ผู้จัดคิวถูกทำลายก่อน
        private CutsceneScheduler scheduler;

        public CutsceneSO Cutscene => cutscene;
        public PlayableDirector Director { get; private set; }
        public Transform PlayerEndPoint => playerEndPoint;

        /// <summary>กล้องของ shot ที่เริ่มตั้งแต่ต้น Timeline null = ต้นเรื่องเป็นมุมสายตาผู้เล่น</summary>
        public CinemachineVirtualCameraBase FirstShot => FindEdgeShot(atStart: true);

        /// <summary>กล้องของ shot ที่ยาวถึงท้าย Timeline null = ท้ายเรื่องเป็นมุมสายตาผู้เล่น</summary>
        public CinemachineVirtualCameraBase LastShot => FindEdgeShot(atStart: false);

        private void Awake() => Director = GetComponent<PlayableDirector>();

        private void Start()
        {
            scheduler = CutsceneScheduler.Instance;
            scheduler?.Register(this);
        }

        private void OnDestroy() => scheduler?.Unregister(this);

        public void BindBrain(CinemachineBrain brain)
        {
            foreach (PlayableBinding output in Director.playableAsset.outputs)
            {
                if (output.sourceObject is CinemachineTrack) Director.SetGenericBinding(output.sourceObject, brain);
            }
        }

        private CinemachineVirtualCameraBase FindEdgeShot(bool atStart)
        {
            if (Director.playableAsset is not TimelineAsset timeline) return null;

            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (track is not CinemachineTrack || track.muted) continue;

                foreach (TimelineClip clip in track.GetClips())
                {
                    bool onEdge = atStart ? clip.start <= EdgeTolerance : clip.end >= timeline.duration - EdgeTolerance;
                    if (onEdge && clip.asset is CinemachineShot shot) return shot.VirtualCamera.Resolve(Director);
                }
            }
            return null;
        }

        // Timeline ห้ามเล่นเองตอนโหลดฉาก ต้องรอผู้จัดคิวสั่ง
        private void OnValidate() => GetComponent<PlayableDirector>().playOnAwake = false;
    }
}
