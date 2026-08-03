using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// MEMO: 今後暗転以外のフェードも管理する可能性あり
public class ScreenFadeController : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.4f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private int sortingOrder = 100;

    private Image fadeImage;
    private Sequence fadeSequence;

    private void Awake()
    {
        CreateFadeOverlay();
    }

    private void OnDestroy()
    {
        fadeSequence?.Kill();
    }

    public void PlayTransition(Action onCovered)
    {
        CreateFadeOverlay();
        fadeSequence?.Kill();
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        fadeSequence = DOTween.Sequence()
            .Append(fadeImage.DOFade(1f, fadeDuration))
            .AppendCallback(() => onCovered?.Invoke())
            .Append(fadeImage.DOFade(0f, fadeDuration))
            .OnComplete(() => fadeImage.gameObject.SetActive(false));
    }

    private void CreateFadeOverlay()
    {
        if (fadeImage != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("ScreenFadeOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(Image));
        overlayObject.transform.SetParent(transform, false);

        Canvas canvas = overlayObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        fadeImage = overlayObject.GetComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.raycastTarget = false;
        overlayObject.SetActive(false);
    }
}
