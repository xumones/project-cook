using UnityEngine;
using Unity.Cinemachine;
using ProjectCook.Core;
using ProjectCook.Dialogue;

namespace ProjectCook.Cinematics
{
    /// <summary>มุมกล้องประจำตัวผู้พูดสำหรับบทสนทนาปกติ วางเป็นลูกของ NPC ระหว่างคุยจะค้างทิศหันหาผู้เล่นไว้ ไม่หมุนตามตัว NPC</summary>
    // กล้องทุกตัวตั้ง Priority 0 ไว้ DialogueShotDirector เป็นคนยกขึ้นเอง ชื่อ GameObject ต้องตรงกับในไฟล์ blends (ตัดภาพ)
    // ทำงานก่อน CinemachineBrain ใน LateUpdate กล้องจึงอ่านทิศที่ล็อกแล้วในเฟรมเดียวกัน
    [DefaultExecutionOrder(-100)]
    public class DialogueShotRig : MonoBehaviour
    {
        [Tooltip("ปิด = คุยกับ NPC นี้ใช้สายตาผู้เล่นตลอด เช่น ร้านค้าที่คุยบ่อย")]
        [SerializeField] private bool useInDialogue = true;

        [Tooltip("มุมใกล้หน้า ชื่อ GameObject: DialogueShot_CloseUp")]
        [SerializeField] private CinemachineCamera closeUp;

        [Tooltip("มุมครึ่งตัว ชื่อ GameObject: DialogueShot_Medium")]
        [SerializeField] private CinemachineCamera medium;

        // แคชไว้ตอนลงทะเบียน จะได้ถอนได้แม้ director ถูกทำลายก่อน
        private DialogueShotDirector director;

        private bool locked;
        private Quaternion lockedRotation;
        private Quaternion restLocalRotation;

        public bool UseInDialogue => useInDialogue;
        public CinemachineCamera CloseUp => closeUp;
        public CinemachineCamera Medium => medium;
        public string SpeakerId { get; private set; }

        private void Start()
        {
            SpeakerId = GetComponentInParent<IDialogueSpeaker>()?.SpeakerId;
            director = DialogueShotDirector.Instance;
            director?.Register(this);
        }

        private void OnDestroy() => director?.Unregister(this);

        private void OnEnable()
        {
            MessageBus<DialogueStarted>.Subscribe(HandleStarted);
            MessageBus<DialogueEnded>.Subscribe(HandleEnded);
        }

        private void OnDisable()
        {
            MessageBus<DialogueStarted>.Unsubscribe(HandleStarted);
            MessageBus<DialogueEnded>.Unsubscribe(HandleEnded);
        }

        // ตัว NPC อาจกำลังหมุนหาผู้เล่น (NPCFacing) ถ้ากล้องหมุนตามภาพจะวน จึงค้างทิศไว้ทุกเฟรม
        private void LateUpdate()
        {
            if (locked) transform.rotation = lockedRotation;
        }

        // หันชุดกล้องไปทางผู้เล่น (แนวนอน) มุมทั้งหมดจึงอยู่ฝั่งผู้เล่นตามกฎ 180 องศาเสมอ
        private void HandleStarted(DialogueStarted message)
        {
            if (message.SpeakerId != SpeakerId || Camera.main == null) return;

            Vector3 toPlayer = Camera.main.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            restLocalRotation = transform.localRotation;
            lockedRotation = Quaternion.LookRotation(toPlayer);
            transform.rotation = lockedRotation;
            locked = true;
        }

        private void HandleEnded(DialogueEnded message)
        {
            if (!locked || message.SpeakerId != SpeakerId) return;

            locked = false;
            transform.localRotation = restLocalRotation;
        }
    }
}
