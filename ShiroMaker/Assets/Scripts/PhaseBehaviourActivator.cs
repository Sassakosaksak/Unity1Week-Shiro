using UnityEngine;

public class PhaseBehaviourActivator : MonoBehaviour
{
    [SerializeField] private GameController.GamePhase activePhase;
    [SerializeField] private Behaviour[] targetBehaviours;

    private GameController gameController;

    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
            return;
        }

        gameController.PhaseChanged += OnPhaseChanged;
        ApplyPhase(gameController.CurrentPhase);
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        ApplyPhase(phase);
    }

    private void ApplyPhase(GameController.GamePhase phase)
    {
        bool shouldEnable = phase == activePhase;

        foreach (Behaviour targetBehaviour in targetBehaviours)
        {
            if (targetBehaviour != null)
            {
                targetBehaviour.enabled = shouldEnable;
            }
        }
    }
}
