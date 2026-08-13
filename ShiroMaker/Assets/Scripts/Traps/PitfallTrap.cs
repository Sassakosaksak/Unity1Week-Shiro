using UnityEngine;
using UnityEngine.Tilemaps;

public class PitfallTrap : TrapBase
{
    [SerializeField] private Collider2D trapCollider;
    [SerializeField] private SpriteRenderer trapRenderer;
    [SerializeField] private Color sealedColor = new Color(0.6f, 0.85f, 1f, 0.9f);
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private string groundTilemapName = "Ground";
    [SerializeField] private bool clearToBottomOfTilemap = true;
    [SerializeField, Min(1)] private int clearDepth = 32;

    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private readonly RemovedTile[] removedTiles = new RemovedTile[64];
    private Color defaultColor = Color.white;
    private float sealedRideTimeRemaining;
    private int removedTileCount;
    private bool removedGroundTiles;
    private bool isSealed;
    private TemporaryGround temporaryGround;

    public bool CanBeSealed => !isSealed;

    protected override void Awake()
    {
        base.Awake();

        if (trapCollider == null)
        {
            trapCollider = GetComponent<Collider2D>();
        }

        if (trapRenderer == null)
        {
            trapRenderer = GetComponent<SpriteRenderer>();
        }

        if (trapRenderer != null)
        {
            defaultColor = trapRenderer.color;
        }
    }

    private void Start()
    {
        RemoveGroundTiles();
    }

    protected override void OnDestroy()
    {
        DestroyTemporaryGround();
        RestoreGroundTiles();
        base.OnDestroy();
    }

    private void Update()
    {
        if (!isSealed || temporaryGround != null)
        {
            return;
        }

        if (!HasHeroOnFloor())
        {
            return;
        }

        sealedRideTimeRemaining -= Time.deltaTime;
        if (sealedRideTimeRemaining <= 0f)
        {
            SetSealed(false);
            DamageOverlappingHeroes();
        }
    }

    public void Seal(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        sealedRideTimeRemaining = duration;
        SetSealed(true);
    }

    public bool BeginTemporaryGroundSetting(
        GameObject temporaryGroundPrefab,
        Vector3 bottomCenterOffset,
        Transform parent)
    {
        if (temporaryGroundPrefab == null)
        {
            return false;
        }

        DestroyTemporaryGround();
        SetSealed(true);

        GameObject temporaryGroundObject = Instantiate(
            temporaryGroundPrefab,
            GetTemporaryGroundBottomCenterPosition(bottomCenterOffset),
            Quaternion.identity,
            parent);

        temporaryGround = temporaryGroundObject.GetComponent<TemporaryGround>();
        if (temporaryGround == null)
        {
            temporaryGround = temporaryGroundObject.AddComponent<TemporaryGround>();
        }

        temporaryGround.Initialize(OnTemporaryGroundExpired, true);
        return true;
    }

    public void CancelTemporaryGroundSetting()
    {
        if (temporaryGround == null || !temporaryGround.IsSetting)
        {
            return;
        }

        DestroyTemporaryGround();
        SetSealed(false);
    }

    public void CompleteTemporaryGroundSetting()
    {
        temporaryGround?.CompleteSetting();
    }

    public void UpdatePlacementPreview(bool canPlace)
    {
        RestoreGroundTiles();

        if (canPlace)
        {
            RemoveGroundTiles();
        }
    }

    public void CancelPlacementPreview()
    {
        RestoreGroundTiles();
    }

    public void CommitPlacementPreview()
    {
        RemoveGroundTiles();
    }

    public void RestoreGroundForRemoval()
    {
        DestroyTemporaryGround();
        RestoreGroundTiles();
    }

    public override void OnHeroHit(HeroController hero)
    {
        // 封鎖されていない落とし穴は実際の穴として扱い、落下判定はDeathZoneが処理
    }

    public override void RestoreForRewind()
    {
        base.RestoreForRewind();
        sealedRideTimeRemaining = 0f;
        DestroyTemporaryGround();
        SetSealed(false);
    }

    private void SetSealed(bool sealedValue)
    {
        isSealed = sealedValue;

        if (trapRenderer != null)
        {
            trapRenderer.color = isSealed ? sealedColor : defaultColor;
        }
    }

    private bool HasHeroOnFloor()
    {
        int count = FindOverlappingColliders();
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit != null && hit.GetComponentInParent<HeroController>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void DamageOverlappingHeroes()
    {
        int count = FindOverlappingColliders();
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            HeroController hero = hit != null ? hit.GetComponentInParent<HeroController>() : null;
            if (hero != null)
            {
                base.OnHeroHit(hero);
            }
        }
    }

    private Vector3 GetTemporaryGroundBottomCenterPosition(Vector3 bottomCenterOffset)
    {
        return transform.position + bottomCenterOffset;
    }

    private void OnTemporaryGroundExpired()
    {
        temporaryGround = null;
        SetSealed(false);
    }

    private void DestroyTemporaryGround()
    {
        if (temporaryGround == null)
        {
            return;
        }

        TemporaryGround target = temporaryGround;
        temporaryGround = null;
        target.ClearExpiredCallback();
        Destroy(target.gameObject);
    }

    private int FindOverlappingColliders()
    {
        if (trapCollider == null)
        {
            return 0;
        }

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        Bounds bounds = trapCollider.bounds;
        return Physics2D.OverlapBox(bounds.center, bounds.size, 0f, contactFilter, overlapResults);
    }

    private void RemoveGroundTiles()
    {
        if (removedGroundTiles)
        {
            return;
        }

        if (groundTilemap == null)
        {
            groundTilemap = FindGroundTilemap();
        }

        if (groundTilemap == null)
        {
            Debug.LogWarning("Ground Tilemap was not found for pitfall.", this);
            return;
        }

        Vector3Int originCell = groundTilemap.WorldToCell(transform.position);
        BoundsInt cellBounds = groundTilemap.cellBounds;
        int bottomY = clearToBottomOfTilemap
            ? cellBounds.yMin
            : originCell.y - clearDepth + 1;

        for (int y = originCell.y; y >= bottomY && removedTileCount < removedTiles.Length; y--)
        {
            Vector3Int cellPosition = new Vector3Int(originCell.x, y, originCell.z);
            TileBase tile = groundTilemap.GetTile(cellPosition);
            if (tile == null)
            {
                continue;
            }

            removedTiles[removedTileCount] = new RemovedTile(
                cellPosition,
                tile,
                groundTilemap.GetTileFlags(cellPosition),
                groundTilemap.GetColor(cellPosition),
                groundTilemap.GetTransformMatrix(cellPosition));
            removedTileCount++;
            groundTilemap.SetTile(cellPosition, null);
        }

        removedGroundTiles = removedTileCount > 0;
        ProcessGroundColliderChanges();
    }

    private void RestoreGroundTiles()
    {
        if (!removedGroundTiles || groundTilemap == null)
        {
            return;
        }

        for (int i = 0; i < removedTileCount; i++)
        {
            RemovedTile removedTile = removedTiles[i];
            groundTilemap.SetTile(removedTile.Position, removedTile.Tile);
            groundTilemap.SetTileFlags(removedTile.Position, TileFlags.None);
            groundTilemap.SetColor(removedTile.Position, removedTile.Color);
            groundTilemap.SetTransformMatrix(removedTile.Position, removedTile.TransformMatrix);
            groundTilemap.SetTileFlags(removedTile.Position, removedTile.Flags);
        }

        removedTileCount = 0;
        removedGroundTiles = false;
        ProcessGroundColliderChanges();
    }

    private void ProcessGroundColliderChanges()
    {
        if (groundTilemap != null
            && groundTilemap.TryGetComponent(out TilemapCollider2D tilemapCollider)
            && tilemapCollider.hasTilemapChanges)
        {
            tilemapCollider.ProcessTilemapChanges();
        }
    }

    private Tilemap FindGroundTilemap()
    {
        GameObject groundObject = GameObject.Find(groundTilemapName);
        if (groundObject != null && groundObject.TryGetComponent(out Tilemap tilemap))
        {
            return tilemap;
        }

        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.name == groundTilemapName)
            {
                return tm;
            }
        }

        return null;
    }

    private readonly struct RemovedTile
    {
        public RemovedTile(Vector3Int position, TileBase tile, TileFlags flags, Color color, Matrix4x4 transformMatrix)
        {
            Position = position;
            Tile = tile;
            Flags = flags;
            Color = color;
            TransformMatrix = transformMatrix;
        }

        public readonly Vector3Int Position;
        public readonly TileBase Tile;
        public readonly TileFlags Flags;
        public readonly Color Color;
        public readonly Matrix4x4 TransformMatrix;
    }
}
