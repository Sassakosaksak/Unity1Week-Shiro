using UnityEngine;
using UnityEngine.UI;

public class TitleVolumeControl : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    private void OnEnable()
    {
        if (volumeSlider == null)
        {
            return;
        }

        float volume = MasterVolumeController.Instance != null
            ? MasterVolumeController.Instance.Volume
            : 0.2f;
        volumeSlider.SetValueWithoutNotify(volume);
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }
    }

    private void SetVolume(float volume)
    {
        if (MasterVolumeController.Instance != null)
        {
            MasterVolumeController.Instance.SetVolume(volume);
            return;
        }

        AudioListener.volume = Mathf.Clamp01(volume);
    }
}
