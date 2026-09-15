using UnityEngine;

namespace ProjectCook.Stations
{
    /// <summary>แผ่นเพลงหนึ่งใบ เพิ่มเพลงใหม่ = สร้าง asset นี้แล้วลากใส่ DiscPlayer</summary>
    [CreateAssetMenu(fileName = "Disc_", menuName = "ProjectCook/Audio/Disc")]
    public class DiscSO : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private AudioClip clip;
        [Tooltip("ปกแผ่น เว้นว่างได้ UI จะใช้ placeholder")]
        [SerializeField] private Sprite cover;

        public string Title => title;
        public AudioClip Clip => clip;
        public Sprite Cover => cover;
    }
}
