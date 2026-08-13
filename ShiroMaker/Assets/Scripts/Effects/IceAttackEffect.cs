using UnityEngine;

public class IceAttackEffect : MonoBehaviour
{
    private WizardHeroBehavior owner;

    public void Initialize(WizardHeroBehavior wizard)
    {
        owner = wizard;
    }

    // IceAttackクリップの最後のHitアニメーションイベントから呼び出される
    public void Hit()
    {
        owner?.OnIceAttackHit(this);
    }
}
