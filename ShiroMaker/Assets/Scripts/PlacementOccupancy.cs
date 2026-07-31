using System.Collections.Generic;
using UnityEngine;

public class PlacementOccupancy : MonoBehaviour
{
    private static PlacementOccupancy instance;

    private readonly Dictionary<Vector3Int, PlacementOccupant> occupants = new Dictionary<Vector3Int, PlacementOccupant>();

    public static PlacementOccupancy Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlacementOccupancy>();
            }

            return instance;
        }
    }

    public static PlacementOccupancy GetOrCreate()
    {
        if (Instance != null)
        {
            return instance;
        }

        GameObject occupancyObject = new GameObject("PlacementOccupancy");
        instance = occupancyObject.AddComponent<PlacementOccupancy>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public bool IsOccupied(Vector3Int cell)
    {
        if (!occupants.TryGetValue(cell, out PlacementOccupant occupant))
        {
            return false;
        }

        if (occupant != null)
        {
            return true;
        }

        occupants.Remove(cell);
        return false;
    }

    public void Register(GameObject placedObject, Vector3Int cell)
    {
        if (placedObject == null)
        {
            return;
        }

        if (occupants.TryGetValue(cell, out PlacementOccupant existingOccupant) && existingOccupant != null)
        {
            return;
        }

        PlacementOccupant occupant = placedObject.GetComponent<PlacementOccupant>();
        if (occupant == null)
        {
            occupant = placedObject.AddComponent<PlacementOccupant>();
        }

        occupant.Initialize(this, cell);
        occupants[cell] = occupant;
    }

    public void Unregister(PlacementOccupant occupant, Vector3Int cell)
    {
        if (occupants.TryGetValue(cell, out PlacementOccupant currentOccupant) && currentOccupant == occupant)
        {
            occupants.Remove(cell);
        }
    }
}
