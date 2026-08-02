using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class GameController : MonoBehaviour
{
    public enum GamePhase
    {
        Preparation = 0,
        Invasion = 1,
        Result = 2,
        Rewinding = 3,
        Opening = 4,
        Ending = 5
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

    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;
    [SerializeField] private GameObject openingUiObject;
    [SerializeField] private TextAsset endingDialogue;
    [SerializeField] private ScreenFadeController screenFadeController;
    [SerializeField] private GameObject preparationUiObject;
    [SerializeField] private GameObject invasionUiObject;
    [SerializeField] private Button returnButton;
    [SerializeField] private ReturnMode returnMode = ReturnMode.LastPlaced;
    [SerializeField, Min(0f)] private float rewindDuration = 0.45f;
    [SerializeField] private Ease rewindEase = Ease.OutCubic;

    public static GameController Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> PhaseChanged;
    public event Action<GameResult> ResultShown;
    public event Action<GameObject> TrapPlaced;
    public event Action<GameObject, GameObject> TrapReturned;

    private readonly List<HeroSnapshot> heroSnapshots = new List<HeroSnapshot>();
    private readonly List<TrapSnapshot> trapSnapshots = new List<TrapSnapshot>();
    private readonly List<PlacedTrapRecord> placedTrapHistory = new List<PlacedTrapRecord>();
    private Sequence rewindSequence;
    private GameResult? currentResult;
    private GameObject endingUiObject;
    private EndingController endingController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple GameControllers were found in the scene.", this);
            return;
        }

        Instance = this;
        CurrentPhase = GamePhase.Opening;
        CreateEndingUi();
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
        ApplyPhaseUi(CurrentPhase);
        UpdateReturnButtonState();
    }

    public void StartInvasion()
    {
        CaptureInvasionSnapshot();
        ChangePhase(GamePhase.Invasion);
    }

    public void AdvanceStage()
    {
        if (StageController.Instance != null && StageController.Instance.AdvanceSmallStage())
        {
            return;
        }

        if (StageController.Instance != null
            && StageController.Instance.IsFinalSmallStage
            && currentResult == GameResult.Success)
        {
            BeginEnding();
            return;
        }

        StartInvasion();
    }

    public void BeginPreparation()
    {
        SetResultObjectsActive(false, false);
        ChangePhase(GamePhase.Preparation);
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
        SetResultObjectsActive(isSuccess, !isSuccess);
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

        placedTrapHistory.Add(new PlacedTrapRecord(placedTrap, trapPrefab));
        TrapPlaced?.Invoke(placedTrap);
        UpdateReturnButtonState();
    }

    public void ClearCurrentStageUndoHistory()
    {
        placedTrapHistory.Clear();
        UpdateReturnButtonState();
    }

    public void ReturnPlacedTrap()
    {
        if (CurrentPhase != GamePhase.Preparation)
        {
            return;
        }

        if (returnMode == ReturnMode.AllPlaced)
        {
            ReturnAllPlacedTraps();
            return;
        }

        ReturnLastPlacedTrap();
    }

    public void RewindInvasion()
    {
        if (CurrentPhase != GamePhase.Invasion)
        {
            return;
        }

        BeginInvasionRewind();
    }

    public void RetryInvasion()
    {
        if (CurrentPhase != GamePhase.Result)
        {
            return;
        }

        BeginInvasionRewind();
    }

    private void BeginInvasionRewind()
    {
        if (rewindSequence != null && rewindSequence.IsActive())
        {
            return;
        }

        if (heroSnapshots.Count == 0)
        {
            return;
        }

        SetResultObjectsActive(false, false);
        ChangePhase(GamePhase.Rewinding);

        rewindSequence = DOTween.Sequence();
        RestoreHeroesForRewind();
        RestoreTrapsForRewind();

        if (rewindSequence.Duration() <= 0f)
        {
            rewindSequence.Kill();
            rewindSequence = null;
            ChangePhase(GamePhase.Preparation);
            return;
        }

        rewindSequence.OnComplete(() =>
        {
            rewindSequence = null;
            ChangePhase(GamePhase.Preparation);
        });
    }

    private void ChangePhase(GamePhase nextPhase)
    {
        if (CurrentPhase == nextPhase)
        {
            return;
        }

        CurrentPhase = nextPhase;
        ApplyPhaseUi(CurrentPhase);
        UpdateReturnButtonState();
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

    private void ApplyPhaseUi(GamePhase phase)
    {
        if (preparationUiObject != null)
        {
            preparationUiObject.SetActive(phase == GamePhase.Preparation);
        }

        if (invasionUiObject != null)
        {
            invasionUiObject.SetActive(phase == GamePhase.Invasion);
        }

        if (openingUiObject != null)
        {
            openingUiObject.SetActive(phase == GamePhase.Opening);
        }
    }

    private void BeginEnding()
    {
        if (CurrentPhase == GamePhase.Ending)
        {
            return;
        }

        SetResultObjectsActive(false, false);
        ChangePhase(GamePhase.Ending);

        if (screenFadeController == null)
        {
            ActivateEndingUi();
            return;
        }

        screenFadeController.PlayTransition(ActivateEndingUi);
    }

    private void ActivateEndingUi()
    {
        if (endingUiObject == null || endingController == null)
        {
            return;
        }

        endingUiObject.SetActive(true);
        endingController.Begin();
    }

    private void CreateEndingUi()
    {
        if (openingUiObject == null)
        {
            return;
        }

        endingUiObject = Instantiate(openingUiObject);
        endingUiObject.name = "EndingUI";

        OpeningController openingController = endingUiObject.GetComponent<OpeningController>();
        if (openingController != null)
        {
            openingController.enabled = false;
        }

        Canvas endingCanvas = endingUiObject.GetComponent<Canvas>();
        if (endingCanvas != null)
        {
            endingCanvas.overrideSorting = true;
            endingCanvas.sortingOrder = 1;
        }

        endingController = endingUiObject.AddComponent<EndingController>();
        endingController.Configure(
            endingUiObject.GetComponent<MessageWindowController>(),
            endingDialogue);
        endingUiObject.SetActive(false);
    }

    private void ReturnLastPlacedTrap()
    {
        // RemoveMissingPlacedTraps();

        if (placedTrapHistory.Count == 0)
        {
            UpdateReturnButtonState();
            return;
        }

        int lastIndex = placedTrapHistory.Count - 1;
        PlacedTrapRecord placedTrap = placedTrapHistory[lastIndex];
        placedTrapHistory.RemoveAt(lastIndex);

        if (placedTrap.Instance != null)
        {
            TrapReturned?.Invoke(placedTrap.Instance, placedTrap.Prefab);
            Destroy(placedTrap.Instance);
        }

        UpdateReturnButtonState();
    }

    private void ReturnAllPlacedTraps()
    {
        for (int i = placedTrapHistory.Count - 1; i >= 0; i--)
        {
            PlacedTrapRecord placedTrap = placedTrapHistory[i];
            if (placedTrap.Instance != null)
            {
                TrapReturned?.Invoke(placedTrap.Instance, placedTrap.Prefab);
                Destroy(placedTrap.Instance);
            }
        }

        placedTrapHistory.Clear();
        UpdateReturnButtonState();
    }

    private void UpdateReturnButtonState()
    {
        if (returnButton == null)
        {
            return;
        }

        // RemoveMissingPlacedTraps();
        returnButton.interactable = CurrentPhase == GamePhase.Preparation && placedTrapHistory.Count > 0;
    }

    // 別口でトラップが消えた場合の処理。現状不要なのでコメントアウト
    // private void RemoveMissingPlacedTraps()
    // {
    //     for (int i = placedTrapHistory.Count - 1; i >= 0; i--)
    //     {
    //         if (placedTrapHistory[i] == null)
    //         {
    //             placedTrapHistory.RemoveAt(i);
    //         }
    //     }
    // }

    private readonly struct PlacedTrapRecord
    {
        public GameObject Instance { get; }
        public GameObject Prefab { get; }

        public PlacedTrapRecord(GameObject instance, GameObject prefab)
        {
            Instance = instance;
            Prefab = prefab;
        }
    }

    private void CaptureInvasionSnapshot()
    {
        heroSnapshots.Clear();
        trapSnapshots.Clear();

        foreach (HeroController hero in FindObjectsByType<HeroController>(FindObjectsSortMode.None))
        {
            heroSnapshots.Add(new HeroSnapshot(hero));
        }

        foreach (TrapBase trap in FindObjectsByType<TrapBase>(FindObjectsSortMode.None))
        {
            trapSnapshots.Add(new TrapSnapshot(trap));
        }
    }

    private void RestoreHeroesForRewind()
    {
        foreach (HeroSnapshot snapshot in heroSnapshots)
        {
            if (snapshot.Hero == null)
            {
                continue;
            }

            snapshot.Hero.RestoreForRewind(snapshot.Hp);
            AppendTransformRewind(snapshot.Hero.transform, snapshot.Position, snapshot.Rotation, snapshot.Scale);
        }
    }

    private void RestoreTrapsForRewind()
    {
        foreach (TrapSnapshot snapshot in trapSnapshots)
        {
            if (snapshot.Trap == null)
            {
                continue;
            }

            snapshot.Trap.RestoreForRewind();
            AppendTransformRewind(snapshot.Trap.transform, snapshot.Position, snapshot.Rotation, snapshot.Scale);
        }
    }

    private void AppendTransformRewind(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (rewindDuration <= 0f)
        {
            target.SetPositionAndRotation(position, rotation);
            target.localScale = scale;
            return;
        }

        rewindSequence.Join(target.DOMove(position, rewindDuration).SetEase(rewindEase));
        rewindSequence.Join(target.DORotateQuaternion(rotation, rewindDuration).SetEase(rewindEase));
        rewindSequence.Join(target.DOScale(scale, rewindDuration).SetEase(rewindEase));
    }

    private readonly struct HeroSnapshot
    {
        public HeroSnapshot(HeroController hero)
        {
            Hero = hero;
            Position = hero.transform.position;
            Rotation = hero.transform.rotation;
            Scale = hero.transform.localScale;
            Hp = hero.CurrentHp;
        }

        public readonly HeroController Hero;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly int Hp;
    }

    private readonly struct TrapSnapshot
    {
        public TrapSnapshot(TrapBase trap)
        {
            Trap = trap;
            Position = trap.transform.position;
            Rotation = trap.transform.rotation;
            Scale = trap.transform.localScale;
        }

        public readonly TrapBase Trap;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
    }
}
