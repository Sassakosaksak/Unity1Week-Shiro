using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlacementSetupUtility
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabFolder = "Assets/Prefabs/Placeables";

    [MenuItem("Tools/Shiro Maker/Setup Placement Drag")]
    public static void Setup()
    {
        EditorSceneManager.OpenScene(ScenePath);

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);

        Camera mainCamera = Camera.main;
        Transform placedParent = GameObject.Find("Gimmicks")?.transform;
        GameObject preparationUi = GameObject.Find("PreparationUI");

        if (mainCamera == null || placedParent == null || preparationUi == null)
        {
            Debug.LogError("Could not find Main Camera, Gimmicks, or PreparationUI in SampleScene.");
            return;
        }

        RectTransform[] resources = preparationUi.GetComponentsInChildren<RectTransform>(true)
            .Where(rectTransform => rectTransform.name == "Resource")
            .OrderByDescending(rectTransform => rectTransform.anchoredPosition.y)
            .ToArray();

        for (int index = 0; index < resources.Length; index++)
        {
            RectTransform resource = resources[index];
            Sprite sprite = resource.GetComponentsInChildren<Image>(true)
                .Where(image => image.transform != resource)
                .Select(image => image.sprite)
                .FirstOrDefault(foundSprite => foundSprite != null);

            if (sprite == null)
            {
                Debug.LogWarning($"Resource button '{resource.name}' has no child sprite.", resource);
                continue;
            }

            GameObject prefab = CreatePlaceablePrefab(index, sprite);
            PlacementPaletteItem paletteItem = resource.GetComponent<PlacementPaletteItem>();
            if (paletteItem == null)
            {
                paletteItem = resource.gameObject.AddComponent<PlacementPaletteItem>();
            }

            SerializedObject serializedObject = new SerializedObject(paletteItem);
            serializedObject.FindProperty("placeablePrefab").objectReferenceValue = prefab;
            serializedObject.FindProperty("worldCamera").objectReferenceValue = mainCamera;
            serializedObject.FindProperty("placedParent").objectReferenceValue = placedParent;
            serializedObject.FindProperty("placementOffset").vector2Value = Vector2.zero;
            serializedObject.FindProperty("snapToGrid").boolValue = true;
            serializedObject.FindProperty("gridSize").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(resource.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"Placement drag setup complete. Wired {resources.Length} resource buttons.");
    }

    private static GameObject CreatePlaceablePrefab(int index, Sprite sprite)
    {
        string prefabPath = $"{PrefabFolder}/Placeable_{index + 1}_{SanitizeFileName(sprite.name)}.prefab";
        GameObject temporaryObject = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

        SpriteRenderer spriteRenderer = temporaryObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 10;

        BoxCollider2D collider = temporaryObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        temporaryObject.AddComponent<SpikeTrap>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporaryObject, prefabPath);
        Object.DestroyImmediate(temporaryObject);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value;
    }
}
