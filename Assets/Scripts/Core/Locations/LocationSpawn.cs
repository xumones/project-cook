using UnityEngine;

namespace ProjectCook.Core
{
    /// <summary>จุดเกิดของผู้เล่นในพื้นที่ รหัสว่าง = จุดเกิดหลัก ประตูระบุรหัสเพื่อไปโผล่ตรงประตูฝั่งตรงข้าม</summary>
    public class LocationSpawn : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        public string SpawnId => spawnId;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.3f);
        }
    }
}
