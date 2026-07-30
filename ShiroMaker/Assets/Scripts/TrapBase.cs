using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool pauseAnimatorOnResult;
    [SerializeField, Min(0)] private int damage = 1;

    private GameController gameController;
    private bool isStopped;

    protected Animator TrapAnimator => animator;

    protected bool CanRun => !isStopped
        && gameController != null
        && gameController.CurrentPhase == GameController.GamePhase.Invasion;

    protected virtual void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        gameController = GameController.Instance;
        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
            return;
        }

        gameController.PhaseChanged += OnPhaseChanged;

        if (gameController.CurrentPhase == GameController.GamePhase.Result)
        {
            StopTrap();
        }
    }

    protected virtual void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnPhaseChanged;
        }
    }

    public virtual void OnHeroHit(HeroController hero)
    {
        if (hero == null)
        {
            return;
        }

        hero.TakeDamage(damage);
    }

    /// <summary>
    /// 罠ロジックと必要なAnimatorを停止
    /// </summary>
    protected virtual void StopTrap()
    {
        if (isStopped)
        {
            return;
        }

        isStopped = true;

        if (pauseAnimatorOnResult && animator != null)
        {
            animator.speed = 0f;
        }
    }

    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        if (phase == GameController.GamePhase.Result)
        {
            StopTrap();
        }
    }
}
