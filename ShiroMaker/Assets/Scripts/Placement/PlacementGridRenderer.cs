using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlacementGridRenderer : MonoBehaviour
{
    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private readonly List<SpriteRenderer> cellFills = new List<SpriteRenderer>();
    private Camera targetCamera;
    private BoxCollider2D cameraBounds;
    private BoxCollider2D placementBounds;
    private float cellSize;
    private Color lineColor;
    private Color placeableCellColor;
    private Color blockedCellColor;
    private Color currentPlaceableCellColor;
    private Color currentBlockedCellColor;
    private float lineWidth;
    private float zPosition;
    private float fillZOffset;
    private float currentFillZOffset;
    private int fillSortingOrder;
    private int currentFillSortingOrder;
    private Material lineMaterial;
    private Sprite fillSprite;
    private SpriteRenderer currentCellFill;

    public Bounds PlacementBounds => GetPlacementBounds();

    public void Initialize(
        Camera configuredCamera,
        BoxCollider2D configuredCameraBounds,
        BoxCollider2D configuredPlacementBounds,
        float configuredCellSize,
        Color configuredLineColor,
        Color configuredPlaceableCellColor,
        Color configuredBlockedCellColor,
        Color configuredCurrentPlaceableCellColor,
        Color configuredCurrentBlockedCellColor,
        float configuredLineWidth,
        float configuredZPosition,
        float configuredFillZOffset,
        float configuredCurrentFillZOffset,
        int configuredFillSortingOrder,
        int configuredCurrentFillSortingOrder)
    {
        targetCamera = configuredCamera;
        cameraBounds = configuredCameraBounds;
        placementBounds = configuredPlacementBounds;
        cellSize = configuredCellSize;
        lineColor = configuredLineColor;
        placeableCellColor = configuredPlaceableCellColor;
        blockedCellColor = configuredBlockedCellColor;
        currentPlaceableCellColor = configuredCurrentPlaceableCellColor;
        currentBlockedCellColor = configuredCurrentBlockedCellColor;
        lineWidth = configuredLineWidth;
        zPosition = configuredZPosition;
        fillZOffset = configuredFillZOffset;
        currentFillZOffset = configuredCurrentFillZOffset;
        fillSortingOrder = configuredFillSortingOrder;
        currentFillSortingOrder = configuredCurrentFillSortingOrder;
    }

    public void BuildGrid()
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

    public void ShowPlacementCells(GameObject placeablePrefab, Func<Vector3, bool> canPlace)
    {
        HidePlacementCells();
        if (cellSize <= 0f)
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

                cellFills.Add(CreateCellFill(cellCenter, canPlace(cellCenter) ? placeableCellColor : blockedCellColor));
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

    public void SetGridVisible(bool visible)
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }

        if (fillSprite != null)
        {
            Destroy(fillSprite);
        }
    }

    private Bounds GetGridBounds()
    {
        if (cameraBounds != null)
        {
            Bounds bounds = cameraBounds.bounds;
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

    private bool IsInsidePlacementBounds(Vector3 position)
    {
        Bounds bounds = GetPlacementBounds();
        return position.x >= bounds.min.x
            && position.x <= bounds.max.x
            && position.y >= bounds.min.y
            && position.y <= bounds.max.y;
    }

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
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Placement Grid Material"
            };
        }

        return lineMaterial;
    }

    private void EnsureFillSprite()
    {
        if (fillSprite == null)
        {
            fillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            fillSprite.name = "Placement Grid Fill Sprite";
        }
    }

    private static int GetTopSortingLayerId()
    {
        SortingLayer[] sortingLayers = SortingLayer.layers;
        return sortingLayers.Length > 0 ? sortingLayers[sortingLayers.Length - 1].id : 0;
    }

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
