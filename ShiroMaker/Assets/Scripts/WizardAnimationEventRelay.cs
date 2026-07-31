using UnityEngine;

public class WizardAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private WizardHeroBehavior wizardBehavior;

    private void Awake()
    {
        if (wizardBehavior == null)
        {
            wizardBehavior = GetComponentInParent<WizardHeroBehavior>();
        }
    }

    public void StartFloorSetting()
    {
        wizardBehavior?.StartFloorSettingFromAnimation();
    }
}
