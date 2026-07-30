using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraPanController : MonoBehaviour
{
    [SerializeField] private BoxCollider2D CameraBounds;
    [SerializeField] private float PanSpeed = 1f;

    private Camera targetCamera;
    private GameController gameController;
    private bool isPanning;
    private Vector3 previousPointerWorldPosition;

    /// <summary>
    /// 対象カメラの取得
    /// </summary>
    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    /// <summary>
    /// フェーズ変更の購読と初期位置補正
    /// </summary>
    private void Start()
    {
        gameController = GameController.Instance;

        if (gameController == null)
        {
            Debug.LogWarning("GameController was not found in the scene.", this);
            enabled = false;
            return;
        }

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning("CameraPanController は Orthographic カメラ前提", this);
            enabled = false;
            return;
        }

        gameController.PhaseChanged += OnPhaseChanged;
        transform.position = ClampCameraPosition(transform.position);
    }

    /// <summary>
    /// フェーズ変更の購読解除
    /// </summary>
    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.PhaseChanged -= OnPhaseChanged;
        }
    }

    /// <summary>
    /// ドラッグパン入力の処理
    /// </summary>
    private void Update()
    {
        if (!CanPan() || Mouse.current == null)
        {
            isPanning = false;
            return;
        }

        Vector2 pointerScreenPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // UI上から開始の場合パン操作不可
            if (UiPointerUtility.IsOverUi(pointerScreenPosition))
            {
                return;
            }

            isPanning = true;
            previousPointerWorldPosition = ScreenToWorldPosition(pointerScreenPosition);
        }

        if (!isPanning)
        {
            return;
        }

        if (!Mouse.current.leftButton.isPressed)
        {
            isPanning = false;
            return;
        }

        Vector3 currentPointerWorldPosition = ScreenToWorldPosition(pointerScreenPosition);
        Vector3 pointerDelta = previousPointerWorldPosition - currentPointerWorldPosition;
        Vector3 nextCameraPosition = transform.position + pointerDelta * PanSpeed;

        transform.position = ClampCameraPosition(nextCameraPosition);
        previousPointerWorldPosition = ScreenToWorldPosition(pointerScreenPosition);
    }

    /// <summary>
    /// フェーズ変更時のパン終了
    /// </summary>
    private void OnPhaseChanged(GameController.GamePhase phase)
    {
        if (phase != GameController.GamePhase.Preparation)
        {
            isPanning = false;
        }
    }

    /// <summary>
    /// パン可能フェーズか判定
    /// </summary>
    private bool CanPan()
    {
        return gameController != null
            && gameController.CurrentPhase == GameController.GamePhase.Preparation;
    }

    /// <summary>
    /// 画面座標からワールド座標へ変換
    /// </summary>
    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        Vector3 position = screenPosition;
        position.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(position);
        worldPosition.z = 0f;
        return worldPosition;
    }

    /// <summary>
    /// カメラ表示範囲を CameraBounds 内へ制限
    /// </summary>
    private Vector3 ClampCameraPosition(Vector3 position)
    {
        if (CameraBounds == null || targetCamera == null)
        {
            return position;
        }

        Bounds bounds = CameraBounds.bounds;
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        position.x = ClampAxis(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth, bounds.center.x);
        position.y = ClampAxis(position.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight, bounds.center.y);
        return position;
    }

    /// <summary>
    /// 指定軸の範囲制限
    /// </summary>
    private float ClampAxis(float value, float min, float max, float fallback)
    {
        if (min > max)
        {
            return fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

}
