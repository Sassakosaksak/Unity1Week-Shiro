using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class PlacementRuleEvaluator : MonoBehaviour
{
    private readonly Collider2D[] trapProbeResults = new Collider2D[16];
    private Tilemap groundTilemap;
    private string groundTilemapName;
    private Vector2 trapProbeSize;

    public void Initialize(Tilemap configuredGroundTilemap, string configuredGroundTilemapName, Vector2 configuredTrapProbeSize)
    {
        groundTilemap = configuredGroundTilemap;
        groundTilemapName = configuredGroundTilemapName;
        trapProbeSize = configuredTrapProbeSize;
    }

    public bool CanPlace(GameObject placeablePrefab, Vector3 cellCenter, Bounds placementBounds, float cellSize)
    {
        if (!IsInsidePlacementBounds(cellCenter, placementBounds) || cellSize <= 0f)
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
        if ((occupancy != null && occupancy.IsOccupied(cell)) || HasTrapAt(cellCenter))
        {
            return false;
        }

        PlacementSurfaceType surfaceType = PlaceablePlacementRule.GetSurfaceType(placeablePrefab);
        return CanPlaceOnSurface(tilemap, cell, surfaceType);
    }

    public void RegisterPlacedTrap(GameObject placedObject, Vector3 cellCenter)
    {
        Tilemap tilemap = GetGroundTilemap();
        if (tilemap != null)
        {
            PlacementOccupancy.GetOrCreate().Register(placedObject, tilemap.WorldToCell(cellCenter));
        }
    }

    private static bool IsInsidePlacementBounds(Vector3 position, Bounds bounds)
    {
        return position.x >= bounds.min.x
            && position.x <= bounds.max.x
            && position.y >= bounds.min.y
            && position.y <= bounds.max.y;
    }

    private static bool CanPlaceOnSurface(Tilemap tilemap, Vector3Int cell, PlacementSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case PlacementSurfaceType.GroundTop:
                return tilemap.GetTile(cell) != null && tilemap.GetTile(cell + Vector3Int.up) == null;
            case PlacementSurfaceType.GroundAbove:
                return tilemap.GetTile(cell) == null && tilemap.GetTile(cell + Vector3Int.down) != null;
            case PlacementSurfaceType.CeilingBottom:
                return tilemap.GetTile(cell) != null && tilemap.GetTile(cell + Vector3Int.down) == null;
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
            TrapBase trap = hit != null ? hit.GetComponentInParent<TrapBase>() : null;
            if (hit != null && hit.enabled && trap != null && trap.enabled)
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
}
