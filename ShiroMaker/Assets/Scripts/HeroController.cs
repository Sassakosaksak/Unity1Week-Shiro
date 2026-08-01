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

    private GameController gameController;
    private int currentHp;
    private bool isStopped;
    private bool isFlinching;
    private bool isDead;
    private float flinchRemainingTime;
    private float invincibilityRemainingTime;
    private float knockbackRemainingTime;
    private Vector3 knockbackVelocity;
    private Tween invincibilityBlinkTween;
    private float blinkRendererDefaultAlpha = 1f;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsInvincible => invincibilityRemainingTime > 0f;
    public bool IsDead => isDead;
    public bool IsFlinching => isFlinching;
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
    }

    private void OnDisable()
    {
        StopInvincibilityBlink();
    }

    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
        }

        if (jobBehavior != null)
        {
            jobBehavior.Initialize(this);
        }
    }

    private void Update()
    {
        if (isStopped)
        {
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

        bool canMove = jobBehavior == null || jobBehavior.CanMove();
        SetMoving(canMove);

        if (canMove)
        {
            Move(MoveDirection * moveSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isStopped)
        {
            return;
        }

        if (other.CompareTag("Goal"))
        {
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

    public void RestoreForRewind(int hp)
    {
        StopAllCoroutines();
        StopInvincibilityBlink();

        currentHp = Mathf.Clamp(hp, 0, maxHp);
        HealthChanged?.Invoke(currentHp, maxHp);

        isStopped = false;
        isFlinching = false;
        isDead = false;
        flinchRemainingTime = 0f;
        invincibilityRemainingTime = 0f;
        knockbackRemainingTime = 0f;
        SetMoving(false);
        jobBehavior?.OnRestored();
    }

    private void OnValidate()
    {
        maxHp = Mathf.Clamp(maxHp, 1, 5);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
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
            gameController.ShowFailure();
        }
    }

    public void CausePlayerDefeat()
    {
        ShowDefeat();
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
            gameController.ShowSuccess();
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
            transform.position += knockbackVelocity * knockbackStepTime;
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
        transform.position += velocity * Time.deltaTime;
    }

    private void SetMoving(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsMovingHash, isMoving);
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
        if (blinkRenderer == null || invincibilityDuration <= 0f)
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
