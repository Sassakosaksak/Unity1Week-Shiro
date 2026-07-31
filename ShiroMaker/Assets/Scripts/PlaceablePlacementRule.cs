using UnityEngine;

public enum PlacementSurfaceType
{
    GroundTop,
    CeilingBottom,
    Air
}

public class PlaceablePlacementRule : MonoBehaviour
{
    [SerializeField] private PlacementSurfaceType surfaceType = PlacementSurfaceType.GroundTop;

    public PlacementSurfaceType SurfaceType => surfaceType;

    public static PlacementSurfaceType GetSurfaceType(GameObject placeableObject)
    {
        if (placeableObject == null)
        {
            return PlacementSurfaceType.GroundTop;
        }

        PlaceablePlacementRule rule = placeableObject.GetComponentInChildren<PlaceablePlacementRule>(true);
        if (rule != null)
        {
            return rule.SurfaceType;
        }

        if (placeableObject.GetComponentInChildren<SpikeTrap>(true) != null
            || placeableObject.GetComponentInChildren<PitfallTrap>(true) != null)
        {
            return PlacementSurfaceType.GroundTop;
        }

        return PlacementSurfaceType.GroundTop;
    }
}
