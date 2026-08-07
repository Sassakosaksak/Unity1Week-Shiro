using UnityEngine;

public class RockSwitchTrap : TrapBase
{
    [SerializeField] private RollingRockTrap targetRock;

    private bool hasActivated;

    public override void OnHeroHit(HeroController hero)
    {
        // スイッチは岩を動かすだけで、勇者が触れてもダメージは受けない
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
            TrapSEController.Instance?.PlayRockSwitch();
        }
    }

    public override void RestoreForRewind()
    {
        hasActivated = false;
        base.RestoreForRewind();
    }
}
