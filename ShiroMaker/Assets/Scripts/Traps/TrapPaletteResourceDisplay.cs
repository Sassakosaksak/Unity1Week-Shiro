using TMPro;
using UnityEngine;

public class TrapPaletteResourceDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text spikeCountText;
    [SerializeField] private TMP_Text rockCountText;
    [SerializeField] private TMP_Text pitfallCountText;
    [SerializeField, Range(0f, 1f)] private float emptyResourceAlpha = 0.45f;

    private StageController stageController;

    private void OnEnable()
    {
        stageController = StageController.Instance;
        if (stageController == null)
        {
            return;
        }

        stageController.TrapSuppliesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (stageController != null)
        {
            stageController.TrapSuppliesChanged -= Refresh;
            stageController = null;
        }
    }

    private void Refresh()
    {
        if (stageController == null)
        {
            return;
        }

        SetResourceState(spikeCountText, stageController.GetRemainingTrapSupply(TrapType.Spike));
        SetResourceState(rockCountText, stageController.GetRemainingTrapSupply(TrapType.Rock));
        SetResourceState(pitfallCountText, stageController.GetRemainingTrapSupply(TrapType.Pitfall));
    }

    private void SetResourceState(TMP_Text countText, int count)
    {
        if (countText == null)
        {
            return;
        }

        countText.text = "\u00D7" + count;

        CanvasGroup canvasGroup = countText.transform.parent.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = countText.transform.parent.gameObject.AddComponent<CanvasGroup>();
        }

        bool hasSupply = count > 0;
        canvasGroup.alpha = hasSupply ? 1f : emptyResourceAlpha;
        canvasGroup.interactable = hasSupply;
        canvasGroup.blocksRaycasts = hasSupply;
    }
}
