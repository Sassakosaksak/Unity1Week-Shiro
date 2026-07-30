using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

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

    [SerializeField] private GameObject successObject;
    [SerializeField] private GameObject failureObject;
    [SerializeField] private GameObject preparationUiObject;
    [SerializeField] private GameObject invasionUiObject;
    [SerializeField, Min(0f)] private float rewindDuration = 0.45f;
    [SerializeField] private Ease rewindEase = Ease.OutCubic;

    public static GameController Instance { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public event Action<GamePhase> PhaseChanged;

    private readonly List<HeroSnapshot> heroSnapshots = new List<HeroSnapshot>();
    private readonly List<TrapSnapshot> trapSnapshots = new List<TrapSnapshot>();
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
        Debug.Log("Success");
    }

    public void ShowFailure()
    {
        ChangePhase(GamePhase.Result);
        SetResultObjectsActive(false, true);
        Debug.Log("Defeat");
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
