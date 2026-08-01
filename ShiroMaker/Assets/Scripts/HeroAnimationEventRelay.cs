using UnityEngine;

public class HeroAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private HeroController hero;

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponentInParent<HeroController>();
        }
    }

    // Called by the Defeat Animation Event on the Warrior Attack01 clip.
    public void Defeat()
    {
        hero?.OnAttackDefeatAnimationEvent();
    }
}
