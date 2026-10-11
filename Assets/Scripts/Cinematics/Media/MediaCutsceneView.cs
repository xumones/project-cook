using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ProjectCook.Cinematics
{
    /// <summary>ชั้นเต็มจอของ cutscene แบบ Video / Slides: พื้นดำ ภาพวิดีโอ หรือภาพนิ่ง แสดงอย่างเดียว ไม่อ่านปุ่ม</summary>
    // เสียงวิดีโอออกทาง AudioSource ที่ไม่สนการหยุดเสียงทั้งเกม เพราะ cutscene สื่อปิดเสียงเกมไว้ระหว่างเล่น
    [RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
    public class MediaCutsceneView : MonoBehaviour
    {
        [SerializeField] private RawImage videoImage;
        [SerializeField] private AspectRatioFitter videoFitter;

        [Tooltip("Image ที่ติ๊ก Preserve Aspect")]
        [SerializeField] private Image slideImage;

        private VideoPlayer player;
        private RenderTexture texture;

        public bool VideoFinished { get; private set; }

        private void Awake()
        {
            player = GetComponent<VideoPlayer>();
            var audioSource = GetComponent<AudioSource>();
            audioSource.ignoreListenerPause = true;

            player.playOnAwake = false;
            player.isLooping = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);
            player.loopPointReached += _ => VideoFinished = true;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            videoImage.gameObject.SetActive(false);
            slideImage.gameObject.SetActive(false);
        }

        public void PlayVideo(VideoClip clip)
        {
            VideoFinished = clip == null;
            if (clip == null) return;

            texture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            player.targetTexture = texture;
            player.clip = clip;
            videoImage.texture = texture;
            videoFitter.aspectRatio = (float)clip.width / clip.height;
            videoImage.gameObject.SetActive(true);
            player.Play();
        }

        /// <summary>null = พื้นดำ (หน้าที่มีแต่ข้อความ)</summary>
        public void ShowSlide(Sprite image)
        {
            slideImage.sprite = image;
            slideImage.gameObject.SetActive(image != null);
        }

        public void Hide()
        {
            player.Stop();
            player.clip = null;
            player.targetTexture = null;
            videoImage.texture = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
                texture = null;
            }
            gameObject.SetActive(false);
        }
    }
}
