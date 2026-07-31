using UnityEngine;

public class WizardHeroBehavior : HeroJobBehavior
{
    private enum CastKind
    {
        None,
        Attack,
        SealPit
    }

    [SerializeField, Min(0f)] private float magicRange = 4f;
    [SerializeField, Min(0.01f)] private float castDuration = 2f;
    [SerializeField, Min(0.01f)] private float temporaryFloorDuration = 3f;
    [SerializeField] private string attackTriggerName = "Attack1";
    [SerializeField] private string sealPitTriggerName = "Attack2";
    [SerializeField] private string goalTag = "Goal";
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private Vector2 trapProbeSize = new Vector2(0.8f, 0.8f);
    [SerializeField] private LayerMask trapLayer;
    [SerializeField] private Color magicRangeGizmoColor = new Color(0.35f, 0.65f, 1f, 0.35f);
    [SerializeField] private Color spikeProbeGizmoColor = new Color(1f, 0.85f, 0.2f, 0.55f);

    private readonly Collider2D[] trapResults = new Collider2D[12];

    private CastKind castKind;
    private float castRemainingTime;
    private PitfallTrap castingPit;
    private Transform goalTarget;

    public override void Tick()
    {
        if (Hero == null)
        {
            return;
        }

        if (castKind != CastKind.None)
        {
            UpdateCasting();
            return;
        }

        if (TryFindGoalInRange())
        {
            StartCasting(CastKind.Attack, null);
            return;
        }

        PitfallTrap pit = FindPitfallInRange();
        if (pit != null)
        {
            StartCasting(CastKind.SealPit, pit);
        }
    }

    public override bool CanMove()
    {
        if (castKind != CastKind.None)
        {
            return false;
        }

        if (Hero == null || Hero.IsInvincible)
        {
            return true;
        }

        SpikeTrap spikeAhead = FindSpikeAt(Hero.transform.position + Hero.MoveDirection * gridSize);
        return spikeAhead == null || spikeAhead.IsSafeToEnter;
    }

    public override void OnInterrupted()
    {
        CancelCasting();
    }

    public override void OnRestored()
    {
        CancelCasting();
    }

    private void StartCasting(CastKind nextCastKind, PitfallTrap pit)
    {
        castKind = nextCastKind;
        castRemainingTime = castDuration;
        castingPit = pit;
        Hero.PlayJobTrigger(GetTriggerName(nextCastKind));
    }

    private void UpdateCasting()
    {
        castRemainingTime -= Time.deltaTime;
        if (castRemainingTime > 0f)
        {
            return;
        }

        CompleteCasting();
    }

    private void CompleteCasting()
    {
        CastKind completedCastKind = castKind;
        PitfallTrap completedPit = castingPit;
        CancelCasting();

        if (completedCastKind == CastKind.Attack)
        {
            Hero.CausePlayerDefeat();
            return;
        }

        if (completedCastKind == CastKind.SealPit && completedPit != null && completedPit.CanBeSealed)
        {
            completedPit.Seal(temporaryFloorDuration);
        }
    }

    private void CancelCasting()
    {
        castKind = CastKind.None;
        castRemainingTime = 0f;
        castingPit = null;
    }

    private string GetTriggerName(CastKind targetCastKind)
    {
        return targetCastKind == CastKind.Attack
            ? attackTriggerName
            : sealPitTriggerName;
    }

    private bool TryFindGoalInRange()
    {
        if (goalTarget == null)
        {
            GameObject goalObject = GameObject.FindGameObjectWithTag(goalTag);
            if (goalObject != null)
            {
                goalTarget = goalObject.transform;
            }
        }

        return goalTarget != null
            && Vector2.Distance(Hero.transform.position, goalTarget.position) <= magicRange;
    }

    private PitfallTrap FindPitfallInRange()
    {
        ContactFilter2D contactFilter = CreateTrapFilter();
        int count = Physics2D.OverlapCircle(Hero.transform.position, magicRange, contactFilter, trapResults);

        PitfallTrap nearestPit = null;
        float nearestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = trapResults[i];
            PitfallTrap pit = hit != null ? hit.GetComponentInParent<PitfallTrap>() : null;
            if (pit == null || !pit.CanBeSealed)
            {
                continue;
            }

            float sqrDistance = (pit.transform.position - Hero.transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestPit = pit;
            }
        }

        return nearestPit;
    }

    private SpikeTrap FindSpikeAt(Vector3 position)
    {
        ContactFilter2D contactFilter = CreateTrapFilter();
        int count = Physics2D.OverlapBox(position, trapProbeSize, 0f, contactFilter, trapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = trapResults[i];
            SpikeTrap spike = hit != null ? hit.GetComponentInParent<SpikeTrap>() : null;
            if (spike != null)
            {
                return spike;
            }
        }

        return null;
    }

    private ContactFilter2D CreateTrapFilter()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();

        if (trapLayer.value != 0)
        {
            contactFilter.SetLayerMask(trapLayer);
        }

        return contactFilter;
    }

    private Vector3 GetSpikeProbeCenter()
    {
        Vector3 moveDirection = Hero != null
            ? Hero.MoveDirection
            : Vector3.right;

        return transform.position + moveDirection * gridSize;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = magicRangeGizmoColor;
        Gizmos.DrawWireSphere(transform.position, magicRange);

        Gizmos.color = spikeProbeGizmoColor;
        Gizmos.DrawWireCube(GetSpikeProbeCenter(), trapProbeSize);
    }
}
