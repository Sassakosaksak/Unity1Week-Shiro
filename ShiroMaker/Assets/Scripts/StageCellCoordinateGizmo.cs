using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class StageCellCoordinateGizmo : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float gridSize = 1f;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D boundsCollider = GetComponent<BoxCollider2D>();
        if (boundsCollider == null || gridSize <= 0f)
        {
            return;
        }

        Bounds bounds = boundsCollider.bounds;
        int width = Mathf.FloorToInt(bounds.size.x / gridSize);
        int height = Mathf.FloorToInt(bounds.size.y / gridSize);
        float minX = Mathf.Floor(bounds.min.x / gridSize) * gridSize;
        float minY = Mathf.Floor(bounds.min.y / gridSize) * gridSize;
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = Color.white }
        };

        for (int x = 0; x < width; x++)
        {
            Vector3 labelPosition = new Vector3(minX + (x + 0.5f) * gridSize, minY, 0f);
            Handles.Label(labelPosition, x.ToString(), labelStyle);
        }

        for (int y = 0; y < height; y++)
        {
            Vector3 labelPosition = new Vector3(minX, minY + (y + 0.5f) * gridSize, 0f);
            Handles.Label(labelPosition, y.ToString(), labelStyle);
        }

        Handles.Label(new Vector3(minX, minY, 0f), "(0, 0)", labelStyle);
    }
#endif
}
