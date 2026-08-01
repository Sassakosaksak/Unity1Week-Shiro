using UnityEngine;

public class SEController : MonoBehaviour
{
    [SerializeField] private AudioSource[] audioSources;

    public static SEController Instance { get; private set; }

    private int nextSourceIndex;

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

    private AudioSource FindAvailableSource()
    {
        if (audioSources == null || audioSources.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        AudioSource fallback = audioSources[nextSourceIndex % audioSources.Length];
        nextSourceIndex = (nextSourceIndex + 1) % audioSources.Length;
        fallback?.Stop();
        return fallback;
    }
}
