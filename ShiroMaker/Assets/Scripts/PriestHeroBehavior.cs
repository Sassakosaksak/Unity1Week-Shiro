using UnityEngine;

public class PriestHeroBehavior : HeroJobBehavior
{
    private enum SpellKind
    {
        None = 0,
        Attack = 1,
        Raise = 2,
        Heal = 3
    }

    [SerializeField, Min(0f)] private float raiseRange = 2.5f;
    [SerializeField, Min(0f)] private float spellStartDelay = 0.5f;
    [SerializeField, Min(0f)] private float spellCooldownDuration = 3f;
    [SerializeField, Min(0.01f)] private float attackDuration = 2f;
    [SerializeField, Min(0.01f)] private float raiseDuration = 5f;
    [SerializeField] private Color raiseFlashColor = Color.green;
    [SerializeField, Min(0.01f)] private float raiseFlashInterval = 0.1f;
    [SerializeField, Min(0f)] private float reviveInvincibilityDuration = 2f;
    [SerializeField, Min(0f)] private float healRange = 4.5f;
    [SerializeField] private string goalTag = "Goal";
    [SerializeField, Min(0.01f)] private float healDuration = 1f;
    [SerializeField, Min(1)] private int healAmount = 1;
    [SerializeField] private Color healFlashColor = Color.green;
    [SerializeField, Min(0.01f)] private float healColorFadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float healColorHoldDuration = 1f;
    [SerializeField] private GameObject priestMagicPrefab;
    [SerializeField] private Transform priestMagicParent;
    [SerializeField] private Vector3 raiseMagicOffset;
    [SerializeField, Min(0.01f)] private float raiseMagicLifetime = 1.5f;
    [SerializeField, Min(0.01f)] private float raiseMagicPlaybackSpeed = 0.5f;
    [SerializeField] private Vector3 healMagicOffset;
    [SerializeField, Min(0.01f)] private float healMagicLifetime = 0.75f;
    [SerializeField, Min(0.01f)] private float healMagicPlaybackSpeed = 1f;
    [SerializeField] private Vector3 attackMagicOffset;
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string raiseTriggerName = "Raise";
    [SerializeField] private string healTriggerName = "Heal";
    [SerializeField] private string finishSpellTriggerName = "FinishSpell";
    [SerializeField] private string raiseEffectTriggerName = "Raise";
    [SerializeField] private string healEffectTriggerName = "Heal";
    [SerializeField] private Color raiseRangeGizmoColor = new Color(0.9f, 0.6f, 1f, 0.45f);
    [SerializeField] private Color healRangeGizmoColor = new Color(0.35f, 1f, 0.55f, 0.35f);

    private SpellKind spellKind;
    private HeroController spellTarget;
    private float spellRemainingTime;
    private SpellKind pendingSpellKind;
    private HeroController pendingSpellTarget;
    private Transform pendingAttackTarget;
    private float pendingSpellDelayRemaining;
    private float spellCooldownRemaining;
    private GameObject activeMagicEffect;
    private Transform attackTarget;
    private bool isWaitingForMagicEffect;
    private bool attackEffectSpawned;

    public override void Tick()
    {
        if (Hero == null)
        {
            return;
        }

        if (spellCooldownRemaining > 0f)
        {
            spellCooldownRemaining = Mathf.Max(0f, spellCooldownRemaining - Time.deltaTime);
        }

        if (spellKind != SpellKind.None)
        {
            UpdateSpell();
            return;
        }

        if (pendingSpellKind != SpellKind.None)
        {
            UpdatePendingSpell();
            return;
        }

        if (spellCooldownRemaining > 0f)
        {
            return;
        }

        Transform goal = FindGoalInRange();
        if (goal != null)
        {
            BeginAttackAfterDelay(goal);
            return;
        }

        HeroController reviveTarget = FindRightmostHero(raiseRange, hero => hero.IsDead);
        if (reviveTarget != null)
        {
            BeginSpellAfterDelay(SpellKind.Raise, reviveTarget);
            return;
        }

        HeroController healTarget = FindRightmostHero(
            healRange,
            hero => !hero.IsDead && hero.CurrentHp < hero.MaxHp);
        if (healTarget != null)
        {
            BeginSpellAfterDelay(SpellKind.Heal, healTarget);
        }
    }

    public override bool CanMove()
    {
        return spellKind == SpellKind.None && !isWaitingForMagicEffect;
    }

    public override void OnInterrupted()
    {
        if (spellKind == SpellKind.Attack)
        {
            CancelActiveMagicEffect();
        }

        CancelSpell();
        CancelPendingSpell();
    }

    public override void OnRestored()
    {
        CancelSpell();
        CancelPendingSpell();
        CancelActiveMagicEffect();
        spellCooldownRemaining = 0f;
    }

    private void StartSpell(SpellKind nextSpellKind, HeroController target)
    {
        spellKind = nextSpellKind;
        isWaitingForMagicEffect = true;
        spellTarget = target;
        spellRemainingTime = nextSpellKind == SpellKind.Raise ? raiseDuration : healDuration;

        if (nextSpellKind == SpellKind.Raise)
        {
            spellTarget.StartGreenFlash(raiseFlashColor, raiseFlashInterval);
        }

        Hero.PlayJobTrigger(nextSpellKind == SpellKind.Raise ? raiseTriggerName : healTriggerName);
    }

    private void StartAttack(Transform target)
    {
        if (target == null)
        {
            return;
        }

        spellKind = SpellKind.Attack;
        attackTarget = target;
        spellRemainingTime = attackDuration;
        attackEffectSpawned = false;
        Hero.PlayJobTrigger(attackTriggerName);
    }

    private void BeginSpellAfterDelay(SpellKind nextSpellKind, HeroController target)
    {
        pendingSpellKind = nextSpellKind;
        pendingSpellTarget = target;
        pendingSpellDelayRemaining = spellStartDelay;
    }

    private void BeginAttackAfterDelay(Transform target)
    {
        pendingSpellKind = SpellKind.Attack;
        pendingAttackTarget = target;
        pendingSpellDelayRemaining = spellStartDelay;
    }

    private void UpdatePendingSpell()
    {
        if (pendingSpellKind == SpellKind.Attack)
        {
            if (pendingAttackTarget == null)
            {
                CancelPendingSpell();
                return;
            }
        }
        else if (pendingSpellTarget == null)
        {
            CancelPendingSpell();
            return;
        }

        pendingSpellDelayRemaining -= Time.deltaTime;
        if (pendingSpellDelayRemaining > 0f)
        {
            return;
        }

        SpellKind nextSpellKind = pendingSpellKind;
        HeroController target = pendingSpellTarget;
        Transform attackSpellTarget = pendingAttackTarget;
        CancelPendingSpell();

        if (nextSpellKind == SpellKind.Attack)
        {
            StartAttack(attackSpellTarget);
            return;
        }

        StartSpell(nextSpellKind, target);
    }

    private void UpdateSpell()
    {
        if (spellKind == SpellKind.Attack)
        {
            if (attackEffectSpawned)
            {
                return;
            }

            if (attackTarget == null)
            {
                CancelSpell();
                return;
            }
        }
        else if (spellTarget == null)
        {
            CancelSpell();
            return;
        }

        spellRemainingTime -= Time.deltaTime;
        if (spellRemainingTime > 0f)
        {
            return;
        }

        if (spellKind == SpellKind.Attack)
        {
            if (priestMagicPrefab == null)
            {
                CancelSpell();
                return;
            }

            attackEffectSpawned = true;
            Hero?.GetComponent<HeroSEController>()?.StopMagicCasting();
            Hero.PlayJobTrigger(finishSpellTriggerName);
            SpawnAttackMagicEffect(attackTarget);
            return;
        }

        SpellKind completedSpellKind = spellKind;
        HeroController completedTarget = spellTarget;
        CancelSpell(false);

        if (completedSpellKind == SpellKind.Raise)
        {
            SpawnMagicEffect(
                completedTarget,
                completedSpellKind,
                raiseEffectTriggerName,
                raiseMagicOffset,
                raiseMagicLifetime,
                raiseMagicPlaybackSpeed);
            isWaitingForMagicEffect = false;
            Hero.PlayJobTrigger(finishSpellTriggerName);
            if (completedTarget.ReviveAtFullHealth())
            {
                completedTarget.GrantInvincibility(reviveInvincibilityDuration);
                StartSpellCooldown();
            }

            return;
        }

        if (completedTarget.Heal(healAmount))
        {
            SpawnMagicEffect(
                completedTarget,
                completedSpellKind,
                healEffectTriggerName,
                healMagicOffset,
                healMagicLifetime,
                healMagicPlaybackSpeed);
            isWaitingForMagicEffect = false;
            Hero.PlayJobTrigger(finishSpellTriggerName);
            completedTarget.PlayColorPulse(healFlashColor, healColorFadeDuration, healColorHoldDuration);
            StartSpellCooldown();
        }
        else
        {
            isWaitingForMagicEffect = false;
            Hero.PlayJobTrigger(finishSpellTriggerName);
        }
    }

    private void CancelSpell(bool clearMagicEffectWait = true)
    {
        Hero?.GetComponent<HeroSEController>()?.StopMagicCasting();

        if (clearMagicEffectWait)
        {
            isWaitingForMagicEffect = false;
        }

        if (spellKind == SpellKind.Raise && spellTarget != null)
        {
            spellTarget.StopGreenFlash();
        }

        spellKind = SpellKind.None;
        spellTarget = null;
        attackTarget = null;
        spellRemainingTime = 0f;
        attackEffectSpawned = false;
    }

    private void CancelPendingSpell()
    {
        pendingSpellKind = SpellKind.None;
        pendingSpellTarget = null;
        pendingAttackTarget = null;
        pendingSpellDelayRemaining = 0f;
    }

    private void StartSpellCooldown()
    {
        spellCooldownRemaining = spellCooldownDuration;
    }

    private void SpawnMagicEffect(
        HeroController target,
        SpellKind completedSpellKind,
        string triggerName,
        Vector3 positionOffset,
        float lifetime,
        float playbackSpeed)
    {
        if (target == null || priestMagicPrefab == null)
        {
            return;
        }

        CancelActiveMagicEffect();

        activeMagicEffect = Instantiate(
            priestMagicPrefab,
            target.transform.position + positionOffset,
            Quaternion.identity,
            priestMagicParent);

        PriestMagicEffectRelay effectRelay = activeMagicEffect.GetComponent<PriestMagicEffectRelay>();
        if (completedSpellKind == SpellKind.Raise)
        {
            effectRelay?.PlayRaiseSound();
        }
        else if (completedSpellKind == SpellKind.Heal)
        {
            effectRelay?.PlayHealSound();
        }

        Animator effectAnimator = activeMagicEffect.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            effectAnimator.speed = playbackSpeed;
            if (!string.IsNullOrEmpty(triggerName))
            {
                effectAnimator.SetTrigger(triggerName);
            }
        }

        Destroy(activeMagicEffect, lifetime);
    }

    private void SpawnAttackMagicEffect(Transform target)
    {
        if (target == null || priestMagicPrefab == null)
        {
            return;
        }

        CancelActiveMagicEffect();

        activeMagicEffect = Instantiate(
            priestMagicPrefab,
            target.position + attackMagicOffset,
            Quaternion.identity,
            priestMagicParent);

        PriestMagicEffectRelay effectRelay = activeMagicEffect.GetComponent<PriestMagicEffectRelay>();
        if (effectRelay != null)
        {
            effectRelay.Initialize(this);
            effectRelay.PlayAttackSound();
        }

        Animator effectAnimator = activeMagicEffect.GetComponent<Animator>();
        if (effectAnimator != null)
        {
            effectAnimator.SetTrigger(attackTriggerName);
        }
    }

    public void OnAttackEffectCompleted(PriestMagicEffectRelay effect)
    {
        if (spellKind != SpellKind.Attack
            || effect == null
            || effect.gameObject != activeMagicEffect)
        {
            return;
        }

        activeMagicEffect = null;
        CancelSpell();
        Destroy(effect.gameObject);
        MaouController.Instance?.TakeDamage();
    }

    private void CancelActiveMagicEffect()
    {
        if (activeMagicEffect == null)
        {
            return;
        }

        Destroy(activeMagicEffect);
        activeMagicEffect = null;
    }

    private HeroController FindRightmostHero(float range, System.Predicate<HeroController> predicate)
    {
        HeroController rightmostHero = null;
        float rightmostX = float.NegativeInfinity;
        float rangeSqr = range * range;

        foreach (HeroController candidate in FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            if (candidate == null || !predicate(candidate))
            {
                continue;
            }

            Vector2 offset = candidate.transform.position - Hero.transform.position;
            if (offset.sqrMagnitude > rangeSqr || candidate.transform.position.x <= rightmostX)
            {
                continue;
            }

            rightmostHero = candidate;
            rightmostX = candidate.transform.position.x;
        }

        return rightmostHero;
    }

    private Transform FindGoalInRange()
    {
        GameObject goalObject = GameObject.FindGameObjectWithTag(goalTag);
        if (goalObject == null)
        {
            return null;
        }

        return Vector2.Distance(Hero.transform.position, goalObject.transform.position) <= healRange
            ? goalObject.transform
            : null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = raiseRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, raiseRange);

        Gizmos.color = healRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, healRange);
    }
}
