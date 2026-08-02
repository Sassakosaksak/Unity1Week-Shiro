using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField, Range(1, 5)] private int maxHp = 3;
    [SerializeField] private HeroJobBehavior jobBehavior;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer blinkRenderer;
    [SerializeField, Min(0f)] private float flinchDuration = 1f;
    [SerializeField, Min(0f)] private float invincibilityDuration = 5f;
    [SerializeField, Range(0f, 1f)] private float invincibilityBlinkAlpha = 0.1f;
    [SerializeField, Min(0.01f)] private float invincibilityBlinkInterval = 0.2f;
    [SerializeField, Min(0f)] private float knockbackDistance = 0.45f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.18f;
    [SerializeField, Min(0f)] private float deathResultDelay = 0.8f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private const float DefaultHeroSeparation = 0.3f;

    private GameController gameController;
    private Collider2D bodyCollider;
    private Rigidbody2D bodyRigidbody;
    private int currentHp;
    private bool isInvasionActive;
    private bool isStopped;
    private bool isMoving;
    private bool isFlinching;
    private bool isDead;
    private float flinchRemainingTime;
    private float invincibilityRemainingTime;
    private float knockbackRemainingTime;
    private Vector3 knockbackVelocity;
    private Tween invincibilityBlinkTween;
    private Tween greenFlashTween;
    private Tween greenFlashStopTween;
    private float blinkRendererDefaultAlpha = 1f;
    private Color greenFlashDefaultColor = Color.white;
    private bool isGreenFlashing;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsInvincible => invincibilityRemainingTime > 0f;
    public bool IsDead => isDead;
    public bool IsFlinching => isFlinching;
    public bool IsMoving => isMoving;
    public Vector3 MoveDirection => Vector3.right;

    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        currentHp = maxHp;

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

        if (blinkRenderer != null)
        {
            blinkRendererDefaultAlpha = blinkRenderer.color.a;
        }

        bodyCollider = GetComponent<Collider2D>();
        bodyRigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        StopInvincibilityBlink();
        StopGreenFlash();
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnGamePhaseChanged;
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

        if (jobBehavior != null)
        {
            jobBehavior.Initialize(this);
        }
    }

    private void Update()
    {
        if (isStopped || !isInvasionActive)
        {
            SetMoving(false);
            return;
        }

        UpdateInvincibility();

        if (isFlinching)
        {
            UpdateFlinch();
            return;
        }

        if (jobBehavior != null)
        {
            jobBehavior.Tick();

            if (isStopped)
            {
                return;
            }
        }

        bool canMove = (jobBehavior == null || jobBehavior.CanMove())
            && CanMoveInDirection(MoveDirection);
        SetMoving(canMove);

        if (canMove)
        {
            Move(MoveDirection * moveSpeed);
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
        if (trap != null)
        {
            trap.OnHeroHit(this);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isStopped || isDead || invincibilityRemainingTime > 0f || damage <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        HealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            Die();
            return;
        }

        jobBehavior?.OnInterrupted();
        StartFlinch();
    }

    public void SetMaxHp(int value, bool restoreHealth)
    {
        maxHp = Mathf.Clamp(value, 1, 5);
        currentHp = restoreHealth ? maxHp : Mathf.Min(currentHp, maxHp);
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public void RestoreForRewind(int hp)
    {
        StopAllCoroutines();
        StopInvincibilityBlink();

        currentHp = Mathf.Clamp(hp, 0, maxHp);
        HealthChanged?.Invoke(currentHp, maxHp);

        isStopped = false;
        isFlinching = false;
        isDead = false;
        SetBodyPhysicsEnabled(true);
        flinchRemainingTime = 0f;
        invincibilityRemainingTime = 0f;
        knockbackRemainingTime = 0f;
        ResetAnimatorForRewind();
        SetMoving(false);
        jobBehavior?.OnRestored();
    }

    private void OnValidate()
    {
        maxHp = Mathf.Clamp(maxHp, 1, 5);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private void OnGamePhaseChanged(GameController.GamePhase phase)
    {
        isInvasionActive = phase == GameController.GamePhase.Invasion;

        if (!isInvasionActive)
        {
            SetMoving(false);
        }
    }

    private void ShowDefeat()
    {
        if (isStopped)
        {
            return;
        }

        Stop();
        if (gameController != null)
        {
            gameController.ResolveResult(GameController.GameResult.Failure);
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
        if (isStopped || isDead)
        {
            return;
        }

        currentHp = 0;
        HealthChanged?.Invoke(currentHp, maxHp);
        Die();
    }

    public bool Heal(int amount)
    {
        if (isDead || amount <= 0 || currentHp >= maxHp)
        {
            return false;
        }

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        HealthChanged?.Invoke(currentHp, maxHp);
        return true;
    }

    public bool ReviveAtFullHealth()
    {
        if (!isDead)
        {
            return false;
        }

        StopAllCoroutines();
        StopInvincibilityBlink();

        currentHp = maxHp;
        isStopped = false;
        isFlinching = false;
        isDead = false;
        SetBodyPhysicsEnabled(true);
        flinchRemainingTime = 0f;
        invincibilityRemainingTime = 0f;
        knockbackRemainingTime = 0f;
        ResetAnimatorForRewind();
        SetMoving(false);
        jobBehavior?.OnRestored();
        HealthChanged?.Invoke(currentHp, maxHp);
        return true;
    }

    public void GrantInvincibility(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        invincibilityRemainingTime = Mathf.Max(invincibilityRemainingTime, duration);
        StartInvincibilityBlink();
    }

    public void StartGreenFlash(Color flashColor, float interval)
    {
        if (blinkRenderer == null)
        {
            return;
        }

        StopGreenFlash();
        greenFlashDefaultColor = blinkRenderer.color;
        isGreenFlashing = true;
        flashColor.a = greenFlashDefaultColor.a;
        greenFlashTween = blinkRenderer
            .DOColor(flashColor, Mathf.Max(0.01f, interval))
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void PlayColorPulse(Color pulseColor, float fadeDuration, float holdDuration)
    {
        if (blinkRenderer == null)
        {
            return;
        }

        StopGreenFlash();
        greenFlashDefaultColor = blinkRenderer.color;
        isGreenFlashing = true;
        pulseColor.a = greenFlashDefaultColor.a;

        Sequence pulseSequence = DOTween.Sequence();
        pulseSequence.Append(blinkRenderer.DOColor(pulseColor, Mathf.Max(0.01f, fadeDuration)));
        pulseSequence.AppendInterval(Mathf.Max(0f, holdDuration));
        pulseSequence.Append(blinkRenderer.DOColor(greenFlashDefaultColor, Mathf.Max(0.01f, fadeDuration)));
        pulseSequence.OnComplete(StopGreenFlash);
        greenFlashTween = pulseSequence.SetTarget(this);
    }

    public void StopGreenFlash()
    {
        greenFlashTween?.Kill();
        greenFlashTween = null;
        greenFlashStopTween?.Kill();
        greenFlashStopTween = null;

        if (blinkRenderer == null || !isGreenFlashing)
        {
            isGreenFlashing = false;
            return;
        }

        isGreenFlashing = false;
        Color restoredColor = greenFlashDefaultColor;
        restoredColor.a = blinkRenderer.color.a;
        blinkRenderer.color = restoredColor;
    }

    public void PlayJobTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        int triggerHash = Animator.StringToHash(triggerName);
        if (!HasAnimatorParameter(triggerHash, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        SetTrigger(triggerHash);
    }

    private void ShowSuccess()
    {
        if (gameController != null)
        {
            gameController.ResolveResult(GameController.GameResult.Success);
        }
    }

    private void StartFlinch()
    {
        isFlinching = true;
        flinchRemainingTime = flinchDuration;
        invincibilityRemainingTime = invincibilityDuration;
        knockbackRemainingTime = knockbackDuration;
        knockbackVelocity = Vector3.left * (knockbackDistance / Mathf.Max(knockbackDuration, 0.01f));
        SetMoving(false);
        SetTrigger(HurtHash);
        StartInvincibilityBlink();
    }

    private void UpdateInvincibility()
    {
        if (invincibilityRemainingTime <= 0f)
        {
            return;
        }

        invincibilityRemainingTime = Mathf.Max(0f, invincibilityRemainingTime - Time.deltaTime);

        if (invincibilityRemainingTime <= 0f)
        {
            StopInvincibilityBlink();
        }
    }

    private void UpdateFlinch()
    {
        float deltaTime = Time.deltaTime;

        if (knockbackRemainingTime > 0f)
        {
            float knockbackStepTime = Mathf.Min(deltaTime, knockbackRemainingTime);
            transform.position += GetHeroSafeMovement(knockbackVelocity * knockbackStepTime);
            knockbackRemainingTime -= deltaTime;
        }

        flinchRemainingTime -= deltaTime;
        if (flinchRemainingTime > 0f)
        {
            return;
        }

        isFlinching = false;
    }

    private void Die()
    {
        isDead = true;
        SetBodyPhysicsEnabled(false);
        jobBehavior?.OnInterrupted();
        Stop();
        StopInvincibilityBlink();
        SetTrigger(DeathHash);
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

    private void Move(Vector3 velocity)
    {
        Vector3 requestedMovement = velocity * Time.deltaTime;
        transform.position += GetHeroSafeMovement(requestedMovement);
    }

    private bool CanMoveInDirection(Vector3 direction)
    {
        if (direction.x == 0f)
        {
            return true;
        }

        return Mathf.Abs(GetHeroSafeMovement(new Vector3(Mathf.Sign(direction.x) * 0.01f, 0f, 0f)).x) > 0f;
    }

    private Vector3 GetHeroSafeMovement(Vector3 requestedMovement)
    {
        if (bodyCollider == null || requestedMovement.x == 0f)
        {
            return requestedMovement;
        }

        Bounds ownBounds = bodyCollider.bounds;
        float safeMovementX = requestedMovement.x;

        foreach (HeroController otherHero in FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (otherHero == null || otherHero == this || otherHero.IsDead)
            {
                continue;
            }

            Collider2D otherCollider = otherHero.GetComponent<Collider2D>();
            if (otherCollider == null || !otherCollider.enabled || !IsVerticallyOverlapping(ownBounds, otherCollider.bounds))
            {
                continue;
            }

            if (requestedMovement.x > 0f && otherCollider.bounds.min.x >= ownBounds.max.x)
            {
                float maximumMovement = otherCollider.bounds.min.x - GetHeroSeparation() - ownBounds.max.x;
                safeMovementX = Mathf.Min(safeMovementX, Mathf.Max(0f, maximumMovement));
            }
            else if (requestedMovement.x < 0f && otherCollider.bounds.max.x <= ownBounds.min.x)
            {
                float maximumMovement = otherCollider.bounds.max.x + GetHeroSeparation() - ownBounds.min.x;
                safeMovementX = Mathf.Max(safeMovementX, Mathf.Min(0f, maximumMovement));
            }
        }

        return new Vector3(safeMovementX, requestedMovement.y, requestedMovement.z);
    }

    private static bool IsVerticallyOverlapping(Bounds first, Bounds second)
    {
        return first.min.y < second.max.y && first.max.y > second.min.y;
    }

    private float GetHeroSeparation()
    {
        return DefaultHeroSeparation;
    }

    private void SetBodyPhysicsEnabled(bool enabled)
    {
        if (bodyRigidbody != null)
        {
            bodyRigidbody.simulated = enabled;
        }
    }

    private void SetMoving(bool isMoving)
    {
        this.isMoving = isMoving;

        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, isMoving);
    }

    private void ResetAnimatorForRewind()
    {
        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }

    private void SetTrigger(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(triggerHash);
    }

    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void StartInvincibilityBlink()
    {
        if (blinkRenderer == null || invincibilityRemainingTime <= 0f)
        {
            return;
        }

        invincibilityBlinkTween?.Kill();
        blinkRendererDefaultAlpha = blinkRenderer.color.a;
        invincibilityBlinkTween = blinkRenderer
            .DOFade(invincibilityBlinkAlpha, invincibilityBlinkInterval)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopInvincibilityBlink()
    {
        invincibilityBlinkTween?.Kill();
        invincibilityBlinkTween = null;

        if (blinkRenderer == null)
        {
            return;
        }

        Color color = blinkRenderer.color;
        color.a = blinkRendererDefaultAlpha;
        blinkRenderer.color = color;
    }

    private void Stop()
    {
        isStopped = true;
        SetMoving(false);
    }
}
