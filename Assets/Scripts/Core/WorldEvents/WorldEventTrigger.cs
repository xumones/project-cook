using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>ข่าว: ผู้เล่นไปถึงจุดเกิดเหตุการณ์ในโลก</summary>
    public readonly struct WorldEventTriggered
    {
        public readonly string EventId;

        public WorldEventTriggered(string eventId)
        {
            EventId = eventId;
        }
    }

    /// <summary>กล่อง trigger ในฉาก ผู้เล่นเดินเข้าแล้วส่งข่าวเหตุการณ์ ใช้เริ่มเควสหรือนับเป้าหมาย "ไปถึงจุด"</summary>
    [RequireComponent(typeof(Collider))]
    public class WorldEventTrigger : MonoBehaviour
    {
        [Tooltip("รหัสเหตุการณ์ที่เควสอ้างถึง")]
        [SerializeField] private string eventId;

        [Tooltip("ส่งครั้งเดียวต่อการโหลดพื้นที่ ถ้าไม่ติ๊ก ส่งทุกครั้งที่เดินเข้า")]
        [SerializeField] private bool once = true;

        private bool fired;

        private void OnTriggerEnter(Collider other)
        {
            if (fired && once) return;
            if (other.gameObject.layer != LayerMask.NameToLayer(GameLayers.Player)) return;

            fired = true;
            MessageBus<WorldEventTriggered>.Publish(new WorldEventTriggered(eventId));
        }

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
