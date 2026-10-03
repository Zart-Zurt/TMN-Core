using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;

namespace Core.Audio
{
    [AddComponentMenu("Core/Audio/Audio Manager")]
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        public void PlaySfx(AudioCueSO audioCue)
        {
            if (audioCue == null)
            {
                return;
            }

            var clip = audioCue.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            if (sfxSource != null)
            {
                sfxSource.pitch = audioCue.GetPitch();
                sfxSource.PlayOneShot(clip, audioCue.Volume);
            }
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (musicClip == null || musicSource == null)
            {
                return;
            }

            musicSource.clip = musicClip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
    }
}
