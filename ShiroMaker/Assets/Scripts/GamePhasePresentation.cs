using UnityEngine;

[DisallowMultipleComponent]
public sealed class GamePhasePresentation : MonoBehaviour
{
    private GameObject successObject;
    private GameObject failureObject;
    private GameObject maouObject;
    private GameObject titleCanvasObject;
    private GameObject preparationUiObject;
    private GameObject invasionUiObject;

    public void Initialize(
        GameObject configuredSuccessObject,
        GameObject configuredFailureObject,
        GameObject configuredMaouObject,
        GameObject configuredTitleCanvasObject,
        GameObject configuredPreparationUiObject,
        GameObject configuredInvasionUiObject)
    {
        successObject = configuredSuccessObject;
        failureObject = configuredFailureObject;
        maouObject = configuredMaouObject;
        titleCanvasObject = configuredTitleCanvasObject;
        preparationUiObject = configuredPreparationUiObject;
        invasionUiObject = configuredInvasionUiObject;
    }

    public void SetResultObjectsActive(bool showSuccess, bool showFailure)
    {
        if (successObject != null)
        {
            successObject.SetActive(showSuccess);
        }

        if (failureObject != null)
        {
            failureObject.SetActive(showFailure);
        }
    }

    public void ApplyPhase(GameController.GamePhase phase)
    {
        if (maouObject != null)
        {
            maouObject.SetActive(phase != GameController.GamePhase.Title && phase != GameController.GamePhase.Dialogue);
        }

        if (titleCanvasObject != null)
        {
            titleCanvasObject.SetActive(phase == GameController.GamePhase.Title);
        }

        if (preparationUiObject != null)
        {
            preparationUiObject.SetActive(phase == GameController.GamePhase.Preparation);
        }

        if (invasionUiObject != null)
        {
            invasionUiObject.SetActive(phase == GameController.GamePhase.Invasion);
        }
    }
}
