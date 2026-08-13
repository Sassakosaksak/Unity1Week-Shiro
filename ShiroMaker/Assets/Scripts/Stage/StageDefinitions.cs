using System;
using UnityEngine;

public enum HeroType
{
    None = 0,
    Warrior = 1,
    Wizard = 2,
    Priest = 3
}

public enum TrapType
{
    None = 0,
    Spike = 1,
    Pitfall = 2,
    Rock = 3
}

[Serializable]
public class HeroSetup
{
    [SerializeField] private HeroType heroType;
    [SerializeField, Range(1, 5)] private int maxHp = 1;
    [SerializeField, Min(-1)] private int spawnColumn = -1;

    public HeroType HeroType => heroType;
    public int MaxHp => maxHp;
    public int SpawnColumn => spawnColumn;
}

[Serializable]
public class TrapSupplySetup
{
    [SerializeField] private TrapType trapType;
    [SerializeField, Min(0)] private int count;

    public TrapType TrapType => trapType;
    public int Count => count;
}

[Serializable]
public class InitialTrapSetup
{
    [SerializeField] private TrapType trapType;
    [SerializeField] private Vector2Int cell;

    public TrapType TrapType => trapType;
    public Vector2Int Cell => cell;
}
