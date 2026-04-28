using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(450)]
public class StartMenu : MonoBehaviour
{
    const string LogoResourcePath = "UI/slide-and-strike-logo";

    GameObject _panel;
    bool _open;

    public static bool IsOpen { get; private set; }

    void Awake()
    {
        EnsureEventSystem();
        BuildUi();
        Show();
    }

    void OnDestroy()
    {
        if (_open)
            Time.timeScale = 1f;
        IsOpen = false;
    }

    public void Play()
    {
        _open = false;
        IsOpen = false;
        Time.timeScale = 1f;
        if (_panel != null)
            _panel.SetActive(false);
    }

    void Show()
    {
        _open = true;
        IsOpen = true;
        Time.timeScale = 0f;
        if (_panel != null)
            _panel.SetActive(true);
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("StartCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("StartPanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = _panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.06f, 0.08f, 0.9f);

        CreateLogo(_panel.transform);
        CreateButton(_panel.transform, new Vector2(0f, -230f), "Jouer", Play);
    }

    void CreateLogo(Transform parent)
    {
        var logoGo = new GameObject("Logo");
        logoGo.transform.SetParent(parent, false);
        var rt = logoGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -35f);
        rt.sizeDelta = new Vector2(540f, 250f);

        var texture = Resources.Load<Texture2D>(LogoResourcePath);
        if (texture != null)
        {
            var image = logoGo.AddComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            var fitter = logoGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)texture.width / texture.height;
            return;
        }

        var text = logoGo.AddComponent<Text>();
        text.font = CreateUiFont();
        text.fontSize = 84;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.95f, 0.98f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "Slide & Strike";
    }

    void CreateButton(Transform parent, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonGo = new GameObject("PlayButton");
        buttonGo.transform.SetParent(parent, false);
        var buttonRt = buttonGo.AddComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.anchoredPosition = anchoredPosition;
        buttonRt.sizeDelta = new Vector2(380f, 100f);

        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.9f, 0.96f, 1f, 0.95f);
        var button = buttonGo.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.font = CreateUiFont();
        text.fontSize = 46;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.02f, 0.08f, 0.1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static Font CreateUiFont()
    {
        foreach (var name in new[] { "Segoe UI", "Arial", "Liberation Sans" })
        {
            try
            {
                return Font.CreateDynamicFontFromOSFont(name, 46);
            }
            catch
            {
            }
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
