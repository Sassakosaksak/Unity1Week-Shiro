using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueController : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TextAsset openingDialogue;
    [SerializeField] private TextAsset endingDialogue;

    private string[] messages = Array.Empty<string>();
    private int messageIndex;
    private Action onCompleted;
    private bool isPlaying;

    public void PlayOpening(Action completed)
    {
        Play(openingDialogue, completed);
    }

    public void PlayEnding(Action completed)
    {
        Play(endingDialogue, completed);
    }

    public void Play(TextAsset dialogue, Action completed)
    {
        ResetState();

        if (dialogue == null)
        {
            Debug.LogError("Dialogue text is not assigned.", this);
            return;
        }

        string text = dialogue.text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogError("Dialogue text is empty.", dialogue);
            return;
        }

        messages = Regex.Split(text, @"(?:\r?\n){2,}");
        messageIndex = 0;
        onCompleted = completed;
        isPlaying = true;
        gameObject.SetActive(true);
        ShowCurrentMessage();
    }

    public void ResetState()
    {
        messages = Array.Empty<string>();
        messageIndex = 0;
        onCompleted = null;
        isPlaying = false;

        if (messageText != null)
        {
            messageText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying
            || Mouse.current == null
            || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        messageIndex++;
        if (messageIndex < messages.Length)
        {
            ShowCurrentMessage();
            return;
        }

        Action completed = onCompleted;
        ResetState();
        completed?.Invoke();
    }

    private void ShowCurrentMessage()
    {
        if (messageText != null && messageIndex < messages.Length)
        {
            messageText.text = messages[messageIndex];
        }
    }
}
