using System;
using DG.Tweening;
using UnityEngine;

public class TemporaryGround : MonoBehaviour
{
    [SerializeField] private Collider2D floorCollider;
    [SerializeField] private SpriteRenderer floorRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private string completeTriggerName = "Complete";
    [SerializeField, Min(0f)] private float settingDuration = 1f;
    [SerializeField, Min(0.01f)] private float rideDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.25f;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.12f;
    [SerializeField, Min(0.01f)] private float vanishFlashDuration = 0.12f;
    [SerializeField] private Vector2 heroProbePadding = new Vector2(0.05f, 0.05f);

    private readonly Collider2D[] overlapResults = new Collider2D[8];

    private Action expiredCallback;
    private Color defaultColor = Color.white;
    private float riddenTime;
    private float settingRemainingTime;
    private Tween blinkTween;
    private bool isExpiring;
    private bool isSetting;
    private bool waitForManualCompletion;

    public bool IsSetting => isSetting;

    private void Awake()
    {
        if (floorCollider == null)
        {
            floorCollider = GetComponent<Collider2D>();
        }

        if (floorCollider == null)
        {
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = Vector2.one;
            boxCollider.offset = new Vector2(0f, 0.5f);
            floorCollider = boxCollider;
        }

        if (floorRenderer == null)
        {
            floorRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (floorRenderer != null)
        {
            defaultColor = floorRenderer.color;
        }

        if (floorCollider != null)
        {
            floorCollider.enabled = false;
        }
    }

    private void Update()
    {
        if (isExpiring)
        {
            return;
        }

        if (isSetting)
        {
            if (waitForManualCompletion)
            {
                return;
            }

            settingRemainingTime -= Time.deltaTime;
            if (settingRemainingTime <= 0f)
            {
                CompleteSetting();
            }

            return;
        }

        if (!HasHeroOnFloor())
        {
            return;
        }

        riddenTime += Time.deltaTime;
        if (riddenTime >= rideDuration * 0.5f)
        {
            StartBlink();
        }

        if (riddenTime >= rideDuration)
        {
            Expire();
        }
    }

    private void OnDestroy()
    {
        KillBlink();
        expiredCallback?.Invoke();
        expiredCallback = null;
    }

    public void Initialize(Action onExpired, bool completeManually = false)
    {
        rideDuration = Mathf.Max(0.01f, rideDuration);
        settingDuration = Mathf.Max(0f, settingDuration);
        settingRemainingTime = settingDuration;
        expiredCallback = onExpired;
        isSetting = true;
        waitForManualCompletion = completeManually;

        if (!waitForManualCompletion && settingRemainingTime <= 0f)
        {
            CompleteSetting();
        }
    }

    public void ClearExpiredCallback()
    {
        expiredCallback = null;
    }

    public void CompleteSetting()
    {
        if (!isSetting)
        {
            return;
        }

        isSetting = false;
        waitForManualCompletion = false;

        if (floorCollider != null)
        {
            floorCollider.enabled = true;
        }

        if (animator != null && !string.IsNullOrEmpty(completeTriggerName))
        {
            animator.SetTrigger(completeTriggerName);
        }
    }

    private bool HasHeroOnFloor()
    {
        if (floorCollider == null)
        {
            return false;
        }

        Bounds bounds = floorCollider.bounds;
        Vector2 probeSize = new Vector2(
            bounds.size.x + heroProbePadding.x,
            bounds.size.y + heroProbePadding.y);

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.NoFilter();
        int count = Physics2D.OverlapBox(bounds.center, probeSize, 0f, contactFilter, overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            if (hit != null && hit.GetComponentInParent<HeroController>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void StartBlink()
    {
        if (blinkTween != null && blinkTween.IsActive())
        {
            return;
        }

        if (floorRenderer == null)
        {
            return;
        }

        blinkTween = floorRenderer
            .DOFade(blinkAlpha, blinkInterval)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Expire()
    {
        isExpiring = true;
        KillBlink();

        if (floorCollider != null)
        {
            floorCollider.enabled = false;
        }

        if (floorRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        floorRenderer.color = defaultColor;
        floorRenderer
            .DOColor(Color.white, vanishFlashDuration)
            .OnComplete(() => Destroy(gameObject));
    }

    private void KillBlink()
    {
        if (blinkTween != null && blinkTween.IsActive())
        {
            blinkTween.Kill();
        }

        blinkTween = null;
    }
}
