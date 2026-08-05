using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class TutorialView : MonoBehaviour
{
    private const string InputBlockerName = "TutorialInputBlocker";
    private readonly List<GameObject> pages = new List<GameObject>();
    private readonly List<CanvasState> hiddenCanvases = new List<CanvasState>();

    private Canvas tutorialCanvas;
    private Button inputBlocker;
    private int currentPageIndex;
    private bool isShowing;

    private void Awake()
    {
        tutorialCanvas = GetComponent<Canvas>();
        tutorialCanvas.overrideSorting = true;
        tutorialCanvas.sortingOrder = 200;
        CreateInputBlocker();
    }

    public void Show(TutorialDefinition definition)
    {
        CollectPages(definition);
        if (pages.Count == 0)
        {
            Debug.LogWarning("No tutorial pages were found.", this);
            return;
        }

        gameObject.SetActive(true);
        HideOtherUi();
        isShowing = true;
        currentPageIndex = 0;
        SetPage(currentPageIndex);
        inputBlocker.gameObject.SetActive(true);
        inputBlocker.transform.SetAsLastSibling();
    }

    private void OnDisable()
    {
        if (isShowing)
        {
            RestoreOtherUi();
            isShowing = false;
        }
    }

    private void CreateInputBlocker()
    {
        Transform existing = transform.Find(InputBlockerName);
        GameObject blockerObject = existing != null ? existing.gameObject : new GameObject(InputBlockerName, typeof(RectTransform), typeof(Image), typeof(Button));
        if (existing == null)
        {
            blockerObject.transform.SetParent(transform, false);
        }

        RectTransform rectTransform = blockerObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = blockerObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        inputBlocker = blockerObject.GetComponent<Button>();
        if (blockerObject.GetComponent<UIButtonSE>() == null)
        {
            blockerObject.AddComponent<UIButtonSE>();
        }
        inputBlocker.targetGraphic = image;
        inputBlocker.onClick.RemoveListener(ShowNextPage);
        inputBlocker.onClick.AddListener(ShowNextPage);
        blockerObject.SetActive(false);
    }

    private void CollectPages(TutorialDefinition definition)
    {
        pages.Clear();
        string suffix = Regex.Escape(definition.PageNameSuffix);
        Regex pageNamePattern = new Regex($"^(\\d+){suffix}$");

        foreach (Transform child in transform)
        {
            if (pageNamePattern.IsMatch(child.name))
            {
                pages.Add(child.gameObject);
            }
        }

        pages.Sort((left, right) => GetPageNumber(left.name).CompareTo(GetPageNumber(right.name)));
    }

    private void SetPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == index);
        }
    }

    private void ShowNextPage()
    {
        if (!isShowing)
        {
            return;
        }

        currentPageIndex++;
        if (currentPageIndex >= pages.Count)
        {
            Hide();
            return;
        }

        SetPage(currentPageIndex);
    }

    private void Hide()
    {
        foreach (GameObject page in pages)
        {
            page.SetActive(false);
        }

        inputBlocker.gameObject.SetActive(false);
        RestoreOtherUi();
        isShowing = false;
        gameObject.SetActive(false);
    }

    private void HideOtherUi()
    {
        hiddenCanvases.Clear();
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == tutorialCanvas || canvas.transform.IsChildOf(transform))
            {
                continue;
            }

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            hiddenCanvases.Add(new CanvasState(canvas, raycaster));
            canvas.enabled = false;
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }
    }

    private void RestoreOtherUi()
    {
        foreach (CanvasState state in hiddenCanvases)
        {
            state.Restore();
        }

        hiddenCanvases.Clear();
    }

    private static int GetPageNumber(string pageName)
    {
        int end = 0;
        while (end < pageName.Length && char.IsDigit(pageName[end]))
        {
            end++;
        }

        return int.TryParse(pageName.Substring(0, end), out int number) ? number : int.MaxValue;
    }

    private readonly struct CanvasState
    {
        private readonly Canvas canvas;
        private readonly bool canvasEnabled;
        private readonly GraphicRaycaster raycaster;
        private readonly bool raycasterEnabled;

        public CanvasState(Canvas canvas, GraphicRaycaster raycaster)
        {
            this.canvas = canvas;
            canvasEnabled = canvas.enabled;
            this.raycaster = raycaster;
            raycasterEnabled = raycaster != null && raycaster.enabled;
        }

        public void Restore()
        {
            if (canvas != null)
            {
                canvas.enabled = canvasEnabled;
            }

            if (raycaster != null)
            {
                raycaster.enabled = raycasterEnabled;
            }
        }
    }
}
