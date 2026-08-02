using UnityEngine;

public class MaouController : MonoBehaviour
{
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WinHash = Animator.StringToHash("Win");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [SerializeField] private Animator animator;
    [SerializeField] private AudioClip damagedClip;
    [SerializeField, Range(0f, 1f)] private float damagedVolume = 0.7f;

    public static MaouController Instance { get; private set; }

    private bool isDefeated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple MaouControllers were found in the scene.", this);
            return;
        }

        Instance = this;
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.ResultShown += OnResultShown;
            GameController.Instance.PhaseChanged += OnPhaseChanged;
        }

        ResetForPreparation();
    }

    private void OnDestroy()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.ResultShown -= OnResultShown;
            GameController.Instance.PhaseChanged -= OnPhaseChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TakeDamage()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;
        SEController.Instance?.Play(damagedClip, damagedVolume);
        animator?.SetTrigger(DeathHash);
        GameController.Instance?.ResolveResult(GameController.GameResult.Failure);
    }

    private void OnResultShown(GameController.GameResult result)
    {
        if (result == GameController.GameResult.Success)
        {
            animator?.SetTrigger(WinHash);
        }
    }

    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        if (phase == GameController.GamePhase.Preparation)
        {
            ResetForPreparation();
        }
    }

    private void ResetForPreparation()
    {
        isDefeated = false;
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(WinHash);
        animator.ResetTrigger(DeathHash);
        animator.Play(IdleHash, 0, 0f);
        animator.Update(0f);
    }
}
