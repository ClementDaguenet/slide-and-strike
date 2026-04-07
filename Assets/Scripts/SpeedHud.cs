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
    Rigidbody _target;

    void Awake()
    {
        EnsureEventSystem();
        BuildUi();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
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
    }

    void LateUpdate()
    {
        if (_label == null)
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
