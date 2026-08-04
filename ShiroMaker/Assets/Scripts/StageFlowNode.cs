using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Game Flow/Stage Node", fileName = "StageFlowNode")]
public class StageFlowNode : FlowNode
{
    [SerializeField] private SmallStageDefinition stage;
    [SerializeField] private TutorialDefinition tutorial;

    public SmallStageDefinition Stage => stage;
    public TutorialDefinition Tutorial => tutorial;
}
