using UnityEngine;

public class RockSwitchTrap : TrapBase
{
    [SerializeField] private RollingRockTrap targetRock;

    private bool hasActivated;

    public override void OnHeroHit(HeroController hero)
    {
        // The switch only starts the rock; touching it never damages a hero.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanRun || hasActivated || other.GetComponentInParent<HeroController>() == null)
        {
            return;
        }

        if (targetRock != null && targetRock.Activate())
        {
            hasActivated = true;
        }
    }

    public override void RestoreForRewind()
    {
        hasActivated = false;
        base.RestoreForRewind();
    }
}
