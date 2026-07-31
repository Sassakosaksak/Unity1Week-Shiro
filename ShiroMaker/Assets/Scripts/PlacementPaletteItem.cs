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
    private PlaceableAnchor previewAnchor;
    private SpriteRenderer[] previewRenderers;
    private Color[] previewBaseColors;
    private Collider2D[] previewColliders;
    private bool[] previewColliderEnabledStates;
    private MonoBehaviour[] previewBehaviours;
    private bool[] previewBehaviourEnabledStates;
    private PitfallTrap previewPitfallTrap;

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
        previewAnchor = previewObject.GetComponent<PlaceableAnchor>();

        previewColliders = previewObject.GetComponentsInChildren<Collider2D>();
        previewColliderEnabledStates = new bool[previewColliders.Length];
        for (int i = 0; i < previewColliders.Length; i++)
        {
            previewColliderEnabledStates[i] = previewColliders[i].enabled;
            previewColliders[i].enabled = false;
        }

        previewBehaviours = previewObject.GetComponentsInChildren<MonoBehaviour>();
        previewBehaviourEnabledStates = new bool[previewBehaviours.Length];
        for (int i = 0; i < previewBehaviours.Length; i++)
        {
            previewBehaviourEnabledStates[i] = previewBehaviours[i].enabled;
            previewBehaviours[i].enabled = false;
        }

        previewPitfallTrap = previewObject.GetComponentInChildren<PitfallTrap>(true);

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
            if (previewPitfallTrap != null)
            {
                PlacePreviewObject();
                return;
            }
            else
            {
                GameObject placedObject = Instantiate(placeablePrefab, previewObject.transform.position, previewObject.transform.rotation, placedParent);
                placedObject.name = placeablePrefab.name;
                placedObject.SetActive(true);
                GameController.Instance?.RegisterPlacedTrap(placedObject);
            }
        }

        if (previewPitfallTrap != null)
        {
            previewPitfallTrap.CancelPlacementPreview();
        }

        // Destroy はフレーム終端まで遅延するため、手元のプレビュー状態は先に消す
        Destroy(previewObject);
        previewObject = null;
        previewAnchor = null;
        previewRenderers = null;
        previewBaseColors = null;
        previewColliders = null;
        previewColliderEnabledStates = null;
        previewBehaviours = null;
        previewBehaviourEnabledStates = null;
        previewPitfallTrap = null;
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

        previewObject.transform.position = GetRootPositionForCellCenter(worldPosition);

        bool canPlace = IsInsideCameraView(eventData.position) && !UiPointerUtility.IsOverUi(eventData);
        ApplyPreviewColor(canPlace);

        if (previewPitfallTrap != null)
        {
            previewPitfallTrap.UpdatePlacementPreview(canPlace);
        }

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
    /// セル中心へ合わせる配置ルート位置
    /// </summary>
    private Vector3 GetRootPositionForCellCenter(Vector3 cellCenter)
    {
        if (previewAnchor == null)
        {
            return cellCenter;
        }

        return previewAnchor.GetRootPositionForCellCenter(cellCenter);
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

    private void PlacePreviewObject()
    {
        RestorePreviewComponentStates();
        RestorePreviewRendererColors();

        previewPitfallTrap.CommitPlacementPreview();
        previewObject.name = placeablePrefab.name;
        previewObject.transform.SetParent(placedParent, true);
        previewObject.SetActive(true);
        GameController.Instance?.RegisterPlacedTrap(previewObject);

        previewObject = null;
        previewAnchor = null;
        previewRenderers = null;
        previewBaseColors = null;
        previewColliders = null;
        previewColliderEnabledStates = null;
        previewBehaviours = null;
        previewBehaviourEnabledStates = null;
        previewPitfallTrap = null;
    }

    private void RestorePreviewComponentStates()
    {
        for (int i = 0; i < previewColliders.Length; i++)
        {
            if (previewColliders[i] != null)
            {
                previewColliders[i].enabled = previewColliderEnabledStates[i];
            }
        }

        for (int i = 0; i < previewBehaviours.Length; i++)
        {
            if (previewBehaviours[i] != null)
            {
                previewBehaviours[i].enabled = previewBehaviourEnabledStates[i];
            }
        }
    }

    private void RestorePreviewRendererColors()
    {
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null)
            {
                previewRenderers[i].color = previewBaseColors[i];
            }
        }
    }

}
