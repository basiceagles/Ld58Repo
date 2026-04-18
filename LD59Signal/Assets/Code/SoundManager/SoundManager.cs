using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SmallHedge.SoundManager
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundsSO SO;
        private static SoundManager instance = null;
        private AudioSource audioSource;

        private void Awake()
        {
            if(!instance)
            {
                instance = this;
                audioSource = GetComponent<AudioSource>();
            }
        }

        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            SoundList soundList = instance.SO.sounds[(int)sound];
            AudioClip[] clips = soundList.sounds;
            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

            if (source)
            {
                source.outputAudioMixerGroup = soundList.mixer;
                source.clip = randomClip;
                source.volume = volume * soundList.volume;
                source.Play();
            }
            else
            {
                instance.audioSource.outputAudioMixerGroup = soundList.mixer;
                instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
            }
        }
        public static void StopSound(SoundType sound, AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            SoundList soundList = instance.SO.sounds[(int)sound];
            AudioClip[] clips = soundList.sounds;

            foreach (AudioClip clip in clips)
            {
                if (source.clip == clip)
                {
                    source.Stop();
                    return;
                }
            }
        }
    }

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [Range(0, 1)] public float volume;
        public AudioMixerGroup mixer;
        public AudioClip[] sounds;
    }
}