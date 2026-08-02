using UnityEngine;
using UnityEngine.InputSystem;

public class TitleController : MonoBehaviour
{
    private void Update()
    {
        if (GameController.Instance == null
            || GameController.Instance.CurrentPhase != GameController.GamePhase.Title
            || Pointer.current == null
            || !Pointer.current.press.wasPressedThisFrame)
        {
            return;
        }

        GameController.Instance.BeginOpening();
    }
}
