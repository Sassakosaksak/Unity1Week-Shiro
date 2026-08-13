using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HeroVitality : MonoBehaviour
{
    public enum DamageResult
    {
        Ignored,
        Flinched,
        Died
    }

    private int maxHp;
    private int currentHp;
    private float flinchDuration;
    private float invincibilityDuration;
    private float knockbackDistance;
    private float knockbackDuration;
    private bool isFlinching;
    private bool isDead;
    private float flinchRemainingTime;
    private float invincibilityRemainingTime;
    private float knockbackRemainingTime;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsInvincible => invincibilityRemainingTime > 0f;
    public bool IsDead => isDead;
    public bool IsFlinching => isFlinching;
    public event Action<int, int> HealthChanged;

    public void Initialize(
        int initialMaxHp,
        float configuredFlinchDuration,
        float configuredInvincibilityDuration,
        float configuredKnockbackDistance,
        float configuredKnockbackDuration)
    {
        maxHp = Mathf.Clamp(initialMaxHp, 1, 5);
        currentHp = maxHp;
        flinchDuration = Mathf.Max(0f, configuredFlinchDuration);
        invincibilityDuration = Mathf.Max(0f, configuredInvincibilityDuration);
        knockbackDistance = Mathf.Max(0f, configuredKnockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, configuredKnockbackDuration);
    }

    public DamageResult TakeDamage(int damage)
    {
        if (isDead || IsInvincible || damage <= 0)
        {
            return DamageResult.Ignored;
        }

        currentHp = Mathf.Max(0, currentHp - damage);
        HealthChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0)
        {
            isDead = true;
            return DamageResult.Died;
        }

        StartFlinch();
        return DamageResult.Flinched;
    }

    public bool Kill()
    {
        if (isDead)
        {
            return false;
        }

        currentHp = 0;
        isDead = true;
        HealthChanged?.Invoke(currentHp, maxHp);
        return true;
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

    public void SetMaxHp(int value, bool restoreHealth)
    {
        maxHp = Mathf.Clamp(value, 1, 5);
        currentHp = restoreHealth ? maxHp : Mathf.Min(currentHp, maxHp);
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public void RestoreForRewind(int hp)
    {
        currentHp = Mathf.Clamp(hp, 1, maxHp);
        isDead = false;
        isFlinching = false;
        flinchRemainingTime = 0f;
        invincibilityRemainingTime = 0f;
        knockbackRemainingTime = 0f;
        HealthChanged?.Invoke(currentHp, maxHp);
    }

    public bool ReviveAtFullHealth()
    {
        if (!isDead)
        {
            return false;
        }

        currentHp = maxHp;
        isDead = false;
        isFlinching = false;
        flinchRemainingTime = 0f;
        invincibilityRemainingTime = 0f;
        knockbackRemainingTime = 0f;
        HealthChanged?.Invoke(currentHp, maxHp);
        return true;
    }

    public bool GrantInvincibility(float duration)
    {
        if (duration <= 0f)
        {
            return false;
        }

        invincibilityRemainingTime = Mathf.Max(invincibilityRemainingTime, duration);
        return true;
    }

    public bool UpdateInvincibility(float deltaTime)
    {
        if (invincibilityRemainingTime <= 0f)
        {
            return false;
        }

        invincibilityRemainingTime = Mathf.Max(0f, invincibilityRemainingTime - deltaTime);
        return invincibilityRemainingTime <= 0f;
    }

    public bool TickFlinch(float deltaTime, out Vector3 knockbackMovement)
    {
        knockbackMovement = Vector3.zero;
        if (!isFlinching)
        {
            return false;
        }

        if (knockbackRemainingTime > 0f)
        {
            float stepTime = Mathf.Min(deltaTime, knockbackRemainingTime);
            knockbackMovement = Vector3.left * (knockbackDistance / knockbackDuration) * stepTime;
            knockbackRemainingTime -= deltaTime;
        }

        flinchRemainingTime -= deltaTime;
        if (flinchRemainingTime <= 0f)
        {
            isFlinching = false;
        }

        return true;
    }

    private void StartFlinch()
    {
        isFlinching = true;
        flinchRemainingTime = flinchDuration;
        invincibilityRemainingTime = invincibilityDuration;
        knockbackRemainingTime = knockbackDuration;
    }
}
