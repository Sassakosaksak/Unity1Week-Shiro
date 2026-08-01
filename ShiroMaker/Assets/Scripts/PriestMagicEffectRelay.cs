using UnityEngine;

public class PriestMagicEffectRelay : MonoBehaviour
{
    private PriestHeroBehavior source;

    public void Initialize(PriestHeroBehavior priest)
    {
        source = priest;
    }

    // Called by the Attack Effect's final Animation Event.
    public void Defeat()
    {
        source?.OnAttackEffectCompleted(this);
    }
}
