using UnityEngine;

public class HeroAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private HeroController hero;

    private HeroSEController heroSeController;
    private WarriorHeroBehavior warriorBehavior;

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponentInParent<HeroController>();
        }

        heroSeController = GetComponentInParent<HeroSEController>();
        warriorBehavior = GetComponentInParent<WarriorHeroBehavior>();
    }

    // WarriorのAttack01クリップにあるDefeatアニメーションイベントから呼び出されます。
    public void Defeat()
    {
        hero?.OnAttackDefeatAnimationEvent();
    }

    // WarriorのAttack02クリップにあるBreakRockアニメーションイベントから呼び出されます。
    public void BreakRock()
    {
        heroSeController?.PlayRockBreakAttack();
        hero?.OnRockBreakAnimationEvent();
    }

    // WarriorのAttack02クリップの最後のアニメーションイベントから呼び出されます。
    public void FinishRockAttack()
    {
        warriorBehavior?.OnRockAttackAnimationFinished();
    }

    // WarriorのVisualにあるAttack01アニメーションイベントから呼び出されます。
    public void PlayAttack()
    {
        heroSeController?.PlayAttack();
    }

    // HeroのVisualにあるHurtアニメーションイベントから呼び出されます。
    public void PlayHurt()
    {
        heroSeController?.PlayHurt();
    }

    // HeroのVisualにあるDeathアニメーションイベントから呼び出されます。
    public void PlayDeath()
    {
        heroSeController?.PlayDeath();
    }

    public void StartMagicCasting()
    {
        heroSeController?.StartMagicCasting();
    }
}
