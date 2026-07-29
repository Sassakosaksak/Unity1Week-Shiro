using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;

    private void Start()
    {
        SetResultObjectsActive(false, false);
    }

    public void ShowSuccess()
    {
        SetResultObjectsActive(true, false);
        Debug.Log("Success");
    }

    public void ShowFailure()
    {
        SetResultObjectsActive(false, true);
        Debug.Log("Defeat");
    }

    private void SetResultObjectsActive(bool showSuccess, bool showFailure)
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
}
