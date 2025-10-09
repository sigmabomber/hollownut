using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private AudioSource SFXSource;
    private AudioSource MusicSource;

    private string musicPlaying = "";

    private Dictionary<string, AudioClip> CachedSFXClips = new();
    private Dictionary<string, AudioClip> CachedMusicClips = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        SFXSource = gameObject.AddComponent<AudioSource>();
        MusicSource = gameObject.AddComponent<AudioSource>();
        MusicSource.loop = true;

      
    }

    public void PlaySFX(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName)) return;

        if (!CachedSFXClips.TryGetValue(sfxName, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"SFX not found: {sfxName}");
            return;
        }

        SFXSource.PlayOneShot(clip);
    }

    public void PlayMusic(string musicName)
    {
        if (string.IsNullOrEmpty(musicName)) return;

        if (!CachedMusicClips.TryGetValue(musicName, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"Music not found: {musicName}");
            return;
        }

        if (musicPlaying == musicName && MusicSource.isPlaying) return;

        if (MusicSource.isPlaying && musicPlaying != "")
            StopMusic();

        MusicSource.clip = clip;
        MusicSource.Play();
        musicPlaying = musicName;
    }

    public void StopMusic()
    {
        if (!MusicSource.isPlaying) return;

        MusicSource.Stop();
        musicPlaying = "";
    }
}
