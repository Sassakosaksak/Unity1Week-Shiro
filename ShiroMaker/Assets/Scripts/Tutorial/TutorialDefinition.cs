using UnityEngine;

[CreateAssetMenu(menuName = "ShiroMaker/Game Flow/Tutorial", fileName = "Tutorial")]
public class TutorialDefinition : ScriptableObject
{
    [SerializeField] private string pageNameSuffix = "P";
    [SerializeField] private string tutorialButtonObjectName = "TutorialButton";

    public string PageNameSuffix => pageNameSuffix;
    public string TutorialButtonObjectName => tutorialButtonObjectName;
}
