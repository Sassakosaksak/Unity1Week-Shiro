using System;
using System.Collections;
using UnityEngine;

public class HeroController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField, Range(1, 5)] private int maxHp = 3;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float flinchDuration = 1f;
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
    private float knockbackRemainingTime;
    private Vector3 knockbackVelocity;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        currentHp = maxHp;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
        }
    }

    private void Update()
    {
        if (isStopped)
        {
            return;
        }

        if (isFlinching)
        {
            UpdateFlinch();
            return;
        }

        SetMoving(true);
        Move(Vector3.right * moveSpeed);
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
        if (isStopped || isDead || damage <= 0)
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

        StartFlinch();
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
        knockbackRemainingTime = knockbackDuration;
        knockbackVelocity = Vector3.left * (knockbackDistance / Mathf.Max(knockbackDuration, 0.01f));
        SetMoving(false);
        SetTrigger(HurtHash);
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
        Stop();
        SetTrigger(DeathHash);
        StartCoroutine(ShowSuccessAfterDeath());
    }

    private IEnumerator ShowSuccessAfterDeath()
    {
        yield return new WaitForSeconds(deathResultDelay);
        ShowSuccess();
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

    private void Stop()
    {
        isStopped = true;
        SetMoving(false);
    }
}
