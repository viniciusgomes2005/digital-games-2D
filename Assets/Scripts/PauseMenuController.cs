using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private bool createRuntimeUiIfMissing = true;

    private bool isPaused;

    private void Start()
    {
        Time.timeScale = 1f;

        if (createRuntimeUiIfMissing)
        {
            EnsureRuntimeUi();
        }

        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        isPaused = true;
        if (GameStateController.Instance != null)
        {
            GameStateController.Instance.Pause();
        }

        Time.timeScale = 0f;
        SetPanelVisible(true);
    }

    public void Resume()
    {
        isPaused = false;
        if (GameStateController.Instance != null)
        {
            GameStateController.Instance.Resume();
        }

        Time.timeScale = 1f;
        SetPanelVisible(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    private void SetPanelVisible(bool visible)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(visible);
        }
    }

    private void EnsureRuntimeUi()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
#if UNITY_2023_1_OR_NEWER
            canvas = FindAnyObjectByType<Canvas>();
#else
            canvas = FindObjectOfType<Canvas>();
#endif
        }

        if (canvas == null)
        {
            return;
        }

        if (pauseButton == null)
        {
            pauseButton = CreateTextButton(canvas.transform, "PauseButton", "II", new Vector2(1f, 1f), new Vector2(-85f, -70f), new Vector2(92f, 72f));
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (pausePanel != null)
        {
            return;
        }

        GameObject panelObject = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        pausePanel = panelObject;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);

        CreatePanelTitle(panelObject.transform, "Pausado", new Vector2(0.5f, 0.5f), new Vector2(0f, 165f));
        CreateTextButton(panelObject.transform, "ResumeButton", "Continuar", new Vector2(0.5f, 0.5f), new Vector2(0f, 55f), new Vector2(320f, 76f)).onClick.AddListener(Resume);
        CreateTextButton(panelObject.transform, "RestartButton", "Reiniciar fase", new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(320f, 76f)).onClick.AddListener(RestartLevel);
        CreateTextButton(panelObject.transform, "MenuButton", "Voltar ao menu", new Vector2(0.5f, 0.5f), new Vector2(0f, -145f), new Vector2(320f, 76f)).onClick.AddListener(BackToMenu);
    }

    private Button CreateTextButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.1f, 0.08f, 0.08f, 0.88f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = label.Length > 3 ? 30f : 36f;
        text.color = Color.white;
        text.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    private void CreatePanelTitle(Transform parent, string label, Vector2 anchor, Vector2 position)
    {
        GameObject textObject = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(420f, 72f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 46f;
        text.color = Color.white;
    }
}
