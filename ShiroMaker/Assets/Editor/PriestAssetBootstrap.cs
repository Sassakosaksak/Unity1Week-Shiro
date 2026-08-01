using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PriestAssetBootstrap
{
    private const string SourcePath = "Assets/Visual/Tiny RPG Character Asset Pack 01 v2.0 -Full 22 Characters/Priest.aseprite";
    private const string AnimatorDirectory = "Assets/Animator/Priest";
    private const string ControllerPath = AnimatorDirectory + "/Priest.controller";
    private const string PrefabDirectory = "Assets/Prefabs/Heroes";
    private const string PrefabPath = PrefabDirectory + "/Priest.prefab";

    static PriestAssetBootstrap()
    {
        EditorApplication.delayCall += CreateAssetsIfNeeded;
    }

    [MenuItem("Tools/ShiroMaker/Create Priest Assets")]
    private static void CreateAssetsIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        Dictionary<string, AnimationClip> sourceClips = AssetDatabase
            .LoadAllAssetsAtPath(SourcePath)
            .OfType<AnimationClip>()
            .ToDictionary(clip => clip.name, clip => clip);

        string[] requiredClipNames = { "Idle", "Walk", "Hurt", "Death", "Attack", "Heal" };
        if (requiredClipNames.Any(clipName => !sourceClips.ContainsKey(clipName)))
        {
            Debug.LogError("Priest Aseprite clips were not imported yet.");
            return;
        }

        EnsureDirectory(AnimatorDirectory);
        EnsureDirectory(PrefabDirectory);

        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>
        {
            ["Idle"] = CopyClip(sourceClips["Idle"], "Idle"),
            ["Walk"] = CopyClip(sourceClips["Walk"], "Walk"),
            ["Hurt"] = CopyClip(sourceClips["Hurt"], "Hurt"),
            ["Death"] = CopyClip(sourceClips["Death"], "Death"),
            ["Raise"] = CopyClip(sourceClips["Attack"], "Raise"),
            ["Heal"] = CopyClip(sourceClips["Heal"], "Heal")
        };

        AnimatorController controller = CreateController(clips);
        CreatePrefab(controller, clips["Idle"]);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created Priest animation assets and prefab.");
    }

    private static AnimationClip CopyClip(AnimationClip source, string outputName)
    {
        string path = AnimatorDirectory + "/" + outputName + ".anim";
        AnimationClip copiedClip = new AnimationClip { name = outputName };
        EditorUtility.CopySerialized(source, copiedClip);
        copiedClip.name = outputName;
        AssetDatabase.CreateAsset(copiedClip, path);
        return copiedClip;
    }

    private static AnimatorController CreateController(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Raise", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Heal", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle");
        AnimatorState walk = stateMachine.AddState("Walk");
        AnimatorState hurt = stateMachine.AddState("Hurt");
        AnimatorState death = stateMachine.AddState("Death");
        AnimatorState raise = stateMachine.AddState("Raise");
        AnimatorState heal = stateMachine.AddState("Heal");
        idle.motion = clips["Idle"];
        walk.motion = clips["Walk"];
        hurt.motion = clips["Hurt"];
        death.motion = clips["Death"];
        raise.motion = clips["Raise"];
        heal.motion = clips["Heal"];
        stateMachine.defaultState = idle;

        AddMoveTransition(idle, walk, true);
        AddMoveTransition(walk, idle, false);
        AddAnyStateTriggerTransition(stateMachine, hurt, "Hurt");
        AddAnyStateTriggerTransition(stateMachine, death, "Death");
        AddAnyStateTriggerTransition(stateMachine, raise, "Raise");
        AddAnyStateTriggerTransition(stateMachine, heal, "Heal");
        AddReturnTransition(hurt, idle);
        AddReturnTransition(raise, idle);
        AddReturnTransition(heal, idle);

        return controller;
    }

    private static void CreatePrefab(AnimatorController controller, AnimationClip idleClip)
    {
        WizardHeroBehavior wizard = Object.FindFirstObjectByType<WizardHeroBehavior>();
        if (wizard == null)
        {
            Debug.LogError("A Wizard scene object is required to create the Priest prefab.");
            return;
        }

        GameObject priestObject = Object.Instantiate(wizard.gameObject);
        priestObject.name = "Priest";
        priestObject.transform.SetParent(null);
        priestObject.transform.localPosition = Vector3.zero;

        WizardHeroBehavior wizardBehavior = priestObject.GetComponent<WizardHeroBehavior>();
        if (wizardBehavior != null)
        {
            Object.DestroyImmediate(wizardBehavior);
        }

        WizardAnimationEventRelay wizardRelay = priestObject.GetComponentInChildren<WizardAnimationEventRelay>();
        if (wizardRelay != null)
        {
            Object.DestroyImmediate(wizardRelay);
        }

        PriestHeroBehavior priestBehavior = priestObject.AddComponent<PriestHeroBehavior>();
        HeroController hero = priestObject.GetComponent<HeroController>();
        SerializedObject heroSerializedObject = new SerializedObject(hero);
        heroSerializedObject.FindProperty("jobBehavior").objectReferenceValue = priestBehavior;
        heroSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        Animator animator = priestObject.GetComponentInChildren<Animator>();
        animator.runtimeAnimatorController = controller;

        SpriteRenderer spriteRenderer = priestObject.GetComponentInChildren<SpriteRenderer>();
        spriteRenderer.sprite = GetFirstSprite(idleClip);

        PrefabUtility.SaveAsPrefabAsset(priestObject, PrefabPath);
        Object.DestroyImmediate(priestObject);
    }

    private static Sprite GetFirstSprite(AnimationClip clip)
    {
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        return keyframes.Length > 0 ? keyframes[0].value as Sprite : null;
    }

    private static void AddMoveTransition(AnimatorState from, AnimatorState to, bool isMoving)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(isMoving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsMoving");
    }

    private static void AddAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string triggerName)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.duration = 0.05f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void AddReturnTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = 0.05f;
    }

    private static void EnsureDirectory(string assetDirectory)
    {
        if (AssetDatabase.IsValidFolder(assetDirectory))
        {
            return;
        }

        Directory.CreateDirectory(assetDirectory);
    }
}
