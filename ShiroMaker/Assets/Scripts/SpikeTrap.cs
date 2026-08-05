using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SpikeTrap : TrapBase
{
    private static readonly List<SpikeTrap> activeSpikes = new List<SpikeTrap>();

    [SerializeField] private Sprite previewImage;
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private bool detectFromCellTop = true;
    [SerializeField] private float detectCooldown = 3f;

    private static readonly int DetectHash = Animator.StringToHash("Detect");
    private PlaceableAnchor placeableAnchor;
    private float nextDetectTime;
    private bool isActivationCycleRunning;

    public Sprite PreviewImage => previewImage;
    public bool IsSafeToEnter => !isActivationCycleRunning;
    public static IReadOnlyList<SpikeTrap> ActiveSpikes => activeSpikes;

    protected override void Awake()
    {
        base.Awake();

        placeableAnchor = GetComponent<PlaceableAnchor>();

        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }
    }

    private void OnEnable()
    {
        if (!activeSpikes.Contains(this))
        {
            activeSpikes.Add(this);
        }
    }

    private void OnDisable()
    {
        activeSpikes.Remove(this);
    }

    public bool IsBlockingProbe(Bounds probeBounds)
    {
        return !IsSafeToEnter
            && GetDetectionBounds().Intersects(probeBounds);
    }

    private void Update()
    {
        if (!CanDetect())
        {
            return;
        }

        if (!TryDetectHero())
        {
            return;
        }

        isActivationCycleRunning = true;
        nextDetectTime = Time.time + detectCooldown;
        TrapSEController.Instance?.PlaySpikeActivation();

        if (TrapAnimator != null)
        {
            TrapAnimator.SetTrigger(DetectHash);
            return;
        }

        isActivationCycleRunning = false;
    }

    public override void RestoreForRewind()
    {
        base.RestoreForRewind();
        nextDetectTime = 0f;
        isActivationCycleRunning = false;
    }

    public void FinishActivationFromAnimation()
    {
        isActivationCycleRunning = false;
    }

    /// <summary>
    /// 検知できる状態か判定
    /// </summary>
    private bool CanDetect()
    {
        return CanRun
            && !isActivationCycleRunning
            && Time.time >= nextDetectTime;
    }

    /// <summary>
    /// 検知範囲内の勇者を探索
    /// </summary>
    private bool TryDetectHero()
    {
        Bounds detectionBounds = GetDetectionBounds();
        IReadOnlyList<HeroController> heroes = HeroController.ActiveHeroes;
        for (int i = 0; i < heroes.Count; i++)
        {
            HeroController hero = heroes[i];
            if (hero != null && !hero.IsDead && hero.IsBodyOverlappingBounds(detectionBounds))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 設置している床セルの中心、または床セルの上端中心
    /// </summary>
    private Vector2 GetDetectionCenter()
    {
        if (placeableAnchor == null)
        {
            placeableAnchor = GetComponent<PlaceableAnchor>();
        }

        Vector3 placementPoint = placeableAnchor != null
            ? placeableAnchor.PlacementPointWorldPosition
            : transform.position;

        return detectFromCellTop
            ? placementPoint + Vector3.up * (gridSize * 0.5f)
            : placementPoint;
    }

    private Bounds GetDetectionBounds()
    {
        return new Bounds(GetDetectionCenter(), Vector2.one * (detectionRadius * 2f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(GetDetectionBounds().center, GetDetectionBounds().size);
    }
}
