using UnityEngine;

/// <summary>
/// Trap に関係する SE をまとめて管理します。
/// </summary>
public class TrapSEController : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private AudioClip placementClip;
    [SerializeField, Range(0f, 1f)] private float placementVolume = 1f;

    [Header("Spike")]
    [SerializeField] private AudioClip spikeActivationClip;
    [SerializeField, Range(0f, 1f)] private float spikeActivationVolume = 1f;

    [Header("Rolling Rock")]
    [SerializeField] private AudioClip rockSwitchClip;
    [SerializeField, Range(0f, 1f)] private float rockSwitchVolume = 1f;
    [SerializeField] private AudioClip rockRollingLoopClip;
    [SerializeField, Range(0f, 1f)] private float rockRollingLoopVolume = 1f;
    [SerializeField] private AudioClip rockBreakClip;
    [SerializeField, Range(0f, 1f)] private float rockBreakVolume = 1f;

    [Header("Pitfall")]
    [SerializeField] private AudioClip pitfallFallClip;
    [SerializeField, Range(0f, 1f)] private float pitfallFallVolume = 1f;

    public static TrapSEController Instance { get; private set; }

    private AudioSource rockRollingLoopSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple TrapSEControllers were found in the scene.", this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        StopRockRollingLoop();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayPlacement() => SEController.Instance?.Play(placementClip, placementVolume);
    public void PlaySpikeActivation() => SEController.Instance?.Play(spikeActivationClip, spikeActivationVolume);
    public void PlayRockSwitch() => SEController.Instance?.Play(rockSwitchClip, rockSwitchVolume);
    public void PlayRockBreak() => SEController.Instance?.Play(rockBreakClip, rockBreakVolume);
    public void PlayPitfallFall() => SEController.Instance?.Play(pitfallFallClip, pitfallFallVolume);

    public void StartRockRollingLoop()
    {
        if (rockRollingLoopSource == null)
        {
            rockRollingLoopSource = SEController.Instance?.PlayLoop(rockRollingLoopClip, rockRollingLoopVolume);
        }
    }

    public void StopRockRollingLoop()
    {
        if (rockRollingLoopSource == null)
        {
            return;
        }

        SEController.Instance?.StopLoop(rockRollingLoopSource);
        rockRollingLoopSource = null;
    }
}
