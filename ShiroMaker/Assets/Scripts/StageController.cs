using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StageController : MonoBehaviour
{
    [SerializeField] private SmallStageDefinition firstSmallStage;
    [SerializeField] private StagePrefabLibrary prefabLibrary;
    [SerializeField] private BoxCollider2D heroSpawnArea;
    [SerializeField] private BoxCollider2D stageCellBounds;
    [SerializeField] private Transform heroParent;
    [SerializeField] private Transform trapParent;
    [SerializeField] private TMP_Text preparationStageNameText;
    [SerializeField] private TMP_Text invasionStageNameText;
    [SerializeField, Min(0.01f)] private float gridSize = 1f;

    private readonly Dictionary<TrapType, int> remainingTrapSupplies = new Dictionary<TrapType, int>();
    private readonly List<GameObject> playerPlacedTraps = new List<GameObject>();
    private readonly List<GameObject> largeStageInitialTraps = new List<GameObject>();
    private readonly List<HeroController> spawnedHeroes = new List<HeroController>();

    private SmallStageDefinition currentSmallStage;
    private LargeStageDefinition currentLargeStage;

    public static StageController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (firstSmallStage == null)
        {
            return;
        }

        GameController.Instance.TrapPlaced += OnTrapPlaced;
        GameController.Instance.TrapReturned += OnTrapReturned;
        StartSmallStage(firstSmallStage, true);
    }

    private void OnDestroy()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.TrapPlaced -= OnTrapPlaced;
            GameController.Instance.TrapReturned -= OnTrapReturned;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanPlaceTrap(GameObject trapPrefab)
    {
        if (firstSmallStage == null)
        {
            return true;
        }

        return prefabLibrary != null
            && prefabLibrary.TryGetTrapType(trapPrefab, out TrapType trapType)
            && remainingTrapSupplies.TryGetValue(trapType, out int remaining)
            && remaining > 0;
    }

    public bool TryConsumeTrap(GameObject trapPrefab)
    {
        if (firstSmallStage == null)
        {
            return true;
        }

        if (prefabLibrary == null
            || !prefabLibrary.TryGetTrapType(trapPrefab, out TrapType trapType)
            || !remainingTrapSupplies.TryGetValue(trapType, out int remaining)
            || remaining <= 0)
        {
            return false;
        }

        remainingTrapSupplies[trapType] = remaining - 1;
        return true;
    }

    public bool AdvanceSmallStage()
    {
        if (currentSmallStage == null || currentSmallStage.NextSmallStage == null)
        {
            return false;
        }

        StartSmallStage(currentSmallStage.NextSmallStage, false);
        return true;
    }

    private void StartSmallStage(SmallStageDefinition nextStage, bool isFirstStage)
    {
        bool isNewLargeStage = isFirstStage || nextStage.LargeStage != currentLargeStage;
        if (isNewLargeStage)
        {
            DestroyGameObjects(playerPlacedTraps);
            DestroyGameObjects(largeStageInitialTraps);
            currentLargeStage = nextStage.LargeStage;
            SpawnLargeStageInitialTraps();
        }

        DestroyHeroes();
        DestroyTemporaryGrounds();
        ResetTrapRuntimeStates();

        currentSmallStage = nextStage;
        ResetTrapSupplies(nextStage);
        SpawnHeroes(nextStage);
        GameController.Instance.ClearCurrentStageUndoHistory();
        GameController.Instance.BeginPreparation();

        UpdateStageName(nextStage.StageTitle);
    }

    private void SpawnLargeStageInitialTraps()
    {
        if (currentLargeStage == null)
        {
            return;
        }

        foreach (InitialTrapSetup setup in currentLargeStage.InitialTraps)
        {
            GameObject trapPrefab = setup != null && prefabLibrary != null
                ? prefabLibrary.GetTrapPrefab(setup.TrapType)
                : null;
            if (trapPrefab == null)
            {
                continue;
            }

            Vector3 cellCenter = GetStageCellCenter(setup.Cell);
            GameObject trap = Instantiate(trapPrefab, Vector3.zero, Quaternion.identity, trapParent);
            PlaceableAnchor anchor = trap.GetComponent<PlaceableAnchor>();
            trap.transform.position = anchor != null ? anchor.GetRootPositionForCellCenter(cellCenter) : cellCenter;
            RollingRockTrap rock = trap.GetComponentInChildren<RollingRockTrap>();
            if (rock != null)
            {
                rock.CaptureInitialPosition();
            }

            largeStageInitialTraps.Add(trap);
        }
    }

    private void SpawnHeroes(SmallStageDefinition stage)
    {
        if (heroSpawnArea == null)
        {
            Debug.LogWarning("Hero spawn area is not assigned.", this);
            return;
        }

        HeroSetup[] heroSetups = stage.Heroes;
        for (int i = 0; i < heroSetups.Length && i < 5; i++)
        {
            HeroSetup setup = heroSetups[i];
            HeroController heroPrefab = setup != null && prefabLibrary != null
                ? prefabLibrary.GetHeroPrefab(setup.HeroType)
                : null;
            if (heroPrefab == null)
            {
                continue;
            }

            int column = setup.SpawnColumn >= 0 ? setup.SpawnColumn : i;
            Vector3 position = GetHeroSpawnPosition(column);
            HeroController hero = Instantiate(heroPrefab, position, Quaternion.identity, heroParent);
            hero.SetMaxHp(setup.MaxHp, true);
            spawnedHeroes.Add(hero);
        }
    }

    private Vector3 GetHeroSpawnPosition(int column)
    {
        Bounds bounds = heroSpawnArea.bounds;
        float rightEdge = Mathf.Ceil(bounds.max.x / gridSize) * gridSize;
        float x = rightEdge - (column + 0.5f) * gridSize;
        return new Vector3(x, heroSpawnArea.transform.position.y, 0f);
    }

    private void UpdateStageName(string stageTitle)
    {
        if (preparationStageNameText != null)
        {
            preparationStageNameText.text = stageTitle;
        }

        if (invasionStageNameText != null)
        {
            invasionStageNameText.text = stageTitle;
        }
    }

    private Vector3 GetStageCellCenter(Vector2Int cell)
    {
        if (stageCellBounds == null)
        {
            return new Vector3((cell.x + 0.5f) * gridSize, (cell.y + 0.5f) * gridSize, 0f);
        }

        Bounds bounds = stageCellBounds.bounds;
        float x = Mathf.Floor(bounds.min.x / gridSize) * gridSize + (cell.x + 0.5f) * gridSize;
        float y = Mathf.Floor(bounds.min.y / gridSize) * gridSize + (cell.y + 0.5f) * gridSize;
        return new Vector3(x, y, 0f);
    }

    private void ResetTrapSupplies(SmallStageDefinition stage)
    {
        remainingTrapSupplies.Clear();
        foreach (TrapSupplySetup supply in stage.TrapSupplies)
        {
            if (supply != null)
            {
                remainingTrapSupplies[supply.TrapType] = supply.Count;
            }
        }
    }

    private void OnTrapPlaced(GameObject trap)
    {
        if (trap != null)
        {
            playerPlacedTraps.Add(trap);
        }
    }

    private void OnTrapReturned(GameObject trap, GameObject trapPrefab)
    {
        playerPlacedTraps.Remove(trap);

        if (prefabLibrary == null
            || !prefabLibrary.TryGetTrapType(trapPrefab, out TrapType trapType)
            || !remainingTrapSupplies.ContainsKey(trapType))
        {
            return;
        }

        remainingTrapSupplies[trapType]++;
    }

    private static void DestroyGameObjects(List<GameObject> objects)
    {
        foreach (GameObject instance in objects)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        objects.Clear();
    }

    private void DestroyHeroes()
    {
        foreach (HeroController hero in spawnedHeroes)
        {
            if (hero != null)
            {
                Destroy(hero.gameObject);
            }
        }

        spawnedHeroes.Clear();
    }

    private static void DestroyTemporaryGrounds()
    {
        foreach (TemporaryGround floor in FindObjectsByType<TemporaryGround>(FindObjectsSortMode.None))
        {
            Destroy(floor.gameObject);
        }
    }

    private static void ResetTrapRuntimeStates()
    {
        foreach (TrapBase trap in FindObjectsByType<TrapBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            trap.RestoreForRewind();
        }
    }
}
