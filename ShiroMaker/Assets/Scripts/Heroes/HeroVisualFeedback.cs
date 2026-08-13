using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HeroVisualFeedback : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Animator animator;
    private SpriteRenderer blinkRenderer;
    private float invincibilityBlinkInterval;
    private Tween invincibilityBlinkTween;
    private Tween greenFlashTween;
    private Tween greenFlashStopTween;
    private float blinkRendererDefaultAlpha = 1f;
    private bool blinkRendererDefaultEnabled = true;
    private Color greenFlashDefaultColor = Color.white;
    private bool isGreenFlashing;

    public void Initialize(Animator configuredAnimator, SpriteRenderer configuredBlinkRenderer, float configuredBlinkInterval)
    {
        animator = configuredAnimator != null ? configuredAnimator : GetComponentInChildren<Animator>();
        blinkRenderer = configuredBlinkRenderer != null ? configuredBlinkRenderer : GetComponentInChildren<SpriteRenderer>();
        invincibilityBlinkInterval = Mathf.Max(0.01f, configuredBlinkInterval);

        if (blinkRenderer != null)
        {
            blinkRendererDefaultAlpha = blinkRenderer.color.a;
            blinkRendererDefaultEnabled = blinkRenderer.enabled;
        }
    }

    public void SetMoving(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isMoving);
        }
    }

    public void ResetAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }

    public void SetTrigger(int triggerHash)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerHash);
        }
    }

    public void PlayJobTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || animator == null)
        {
            return;
        }

        int triggerHash = Animator.StringToHash(triggerName);
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == triggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerHash);
                return;
            }
        }
    }

    public void StartInvincibilityBlink()
    {
        if (blinkRenderer == null)
        {
            return;
        }

        invincibilityBlinkTween?.Kill();
        blinkRendererDefaultAlpha = blinkRenderer.color.a;
        blinkRendererDefaultEnabled = blinkRenderer.enabled;
        invincibilityBlinkTween = DOTween.Sequence()
            .AppendInterval(invincibilityBlinkInterval)
            .AppendCallback(() => blinkRenderer.enabled = false)
            .AppendInterval(invincibilityBlinkInterval)
            .AppendCallback(() => blinkRenderer.enabled = blinkRendererDefaultEnabled)
            .SetLoops(-1);
    }

    public void StopInvincibilityBlink()
    {
        invincibilityBlinkTween?.Kill();
        invincibilityBlinkTween = null;

        if (blinkRenderer == null)
        {
            return;
        }

        Color color = blinkRenderer.color;
        color.a = blinkRendererDefaultAlpha;
        blinkRenderer.color = color;
        blinkRenderer.enabled = blinkRendererDefaultEnabled;
    }

    public void StartGreenFlash(Color flashColor, float interval)
    {
        if (blinkRenderer == null)
        {
            return;
        }

        StopGreenFlash();
        greenFlashDefaultColor = blinkRenderer.color;
        isGreenFlashing = true;
        flashColor.a = greenFlashDefaultColor.a;
        greenFlashTween = blinkRenderer
            .DOColor(flashColor, Mathf.Max(0.01f, interval))
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void PlayColorPulse(Color pulseColor, float fadeDuration, float holdDuration)
    {
        if (blinkRenderer == null)
        {
            return;
        }

        StopGreenFlash();
        greenFlashDefaultColor = blinkRenderer.color;
        isGreenFlashing = true;
        pulseColor.a = greenFlashDefaultColor.a;

        Sequence pulseSequence = DOTween.Sequence();
        pulseSequence.Append(blinkRenderer.DOColor(pulseColor, Mathf.Max(0.01f, fadeDuration)));
        pulseSequence.AppendInterval(Mathf.Max(0f, holdDuration));
        pulseSequence.Append(blinkRenderer.DOColor(greenFlashDefaultColor, Mathf.Max(0.01f, fadeDuration)));
        pulseSequence.OnComplete(StopGreenFlash);
        greenFlashTween = pulseSequence.SetTarget(this);
    }

    public void StopGreenFlash()
    {
        greenFlashTween?.Kill();
        greenFlashTween = null;
        greenFlashStopTween?.Kill();
        greenFlashStopTween = null;

        if (blinkRenderer == null || !isGreenFlashing)
        {
            isGreenFlashing = false;
            return;
        }

        isGreenFlashing = false;
        Color restoredColor = greenFlashDefaultColor;
        restoredColor.a = blinkRenderer.color.a;
        blinkRenderer.color = restoredColor;
    }

    public void StopAllEffects()
    {
        StopInvincibilityBlink();
        StopGreenFlash();
    }
}
