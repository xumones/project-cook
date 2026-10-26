using UnityEngine;
using UnityEngine.AI;

namespace ProjectCook.AI
{
    public enum MoveStatus
    {
        Idle,
        Moving,
        Arrived,
        Failed
    }

    /// <summary>ขาของตัวละคร: สั่งเดินไปจุดบน NavMesh หยุด หรือย้ายไปทันที แล้วถามสถานะการเดินได้</summary>
    // ยังไม่มีท่าเดิน ตัวละครจึงเลื่อนไปด้วยท่ายืน ท่าเดินมาต่อทีหลังโดยอ่านความเร็วของ NavMeshAgent
    // ตอนเดิน NavMeshAgent เป็นคนหมุนตัว ระบบอื่นที่หมุนตัวต้องรอให้หยุดก่อน
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentMover : MonoBehaviour
    {
        [Tooltip("ระยะหาจุดบน NavMesh ที่ใกล้จุดที่สั่งที่สุด (เมตร) ไกลกว่านี้ถือว่าไปไม่ได้")]
        [Min(0.1f)]
        [SerializeField] private float sampleRadius = 1f;

        private NavMeshAgent agent;
        private NavMeshPath path;
        private bool hasDestination;
        private float baseSpeed;

        /// <summary>คูณความเร็วเดินจากที่ตั้งใน NavMeshAgent เช่น รีบ = มากกว่า 1</summary>
        public void SetSpeedMultiplier(float multiplier) => agent.speed = baseSpeed * multiplier;

        public MoveStatus Status
        {
            get
            {
                if (!hasDestination) return MoveStatus.Idle;
                if (!agent.isOnNavMesh) return MoveStatus.Failed;
                if (agent.pathPending) return MoveStatus.Moving;

                // ทางขาดกลางทาง (เช่น มีของมาขวาง) ตัวละครจะไปหยุดที่ปลายทางที่ไปได้ ซึ่งไม่ใช่จุดหมาย
                if (agent.pathStatus != NavMeshPathStatus.PathComplete) return MoveStatus.Failed;
                return agent.remainingDistance <= agent.stoppingDistance ? MoveStatus.Arrived : MoveStatus.Moving;
            }
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            path = new NavMeshPath();
            baseSpeed = agent.speed;
        }

        // ฉากถูกซ่อน (เช่น ผู้เล่นออกไปข้างนอก) ทางที่ค้างไว้หายไปกับ agent
        private void OnDisable() => hasDestination = false;

        /// <summary>เดินไปจุดที่ใกล้ point ที่สุดบน NavMesh false = ไปไม่ถึง และไม่มีอะไรเปลี่ยน</summary>
        public bool TryMoveTo(Vector3 point)
        {
            if (!agent.isOnNavMesh || !NavMesh.SamplePosition(point, out NavMeshHit hit, sampleRadius, agent.areaMask)) return false;
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;

            hasDestination = agent.SetPath(path);
            return hasDestination;
        }

        public void Stop()
        {
            hasDestination = false;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        /// <summary>ย้ายไปทันทีโดยไม่เดิน ต้องใช้แทนการตั้ง transform.position ไม่งั้นตัวละครหลุดจาก NavMesh</summary>
        public bool Warp(Vector3 position, Quaternion rotation)
        {
            Stop();
            if (!agent.Warp(position)) return false;

            transform.rotation = rotation;
            return true;
        }
    }
}
