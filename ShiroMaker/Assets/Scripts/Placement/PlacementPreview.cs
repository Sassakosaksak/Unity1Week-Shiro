using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlacementPreview : MonoBehaviour
{
    private float previewAlpha;
    private GameObject previewObject;
    private PlaceableAnchor previewAnchor;
    private SpriteRenderer[] previewRenderers;
    private Color[] previewBaseColors;
    private Sprite[] previewBaseSprites;
    private Collider2D[] previewColliders;
    private bool[] previewColliderEnabledStates;
    private MonoBehaviour[] previewBehaviours;
    private bool[] previewBehaviourEnabledStates;
    private Animator[] previewAnimators;
    private bool[] previewAnimatorEnabledStates;
    private PitfallTrap previewPitfallTrap;

    public bool IsActive => previewObject != null;
    public Vector3 CellCenter { get; private set; }

    public void Initialize(float configuredPreviewAlpha)
    {
        previewAlpha = Mathf.Clamp01(configuredPreviewAlpha);
    }

    public void Begin(GameObject placeablePrefab)
    {
        Cancel();
        previewObject = Instantiate(placeablePrefab);
        previewObject.name = $"{placeablePrefab.name} Preview";
        previewObject.SetActive(true);
        previewAnchor = previewObject.GetComponent<PlaceableAnchor>();
        previewPitfallTrap = previewObject.GetComponentInChildren<PitfallTrap>(true);

        DisablePreviewComponents();
        CapturePreviewRenderers();
        ApplyPreviewAppearance();
    }

    public void UpdatePosition(Vector3 cellCenter, bool canPlace)
    {
        if (previewObject == null)
        {
            return;
        }

        CellCenter = cellCenter;
        previewObject.transform.position = previewAnchor != null
            ? previewAnchor.GetRootPositionForCellCenter(cellCenter)
            : cellCenter;

        previewPitfallTrap?.CancelPlacementPreview();
        ApplyPreviewAppearance();
        previewPitfallTrap?.UpdatePlacementPreview(canPlace);
    }

    public GameObject Commit(GameObject placeablePrefab, Transform placedParent)
    {
        if (previewObject == null)
        {
            return null;
        }

        if (previewPitfallTrap != null)
        {
            RestorePreviewState();
            previewPitfallTrap.CommitPlacementPreview();
            previewObject.name = placeablePrefab.name;
            previewObject.transform.SetParent(placedParent, true);
            GameObject placedPitfall = previewObject;
            ClearReferences();
            return placedPitfall;
        }

        GameObject placedObject = Instantiate(
            placeablePrefab,
            previewObject.transform.position,
            previewObject.transform.rotation,
            placedParent);
        placedObject.name = placeablePrefab.name;
        placedObject.SetActive(true);
        Cancel();
        return placedObject;
    }

    public void Cancel()
    {
        if (previewPitfallTrap != null)
        {
            previewPitfallTrap.CancelPlacementPreview();
        }

        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        ClearReferences();
    }

    private void DisablePreviewComponents()
    {
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

        previewAnimators = previewObject.GetComponentsInChildren<Animator>();
        previewAnimatorEnabledStates = new bool[previewAnimators.Length];
        for (int i = 0; i < previewAnimators.Length; i++)
        {
            previewAnimatorEnabledStates[i] = previewAnimators[i].enabled;
            previewAnimators[i].enabled = false;
        }
    }

    private void CapturePreviewRenderers()
    {
        previewRenderers = previewObject.GetComponentsInChildren<SpriteRenderer>();
        previewBaseColors = new Color[previewRenderers.Length];
        previewBaseSprites = new Sprite[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            previewBaseColors[i] = previewRenderers[i].color;
            previewBaseSprites[i] = previewRenderers[i].sprite;
        }

        SpikeTrap previewSpikeTrap = previewObject.GetComponentInChildren<SpikeTrap>(true);
        if (previewSpikeTrap != null && previewSpikeTrap.PreviewImage != null)
        {
            SpriteRenderer spikeRenderer = previewSpikeTrap.GetComponent<SpriteRenderer>();
            if (spikeRenderer != null)
            {
                spikeRenderer.sprite = previewSpikeTrap.PreviewImage;
            }
        }
    }

    private void ApplyPreviewAppearance()
    {
        if (previewRenderers == null)
        {
            return;
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = previewRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color color = previewBaseColors[i];
            color.a = previewAlpha;
            spriteRenderer.color = color;
        }
    }

    private void RestorePreviewState()
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

        for (int i = 0; i < previewAnimators.Length; i++)
        {
            if (previewAnimators[i] != null)
            {
                previewAnimators[i].enabled = previewAnimatorEnabledStates[i];
            }
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null)
            {
                previewRenderers[i].color = previewBaseColors[i];
                previewRenderers[i].sprite = previewBaseSprites[i];
            }
        }
    }

    private void ClearReferences()
    {
        previewObject = null;
        previewAnchor = null;
        previewRenderers = null;
        previewBaseColors = null;
        previewBaseSprites = null;
        previewColliders = null;
        previewColliderEnabledStates = null;
        previewBehaviours = null;
        previewBehaviourEnabledStates = null;
        previewAnimators = null;
        previewAnimatorEnabledStates = null;
        previewPitfallTrap = null;
        CellCenter = Vector3.zero;
    }
}
