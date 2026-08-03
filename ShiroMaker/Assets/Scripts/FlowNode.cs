using UnityEngine;

public abstract class FlowNode : ScriptableObject
{
    [SerializeField] private FlowNode nextNode;

    public FlowNode NextNode => nextNode;
}
