using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource sfxAS;
    public static AudioManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySFX(AudioClip sfx, float volume = 1, float pitch = 1)
    {
        sfxAS.pitch = pitch;
        sfxAS.PlayOneShot(sfx, volume);
    }
}
