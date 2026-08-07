using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinates game-wide flow while delegating presentation, placement history,
/// and rewind state to focused runtime components.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameController : MonoBehaviour
{
    public enum GamePhase
    {
        Preparation = 0,
        Invasion = 1,
        Result = 2,
        Rewinding = 3,
        Dialogue = 4,
        Title = 6
    }

    public enum GameResult
    {
        Success = 0,
        Failure = 1
    }

    private enum ReturnMode
    {
        LastPlaced = 0,
        AllPlaced = 1
    }

    // These serialized fields intentionally stay on GameController. Existing scenes
    // keep their assigned references while the composed components receive them in Awake.
    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;
    [SerializeField] private GameObject maouObject;
    [SerializeField] private GameObject titleCanvasObject;
    [SerializeField] private DialogueController dialogueController;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private GameObject preparationUiObject;
    [SerializeField] private GameObject invasionUiObject;
    [SerializeField] private Button returnButton;
    [SerializeField] private ReturnMode returnMode = ReturnMode.LastPlaced;
    [SerializeField, Min(0f)] private float rewindDuration = 0.45f;
    [SerializeField] private DG.Tweening.Ease rewindEase = DG.Tweening.Ease.OutCubic;

    private GamePhasePresentation phasePresentation;
    private GamePlacementHistory placementHistory;
    private GameRewindService rewindService;
    private GameResult? currentResult;

    public static GameController Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> PhaseChanged;
    public event Action<GameResult> ResultShown;
    public event Action<GameObject> TrapPlaced;
    public event Action<GameObject, GameObject> TrapReturned;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple GameControllers were found in the scene.", this);
            return;
        }

        Instance = this;
        CurrentPhase = GamePhase.Title;

        phasePresentation = GetOrAddComponent<GamePhasePresentation>();
        placementHistory = GetOrAddComponent<GamePlacementHistory>();
        rewindService = GetOrAddComponent<GameRewindService>();

        phasePresentation.Initialize(
            successObject,
            failureObject,
            maouObject,
            titleCanvasObject,
            preparationUiObject,
            invasionUiObject);
        placementHistory.Initialize(returnButton, returnMode == ReturnMode.AllPlaced);
        rewindService.Initialize(rewindDuration, rewindEase);
    }

    private void OnDestroy()
    {
        rewindService?.Cancel();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        phasePresentation.SetResultObjectsActive(false, false);
        dialogueController?.ResetState();
        phasePresentation.ApplyPhase(CurrentPhase);
        placementHistory.Refresh(CurrentPhase);
    }

    public void StartInvasion()
    {
        rewindService.CaptureSnapshot();
        ChangePhase(GamePhase.Invasion);
    }

    public void AdvanceStage()
    {
        if (CurrentPhase == GamePhase.Result && currentResult == GameResult.Success)
        {
            gameFlowController?.Advance();
        }
    }

    public void BeginPreparation()
    {
        phasePresentation.SetResultObjectsActive(false, false);
        ChangePhase(GamePhase.Preparation);
    }

    public void BeginOpening()
    {
        if (CurrentPhase == GamePhase.Title)
        {
            StartGameFlow();
        }
    }

    public void PlayDialogue(TextAsset dialogue, Action completed)
    {
        phasePresentation.SetResultObjectsActive(false, false);
        ChangePhase(GamePhase.Dialogue);
        dialogueController?.Play(dialogue, completed);
    }

    public void CompleteGameFlow()
    {
        ResetForTitle();
    }

    public void ShowSuccess()
    {
        ResolveResult(GameResult.Success);
    }

    public void ShowFailure()
    {
        ResolveResult(GameResult.Failure);
    }

    public void ResolveResult(GameResult result)
    {
        if (CurrentPhase == GamePhase.Result)
        {
            return;
        }

        ChangePhase(GamePhase.Result);
        currentResult = result;
        bool isSuccess = result == GameResult.Success;
        phasePresentation.SetResultObjectsActive(isSuccess, !isSuccess);
        ResultShown?.Invoke(result);
        Debug.Log(isSuccess ? "Success" : "Defeat");
    }

    public bool AreAllHeroesDead()
    {
        HeroController[] heroes = FindObjectsByType<HeroController>(FindObjectsSortMode.None);
        if (heroes.Length == 0)
        {
            return false;
        }

        foreach (HeroController hero in heroes)
        {
            if (!hero.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    public void RegisterPlacedTrap(GameObject placedTrap, GameObject trapPrefab)
    {
        if (placedTrap == null || CurrentPhase != GamePhase.Preparation)
        {
            return;
        }

        placementHistory.Register(placedTrap, trapPrefab);
        TrapPlaced?.Invoke(placedTrap);
    }

    public void ClearCurrentStageUndoHistory()
    {
        placementHistory.Clear(CurrentPhase);
    }

    public void ReturnPlacedTrap()
    {
        placementHistory.Return(CurrentPhase, OnPlacedTrapReturned);
    }

    public void RewindInvasion()
    {
        if (CurrentPhase == GamePhase.Invasion)
        {
            BeginInvasionRewind();
        }
    }

    public void RetryInvasion()
    {
        if (CurrentPhase == GamePhase.Result)
        {
            BeginInvasionRewind();
        }
    }

    private void BeginInvasionRewind()
    {
        rewindService.TryBeginRewind(
            () =>
            {
                phasePresentation.SetResultObjectsActive(false, false);
                ChangePhase(GamePhase.Rewinding);
            },
            () => ChangePhase(GamePhase.Preparation));
    }

    private void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase)
        {
            return;
        }

        CurrentPhase = nextPhase;
        phasePresentation.ApplyPhase(CurrentPhase);
        placementHistory.Refresh(CurrentPhase);
        PhaseChanged?.Invoke(CurrentPhase);
    }

    private void StartGameFlow()
    {
        StageController.Instance?.ResetForTitle();
        gameFlowController?.StartFlow();
    }

    private void ResetForTitle()
    {
        dialogueController?.ResetState();
        StageController.Instance?.ResetForTitle();
        ChangePhase(GamePhase.Title);
    }

    private void OnPlacedTrapReturned(GameObject trap, GameObject trapPrefab)
    {
        TrapReturned?.Invoke(trap, trapPrefab);
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
