using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Stage/Large Stage", fileName = "LargeStage")]
public class LargeStageDefinition : ScriptableObject
{
    [SerializeField] private InitialTrapSetup[] initialTraps = Array.Empty<InitialTrapSetup>();

    public InitialTrapSetup[] InitialTraps => initialTraps;
}
