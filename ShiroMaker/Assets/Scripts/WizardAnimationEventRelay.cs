using UnityEngine;

public class WizardAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private WizardHeroBehavior wizardBehavior;

    private HeroSEController heroSeController;

    private void Awake()
    {
        if (wizardBehavior == null)
        {
            wizardBehavior = GetComponentInParent<WizardHeroBehavior>();
        }

        heroSeController = GetComponentInParent<HeroSEController>();
    }

    public void StartFloorSetting()
    {
        wizardBehavior?.StartFloorSettingFromAnimation();
    }

    public void SpawnIceAttack()
    {
        wizardBehavior?.SpawnIceAttackFromAnimation();
    }

    public void StartMagicCasting()
    {
        heroSeController?.StartMagicCasting();
    }

    public void StopMagicCasting()
    {
        heroSeController?.StopMagicCasting();
    }

    public void PlayMagicShot()
    {
        heroSeController?.PlayMagicShot();
    }
}
