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
        Preparation,
        Invasion,
        Result,
        Rewinding
    }

    public enum GameResult
    {
        Success,
        Failure
    }

    private enum ReturnMode
    {
        LastPlaced,
        AllPlaced
    }

    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;
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

    private readonly List<HeroSnapshot> heroSnapshots = new List<HeroSnapshot>();
    private readonly List<TrapSnapshot> trapSnapshots = new List<TrapSnapshot>();
    private readonly List<GameObject> placedTrapHistory = new List<GameObject>();
    private Sequence rewindSequence;

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
        ApplyPhaseUi(CurrentPhase);
        UpdateReturnButtonState();
    }

    public void StartInvasion()
    {
        CaptureInvasionSnapshot();
        ChangePhase(GamePhase.Invasion);
    }


    public void ShowSuccess()
    {
        ChangePhase(GamePhase.Result);
        SetResultObjectsActive(true, false);
        ResultShown?.Invoke(GameResult.Success);
        Debug.Log("Success");
    }

    public void ShowFailure()
    {
        ChangePhase(GamePhase.Result);
        SetResultObjectsActive(false, true);
        ResultShown?.Invoke(GameResult.Failure);
        Debug.Log("Defeat");
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
    
    public void RegisterPlacedTrap(GameObject placedTrap)
    {
        if (placedTrap == null || CurrentPhase != GamePhase.Preparation)
        {
            return;
        }

        placedTrapHistory.Add(placedTrap);
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
        GameObject placedTrap = placedTrapHistory[lastIndex];
        placedTrapHistory.RemoveAt(lastIndex);

        if (placedTrap != null)
        {
            Destroy(placedTrap);
        }

        UpdateReturnButtonState();
    }

    private void ReturnAllPlacedTraps()
    {
        for (int i = placedTrapHistory.Count - 1; i >= 0; i--)
        {
            if (placedTrapHistory[i] != null)
            {
                Destroy(placedTrapHistory[i]);
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
