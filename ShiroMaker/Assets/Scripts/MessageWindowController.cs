using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class MessageWindowController : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    private string[] messages = Array.Empty<string>();
    private int messageIndex;

    public bool IsShowing => messages.Length > 0;

    public void Show(TextAsset dialogue)
    {
        if (dialogue == null)
        {
            Hide();
            return;
        }

        messages = Regex.Split(dialogue.text.Trim(), @"(?:\r?\n){2,}");
        messageIndex = 0;
        gameObject.SetActive(messages.Length > 0);
        ShowCurrentMessage();
    }

    public bool Advance()
    {
        if (!IsShowing)
        {
            return false;
        }

        messageIndex++;
        if (messageIndex < messages.Length)
        {
            ShowCurrentMessage();
            return false;
        }

        Hide();
        return true;
    }

    public void Hide()
    {
        messages = Array.Empty<string>();
        messageIndex = 0;
        gameObject.SetActive(false);
    }

    private void ShowCurrentMessage()
    {
        if (messageText != null && messageIndex < messages.Length)
        {
            messageText.text = messages[messageIndex];
        }
    }
}
