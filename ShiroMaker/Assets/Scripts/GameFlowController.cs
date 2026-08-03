using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameFlowDefinition gameFlow;

    private FlowNode currentNode;

    public void StartFlow()
    {
        if (gameFlow == null || gameFlow.StartNode == null)
        {
            Debug.LogError("Game flow or its start node is not assigned.", this);
            return;
        }

        currentNode = gameFlow.StartNode;
        PlayCurrentNode();
    }

    public void Advance()
    {
        if (currentNode == null)
        {
            return;
        }

        currentNode = currentNode.NextNode;
        PlayCurrentNode();
    }

    private void PlayCurrentNode()
    {
        if (currentNode == null)
        {
            GameController.Instance?.CompleteGameFlow();
            return;
        }

        if (currentNode is DialogueFlowNode dialogueNode)
        {
            GameController.Instance?.PlayDialogue(dialogueNode.Dialogue, Advance);
            if (dialogueNode.UseEndingMusic)
            {
                FindFirstObjectByType<GameAudioController>()?.PlayEndingMusic();
            }

            return;
        }

        if (currentNode is StageFlowNode stageNode)
        {
            StageController.Instance?.StartStage(stageNode.Stage);
            return;
        }

        Debug.LogError($"Unsupported flow node type: {currentNode.GetType().Name}", currentNode);
    }
}
