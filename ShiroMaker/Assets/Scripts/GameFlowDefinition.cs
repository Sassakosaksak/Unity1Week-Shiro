using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Game Flow/Definition", fileName = "GameFlow")]
public class GameFlowDefinition : ScriptableObject
{
    [SerializeField] private FlowNode startNode;

    public FlowNode StartNode => startNode;
}
