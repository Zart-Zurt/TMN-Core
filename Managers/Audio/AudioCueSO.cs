using UnityEngine;

namespace Core.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "Core/Audio/Audio Cue")]
    public class AudioCueSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] audioClips;
        [field: SerializeField] [field: Range(0f, 1f)] public float Volume { get; private set; } = 1f;
        [SerializeField] [Range(0.1f, 2f)] private float pitch = 1f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] [Range(0f, 0.5f)] private float pitchVariance = 0.1f;

        public AudioClip GetRandomClip()
        {
            if (audioClips == null || audioClips.Length == 0)
            {
                return null;
            }

            var randomIndex = Random.Range(0, audioClips.Length);
            return audioClips[randomIndex];
        }

        public float GetPitch()
        {
            if (!randomizePitch)
            {
                return pitch;
            }

            return pitch + Random.Range(-pitchVariance, pitchVariance);
        }
    }
}
