using UnityEngine;

public class PriestHeroBehavior : HeroJobBehavior
{
    private enum SpellKind
    {
        None,
        Raise,
        Heal
    }

    [SerializeField, Min(0f)] private float raiseRange = 2.5f;
    [SerializeField, Min(0.01f)] private float raiseDuration = 5f;
    [SerializeField] private Color raiseFlashColor = Color.green;
    [SerializeField, Min(0.01f)] private float raiseFlashInterval = 0.1f;
    [SerializeField, Min(0f)] private float reviveInvincibilityDuration = 2f;
    [SerializeField, Min(0f)] private float healRange = 4.5f;
    [SerializeField, Min(0.01f)] private float healDuration = 1f;
    [SerializeField, Min(1)] private int healAmount = 1;
    [SerializeField] private Color healFlashColor = Color.green;
    [SerializeField, Min(0.01f)] private float healColorFadeDuration = 0.2f;
    [SerializeField, Min(0f)] private float healColorHoldDuration = 1f;
    [SerializeField] private string raiseTriggerName = "Raise";
    [SerializeField] private string healTriggerName = "Heal";
    [SerializeField] private Color raiseRangeGizmoColor = new Color(0.9f, 0.6f, 1f, 0.45f);
    [SerializeField] private Color healRangeGizmoColor = new Color(0.35f, 1f, 0.55f, 0.35f);

    private SpellKind spellKind;
    private HeroController spellTarget;
    private float spellRemainingTime;

    public override void Tick()
    {
        if (Hero == null)
        {
            return;
        }

        if (spellKind != SpellKind.None)
        {
            UpdateSpell();
            return;
        }

        HeroController reviveTarget = FindRightmostHero(raiseRange, hero => hero.IsDead);
        if (reviveTarget != null)
        {
            StartSpell(SpellKind.Raise, reviveTarget);
            return;
        }

        HeroController healTarget = FindRightmostHero(
            healRange,
            hero => !hero.IsDead && hero.CurrentHp < hero.MaxHp);
        if (healTarget != null)
        {
            StartSpell(SpellKind.Heal, healTarget);
        }
    }

    public override bool CanMove()
    {
        return spellKind == SpellKind.None;
    }

    public override void OnInterrupted()
    {
        CancelSpell();
    }

    public override void OnRestored()
    {
        CancelSpell();
    }

    private void StartSpell(SpellKind nextSpellKind, HeroController target)
    {
        spellKind = nextSpellKind;
        spellTarget = target;
        spellRemainingTime = nextSpellKind == SpellKind.Raise ? raiseDuration : healDuration;

        if (nextSpellKind == SpellKind.Raise)
        {
            spellTarget.StartGreenFlash(raiseFlashColor, raiseFlashInterval);
        }

        Hero.PlayJobTrigger(nextSpellKind == SpellKind.Raise ? raiseTriggerName : healTriggerName);
    }

    private void UpdateSpell()
    {
        if (spellTarget == null)
        {
            CancelSpell();
            return;
        }

        spellRemainingTime -= Time.deltaTime;
        if (spellRemainingTime > 0f)
        {
            return;
        }

        SpellKind completedSpellKind = spellKind;
        HeroController completedTarget = spellTarget;
        CancelSpell();

        if (completedSpellKind == SpellKind.Raise)
        {
            if (completedTarget.ReviveAtFullHealth())
            {
                completedTarget.GrantInvincibility(reviveInvincibilityDuration);
            }

            return;
        }

        if (completedTarget.Heal(healAmount))
        {
            completedTarget.PlayColorPulse(healFlashColor, healColorFadeDuration, healColorHoldDuration);
        }
    }

    private void CancelSpell()
    {
        if (spellKind == SpellKind.Raise && spellTarget != null)
        {
            spellTarget.StopGreenFlash();
        }

        spellKind = SpellKind.None;
        spellTarget = null;
        spellRemainingTime = 0f;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = raiseRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, raiseRange);

        Gizmos.color = healRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, healRange);
    }
}
