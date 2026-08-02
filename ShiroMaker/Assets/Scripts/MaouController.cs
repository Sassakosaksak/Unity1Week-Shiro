using UnityEngine;

public class MaouController : MonoBehaviour
{
    [SerializeField] private AudioClip damagedClip;
    [SerializeField, Range(0f, 1f)] private float damagedVolume = 0.7f;

    public static MaouController Instance { get; private set; }

    private bool isDefeated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple MaouControllers were found in the scene.", this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TakeDamage()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;
        SEController.Instance?.Play(damagedClip, damagedVolume);
        GameController.Instance?.ResolveResult(GameController.GameResult.Failure);
    }
}
