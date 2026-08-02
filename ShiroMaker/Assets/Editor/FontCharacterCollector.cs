using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FontCharacterCollector
{
    private const string OutputPath = "Assets/Fonts/cp_period_Characters.txt";
    private const string ResourcesFolder = "Assets/Resources/";

    [MenuItem("Tools/Shiro Maker/Font/Collect Characters for cp_period")]
    public static void CollectCharacters()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        HashSet<char> characters = new HashSet<char>();
        CollectTextAssets(characters);
        CollectScriptableObjectStrings(characters);
        CollectPrefabText(characters);
        CollectSceneText(characters);

        string content = new string(characters.OrderBy(character => character).ToArray());
        File.WriteAllText(OutputPath, content, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(OutputPath);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(OutputPath);
        Debug.Log($"Collected {characters.Count} characters for cp_period.", Selection.activeObject);
    }

    private static void CollectTextAssets(HashSet<char> characters)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == OutputPath || !path.StartsWith(ResourcesFolder))
            {
                continue;
            }

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
            {
                AddCharacters(characters, asset.text);
            }
        }
    }

    private static void CollectScriptableObjectStrings(HashSet<char> characters)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
        {
            foreach (ScriptableObject asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<ScriptableObject>())
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                SerializedProperty property = serializedObject.GetIterator();
                bool enterChildren = true;

                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType == SerializedPropertyType.String)
                    {
                        AddCharacters(characters, property.stringValue);
                    }
                }
            }
        }
    }

    private static void CollectPrefabText(HashSet<char> characters)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            try
            {
                CollectText(prefabRoot, characters);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void CollectSceneText(HashSet<char> characters)
    {
        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = SceneManager.GetSceneByPath(path);
                bool openedForCollection = !scene.isLoaded;
                if (openedForCollection)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    CollectText(root, characters);
                }

                if (openedForCollection)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    private static void CollectText(GameObject root, HashSet<char> characters)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            AddCharacters(characters, text.text);
        }
    }

    private static void AddCharacters(HashSet<char> characters, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        foreach (char character in value)
        {
            if (character != '\r' && character != '\n' && character != '\t')
            {
                characters.Add(character);
            }
        }
    }
}
