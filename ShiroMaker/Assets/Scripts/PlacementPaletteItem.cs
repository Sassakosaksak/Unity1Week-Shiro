using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class PlacementPaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Kept here to preserve every existing palette item's serialized configuration.
    [SerializeField] private GameObject placeablePrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform placedParent;
    [SerializeField] private Vector2 placementOffset;
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float placementZ = 0f;
    [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.55f;
    [SerializeField] private PlacementGridOverlay placementGridOverlay;

    private PlacementPreview preview;

    private void Awake()
    {
        preview = GetComponent<PlacementPreview>();
        if (preview == null)
        {
            preview = gameObject.AddComponent<PlacementPreview>();
        }

        preview.Initialize(previewAlpha);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartPlacement())
        {
            return;
        }

        preview.Begin(placeablePrefab);
        placementGridOverlay?.ShowPlacementCells(placeablePrefab);
        UpdatePreviewPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (preview.IsActive)
        {
            UpdatePreviewPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!preview.IsActive)
        {
            return;
        }

        bool canPlace = UpdatePreviewPosition(eventData);
        if (canPlace && StageController.Instance != null)
        {
            canPlace = StageController.Instance.TryConsumeTrap(placeablePrefab);
        }

        if (canPlace)
        {
            Vector3 placementCellCenter = preview.CellCenter;
            GameObject placedObject = preview.Commit(placeablePrefab, placedParent);
            RegisterPlacedObject(placedObject, placementCellCenter);
            placementGridOverlay?.HidePlacementCells();
            return;
        }

        preview.Cancel();
        placementGridOverlay?.HidePlacementCells();
    }

    private bool CanStartPlacement()
    {
        if (placeablePrefab == null)
        {
            Debug.LogWarning("Placeable prefab is not assigned.", this);
            return false;
        }

        if (GameController.Instance == null || GameController.Instance.CurrentPhase != GameController.GamePhase.Preparation)
        {
            return false;
        }

        if (StageController.Instance != null && !StageController.Instance.CanPlaceTrap(placeablePrefab))
        {
            return false;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (placementGridOverlay == null)
        {
            placementGridOverlay = FindFirstObjectByType<PlacementGridOverlay>();
        }

        return worldCamera != null;
    }

    private bool UpdatePreviewPosition(PointerEventData eventData)
    {
        Vector3 cellCenter = GetCellCenter(eventData.position);
        bool canPlace = IsInsideCameraView(eventData.position)
            && !UiPointerUtility.IsOverUi(eventData)
            && CanPlaceAt(cellCenter);

        preview.UpdatePosition(cellCenter, canPlace);
        placementGridOverlay?.ShowCurrentPlacementCell(cellCenter, canPlace);
        return canPlace;
    }

    private Vector3 GetCellCenter(Vector2 screenPosition)
    {
        Vector3 worldPosition = screenPosition;
        worldPosition.z = Mathf.Abs(worldCamera.transform.position.z - placementZ);
        worldPosition = worldCamera.ScreenToWorldPoint(worldPosition);
        worldPosition.z = placementZ;
        worldPosition += (Vector3)placementOffset;

        if (snapToGrid && gridSize > 0f)
        {
            worldPosition.x = SnapToCellCenter(worldPosition.x);
            worldPosition.y = SnapToCellCenter(worldPosition.y);
        }

        return worldPosition;
    }

    private float SnapToCellCenter(float position)
    {
        float cellStart = Mathf.Floor(position / gridSize) * gridSize;
        return cellStart + gridSize * 0.5f;
    }

    private bool IsInsideCameraView(Vector2 screenPosition)
    {
        Vector3 viewportPosition = worldCamera.ScreenToViewportPoint(screenPosition);
        return viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;
    }

    private bool CanPlaceAt(Vector3 cellCenter)
    {
        return placementGridOverlay == null || placementGridOverlay.CanPlace(placeablePrefab, cellCenter);
    }

    private void RegisterPlacedObject(GameObject placedObject, Vector3 placementCellCenter)
    {
        if (placedObject == null)
        {
            return;
        }

        placementGridOverlay?.RegisterPlacedTrap(placedObject, placementCellCenter);
        GameController.Instance?.RegisterPlacedTrap(placedObject, placeablePrefab);
        TrapSEController.Instance?.PlayPlacement();
    }
}
