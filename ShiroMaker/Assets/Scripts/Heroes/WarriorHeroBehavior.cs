using UnityEngine;

public class WarriorHeroBehavior : HeroJobBehavior
{
    [SerializeField] private string attackTriggerName = "Attack01";
    [SerializeField] private string rockAttackTriggerName = "Attack2";
    [SerializeField, Min(0f), Tooltip("Time to remain still after the rock-breaking animation finishes.")]
    private float rockBreakPostAnimationWaitSeconds = 0.5f;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private Vector2 trapProbeSize = new Vector2(0.8f, 0.8f);
    [SerializeField, Min(0f)] private float rockProbeForwardExtension = 0.5f;
    [SerializeField] private LayerMask trapLayer;

    private readonly Collider2D[] trapResults = new Collider2D[12];
    private bool isAttacking;
    private MaouController targetMaou;
    private RollingRockTrap targetRock;
    private bool isRockAttacking;
    private float rockBreakPostAnimationWaitRemaining;

    public override bool CanMove()
    {
        return !isAttacking;
    }

    public override void Tick()
    {
        if (Hero == null)
        {
            return;
        }

        if (isRockAttacking)
        {
            return;
        }

        if (rockBreakPostAnimationWaitRemaining > 0f)
        {
            rockBreakPostAnimationWaitRemaining -= Time.deltaTime;
            if (rockBreakPostAnimationWaitRemaining <= 0f)
            {
                rockBreakPostAnimationWaitRemaining = 0f;
                isAttacking = false;
            }

            return;
        }

        if (isAttacking)
        {
            return;
        }

        RollingRockTrap rockAhead = FindRollingRockAt(Hero.transform.position, Hero.MoveDirection);
        if (rockAhead != null)
        {
            StartRockAttack(rockAhead);
        }
    }

    public override void OnInterrupted()
    {
        isAttacking = false;
        targetMaou = null;
        targetRock = null;
        isRockAttacking = false;
        rockBreakPostAnimationWaitRemaining = 0f;
    }

    public override void OnRestored()
    {
        isAttacking = false;
        targetMaou = null;
        targetRock = null;
        isRockAttacking = false;
        rockBreakPostAnimationWaitRemaining = 0f;
    }

    public override bool TryHandleGoalContact(Collider2D goal)
    {
        if (Hero == null || Hero.IsDead || isAttacking || goal == null || !goal.CompareTag("Goal"))
        {
            return false;
        }

        targetMaou = goal.GetComponent<MaouController>();
        if (targetMaou == null)
        {
            return false;
        }

        isAttacking = true;
        Hero.PlayJobTrigger(attackTriggerName);
        return true;
    }

    public override void OnAttackDefeatAnimationEvent()
    {
        if (!isAttacking)
        {
            return;
        }

        targetMaou?.TakeDamage();
    }

    public void StartRockAttack(RollingRockTrap rock)
    {
        if (Hero == null || Hero.IsDead || isAttacking || rock == null || !rock.IsRolling)
        {
            return;
        }

        isAttacking = true;
        isRockAttacking = true;
        targetRock = rock;
        Hero.PlayJobTrigger(rockAttackTriggerName);
    }

    public override void OnRockBreakAnimationEvent()
    {
        if (!isAttacking || targetRock == null)
        {
            return;
        }

        targetRock.BreakFromWarrior();
        targetRock = null;
    }

    // WarriorのAttack02クリップの最後のアニメーションイベントから呼び出される
    public void OnRockAttackAnimationFinished()
    {
        if (!isRockAttacking)
        {
            return;
        }

        isRockAttacking = false;
        rockBreakPostAnimationWaitRemaining = rockBreakPostAnimationWaitSeconds;
        if (rockBreakPostAnimationWaitRemaining <= 0f)
        {
            isAttacking = false;
        }
    }

    private RollingRockTrap FindRollingRockAt(Vector3 heroPosition, Vector3 moveDirection)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        if (trapLayer.value != 0)
        {
            filter.SetLayerMask(trapLayer);
        }

        Vector2 probeSize = trapProbeSize;
        probeSize.x += rockProbeForwardExtension;
        Vector3 probeCenter = heroPosition
            + moveDirection * (gridSize + rockProbeForwardExtension * 0.5f);
        int count = Physics2D.OverlapBox(probeCenter, probeSize, 0f, filter, trapResults);
        for (int i = 0; i < count; i++)
        {
            RollingRockTrap rock = trapResults[i] != null
                ? trapResults[i].GetComponentInParent<RollingRockTrap>()
                : null;
            if (rock != null && rock.IsRolling)
            {
                return rock;
            }
        }

        return null;
    }
}
