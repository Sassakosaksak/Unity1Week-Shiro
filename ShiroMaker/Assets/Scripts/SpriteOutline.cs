using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutline : MonoBehaviour
{
    private static readonly Vector2[] Offsets =
    {
        new Vector2(-1f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, -1f),
        new Vector2(0f, 1f),
        new Vector2(-0.7071f, -0.7071f),
        new Vector2(-0.7071f, 0.7071f),
        new Vector2(0.7071f, -0.7071f),
        new Vector2(0.7071f, 0.7071f)
    };

    [SerializeField] private bool useSourceColor = true;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField, Min(0.001f)] private float thickness = 0.035f;

    private SpriteRenderer source;
    private SpriteRenderer[] outlineRenderers;

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        CreateOutlineRenderers();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (outlineRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer outlineRenderer in outlineRenderers)
        {
            if (outlineRenderer != null)
            {
                Destroy(outlineRenderer.gameObject);
            }
        }

        outlineRenderers = null;
    }

    private void CreateOutlineRenderers()
    {
        if (source == null || outlineRenderers != null)
        {
            return;
        }

        outlineRenderers = new SpriteRenderer[Offsets.Length];
        for (int i = 0; i < Offsets.Length; i++)
        {
            GameObject outlineObject = new GameObject("Sprite Outline");
            outlineObject.transform.SetParent(transform, false);
            outlineRenderers[i] = outlineObject.AddComponent<SpriteRenderer>();
        }
    }

    private void Refresh()
    {
        if (source == null || outlineRenderers == null)
        {
            return;
        }

        float scaleX = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
        Vector2 localThickness = new Vector2(thickness / scaleX, thickness / scaleY);
        Color color = useSourceColor ? source.color : outlineColor;

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            SpriteRenderer outlineRenderer = outlineRenderers[i];
            if (outlineRenderer == null)
            {
                continue;
            }

            outlineRenderer.transform.localPosition = Offsets[i] * localThickness;
            outlineRenderer.sprite = source.sprite;
            outlineRenderer.color = color;
            outlineRenderer.flipX = source.flipX;
            outlineRenderer.flipY = source.flipY;
            outlineRenderer.drawMode = source.drawMode;
            outlineRenderer.size = source.size;
            outlineRenderer.tileMode = source.tileMode;
            outlineRenderer.maskInteraction = source.maskInteraction;
            outlineRenderer.sortingLayerID = source.sortingLayerID;
            outlineRenderer.sortingOrder = source.sortingOrder - 1;
            outlineRenderer.enabled = source.enabled;
        }
    }
}
