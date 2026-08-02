using UnityEngine;

public class WarriorHeroBehavior : HeroJobBehavior
{
    [SerializeField] private string attackTriggerName = "Attack01";

    private bool isAttacking;
    private MaouController targetMaou;

    public override bool CanMove()
    {
        return !isAttacking;
    }

    public override void OnInterrupted()
    {
        isAttacking = false;
        targetMaou = null;
    }

    public override void OnRestored()
    {
        isAttacking = false;
        targetMaou = null;
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
}
