using UnityEngine;

public class TitleController : MonoBehaviour
{
    public void StartGame()
    {
        if (GameController.Instance == null
            || GameController.Instance.CurrentPhase != GameController.GamePhase.Title)
        {
            return;
        }

        GameController.Instance.BeginOpening();
    }
}
