using UnityEngine;

public class SpikeTrap : TrapBase
{
    [SerializeField] private Sprite previewImage;
    [SerializeField] private LayerMask heroLayer;
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private bool detectFromCellTop = true;
    [SerializeField] private float detectCooldown = 3f;

    private static readonly int DetectHash = Animator.StringToHash("Detect");
    private readonly Collider2D[] detectResults = new Collider2D[8];

    private PlaceableAnchor placeableAnchor;
    private float nextDetectTime;

    public Sprite PreviewImage => previewImage;
    public bool IsSafeToEnter => damageCollider == null || !damageCollider.enabled;

    protected override void Awake()
    {
        base.Awake();

        placeableAnchor = GetComponent<PlaceableAnchor>();

        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }
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
        TrapSEController.Instance?.PlaySpikeActivation();

        if (TrapAnimator != null)
        {
            TrapAnimator.SetTrigger(DetectHash);
        }
    }

    public override void RestoreForRewind()
    {
        base.RestoreForRewind();
        nextDetectTime = 0f;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(GetDetectionCenter(), detectionRadius);
    }
}
