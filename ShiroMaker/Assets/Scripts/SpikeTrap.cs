using UnityEngine;

public class SpikeTrap : TrapBase
{
    [SerializeField] private LayerMask heroLayer;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private float detectCooldown = 3f;

    private static readonly int DetectHash = Animator.StringToHash("Detect");
    private readonly Collider2D[] detectResults = new Collider2D[8];

    private PlaceableAnchor placeableAnchor;
    private float nextDetectTime;

    protected override void Awake()
    {
        base.Awake();

        placeableAnchor = GetComponent<PlaceableAnchor>();
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

        nextDetectTime = Time.time + detectCooldown;

        if (TrapAnimator != null)
        {
            TrapAnimator.SetTrigger(DetectHash);
        }
    }

    public override void OnHeroHit(HeroController hero)
    {
    }

    /// <summary>
    /// 検知できる状態か判定
    /// </summary>
    private bool CanDetect()
    {
        return CanRun && Time.time >= nextDetectTime;
    }

    /// <summary>
    /// 検知範囲内の勇者を探索
    /// </summary>
    private bool TryDetectHero()
    {
        Vector2 center = GetDetectionCenter();
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();

        if (heroLayer.value != 0)
        {
            contactFilter.SetLayerMask(heroLayer);
        }

        int count = Physics2D.OverlapCircle(center, detectionRadius, contactFilter, detectResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = detectResults[i];
            if (hit != null && hit.GetComponentInParent<HeroController>() != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 設置マスの下端中心
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

        return placementPoint + Vector3.down * (gridSize * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(GetDetectionCenter(), detectionRadius);
    }
}
