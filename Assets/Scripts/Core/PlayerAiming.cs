using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ยิง Raycast เล็งเส้นเดียวต่อเฟรม แล้วเปิดผลให้ทุกระบบที่สนใจอ่านร่วมกัน</summary>
    // รันก่อนระบบอื่นเพื่อให้ผลที่อ่านได้ในเฟรมนั้นเป็นของเฟรมปัจจุบัน ไม่ใช่ของเฟรมก่อน
    [DefaultExecutionOrder(-100)]
    public class PlayerAiming : SceneSingleton<PlayerAiming>
    {
        [Header("Aiming")]
        [Tooltip("จุดเริ่ม Raycast เว้นว่าง = กล้องหลัก")]
        [SerializeField] private Transform aimOrigin;

        [Tooltip("ระยะเอื้อมสูงสุดของผู้เล่น (เมตร) ระบบที่ต้องการระยะสั้นกว่านี้ให้เช็ค Hit.distance เอง")]
        [SerializeField] private float maxDistance = 2.5f;

        [Tooltip("Layer ที่เล็งโดนได้ ต้องกว้างพอให้หาพื้นผิวสำหรับวางของได้ด้วย เว้นว่าง = ทุกอย่างที่เป็นของแข็ง")]
        [SerializeField] private LayerMask aimMask;

        /// <summary>เป้าที่เล็งอยู่ตอนนี้ null = ไม่ได้เล็งโดนอะไร</summary>
        public Collider Target { get; private set; }

        /// <summary>ผล Raycast ล่าสุด อ่านได้เมื่อ Target ไม่เป็น null</summary>
        public RaycastHit Hit => hit;

        /// <summary>เส้นเล็งของเฟรมนี้ ใช้หาทิศที่ผู้เล่นหันอยู่แม้ตอนเล็งไม่โดนอะไร</summary>
        public Ray AimRay { get; private set; }

        private RaycastHit hit;
        private Camera cachedCamera;
        private int queryMask;

        protected override void Awake()
        {
            base.Awake();

            // กว้างไว้ก่อน เพราะเส้นนี้ถูกใช้หาพื้นผิวสำหรับวางของด้วย ไม่ใช่แค่หาของที่เล็งอยู่
            // ระบบปลายทางกรองซ้ำด้วย component เองอยู่แล้ว Layer จึงไม่ต้องแคบ
            if (aimMask.value == 0)
            {
                aimMask = Physics.DefaultRaycastLayers;
            }

            // ของที่ถืออยู่ห่างกล้องแค่ครึ่งเมตร ถ้าไม่ตัดออกมันจะบังทุกอย่างที่อยู่ข้างหลัง
            int heldItem = LayerMask.NameToLayer(GameLayers.HeldItem);
            queryMask = heldItem >= 0 ? aimMask.value & ~(1 << heldItem) : aimMask.value;

            if (queryMask == 0)
            {
                Debug.LogError($"[PlayerAiming] Aim Mask ไม่เหลือ Layer ให้เล็งเลย " +
                               $"(ตั้งไว้เป็น '{GameLayers.HeldItem}' ซึ่งเป็น Layer ชั่วคราวตอนถืออยู่ ไม่ใช่ Layer ที่ของวางอยู่ในฉาก) " +
                               $"ปล่อยว่างไว้เพื่อใช้ค่าเริ่มต้นจะปลอดภัยกว่า", this);
            }
        }

        private void Update()
        {
            Transform origin = GetAimOrigin();
            if (origin == null)
            {
                Target = null;
                return;
            }

            // เคอร์เซอร์ปลดล็อกอยู่ (เช่น สถานีจัดจาน) ให้เล็งตามเมาส์แทนกลางจอ
            Camera aimCamera = origin.GetComponent<Camera>();
            AimRay = aimCamera != null && CursorManager.TryGetPointerPosition(out Vector2 pointer)
                ? aimCamera.ScreenPointToRay(pointer)
                : new Ray(origin.position, origin.forward);
            Target = Physics.Raycast(AimRay, out hit, maxDistance, queryMask, QueryTriggerInteraction.Ignore)
                ? hit.collider
                : null;
        }

        private Transform GetAimOrigin()
        {
            if (aimOrigin != null) return aimOrigin;

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }
            return cachedCamera != null ? cachedCamera.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = aimOrigin != null ? aimOrigin : (Camera.main != null ? Camera.main.transform : transform);
            Gizmos.color = Target != null ? Color.red : Color.green;
            Gizmos.DrawRay(origin.position, origin.forward * maxDistance);
        }
    }
}
