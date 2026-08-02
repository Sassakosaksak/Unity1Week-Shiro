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
    [SerializeField, Min(0f)] private float attackCastStartDelay = 0.5f;
    [SerializeField, Min(0f)] private float sealPitCastStartDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float attackCastDuration = 2f;
    [SerializeField, Min(0.01f)] private float sealPitCastDuration = 2f;
    [SerializeField] private IceAttackEffect iceAttackPrefab;
    [SerializeField] private Transform iceAttackParent;
    [SerializeField] private GameObject temporaryGroundPrefab;
    [SerializeField] private Transform temporaryGroundParent;
    [SerializeField] private Vector3 temporaryGroundBottomCenterOffset = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private string attackTriggerName = "Attack1";
    [SerializeField] private string attackShotTriggerName = "Attack1_Shot";
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
    private float attackDelayRemainingTime;
    private float sealPitDelayRemainingTime;
    private bool isAttackPending;
    private PitfallTrap pendingSealPit;
    private PitfallTrap castingPit;
    private Transform goalTarget;
    private bool floorSettingStarted;
    private bool attackShotTriggered;
    private IceAttackEffect activeIceAttack;
    private GameController gameController;

    public override void Initialize(HeroController hero)
    {
        base.Initialize(hero);
        gameController = GameController.Instance;

        if (gameController != null)
        {
            gameController.PhaseChanged += OnGamePhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnGamePhaseChanged;
        }
    }

    public override void Tick()
    {
        if (Hero == null)
        {
            return;
        }

        if (activeIceAttack != null)
        {
            return;
        }

        if (castKind != CastKind.None)
        {
            UpdateCasting();
            return;
        }

        if (isAttackPending)
        {
            UpdateAttackDelay();
            return;
        }

        if (TryFindGoalInRange())
        {
            CancelPendingSealPit();
            StartAttackDelay();
            return;
        }

        if (pendingSealPit != null)
        {
            UpdateSealPitDelay();
            return;
        }

        PitfallTrap pit = FindPitfallInRange();
        if (pit != null)
        {
            StartSealPitDelay(pit);
        }
    }

    public override bool CanMove()
    {
        if (castKind != CastKind.None || activeIceAttack != null)
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
        Hero?.GetComponent<HeroSEController>()?.StopMagicCasting();
        CancelPendingAttack();
        CancelPendingSealPit();
        CancelCasting();
    }

    public override void OnRestored()
    {
        Hero?.GetComponent<HeroSEController>()?.StopMagicCasting();
        CancelPendingAttack();
        CancelPendingSealPit();
        CancelCasting();
        CancelActiveIceAttack();
    }

    private void OnGamePhaseChanged(GameController.GamePhase phase)
    {
        if (phase != GameController.GamePhase.Rewinding
            && phase != GameController.GamePhase.Result)
        {
            return;
        }

        Hero?.GetComponent<HeroSEController>()?.StopMagicCasting();
        CancelActiveIceAttack();
    }

    // Called by the StartFloorSetting Animation Event in Attack2.
    public void StartFloorSettingFromAnimation()
    {
        if (castKind != CastKind.SealPit
            || floorSettingStarted
            || castingPit == null
            || !castingPit.CanBeSealed)
        {
            return;
        }

        floorSettingStarted = castingPit.BeginTemporaryGroundSetting(
            temporaryGroundPrefab,
            temporaryGroundBottomCenterOffset,
            temporaryGroundParent);
    }

    // Called by the SpawnIceAttack Animation Event in Attack1_Shot.
    public void SpawnIceAttackFromAnimation()
    {
        if (castKind != CastKind.Attack
            || activeIceAttack != null
            || iceAttackPrefab == null
            || !TryFindGoalInRange())
        {
            return;
        }

        Vector3 targetPosition = goalTarget.position;
        IceAttackEffect iceAttack = Instantiate(iceAttackPrefab, Vector3.zero, Quaternion.identity, iceAttackParent);
        PlaceableAnchor placementAnchor = iceAttack.GetComponent<PlaceableAnchor>();
        iceAttack.transform.position = placementAnchor != null
            ? placementAnchor.GetRootPositionForCellCenter(targetPosition)
            : targetPosition;

        activeIceAttack = iceAttack;
        activeIceAttack.Initialize(this);
        CancelCasting(false);
    }

    public void OnIceAttackHit(IceAttackEffect iceAttack)
    {
        if (iceAttack == null || iceAttack != activeIceAttack)
        {
            return;
        }

        activeIceAttack = null;
        Destroy(iceAttack.gameObject);
        MaouController.Instance?.TakeDamage();
    }

    private void StartCasting(CastKind nextCastKind, PitfallTrap pit)
    {
        castKind = nextCastKind;
        castRemainingTime = GetCastDuration(nextCastKind);
        castingPit = pit;
        floorSettingStarted = false;
        attackShotTriggered = false;
        Hero.PlayJobTrigger(GetTriggerName(nextCastKind));
    }

    private void StartSealPitDelay(PitfallTrap pit)
    {
        pendingSealPit = pit;
        sealPitDelayRemainingTime = sealPitCastStartDelay;
    }

    private void StartAttackDelay()
    {
        isAttackPending = true;
        attackDelayRemainingTime = attackCastStartDelay;
    }

    private void UpdateAttackDelay()
    {
        if (!TryFindGoalInRange())
        {
            CancelPendingAttack();
            return;
        }

        attackDelayRemainingTime -= Time.deltaTime;
        if (attackDelayRemainingTime > 0f)
        {
            return;
        }

        CancelPendingAttack();
        StartCasting(CastKind.Attack, null);
    }

    private void UpdateSealPitDelay()
    {
        if (pendingSealPit == null || !pendingSealPit.CanBeSealed)
        {
            CancelPendingSealPit();
            return;
        }

        sealPitDelayRemainingTime -= Time.deltaTime;
        if (sealPitDelayRemainingTime > 0f)
        {
            return;
        }

        PitfallTrap targetPit = pendingSealPit;
        CancelPendingSealPit();
        StartCasting(CastKind.SealPit, targetPit);
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
        if (castKind == CastKind.Attack)
        {
            TriggerAttackShot();
            return;
        }

        CancelCasting(false);
    }

    private void TriggerAttackShot()
    {
        if (attackShotTriggered)
        {
            return;
        }

        attackShotTriggered = true;
        Hero.PlayJobTrigger(attackShotTriggerName);
    }

    private void CancelPendingAttack()
    {
        isAttackPending = false;
        attackDelayRemainingTime = 0f;
    }

    private void CancelPendingSealPit()
    {
        pendingSealPit = null;
        sealPitDelayRemainingTime = 0f;
    }

    private void CancelCasting(bool cancelFloorSetting = true)
    {
        if (cancelFloorSetting && floorSettingStarted && castingPit != null)
        {
            castingPit.CancelTemporaryGroundSetting();
        }

        castKind = CastKind.None;
        castRemainingTime = 0f;
        castingPit = null;
        floorSettingStarted = false;
        attackShotTriggered = false;
    }

    private void CancelActiveIceAttack()
    {
        if (activeIceAttack == null)
        {
            return;
        }

        IceAttackEffect iceAttack = activeIceAttack;
        activeIceAttack = null;
        Destroy(iceAttack.gameObject);
    }

    private string GetTriggerName(CastKind targetCastKind)
    {
        return targetCastKind == CastKind.Attack
            ? attackTriggerName
            : sealPitTriggerName;
    }

    private float GetCastDuration(CastKind targetCastKind)
    {
        return targetCastKind == CastKind.Attack
            ? attackCastDuration
            : sealPitCastDuration;
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

        PitfallTrap rightmostPit = null;
        float rightmostX = float.NegativeInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = trapResults[i];
            PitfallTrap pit = hit != null ? hit.GetComponentInParent<PitfallTrap>() : null;
            if (pit == null || !pit.CanBeSealed)
            {
                continue;
            }

            float x = pit.transform.position.x;
            if (x > rightmostX)
            {
                rightmostX = x;
                rightmostPit = pit;
            }
        }

        return rightmostPit;
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
