using UnityEngine;

/// <summary>
/// プレイヤーが Trap を盤面へ確定配置したときの SE を管理します。
/// </summary>
public class TrapSEController : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private AudioClip placementClip;
    [SerializeField, Range(0f, 1f)] private float placementVolume = 1f;

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = GameController.Instance;
        }
    }

    private void OnEnable()
    {
        if (gameController != null)
        {
            gameController.TrapPlaced += PlayPlacementSE;
        }
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.TrapPlaced -= PlayPlacementSE;
        }
    }

    private void PlayPlacementSE(GameObject _)
    {
        SEController.Instance?.Play(placementClip, placementVolume);
    }
}
