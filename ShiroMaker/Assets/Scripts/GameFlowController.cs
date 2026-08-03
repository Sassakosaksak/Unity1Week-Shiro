using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    [SerializeField] private GameFlowDefinition gameFlow;
    [SerializeField] private ScreenFadeController screenFadeController;

    private FlowNode currentNode;
    private bool isTransitioning;

    public void StartFlow()
    {
        if (gameFlow == null || gameFlow.StartNode == null)
        {
            Debug.LogError("Game flow or its start node is not assigned.", this);
            return;
        }

        currentNode = gameFlow.StartNode;
        TransitionToCurrentNode();
    }

    public void Advance()
    {
        if (currentNode == null || isTransitioning)
        {
            return;
        }

        currentNode = currentNode.NextNode;
        TransitionToCurrentNode();
    }

    private void TransitionToCurrentNode()
    {
        if (screenFadeController == null)
        {
            PlayCurrentNode();
            return;
        }

        isTransitioning = true;
        screenFadeController.PlayTransition(() =>
        {
            isTransitioning = false;
            PlayCurrentNode();
        });
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
