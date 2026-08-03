using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// 空行で区切られたテキストを、1メッセージずつ表示する。
///
/// テキスト例:
/// おお……
/// おぬしが噂に聞く罠師か。
///
/// 最近勇者が涌いて困っていてなぁ。
///
/// 表示結果:
/// 1回目は「おお……\nおぬしが噂に聞く罠師か。」、次へ進むと
/// 2回目に「最近勇者が涌いて困っていてなぁ。」が表示される。
/// 
/// 注意:
/// 表示枠にきれいに収めるため、1メッセージは3行以内で記述する。
/// </summary>
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

        // Windows/Unix両方の改行に対応し、2回以上続く改行（空行）でメッセージを分割する。
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

    public void Advance()
    {
        if (!isPlaying)
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
