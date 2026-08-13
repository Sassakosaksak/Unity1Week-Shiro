using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UIButtonSE の種別を参照して、共通の決定／キャンセル SE を再生します。
/// </summary>
public class UIButtonSEController : MonoBehaviour
{
    [SerializeField] private AudioClip confirmClip;
    [SerializeField, Range(0f, 1f)] private float confirmVolume = 1f;
    [SerializeField] private AudioClip cancelClip;
    [SerializeField, Range(0f, 1f)] private float cancelVolume = 1f;
    [SerializeField] private AudioClip inviteClip;
    [SerializeField, Range(0f, 1f)] private float inviteVolume = 1f;

    private readonly HashSet<UIButtonSE> boundButtons = new HashSet<UIButtonSE>();
    private readonly Dictionary<UIButtonSE, UnityAction> clickHandlers = new Dictionary<UIButtonSE, UnityAction>();

    private void Start()
    {
        BindAllButtons();
    }

    private void LateUpdate()
    {
        BindAllButtons();
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<UIButtonSE, UnityAction> pair in clickHandlers)
        {
            if (pair.Key != null && pair.Key.Button != null)
            {
                pair.Key.Button.onClick.RemoveListener(pair.Value);
            }
        }
    }

    private void BindAllButtons()
    {
        UIButtonSE[] buttons = FindObjectsByType<UIButtonSE>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UIButtonSE buttonSE in buttons)
        {
            if (buttonSE == null || buttonSE.Button == null || !boundButtons.Add(buttonSE))
            {
                continue;
            }

            UIButtonSE targetButtonSE = buttonSE;
            UnityAction handler = () => Play(targetButtonSE.SEType);
            clickHandlers.Add(targetButtonSE, handler);
            targetButtonSE.Button.onClick.AddListener(handler);
        }
    }

    private void Play(UIButtonSEType seType)
    {
        switch (seType)
        {
            case UIButtonSEType.Confirm:
                SEController.Instance?.Play(confirmClip, confirmVolume);
                break;
            case UIButtonSEType.Cancel:
                SEController.Instance?.Play(cancelClip, cancelVolume);
                break;
            case UIButtonSEType.Invite:
                SEController.Instance?.Play(inviteClip, inviteVolume);
                break;
        }
    }
}
