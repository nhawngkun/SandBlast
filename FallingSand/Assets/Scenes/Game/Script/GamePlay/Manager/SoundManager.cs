using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    // Start is called before the first frame update

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // Cho background music
    [SerializeField] private AudioSource sfxSource;   // Cho sound effects

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip[] VFXSound;

    [Header("Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.3f;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.5f;
    public bool TurnOn = true;


    public override void Awake()
    {
        base.Awake();
        InitializeAudio();
        TurnOn = true;
    }
    private void Update()
    {
        if (TurnOn)
        {
            // Thiết lập volume
            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
        }
        else
        {
            musicSource.volume = 0;
            sfxSource.volume = 0;
        }

    }
    private void InitializeAudio()
    {
        // Tạo và thiết lập AudioSource cho music nếu chưa có
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = true;
        }

        // Tạo và thiết lập AudioSource cho sfx nếu chưa có
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Thiết lập volume
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        // Bắt đầu phát nhạc nền
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayClickSound()
    {
        if (VFXSound != null)
        {
            sfxSource.PlayOneShot(VFXSound[2], sfxVolume);
        }
    }
    public void PlayVFXSound(int soundIndex)
    {
        if (VFXSound != null)
        {
            sfxSource.PlayOneShot(VFXSound[soundIndex], sfxVolume);
        }
    }




    // Các phương thức điều chỉnh âm lượng
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void ToggleMusic(bool isOn)
    {
        if (musicSource != null)
        {
            if (isOn)
            {
                musicSource.UnPause();
            }
            else
            {
                musicSource.Pause();
            }
        }
    }

    public void ToggleSFX(bool isOn)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !isOn;
        }
    }

}
