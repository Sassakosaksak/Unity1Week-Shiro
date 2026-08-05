using UnityEngine;
using UnityEngine.UI;

public enum UIButtonSEType
{
    None,
    Confirm,
    Cancel,
    Invite
}

/// <summary>
/// この Button が押されたときに鳴らす共通 UI SE の種類を指定します。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSE : MonoBehaviour
{
    [SerializeField] private UIButtonSEType seType = UIButtonSEType.Confirm;

    public UIButtonSEType SEType => seType;
    public Button Button { get; private set; }

    private void Awake()
    {
        Button = GetComponent<Button>();
    }
}
