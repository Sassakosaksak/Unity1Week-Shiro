using UnityEngine;

public class StagePrefabLibrary : MonoBehaviour
{
    [Header("Heroes")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject wizardPrefab;
    [SerializeField] private GameObject priestPrefab;

    [Header("Traps")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [SerializeField] private GameObject pitfallTrapPrefab;
    [SerializeField] private GameObject rockTrapPrefab;

    public HeroController GetHeroPrefab(HeroType heroType)
    {
        GameObject prefab = heroType switch
        {
            HeroType.Warrior => warriorPrefab,
            HeroType.Wizard => wizardPrefab,
            HeroType.Priest => priestPrefab,
            _ => null
        };

        return prefab != null ? prefab.GetComponent<HeroController>() : null;
    }

    public GameObject GetTrapPrefab(TrapType trapType)
    {
        return trapType switch
        {
            TrapType.Spike => spikeTrapPrefab,
            TrapType.Pitfall => pitfallTrapPrefab,
            TrapType.Rock => rockTrapPrefab,
            _ => null
        };
    }

    public bool TryGetTrapType(GameObject prefab, out TrapType trapType)
    {
        foreach (TrapType candidate in System.Enum.GetValues(typeof(TrapType)))
        {
            if (GetTrapPrefab(candidate) == prefab)
            {
                trapType = candidate;
                return true;
            }
        }

        trapType = default;
        return false;
    }
}
