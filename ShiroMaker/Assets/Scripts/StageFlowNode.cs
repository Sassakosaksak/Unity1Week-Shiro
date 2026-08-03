using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Game Flow/Stage Node", fileName = "StageFlowNode")]
public class StageFlowNode : FlowNode
{
    [SerializeField] private SmallStageDefinition stage;

    public SmallStageDefinition Stage => stage;
}
