using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Stage/Small Stage", fileName = "SmallStage")]
public class SmallStageDefinition : ScriptableObject
{
    [SerializeField] private string stageTitle;
    [SerializeField] private InitialTrapSetup[] initialTraps = Array.Empty<InitialTrapSetup>();
    [SerializeField] private SmallStageDefinition nextSmallStage;
    [SerializeField] private HeroSetup[] heroes = Array.Empty<HeroSetup>();
    [SerializeField] private TrapSupplySetup[] trapSupplies = Array.Empty<TrapSupplySetup>();

    public string StageTitle => stageTitle;
    public InitialTrapSetup[] InitialTraps => initialTraps;
    public SmallStageDefinition NextSmallStage => nextSmallStage;
    public HeroSetup[] Heroes => heroes;
    public TrapSupplySetup[] TrapSupplies => trapSupplies;
}
