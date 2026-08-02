using UnityEngine;

public class HeroAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private HeroController hero;

    private HeroSEController heroSeController;

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponentInParent<HeroController>();
        }

        heroSeController = GetComponentInParent<HeroSEController>();
    }

    // Called by the Defeat Animation Event on the Warrior Attack01 clip.
    public void Defeat()
    {
        hero?.OnAttackDefeatAnimationEvent();
    }

    // Called by the Attack01 Animation Event on the Warrior Visual.
    public void PlayAttack()
    {
        heroSeController?.PlayAttack();
    }

    // Called by the Hurt Animation Event on the Hero Visual.
    public void PlayHurt()
    {
        heroSeController?.PlayHurt();
    }

    // Called by the Death Animation Event on the Hero Visual.
    public void PlayDeath()
    {
        heroSeController?.PlayDeath();
    }
}
