using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileControlsBootstrap : MonoBehaviour
{
    [SerializeField] private bool createControlsOnStart = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private bool alwaysShowInEditor = true;

    private void Start()
    {
        EnsureEventSystem();
        EnsureInputController();
        ConfigureCanvasScaler();

        if (createControlsOnStart && ShouldShowControls())
        {
            EnsureControls();
        }
    }

    private bool ShouldShowControls()
    {
#if UNITY_EDITOR
        return alwaysShowInEditor;
#else
        return Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
#endif
    }

    private void EnsureInputController()
    {
        if (MobileInputController.Instance != null)
        {
            return;
        }

        GameObject inputObject = new GameObject("MobileInputController");
        inputObject.AddComponent<MobileInputController>();
    }

    private void ConfigureCanvasScaler()
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

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void EnsureControls()
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

        if (canvas == null || canvas.transform.Find("MobileControls") != null)
        {
            return;
        }

        RectTransform root = CreatePanel("MobileControls", canvas.transform, Vector2.zero, Vector2.one);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        CreateButton(root, "LeftButton", "<", MobileButton.MobileButtonAction.Left, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(150f, 135f), new Vector2(150f, 125f));
        CreateButton(root, "RightButton", ">", MobileButton.MobileButtonAction.Right, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(330f, 135f), new Vector2(150f, 125f));
        CreateButton(root, "DownButton", "v", MobileButton.MobileButtonAction.Down, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(240f, 285f), new Vector2(135f, 115f));
        CreateButton(root, "JumpButton", "Pular", MobileButton.MobileButtonAction.Jump, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-330f, 150f), new Vector2(185f, 135f));
        CreateButton(root, "AttackButton", "Atacar", MobileButton.MobileButtonAction.Attack, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-125f, 135f), new Vector2(185f, 135f));
    }

    private RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return rectTransform;
    }

    private void CreateButton(RectTransform parent, string objectName, string label, MobileButton.MobileButtonAction action, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MobileButton));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.75f);

        MobileButton mobileButton = buttonObject.GetComponent<MobileButton>();
        mobileButton.Configure(action);

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
        text.fontSize = label.Length > 2 ? 34f : 56f;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        if (FindAnyObjectByType<EventSystem>() != null)
#else
        if (FindObjectOfType<EventSystem>() != null)
#endif
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
