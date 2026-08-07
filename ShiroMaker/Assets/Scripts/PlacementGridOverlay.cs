using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementGridOverlay : MonoBehaviour
{
    // Existing serialized fields stay on this component so the scene keeps all assignments.
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

    private PlacementGridRenderer gridRenderer;
    private PlacementRuleEvaluator ruleEvaluator;
    private GameController gameController;
    private bool isPreparationPhase;

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

        gridRenderer = GetOrAddComponent<PlacementGridRenderer>();
        ruleEvaluator = GetOrAddComponent<PlacementRuleEvaluator>();
        gridRenderer.Initialize(
            targetCamera,
            CameraBounds,
            placementBounds,
            cellSize,
            lineColor,
            placeableCellColor,
            blockedCellColor,
            currentPlaceableCellColor,
            currentBlockedCellColor,
            lineWidth,
            zPosition,
            fillZOffset,
            currentFillZOffset,
            fillSortingOrder,
            currentFillSortingOrder);
        ruleEvaluator.Initialize(groundTilemap, groundTilemapName, trapProbeSize);
        gridRenderer.BuildGrid();
    }

    private void Start()
    {
        gameController = GameController.Instance;
        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
            gridRenderer.SetGridVisible(false);
            return;
        }

        gameController.PhaseChanged += OnPhaseChanged;
        ApplyPhase(gameController.CurrentPhase);
    }

    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnPhaseChanged;
        }
    }

    public bool CanPlace(GameObject placeablePrefab, Vector3 cellCenter)
    {
        return ruleEvaluator.CanPlace(placeablePrefab, cellCenter, gridRenderer.PlacementBounds, cellSize);
    }

    public void RegisterPlacedTrap(GameObject placedObject, Vector3 cellCenter)
    {
        ruleEvaluator.RegisterPlacedTrap(placedObject, cellCenter);
    }

    public void ShowPlacementCells(GameObject placeablePrefab)
    {
        gridRenderer.HidePlacementCells();
        if (!isPreparationPhase)
        {
            return;
        }

        gridRenderer.ShowPlacementCells(placeablePrefab, cellCenter => CanPlace(placeablePrefab, cellCenter));
    }

    public void ShowCurrentPlacementCell(Vector3 cellCenter, bool canPlace)
    {
        gridRenderer.ShowCurrentPlacementCell(cellCenter, canPlace);
    }

    public void HidePlacementCells()
    {
        gridRenderer.HidePlacementCells();
    }

    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        ApplyPhase(phase);
    }

    private void ApplyPhase(GameController.GamePhase phase)
    {
        isPreparationPhase = phase == GameController.GamePhase.Preparation;
        gridRenderer.SetGridVisible(isPreparationPhase);
        if (!isPreparationPhase)
        {
            gridRenderer.HidePlacementCells();
        }
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
