using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class PlacementPaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject placeablePrefab;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform placedParent;
    [SerializeField] private Vector2 placementOffset;
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float placementZ = 0f;
    [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.55f;
    [SerializeField] private Color blockedPreviewColor = new Color(1f, 0.25f, 0.25f, 0.45f);

    private GameObject previewObject;
    private SpriteRenderer[] previewRenderers;
    private Color[] previewBaseColors;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    /// <summary>
    /// 配置開始時のプレビュー生成
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartPlacement())
        {
            return;
        }

        previewObject = Instantiate(placeablePrefab);
        previewObject.name = $"{placeablePrefab.name} Preview";
        previewObject.SetActive(true);

        foreach (Collider2D collider2D in previewObject.GetComponentsInChildren<Collider2D>())
        {
            collider2D.enabled = false;
        }

        foreach (MonoBehaviour behaviour in previewObject.GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }

        previewRenderers = previewObject.GetComponentsInChildren<SpriteRenderer>();
        previewBaseColors = new Color[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = previewRenderers[i];
            previewBaseColors[i] = spriteRenderer.color;

            Color color = spriteRenderer.color;
            color.a = previewAlpha;
            spriteRenderer.color = color;
        }

        UpdatePreviewPosition(eventData);
    }

    /// <summary>
    /// ドラッグ中のプレビュー移動
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (previewObject == null)
        {
            return;
        }

        UpdatePreviewPosition(eventData);
    }

    /// <summary>
    /// ドラッグ終了時の配置確定とプレビュー破棄
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (previewObject == null)
        {
            return;
        }

        bool canPlace = UpdatePreviewPosition(eventData);

        if (canPlace)
        {
            GameObject placedObject = Instantiate(placeablePrefab, previewObject.transform.position, previewObject.transform.rotation, placedParent);
            placedObject.name = placeablePrefab.name;
            placedObject.SetActive(true);
        }

        // Destroy はフレーム終端まで遅延するため、手元のプレビュー状態は先に消す
        Destroy(previewObject);
        previewObject = null;
        previewRenderers = null;
        previewBaseColors = null;
    }

    /// <summary>
    /// 配置操作を開始できるか判定
    /// </summary>
    private bool CanStartPlacement()
    {
        if (placeablePrefab == null)
        {
            Debug.LogWarning("Placeable prefab is not assigned.", this);
            return false;
        }

        if (GameController.Instance == null || GameController.Instance.CurrentPhase != GameController.GamePhase.Preparation)
        {
            return false;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        return worldCamera != null;
    }

    /// <summary>
    /// ポインター座標からプレビュー位置と配置可否を更新
    /// </summary>
    private bool UpdatePreviewPosition(PointerEventData eventData)
    {
        Vector3 screenPosition = eventData.position;
        screenPosition.z = Mathf.Abs(worldCamera.transform.position.z - placementZ);

        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = placementZ;
        worldPosition += (Vector3)placementOffset;

        if (snapToGrid && gridSize > 0f)
        {
            worldPosition.x = SnapToCellCenter(worldPosition.x);
            worldPosition.y = SnapToCellCenter(worldPosition.y);
        }

        previewObject.transform.position = worldPosition;

        bool canPlace = IsInsideCameraView(eventData.position) && !IsOverUi(eventData);
        ApplyPreviewColor(canPlace);
        return canPlace;
    }

    /// <summary>
    /// グリッド線で囲まれたマスの中心座標へ吸着
    /// </summary>
    private float SnapToCellCenter(float position)
    {
        float cellIndex = Mathf.Floor(position / gridSize);
        float cellStart = cellIndex * gridSize;
        float halfCellSize = gridSize * 0.5f;

        return cellStart + halfCellSize;
    }

    /// <summary>
    /// ポインターが配置用カメラの表示範囲内か判定
    /// </summary>
    private bool IsInsideCameraView(Vector2 screenPosition)
    {
        Vector3 viewportPosition = worldCamera.ScreenToViewportPoint(screenPosition);
        return viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;
    }

    /// <summary>
    /// 配置可否に応じたプレビュー色変更
    /// </summary>
    private void ApplyPreviewColor(bool canPlace)
    {
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = previewRenderers[i];
            Color color = canPlace
                ? previewBaseColors[i]
                : Color.Lerp(previewBaseColors[i], blockedPreviewColor, 0.65f);

            color.a = canPlace ? previewAlpha : blockedPreviewColor.a;
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// ポインター位置に UI があるか判定
    /// </summary>
    private bool IsOverUi(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }
}
