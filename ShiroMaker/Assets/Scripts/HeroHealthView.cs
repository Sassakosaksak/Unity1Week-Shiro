using System.Collections.Generic;
using UnityEngine;

public class HeroHealthView : MonoBehaviour
{
    [SerializeField] private HeroController hero;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.4f);
    [SerializeField] private float heartSpacing = 0.35f;
    [SerializeField] private float heartScale = 0.025f;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 30;

    private static Material defaultHeartMaterial;

    private readonly List<SpriteRenderer> heartRenderers = new List<SpriteRenderer>();

    private void Awake()
    {
        if (hero == null)
        {
            hero = GetComponentInParent<HeroController>();
        }
    }

    private void OnEnable()
    {
        if (hero == null)
        {
            return;
        }

        hero.HealthChanged += UpdateHearts;
        RebuildHearts(hero.MaxHp);
        UpdateHearts(hero.CurrentHp, hero.MaxHp);
    }

    private void Update()
    {
        if (hero == null)
        {
            return;
        }

        if (heartRenderers.Count != Mathf.Clamp(hero.MaxHp, 1, 5))
        {
            RebuildHearts(hero.MaxHp);
        }

        ApplyHeartSettings();
        UpdateHearts(hero.CurrentHp, hero.MaxHp);
    }

    private void OnDisable()
    {
        if (hero != null)
        {
            hero.HealthChanged -= UpdateHearts;
        }
    }

    /// <summary>
    /// 最大HPに合わせてハートを作成
    /// </summary>
    private void RebuildHearts(int maxHp)
    {
        for (int i = heartRenderers.Count - 1; i >= 0; i--)
        {
            if (heartRenderers[i] != null)
            {
                Destroy(heartRenderers[i].gameObject);
            }
        }

        heartRenderers.Clear();

        int heartCount = Mathf.Clamp(maxHp, 1, 5);

        for (int i = 0; i < heartCount; i++)
        {
            GameObject heartObject = new GameObject($"Heart_{i + 1}");
            heartObject.transform.SetParent(transform, false);

            SpriteRenderer heartRenderer = heartObject.AddComponent<SpriteRenderer>();
            heartRenderers.Add(heartRenderer);
        }

        ApplyHeartSettings();
    }

    /// <summary>
    /// 位置/サイズ/描画設定を反映
    /// </summary>
    private void ApplyHeartSettings()
    {
        int heartCount = heartRenderers.Count;
        float startX = -((heartCount - 1) * heartSpacing) * 0.5f;
        Material material = GetHeartMaterial();

        for (int i = 0; i < heartCount; i++)
        {
            SpriteRenderer heartRenderer = heartRenderers[i];
            heartRenderer.transform.localPosition = new Vector3(startX + i * heartSpacing + offset.x, offset.y, 0f);
            heartRenderer.transform.localScale = Vector3.one * heartScale;
            heartRenderer.sortingLayerName = sortingLayerName;
            heartRenderer.sortingOrder = sortingOrder;
            heartRenderer.color = Color.white;

            if (material != null)
            {
                heartRenderer.sharedMaterial = material;
            }
        }
    }

    /// <summary>
    /// 現在HPに合わせて満タン/空ハートを切り替え
    /// </summary>
    private void UpdateHearts(int currentHp, int maxHp)
    {
        int heartCount = Mathf.Clamp(maxHp, 1, 5);
        if (heartRenderers.Count != heartCount)
        {
            RebuildHearts(maxHp);
        }

        for (int i = 0; i < heartRenderers.Count; i++)
        {
            heartRenderers[i].sprite = i < currentHp ? fullHeartSprite : emptyHeartSprite;
        }
    }

    private Material GetHeartMaterial()
    {
        if (defaultHeartMaterial != null)
        {
            return defaultHeartMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        defaultHeartMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return defaultHeartMaterial;
    }
}
