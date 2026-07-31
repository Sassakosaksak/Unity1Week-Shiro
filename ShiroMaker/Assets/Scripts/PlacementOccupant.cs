using UnityEngine;

public sealed class PlacementOccupant : MonoBehaviour
{
    private PlacementOccupancy owner;
    private Vector3Int occupiedCell;

    public void Initialize(PlacementOccupancy nextOwner, Vector3Int cell)
    {
        owner = nextOwner;
        occupiedCell = cell;
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.Unregister(this, occupiedCell);
        }
    }
}
