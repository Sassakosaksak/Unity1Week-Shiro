using UnityEngine;

public class HeroSEController : MonoBehaviour
{
    [SerializeField] private HeroController hero;
    [SerializeField] private AudioClip footstepClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;
    [SerializeField, Min(0.01f)] private float footstepInterval = 0.5f;
    [SerializeField] private AudioClip attackClip;
    [SerializeField, Range(0f, 1f)] private float attackVolume = 0.7f;
    [SerializeField] private AudioClip rockBreakAttackClip;
    [SerializeField, Range(0f, 1f)] private float rockBreakAttackVolume = 0.7f;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField, Range(0f, 1f)] private float hurtVolume = 0.7f;
    [SerializeField] private AudioClip deathClip;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.7f;
    [SerializeField] private AudioClip magicCastingClip;
    [SerializeField, Range(0f, 1f)] private float magicCastingVolume = 0.7f;
    [SerializeField] private AudioClip magicShotClip;
    [SerializeField, Range(0f, 1f)] private float magicShotVolume = 0.7f;
    [SerializeField] private AudioClip gravityMagicShotClip;

    private float footstepElapsedTime;
    private AudioSource magicCastingSource;
    private AudioSource magicShotSource;
    private GameController gameController;

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponent<HeroController>();
        }
    }

    private void Start()
    {
        gameController = GameController.Instance;
        if (gameController != null)
        {
            gameController.PhaseChanged += OnGamePhaseChanged;
        }
    }

    private void Update()
    {
        if (hero == null || !hero.IsMoving)
        {
            footstepElapsedTime = 0f;
            return;
        }

        footstepElapsedTime += Time.deltaTime;
        if (footstepElapsedTime < footstepInterval)
        {
            return;
        }

        footstepElapsedTime -= footstepInterval;
        SEController.Instance?.Play(footstepClip, footstepVolume);
    }

    private void OnDisable()
    {
        StopMagicCasting();
        StopMagicShot();
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnGamePhaseChanged;
        }
    }

    public void PlayAttack()
    {
        SEController.Instance?.Play(attackClip, attackVolume);
    }

    public void PlayRockBreakAttack()
    {
        SEController.Instance?.Play(rockBreakAttackClip, rockBreakAttackVolume);
    }

    public void PlayHurt()
    {
        SEController.Instance?.Play(hurtClip, hurtVolume);
    }

    public void PlayDeath()
    {
        SEController.Instance?.Play(deathClip, deathVolume);
    }

    public void StartMagicCasting()
    {
        if (magicCastingSource != null)
        {
            return;
        }

        magicCastingSource = SEController.Instance?.PlayLoop(magicCastingClip, magicCastingVolume);
    }

    public void StopMagicCasting()
    {
        SEController.Instance?.StopLoop(magicCastingSource);
        magicCastingSource = null;
    }

    public void PlayMagicShot()
    {
        StopMagicShot();
        magicShotSource = SEController.Instance?.PlayReserved(magicShotClip, magicShotVolume);
    }

    public void PlayGravityMagicShot()
    {
        StopMagicCasting();
        StopMagicShot();
        magicShotSource = SEController.Instance?.PlayReserved(gravityMagicShotClip, magicShotVolume);
    }

    private void OnGamePhaseChanged(GameController.GamePhase phase)
    {
        if (phase != GameController.GamePhase.Invasion)
        {
            StopMagicCasting();
            StopMagicShot();
        }
    }

    private void StopMagicShot()
    {
        SEController.Instance?.StopReserved(magicShotSource);
        magicShotSource = null;
    }
}
