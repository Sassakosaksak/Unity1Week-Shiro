using System.Collections.Generic;
using UnityEngine;

public class SEController : MonoBehaviour
{
    [SerializeField] private AudioSource[] audioSources;

    public static SEController Instance { get; private set; }

    private int nextSourceIndex;
    private readonly HashSet<AudioSource> reservedSources = new HashSet<AudioSource>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple SEControllers were found in the scene.", this);
            return;
        }

        Instance = this;

        if (audioSources == null || audioSources.Length == 0)
        {
            audioSources = GetComponents<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = FindAvailableSource();
        if (source == null)
        {
            return;
        }

        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    public AudioSource PlayLoop(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            return null;
        }

        AudioSource source = FindAvailableSource();
        if (source == null)
        {
            return null;
        }

        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = true;
        source.Play();
        return source;
    }

    public void StopLoop(AudioSource source)
    {
        if (source == null || !source.loop)
        {
            return;
        }

        source.Stop();
        source.loop = false;
        source.clip = null;
    }

    public AudioSource PlayReserved(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            return null;
        }

        AudioSource source = FindAvailableSource();
        if (source == null)
        {
            return null;
        }

        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.loop = false;
        source.Play();
        reservedSources.Add(source);
        return source;
    }

    public void StopReserved(AudioSource source)
    {
        if (source == null || !reservedSources.Remove(source))
        {
            return;
        }

        source.Stop();
        source.clip = null;
    }

    private AudioSource FindAvailableSource()
    {
        if (audioSources == null || audioSources.Length == 0)
        {
            return null;
        }

        ReleaseFinishedReservedSources();

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            int index = (nextSourceIndex + i) % audioSources.Length;
            AudioSource fallback = audioSources[index];
            if (fallback == null || fallback.loop || reservedSources.Contains(fallback))
            {
                continue;
            }

            nextSourceIndex = (index + 1) % audioSources.Length;
            fallback.Stop();
            return fallback;
        }

        return null;
    }

    private void ReleaseFinishedReservedSources()
    {
        reservedSources.RemoveWhere(source => source == null || !source.isPlaying);
    }
}
