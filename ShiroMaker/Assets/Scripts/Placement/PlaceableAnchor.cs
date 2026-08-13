using UnityEngine;

public class PlaceableAnchor : MonoBehaviour
{
    [SerializeField, Tooltip("配置基準点にするPrefabローカル座標")]
    private Vector2 localPlacementPointOffset;

    /// <summary>
    /// 配置基準点のワールド座標
    /// </summary>
    public Vector3 PlacementPointWorldPosition => transform.TransformPoint(localPlacementPointOffset);

    /// <summary>
    /// セル中心にアンカーを合わせるルート位置
    /// </summary>
    public Vector3 GetRootPositionForCellCenter(Vector3 cellCenter)
    {
        return cellCenter - transform.TransformVector(localPlacementPointOffset);
    }
}
