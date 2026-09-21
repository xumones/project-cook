using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectCook.Stations
{
    /// <summary>หน้าจอเครื่องเล่นแผ่น: ปกกลางจอ + แถวปุ่มด้านล่าง แสดงเฉพาะตอนอยู่ในสถานี (เคอร์เซอร์เป็นหน้าที่ของ Station)</summary>
    public class DiscPlayerUI : MonoBehaviour, IStationModule
    {
        [Header("Source")]
        [SerializeField] private DiscPlayer discPlayer;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image coverImage;
        [Tooltip("ปกที่ใช้เมื่อแผ่นไม่มีรูป")]
        [SerializeField] private Sprite placeholderCover;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Buttons")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button nextButton;

        [Header("Play/Pause Icon")]
        [SerializeField] private Image playPauseIcon;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;

        private void Awake()
        {
            panel.SetActive(false);

            previousButton.onClick.AddListener(() => discPlayer.Previous());
            playPauseButton.onClick.AddListener(() => discPlayer.TogglePlayPause());
            nextButton.onClick.AddListener(() => discPlayer.Next());
        }

        public void OnStationEnter()
        {
            discPlayer.StateChanged += Refresh;
            Refresh();
            panel.SetActive(true);
        }

        public void OnStationExit()
        {
            discPlayer.StateChanged -= Refresh;
            panel.SetActive(false);
        }

        private void Refresh()
        {
            DiscSO disc = discPlayer.CurrentDisc;

            coverImage.sprite = disc != null && disc.Cover != null ? disc.Cover : placeholderCover;
            titleText.text = disc != null ? disc.Title : string.Empty;

            // แผ่นเดียวสลับไม่ได้ ปุ่มยังแสดงให้รู้ว่าจะมีต่อในอนาคต
            bool canSkip = discPlayer.DiscCount > 1;
            previousButton.interactable = canSkip;
            nextButton.interactable = canSkip;

            playPauseButton.interactable = disc != null;
            playPauseIcon.sprite = discPlayer.IsPlaying ? pauseSprite : playSprite;
        }
    }
}
