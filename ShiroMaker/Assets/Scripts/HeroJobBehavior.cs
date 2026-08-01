using UnityEngine;

public abstract class HeroJobBehavior : MonoBehaviour
{
    protected HeroController Hero { get; private set; }

    public virtual void Initialize(HeroController hero)
    {
        Hero = hero;
    }

    public virtual void Tick()
    {
    }

    public virtual bool CanMove()
    {
        return true;
    }

    public virtual void OnInterrupted()
    {
    }

    public virtual void OnRestored()
    {
    }

    public virtual bool TryHandleGoalContact(Collider2D goal)
    {
        return false;
    }

    public virtual void OnAttackDefeatAnimationEvent()
    {
    }
}
