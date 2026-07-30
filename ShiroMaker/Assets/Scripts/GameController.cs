using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameController : MonoBehaviour
{
    public enum GamePhase
    {
        Preparation,
        Invasion,
        Result
    }

    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;

    public static GameController Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> PhaseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple GameControllers were found in the scene.", this);
            return;
        }

        Instance = this;
        CurrentPhase = GamePhase.Preparation;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SetResultObjectsActive(false, false);
    }
    public void StartInvasion()
    {
        ChangePhase(GamePhase.Invasion);
    }


    public void ShowSuccess()
    {
        ChangePhase(GamePhase.Result);
        SetResultObjectsActive(true, false);
        Debug.Log("Success");
    }

    public void ShowFailure()
    {
        ChangePhase(GamePhase.Result);
        SetResultObjectsActive(false, true);
        Debug.Log("Defeat");
    }

    private void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase)
        {
            return;
        }

        CurrentPhase = nextPhase;
        PhaseChanged?.Invoke(CurrentPhase);
    }

    private void SetResultObjectsActive(bool showSuccess, bool showFailure)
    {
        if (successObject != null)
        {
            successObject.SetActive(showSuccess);
        }

        if (failureObject != null)
        {
            failureObject.SetActive(showFailure);
        }
    }
}
