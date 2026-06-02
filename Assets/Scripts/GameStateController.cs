using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStateController : MonoBehaviour
{
    public static GameStateController Instance { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float deathY = -12f;
    [SerializeField] private bool defeatInsteadOfRespawnOnFall;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private bool createFallbackEndPanels = true;

    private Rigidbody2D playerBody;
    private Vector3 checkpointPosition;
    private GameState state = GameState.Playing;

    public GameState State => state;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolvePlayer();
        checkpointPosition = respawnPoint != null ? respawnPoint.position : (player != null ? player.position : transform.position);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (createFallbackEndPanels)
        {
            EnsureFallbackPanels();
        }

        SetEndPanels(false, false);
    }

    private void Update()
    {
        if (state != GameState.Playing)
        {
            return;
        }

        ResolvePlayer();
        if (player != null && player.position.y < deathY)
        {
            if (defeatInsteadOfRespawnOnFall)
            {
                Defeat();
                return;
            }

            RespawnPlayer();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    public void RespawnPlayer()
    {
        ResolvePlayer();
        if (player == null)
        {
            Defeat();
            return;
        }

        if (playerBody == null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        player.position = checkpointPosition;
    }

    public void Victory()
    {
        state = GameState.Victory;
        Time.timeScale = 0f;
        SetEndPanels(true, false);
    }

    public void Defeat()
    {
        state = GameState.Defeat;
        Time.timeScale = 0f;
        SetEndPanels(false, true);
    }

    public void Pause()
    {
        if (state != GameState.Playing)
        {
            return;
        }

        state = GameState.Paused;
    }

    public void Resume()
    {
        if (state != GameState.Paused)
        {
            return;
        }

        state = GameState.Playing;
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

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
        playerBody = player.GetComponent<Rigidbody2D>();
    }

    private void SetEndPanels(bool victoryVisible, bool defeatVisible)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(victoryVisible);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(defeatVisible);
        }
    }

    private void EnsureFallbackPanels()
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

        if (victoryPanel == null)
        {
            victoryPanel = CreateEndPanel(canvas.transform, "VictoryPanel", "Vitoria");
        }

        if (defeatPanel == null)
        {
            defeatPanel = CreateEndPanel(canvas.transform, "DefeatPanel", "Derrota");
        }
    }

    private GameObject CreateEndPanel(Transform parent, string objectName, string title)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.72f);

        CreateText(panelObject.transform, title, new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), 52f, new Vector2(500f, 80f));
        CreatePanelButton(panelObject.transform, "RestartButton", "Reiniciar", new Vector2(0f, 20f)).onClick.AddListener(RestartLevel);
        CreatePanelButton(panelObject.transform, "MenuButton", "Menu", new Vector2(0f, -80f)).onClick.AddListener(BackToMenu);

        return panelObject;
    }

    private Button CreatePanelButton(Transform parent, string objectName, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(300f, 74f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.1f, 0.08f, 0.08f, 0.9f);

        CreateText(buttonObject.transform, label, new Vector2(0.5f, 0.5f), Vector2.zero, 32f, new Vector2(300f, 74f));
        return buttonObject.GetComponent<Button>();
    }

    private void CreateText(Transform parent, string label, Vector2 anchor, Vector2 position, float fontSize, Vector2 size)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    public enum GameState
    {
        Playing,
        Paused,
        Victory,
        Defeat
    }
}
