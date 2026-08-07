using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 勇者を構成する実行時コンポーネントを統括
/// 既存の勇者Prefabに設定済みの値と参照を維持するため、
/// ゲームプレイと表示に関するシリアライズ設定はこのクラスに残している状態
/// </summary>
public class HeroController : MonoBehaviour
{
    private static readonly List<HeroController> activeHeroes = new List<HeroController>();

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField, Range(1, 5)] private int maxHp = 3;
    [SerializeField] private HeroJobBehavior jobBehavior;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer blinkRenderer;
    [SerializeField, Min(0f)] private float flinchDuration = 1f;
    [SerializeField, Min(0f)] private float invincibilityDuration = 5f;
    // 既存Prefabとの互換性のために残している。点滅処理はRendererの有効/無効実行。
    [SerializeField, HideInInspector, FormerlySerializedAs("invincibilityBlinkAlpha")]
    private float legacyInvincibilityBlinkAlpha = 0.1f;
    [SerializeField, Min(0.01f)] private float invincibilityBlinkInterval = 0.2f;
    [SerializeField, Min(0f)] private float knockbackDistance = 0.45f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.18f;
    [SerializeField, Min(0f)] private float deathResultDelay = 0.8f;

    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private GameController gameController;
    private HeroMovement movement;
    private HeroVitality vitality;
    private HeroVisualFeedback visuals;
    private bool isInvasionActive;
    private bool isStopped;

    public int MaxHp => vitality != null ? vitality.MaxHp : maxHp;
    public int CurrentHp => vitality != null ? vitality.CurrentHp : maxHp;
    public bool IsInvincible => vitality != null && vitality.IsInvincible;
    public bool IsDead => vitality != null && vitality.IsDead;
    public bool IsFlinching => vitality != null && vitality.IsFlinching;
    public bool IsMoving => movement != null && movement.IsMoving;
    public Vector3 MoveDirection => Vector3.right;
    public Collider2D BodyCollider => movement != null ? movement.BodyCollider : GetComponent<Collider2D>();
    public static IReadOnlyList<HeroController> ActiveHeroes => activeHeroes;

    public event Action<int, int> HealthChanged;

    public bool IsBodyOverlappingBounds(Bounds detectionBounds)
    {
        return movement != null && movement.IsBodyOverlappingBounds(detectionBounds);
    }

    private void Awake()
    {
        ResolveReferences();

        movement = GetOrAddComponent<HeroMovement>();
        vitality = GetOrAddComponent<HeroVitality>();
        visuals = GetOrAddComponent<HeroVisualFeedback>();

        movement.Initialize(GetComponent<Collider2D>(), GetComponent<Rigidbody2D>());
        vitality.Initialize(maxHp, flinchDuration, invincibilityDuration, knockbackDistance, knockbackDuration);
        vitality.HealthChanged += OnHealthChanged;
        visuals.Initialize(animator, blinkRenderer, invincibilityBlinkInterval);
    }

    private void OnEnable()
    {
        if (!activeHeroes.Contains(this))
        {
            activeHeroes.Add(this);
        }
    }

    private void OnDisable()
    {
        activeHeroes.Remove(this);
        visuals?.StopAllEffects();
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnGamePhaseChanged;
        }

        if (vitality != null)
        {
            vitality.HealthChanged -= OnHealthChanged;
        }
    }

    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
        }
        else
        {
            gameController.PhaseChanged += OnGamePhaseChanged;
            OnGamePhaseChanged(gameController.CurrentPhase);
        }

        jobBehavior?.Initialize(this);
    }

    private void Update()
    {
        if (isStopped || !isInvasionActive)
        {
            SetMoving(false);
            return;
        }

        if (vitality.UpdateInvincibility(Time.deltaTime))
        {
            visuals.StopInvincibilityBlink();
        }

        if (vitality.TickFlinch(Time.deltaTime, out Vector3 knockbackMovement))
        {
            if (knockbackMovement != Vector3.zero)
            {
                movement.Move(knockbackMovement, this, ActiveHeroes);
            }

            return;
        }

        jobBehavior?.Tick();
        if (isStopped)
        {
            return;
        }

        bool canMove = (jobBehavior == null || jobBehavior.CanMove())
            && movement.CanMoveInDirection(MoveDirection, this, ActiveHeroes);
        SetMoving(canMove);

        if (canMove)
        {
            movement.Move(MoveDirection * moveSpeed * Time.deltaTime, this, ActiveHeroes);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStopped || !isInvasionActive)
        {
            return;
        }

        if (other.CompareTag("Goal"))
        {
            if (jobBehavior != null && jobBehavior.TryHandleGoalContact(other))
            {
                return;
            }

            ShowDefeat();
            return;
        }

        TrapBase trap = other.GetComponentInParent<TrapBase>();
        trap?.OnHeroHit(this);
    }

    public void TakeDamage(int damage)
    {
        if (isStopped)
        {
            return;
        }

        HeroVitality.DamageResult result = vitality.TakeDamage(damage);
        if (result == HeroVitality.DamageResult.Ignored)
        {
            return;
        }

        if (result == HeroVitality.DamageResult.Died)
        {
            Die();
            return;
        }

        jobBehavior?.OnInterrupted();
        SetMoving(false);
        visuals.SetTrigger(HurtHash);
        visuals.StartInvincibilityBlink();
    }

    public void SetMaxHp(int value, bool restoreHealth)
    {
        maxHp = Mathf.Clamp(value, 1, 5);
        vitality.SetMaxHp(maxHp, restoreHealth);
    }

    public void RestoreForRewind(int hp)
    {
        StopAllCoroutines();
        visuals.StopInvincibilityBlink();

        vitality.RestoreForRewind(hp);
        isStopped = false;
        movement.SetBodyPhysicsEnabled(false);
        visuals.ResetAnimator();
        SetMoving(false);
        jobBehavior?.OnRestored();
    }

    public void CompleteRewindRestore()
    {
        if (!IsDead)
        {
            movement.SetBodyPhysicsEnabled(true);
        }
    }

    private void OnValidate()
    {
        maxHp = Mathf.Clamp(maxHp, 1, 5);
    }

    private void OnGamePhaseChanged(GameController.GamePhase phase)
    {
        isInvasionActive = phase == GameController.GamePhase.Invasion;
        if (!isInvasionActive)
        {
            SetMoving(false);
        }
    }

    public void CausePlayerDefeat()
    {
        ShowDefeat();
    }

    public void OnAttackDefeatAnimationEvent()
    {
        jobBehavior?.OnAttackDefeatAnimationEvent();
    }

    public void OnRockBreakAnimationEvent()
    {
        jobBehavior?.OnRockBreakAnimationEvent();
    }

    public void Kill()
    {
        if (isStopped || !vitality.Kill())
        {
            return;
        }

        Die();
    }

    public bool Heal(int amount)
    {
        return vitality.Heal(amount);
    }

    public bool ReviveAtFullHealth()
    {
        if (!vitality.ReviveAtFullHealth())
        {
            return false;
        }

        StopAllCoroutines();
        visuals.StopInvincibilityBlink();
        isStopped = false;
        movement.SetBodyPhysicsEnabled(true);
        visuals.ResetAnimator();
        SetMoving(false);
        jobBehavior?.OnRestored();
        return true;
    }

    public void GrantInvincibility(float duration)
    {
        if (vitality.GrantInvincibility(duration))
        {
            visuals.StartInvincibilityBlink();
        }
    }

    public void StartGreenFlash(Color flashColor, float interval)
    {
        visuals.StartGreenFlash(flashColor, interval);
    }

    public void PlayColorPulse(Color pulseColor, float fadeDuration, float holdDuration)
    {
        visuals.PlayColorPulse(pulseColor, fadeDuration, holdDuration);
    }

    public void StopGreenFlash()
    {
        visuals.StopGreenFlash();
    }

    public void PlayJobTrigger(string triggerName)
    {
        visuals.PlayJobTrigger(triggerName);
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (jobBehavior == null)
        {
            jobBehavior = GetComponent<HeroJobBehavior>();
        }

        if (blinkRenderer == null)
        {
            blinkRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private void OnHealthChanged(int currentHp, int changedMaxHp)
    {
        HealthChanged?.Invoke(currentHp, changedMaxHp);
    }

    private void ShowDefeat()
    {
        if (isStopped)
        {
            return;
        }

        Stop();
        gameController?.ResolveResult(GameController.GameResult.Failure);
    }

    private void ShowSuccess()
    {
        gameController?.ResolveResult(GameController.GameResult.Success);
    }

    private void Die()
    {
        movement.SetBodyPhysicsEnabled(false);
        jobBehavior?.OnInterrupted();
        Stop();
        visuals.StopInvincibilityBlink();
        visuals.SetTrigger(DeathHash);
        StartCoroutine(ShowSuccessIfAllHeroesAreDeadAfterDelay());
    }

    private IEnumerator ShowSuccessIfAllHeroesAreDeadAfterDelay()
    {
        yield return new WaitForSeconds(deathResultDelay);

        if (gameController != null && gameController.AreAllHeroesDead())
        {
            ShowSuccess();
        }
    }

    private void SetMoving(bool value)
    {
        movement.SetMoving(value);
        visuals.SetMoving(value);
    }

    private void Stop()
    {
        isStopped = true;
        SetMoving(false);
    }
}
