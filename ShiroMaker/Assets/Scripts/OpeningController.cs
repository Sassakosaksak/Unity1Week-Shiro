using UnityEngine;
using UnityEngine.InputSystem;

public class OpeningController : MonoBehaviour
{
    [SerializeField] private MessageWindowController messageWindow;
    [SerializeField] private TextAsset dialogue;

    private void Start()
    {
        if (messageWindow == null || dialogue == null)
        {
            Debug.LogError("OpeningController needs a message window and dialogue asset.", this);
            return;
        }

        messageWindow.Show(dialogue);
    }

    private void Update()
    {
        if (GameController.Instance == null
            || GameController.Instance.CurrentPhase != GameController.GamePhase.Opening
            || Mouse.current == null
            || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (!messageWindow.Advance())
        {
            return;
        }

        StageController.Instance?.StartFirstSmallStage();
    }
}
