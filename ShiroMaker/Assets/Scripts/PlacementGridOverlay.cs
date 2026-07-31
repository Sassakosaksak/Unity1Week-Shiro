using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementGridOverlay : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BoxCollider2D CameraBounds;
    [SerializeField] private BoxCollider2D placementBounds;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private string groundTilemapName = "Ground";
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color lineColor = new Color(0.2f, 0.95f, 1f, 0.45f);
    [SerializeField] private Color placeableCellColor = new Color(0.3f, 1f, 0.35f, 0.38f);
    [SerializeField] private Color blockedCellColor = new Color(1f, 0.15f, 0.15f, 0.38f);
    [SerializeField] private Color currentPlaceableCellColor = new Color(0.3f, 1f, 0.35f, 0.7f);
    [SerializeField] private Color currentBlockedCellColor = new Color(1f, 0.15f, 0.15f, 0.7f);
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float zPosition = 0f;
    [SerializeField] private float fillZOffset = 0.01f;
    [SerializeField] private float currentFillZOffset = 0.02f;
    [SerializeField] private int fillSortingOrder = -20;
    [SerializeField] private int currentFillSortingOrder = -10;
    [SerializeField] private Vector2 trapProbeSize = new Vector2(0.8f, 0.8f);

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private readonly List<SpriteRenderer> cellFills = new List<SpriteRenderer>();
    private readonly Collider2D[] trapProbeResults = new Collider2D[16];
    private Material lineMaterial;
    private Sprite fillSprite;
    private SpriteRenderer currentCellFill;
    private GameController gameController;
    private bool isPreparationPhase;

    /// <summary>
    /// 対象カメラの補完とグリッド生成
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (placementBounds == null)
        {
            placementBounds = CameraBounds;
        }

        BuildGrid();
    }

    /// <summary>
    /// フェーズ変更の購読と初期表示反映
    /// </summary>
    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
            SetVisible(false);
            return;
        }

        gameController.PhaseChanged += OnPhaseChanged;
        ApplyPhase(gameController.CurrentPhase);
    }

    /// <summary>
    /// フェーズ変更の購読解除とマテリアル破棄
    /// </summary>
    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnPhaseChanged;
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }

        if (fillSprite != null)
        {
            Destroy(fillSprite);
        }
    }

    /// <summary>
    /// フェーズ変更の表示反映
    /// </summary>
    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        ApplyPhase(phase);
    }

    /// <summary>
    /// 準備フェーズだけグリッド表示
    /// </summary>
    private void ApplyPhase(GameController.GamePhase phase)
    {
        isPreparationPhase = phase == GameController.GamePhase.Preparation;
        SetVisible(isPreparationPhase);

        if (!isPreparationPhase)
        {
            HidePlacementCells();
        }
    }

    public bool CanPlace(GameObject placeablePrefab, Vector3 cellCenter)
    {
        if (!IsInsidePlacementBounds(cellCenter))
        {
            return false;
        }

        if (cellSize <= 0f)
        {
            return false;
        }

        Tilemap tilemap = GetGroundTilemap();
        if (tilemap == null)
        {
            return false;
        }

        Vector3Int cell = tilemap.WorldToCell(cellCenter);
        PlacementOccupancy occupancy = PlacementOccupancy.Instance;
        if (occupancy != null && occupancy.IsOccupied(cell))
        {
            return false;
        }

        if (HasTrapAt(cellCenter))
        {
            return false;
        }

        PlacementSurfaceType surfaceType = PlaceablePlacementRule.GetSurfaceType(placeablePrefab);
        return CanPlaceOnSurface(tilemap, cell, surfaceType);
    }

    public void RegisterPlacedTrap(GameObject placedObject, Vector3 cellCenter)
    {
        Tilemap tilemap = GetGroundTilemap();
        if (tilemap == null)
        {
            return;
        }

        PlacementOccupancy.GetOrCreate().Register(placedObject, tilemap.WorldToCell(cellCenter));
    }

    public void ShowPlacementCells(GameObject placeablePrefab)
    {
        HidePlacementCells();

        if (!isPreparationPhase || cellSize <= 0f)
        {
            return;
        }

        EnsureFillSprite();

        Bounds bounds = GetPlacementBounds();
        float minX = Mathf.Floor(bounds.min.x / cellSize) * cellSize;
        float maxX = Mathf.Ceil(bounds.max.x / cellSize) * cellSize;
        float minY = Mathf.Floor(bounds.min.y / cellSize) * cellSize;
        float maxY = Mathf.Ceil(bounds.max.y / cellSize) * cellSize;

        for (float x = minX; x < maxX; x += cellSize)
        {
            for (float y = minY; y < maxY; y += cellSize)
            {
                Vector3 cellCenter = new Vector3(x + cellSize * 0.5f, y + cellSize * 0.5f, zPosition + fillZOffset);
                if (!IsInsidePlacementBounds(cellCenter))
                {
                    continue;
                }

                Color color = CanPlace(placeablePrefab, cellCenter)
                    ? placeableCellColor
                    : blockedCellColor;
                cellFills.Add(CreateCellFill(cellCenter, color));
            }
        }
    }

    public void ShowCurrentPlacementCell(Vector3 cellCenter, bool canPlace)
    {
        EnsureFillSprite();

        if (currentCellFill == null)
        {
            currentCellFill = CreateCellFill(Vector3.zero, Color.clear);
            currentCellFill.gameObject.name = "PlacementGridCurrentCell";
        }

        currentCellFill.transform.position = new Vector3(cellCenter.x, cellCenter.y, zPosition + currentFillZOffset);
        currentCellFill.transform.localScale = new Vector3(cellSize, cellSize, 1f);
        currentCellFill.color = canPlace ? currentPlaceableCellColor : currentBlockedCellColor;
        currentCellFill.sortingOrder = currentFillSortingOrder;
        currentCellFill.enabled = true;
    }

    public void HidePlacementCells()
    {
        foreach (SpriteRenderer cellFill in cellFills)
        {
            if (cellFill != null)
            {
                Destroy(cellFill.gameObject);
            }
        }

        cellFills.Clear();

        if (currentCellFill != null)
        {
            Destroy(currentCellFill.gameObject);
        }

        currentCellFill = null;
    }

    /// <summary>
    /// CameraBounds 範囲へのグリッド線生成
    /// </summary>
    private void BuildGrid()
    {
        if (targetCamera == null || cellSize <= 0f)
        {
            return;
        }

        ClearGrid();

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }

        EnsureLineMaterial();

        Bounds bounds = GetGridBounds();
        float minX = Mathf.Floor(bounds.min.x / cellSize) * cellSize;
        float maxX = Mathf.Ceil(bounds.max.x / cellSize) * cellSize;
        float minY = Mathf.Floor(bounds.min.y / cellSize) * cellSize;
        float maxY = Mathf.Ceil(bounds.max.y / cellSize) * cellSize;

        for (float x = minX; x <= maxX; x += cellSize)
        {
            CreateLine(new Vector3(x, minY, zPosition), new Vector3(x, maxY, zPosition));
        }

        for (float y = minY; y <= maxY; y += cellSize)
        {
            CreateLine(new Vector3(minX, y, zPosition), new Vector3(maxX, y, zPosition));
        }
    }

    /// <summary>
    /// グリッド生成範囲の取得
    /// </summary>
    private Bounds GetGridBounds()
    {
        if (CameraBounds != null)
        {
            Bounds bounds = CameraBounds.bounds;
            bounds.center = new Vector3(bounds.center.x, bounds.center.y, zPosition);
            bounds.size = new Vector3(bounds.size.x, bounds.size.y, 0f);
            return bounds;
        }

        float height = targetCamera.orthographicSize * 2f;
        float width = height * targetCamera.aspect;
        Vector3 center = targetCamera.transform.position;
        center.z = zPosition;

        return new Bounds(center, new Vector3(width, height, 0f));
    }

    private Bounds GetPlacementBounds()
    {
        if (placementBounds != null)
        {
            Bounds bounds = placementBounds.bounds;
            bounds.center = new Vector3(bounds.center.x, bounds.center.y, zPosition);
            bounds.size = new Vector3(bounds.size.x, bounds.size.y, 0f);
            return bounds;
        }

        return GetGridBounds();
    }

    /// <summary>
    /// 指定区間のグリッド線作成
    /// </summary>
    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("PlacementGridLine");
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = EnsureLineMaterial();
        lineRenderer.sortingLayerID = GetTopSortingLayerId();
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;

        lines.Add(lineRenderer);
    }

    private SpriteRenderer CreateCellFill(Vector3 center, Color color)
    {
        GameObject fillObject = new GameObject("PlacementGridCell");
        fillObject.transform.SetParent(transform, false);
        fillObject.transform.position = center;
        fillObject.transform.localScale = new Vector3(cellSize, cellSize, 1f);

        SpriteRenderer spriteRenderer = fillObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = fillSprite;
        spriteRenderer.color = color;
        spriteRenderer.sharedMaterial = EnsureLineMaterial();
        spriteRenderer.sortingLayerID = GetTopSortingLayerId();
        spriteRenderer.sortingOrder = fillSortingOrder;

        return spriteRenderer;
    }

    private Material EnsureLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.name = "Placement Grid Material";
        return lineMaterial;
    }

    private bool IsInsidePlacementBounds(Vector3 position)
    {
        Bounds bounds = GetPlacementBounds();
        return position.x >= bounds.min.x
            && position.x <= bounds.max.x
            && position.y >= bounds.min.y
            && position.y <= bounds.max.y;
    }

    private bool CanPlaceOnSurface(Tilemap tilemap, Vector3Int cell, PlacementSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case PlacementSurfaceType.GroundTop:
                return tilemap.GetTile(cell) != null
                    && tilemap.GetTile(cell + Vector3Int.up) == null;

            case PlacementSurfaceType.CeilingBottom:
                return tilemap.GetTile(cell) != null
                    && tilemap.GetTile(cell + Vector3Int.down) == null;

            case PlacementSurfaceType.Air:
                return tilemap.GetTile(cell) == null;

            default:
                return false;
        }
    }

    private bool HasTrapAt(Vector3 cellCenter)
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();

        int count = Physics2D.OverlapBox(cellCenter, trapProbeSize, 0f, contactFilter, trapProbeResults);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = trapProbeResults[i];
            if (hit != null && hit.GetComponentInParent<TrapBase>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private Tilemap GetGroundTilemap()
    {
        if (groundTilemap != null)
        {
            return groundTilemap;
        }

        GameObject groundObject = GameObject.Find(groundTilemapName);
        if (groundObject != null && groundObject.TryGetComponent(out Tilemap tilemap))
        {
            groundTilemap = tilemap;
            return groundTilemap;
        }

        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tm.name == groundTilemapName)
            {
                groundTilemap = tm;
                return groundTilemap;
            }
        }

        return null;
    }

    private void EnsureFillSprite()
    {
        if (fillSprite != null)
        {
            return;
        }

        fillSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        fillSprite.name = "Placement Grid Fill Sprite";
    }

    /// <summary>
    /// 最前面 Sorting Layer の ID 取得
    /// </summary>
    private int GetTopSortingLayerId()
    {
        SortingLayer[] sortingLayers = SortingLayer.layers;
        return sortingLayers.Length > 0 ? sortingLayers[sortingLayers.Length - 1].id : 0;
    }

    /// <summary>
    /// 生成済みグリッド線の表示切り替え
    /// </summary>
    private void SetVisible(bool visible)
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }
    }

    /// <summary>
    /// 作成済みグリッド線の破棄
    /// </summary>
    private void ClearGrid()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        lines.Clear();
    }
}
