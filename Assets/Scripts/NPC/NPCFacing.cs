using UnityEngine;
using ProjectCook.AI;
using ProjectCook.Core;
using ProjectCook.Dialogue;

namespace ProjectCook.NPC
{
    /// <summary>หันทั้งตัวมาหาผู้เล่นตอนเริ่มคุย แล้วหันกลับท่าเดิมเมื่อผู้เล่นกลับไปเดินสำรวจ</summary>
    // หันกลับตอนกลับโหมด Exploring ไม่ใช่ตอนคุยจบ สิ่งที่เปิดต่อจากบทสนทนา (เช่นร้านค้า) จึงยังเห็น NPC หันหาอยู่
    // กล้องมุมบทสนทนาเป็นลูกของ NPC พอ NPC หันหาผู้เล่น ทุกมุมจึงอยู่ฝั่งผู้เล่นตามกฎ 180 องศาเอง
    // NPC ที่เดินเองได้ ถ้าออกเดินระหว่างหันกลับ ให้ AgentMover หมุนตามทางเดินแทน
    [RequireComponent(typeof(NPCCharacter))]
    public class NPCFacing : MonoBehaviour
    {
        [Tooltip("ความเร็วในการหัน (องศาต่อวินาที)")]
        [Min(1f)]
        [SerializeField] private float turnSpeed = 360f;

        private NPCCharacter character;
        private AgentMover mover;
        private Quaternion restRotation;
        private Quaternion targetRotation;
        private bool facingPlayer;
        private bool turning;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private GameModeManager gameMode;

        private void Awake()
        {
            character = GetComponent<NPCCharacter>();
            mover = GetComponent<AgentMover>();
        }

        private void OnEnable() => MessageBus<DialogueStarted>.Subscribe(HandleStarted);

        private void OnDisable() => MessageBus<DialogueStarted>.Unsubscribe(HandleStarted);

        private void Start()
        {
            gameMode = GameModeManager.Instance;
            if (gameMode != null) gameMode.ModeChanged += HandleModeChanged;
        }

        private void OnDestroy()
        {
            if (gameMode != null) gameMode.ModeChanged -= HandleModeChanged;
        }

        private void Update()
        {
            if (!turning) return;

            if (!facingPlayer && mover != null && mover.Status == MoveStatus.Moving)
            {
                turning = false;
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            turning = transform.rotation != targetRotation;
        }

        private void HandleStarted(DialogueStarted message)
        {
            if (message.SpeakerId != character.SpeakerId || Camera.main == null) return;

            // หันเฉพาะแนวนอน ตัว NPC ไม่เอียงตามความสูงของกล้อง
            Vector3 toPlayer = Camera.main.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f) return;

            if (!facingPlayer) restRotation = transform.rotation;
            targetRotation = Quaternion.LookRotation(toPlayer);
            facingPlayer = true;
            turning = true;
        }

        private void HandleModeChanged(GameMode mode)
        {
            if (!facingPlayer || mode != GameMode.Exploring) return;

            facingPlayer = false;
            targetRotation = restRotation;
            turning = true;
        }
    }
}
