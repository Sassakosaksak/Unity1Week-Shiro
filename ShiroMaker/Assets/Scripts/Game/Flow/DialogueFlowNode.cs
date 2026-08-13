using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Game Flow/Dialogue Node", fileName = "DialogueFlowNode")]
public class DialogueFlowNode : FlowNode
{
    [SerializeField] private TextAsset dialogue;
    [SerializeField] private bool useEndingMusic;

    public TextAsset Dialogue => dialogue;
    public bool UseEndingMusic => useEndingMusic;
}
