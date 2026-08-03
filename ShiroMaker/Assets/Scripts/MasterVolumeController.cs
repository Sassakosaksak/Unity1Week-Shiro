using UnityEngine;

[DefaultExecutionOrder(-1101)]
public class MasterVolumeController : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.2f;

    public static MasterVolumeController Instance { get; private set; }

    public float Volume => AudioListener.volume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple MasterVolumeControllers are not supported.", this);
            return;
        }

        Instance = this;
        SetVolume(defaultVolume);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
