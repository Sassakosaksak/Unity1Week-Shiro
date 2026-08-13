using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private TutorialDefinition defaultTutorial;

    public static TutorialController Instance { get; private set; }

    private TutorialView tutorialView;

    private void Awake()
    {
        Instance = this;
        tutorialView = FindFirstObjectByType<TutorialView>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        BindTutorialButton();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowTutorial(TutorialDefinition tutorial)
    {
        if (tutorial != null)
        {
            defaultTutorial = tutorial;
        }

        if (defaultTutorial == null)
        {
            Debug.LogWarning("Tutorial definition is not assigned.", this);
            return;
        }

        if (tutorialView == null)
        {
            tutorialView = FindFirstObjectByType<TutorialView>(FindObjectsInactive.Include);
        }

        tutorialView?.Show(defaultTutorial);
    }

    public void ShowDefaultTutorial()
    {
        ShowTutorial(defaultTutorial);
    }

    private void BindTutorialButton()
    {
        if (defaultTutorial == null || string.IsNullOrEmpty(defaultTutorial.TutorialButtonObjectName))
        {
            return;
        }

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name != defaultTutorial.TutorialButtonObjectName)
            {
                continue;
            }

            button.onClick.RemoveListener(ShowDefaultTutorial);
            button.onClick.AddListener(ShowDefaultTutorial);
            button.interactable = true;
            return;
        }

        Debug.LogWarning($"Tutorial button '{defaultTutorial.TutorialButtonObjectName}' was not found.", this);
    }
}
