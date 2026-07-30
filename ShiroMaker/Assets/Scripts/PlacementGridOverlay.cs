using System.Collections.Generic;
using UnityEngine;

public class PlacementGridOverlay : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BoxCollider2D CameraBounds;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color lineColor = new Color(0.2f, 0.95f, 1f, 0.45f);
    [SerializeField] private float lineWidth = 0.025f;
    [SerializeField] private float zPosition = 0f;

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private Material lineMaterial;
    private GameController gameController;

    /// <summary>
    /// 対象カメラの補完とグリッド生成
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
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
        SetVisible(phase == GameController.GamePhase.Preparation);
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

        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.name = "Placement Grid Material";

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
        lineRenderer.material = lineMaterial;
        lineRenderer.sortingLayerID = GetTopSortingLayerId();
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;

        lines.Add(lineRenderer);
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
