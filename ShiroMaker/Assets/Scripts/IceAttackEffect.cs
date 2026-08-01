using UnityEngine;

public class IceAttackEffect : MonoBehaviour
{
    private WizardHeroBehavior owner;

    public void Initialize(WizardHeroBehavior wizard)
    {
        owner = wizard;
    }

    // Called by the final Hit Animation Event in the IceAttack clip.
    public void Hit()
    {
        owner?.OnIceAttackHit(this);
    }
}
