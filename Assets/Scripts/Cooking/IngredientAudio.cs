using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์ย่อยจัดการเสียง SFX และเสียงทอดซู่ซ่าของวัตถุดิบ
    /// </summary>
    [DisallowMultipleComponent]
    public class IngredientAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        private bool hasPlayedCookedSFX = false;
        private bool isManagedByPanCarrier = false;

        public AudioSource AudioSource => audioSource;

        public void Init(AudioSource source = null)
        {
            audioSource = source != null ? source : GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D Spatial Audio
            }
        }

        public void SetManagedByPanCarrier(bool managed)
        {
            isManagedByPanCarrier = managed;
            if (managed)
            {
                UpdateSizzleAudio(false, CookingState.Raw, null);
            }
        }

        public void UpdateSizzleAudio(bool isCooking, CookingState currentState, IngredientDataSO data)
        {
            if (audioSource == null || data == null || data.SizzleSFX == null) return;

            if (isCooking && currentState != CookingState.Burnt && !isManagedByPanCarrier)
            {
                if (audioSource.clip != data.SizzleSFX)
                {
                    audioSource.clip = data.SizzleSFX;
                    audioSource.loop = true;
                }

                if (!Mathf.Approximately(audioSource.volume, data.SFXVolume))
                {
                    audioSource.volume = data.SFXVolume;
                }

                if (!Mathf.Approximately(audioSource.pitch, data.SizzlePitch))
                {
                    audioSource.pitch = data.SizzlePitch;
                }

                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource.isPlaying && audioSource.clip == data.SizzleSFX)
                {
                    audioSource.Stop();
                }
            }
        }

        public void PlayDropSFX(IngredientDataSO data)
        {
            if (data != null && data.DropSFX != null && audioSource != null)
            {
                float randomOffset = Random.Range(-data.PitchRandomness, data.PitchRandomness);
                audioSource.pitch = 1f + randomOffset;
                audioSource.PlayOneShot(data.DropSFX, data.SFXVolume);
            }
        }

        public void PlayCookedSFX(IngredientDataSO data)
        {
            if (!hasPlayedCookedSFX && data != null && data.CookedDoneSFX != null && audioSource != null)
            {
                hasPlayedCookedSFX = true;
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(data.CookedDoneSFX, data.SFXVolume);
            }
        }
    }
}
