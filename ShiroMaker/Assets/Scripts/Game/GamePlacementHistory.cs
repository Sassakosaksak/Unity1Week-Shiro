using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GamePlacementHistory : MonoBehaviour
{
    private readonly List<PlacedTrapRecord> placedTraps = new List<PlacedTrapRecord>();
    private Button returnButton;
    private bool returnAllPlacedTraps;

    public void Initialize(Button configuredReturnButton, bool configuredReturnAllPlacedTraps)
    {
        returnButton = configuredReturnButton;
        returnAllPlacedTraps = configuredReturnAllPlacedTraps;
    }

    public void Register(GameObject placedTrap, GameObject trapPrefab)
    {
        placedTraps.Add(new PlacedTrapRecord(placedTrap, trapPrefab));
        Refresh(GameController.GamePhase.Preparation);
    }

    public void Clear(GameController.GamePhase phase)
    {
        placedTraps.Clear();
        Refresh(phase);
    }

    public void Return(GameController.GamePhase phase, Action<GameObject, GameObject> returned)
    {
        if (phase != GameController.GamePhase.Preparation)
        {
            return;
        }

        if (returnAllPlacedTraps)
        {
            ReturnAll(returned);
        }
        else
        {
            ReturnLast(returned);
        }

        Refresh(phase);
    }

    public void Refresh(GameController.GamePhase phase)
    {
        if (returnButton != null)
        {
            returnButton.interactable = phase == GameController.GamePhase.Preparation && placedTraps.Count > 0;
        }
    }

    private void ReturnLast(Action<GameObject, GameObject> returned)
    {
        if (placedTraps.Count == 0)
        {
            return;
        }

        int lastIndex = placedTraps.Count - 1;
        PlacedTrapRecord placedTrap = placedTraps[lastIndex];
        placedTraps.RemoveAt(lastIndex);

        if (placedTrap.Instance != null)
        {
            returned?.Invoke(placedTrap.Instance, placedTrap.Prefab);
            Destroy(placedTrap.Instance);
        }
    }

    private void ReturnAll(Action<GameObject, GameObject> returned)
    {
        for (int i = placedTraps.Count - 1; i >= 0; i--)
        {
            PlacedTrapRecord placedTrap = placedTraps[i];
            if (placedTrap.Instance != null)
            {
                returned?.Invoke(placedTrap.Instance, placedTrap.Prefab);
                Destroy(placedTrap.Instance);
            }
        }

        placedTraps.Clear();
    }

    private readonly struct PlacedTrapRecord
    {
        public readonly GameObject Instance;
        public readonly GameObject Prefab;

        public PlacedTrapRecord(GameObject instance, GameObject prefab)
        {
            Instance = instance;
            Prefab = prefab;
        }
    }
}
