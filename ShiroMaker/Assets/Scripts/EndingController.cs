using UnityEngine;
using UnityEngine.InputSystem;

public class EndingController : MonoBehaviour
{
    [SerializeField] private MessageWindowController messageWindow;
    [SerializeField] private TextAsset dialogue;

    private bool isPlaying;

    public void Configure(MessageWindowController nextMessageWindow, TextAsset nextDialogue)
    {
        messageWindow = nextMessageWindow;
        dialogue = nextDialogue;
    }

    public void Begin()
    {
        if (messageWindow == null || dialogue == null)
        {
            Debug.LogError("EndingController needs a message window and dialogue asset.", this);
            return;
        }

        isPlaying = true;
        messageWindow.Show(dialogue);
    }

    private void Update()
    {
        if (!isPlaying || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (messageWindow.Advance())
        {
            isPlaying = false;
        }
    }
}
