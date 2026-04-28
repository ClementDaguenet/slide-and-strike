using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

[DefaultExecutionOrder(200)]
public class SpeedHud : MonoBehaviour
{
    [SerializeField] string targetName = "pinguin-black";
    [SerializeField] Rigidbody targetRigidbody;
    Text _label;
    Text _scoreLabel;
    Text _finishLabel;
    Rigidbody _target;

    void Awake()
    {
        EnsureEventSystem();
        EnsurePauseMenu();
        EnsureStartMenu();
        BuildUi();
        BottleScore.Finished += OnFinished;
    }

    void OnDestroy()
    {
        BottleScore.Finished -= OnFinished;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static void EnsurePauseMenu()
    {
        if (Object.FindFirstObjectByType<PauseMenu>() != null)
            return;

        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    static void EnsureStartMenu()
    {
        if (Object.FindFirstObjectByType<StartMenu>() != null)
            return;

        new GameObject("StartMenu").AddComponent<StartMenu>();
    }

    void Start()
    {
        if (targetRigidbody != null)
            _target = targetRigidbody;
        else
        {
            var go = GameObject.Find(targetName);
            if (go != null)
                _target = go.GetComponent<Rigidbody>();
        }
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("SpeedHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var textGo = new GameObject("SpeedText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-28f, -28f);
        rt.sizeDelta = new Vector2(420f, 72f);

        _label = textGo.AddComponent<Text>();
        _label.font = CreateUiFont();
        _label.fontSize = 38;
        _label.fontStyle = FontStyle.Bold;
        _label.color = new Color(0.95f, 0.98f, 1f);
        _label.alignment = TextAnchor.UpperRight;
        _label.text = "0.0 km/h";
        _label.horizontalOverflow = HorizontalWrapMode.Overflow;
        _label.verticalOverflow = VerticalWrapMode.Overflow;

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.1f, 0.15f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        var scoreGo = new GameObject("BottleScoreText");
        scoreGo.transform.SetParent(canvasGo.transform, false);
        var scoreRt = scoreGo.AddComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0f, 1f);
        scoreRt.anchorMax = new Vector2(0f, 1f);
        scoreRt.pivot = new Vector2(0f, 1f);
        scoreRt.anchoredPosition = new Vector2(28f, -28f);
        scoreRt.sizeDelta = new Vector2(520f, 72f);

        _scoreLabel = scoreGo.AddComponent<Text>();
        _scoreLabel.font = _label.font;
        _scoreLabel.fontSize = 38;
        _scoreLabel.fontStyle = FontStyle.Bold;
        _scoreLabel.color = new Color(0.95f, 0.98f, 1f);
        _scoreLabel.alignment = TextAnchor.UpperLeft;
        _scoreLabel.text = "Bouteilles: 0";
        _scoreLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _scoreLabel.verticalOverflow = VerticalWrapMode.Overflow;

        var scoreOutline = scoreGo.AddComponent<Outline>();
        scoreOutline.effectColor = new Color(0f, 0.1f, 0.15f, 0.92f);
        scoreOutline.effectDistance = new Vector2(2f, -2f);

        var finishGo = new GameObject("FinishText");
        finishGo.transform.SetParent(canvasGo.transform, false);
        var finishRt = finishGo.AddComponent<RectTransform>();
        finishRt.anchorMin = new Vector2(0.5f, 0.5f);
        finishRt.anchorMax = new Vector2(0.5f, 0.5f);
        finishRt.pivot = new Vector2(0.5f, 0.5f);
        finishRt.anchoredPosition = Vector2.zero;
        finishRt.sizeDelta = new Vector2(1100f, 260f);

        _finishLabel = finishGo.AddComponent<Text>();
        _finishLabel.font = _label.font;
        _finishLabel.fontSize = 64;
        _finishLabel.fontStyle = FontStyle.Bold;
        _finishLabel.color = new Color(0.95f, 0.98f, 1f);
        _finishLabel.alignment = TextAnchor.MiddleCenter;
        _finishLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _finishLabel.verticalOverflow = VerticalWrapMode.Overflow;
        _finishLabel.gameObject.SetActive(false);

        var finishOutline = finishGo.AddComponent<Outline>();
        finishOutline.effectColor = new Color(0f, 0.1f, 0.15f, 0.95f);
        finishOutline.effectDistance = new Vector2(3f, -3f);
    }

    void LateUpdate()
    {
        if (_label == null)
            return;
        if (BottleScore.IsFinished)
            return;
        if (_target == null)
        {
            if (targetRigidbody != null)
                _target = targetRigidbody;
            else
            {
                var go = GameObject.Find(targetName);
                if (go != null)
                    _target = go.GetComponent<Rigidbody>();
            }
            return;
        }

        float ms = _target.linearVelocity.magnitude;
        float kmh = ms * 3.6f;
        _label.text = $"{kmh:F1} km/h";
        if (_scoreLabel != null)
            _scoreLabel.text = $"Bouteilles: {BottleScore.Count}";
    }

    void OnFinished(int score)
    {
        if (_label != null)
            _label.gameObject.SetActive(false);
        if (_scoreLabel != null)
            _scoreLabel.gameObject.SetActive(false);
        if (_finishLabel == null)
            return;

        _finishLabel.text = $"Félicitations\nBouteilles shootées : {score}";
        _finishLabel.gameObject.SetActive(true);
    }

    static Font CreateUiFont()
    {
        foreach (var name in new[] { "Segoe UI", "Arial", "Liberation Sans" })
        {
            try
            {
                return Font.CreateDynamicFontFromOSFont(name, 42);
            }
            catch
            {
            }
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
