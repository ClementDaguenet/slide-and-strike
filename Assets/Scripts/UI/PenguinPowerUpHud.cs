using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(210)]
public class PenguinPowerUpHud : MonoBehaviour
{
    Image[] _slotBgs;
    RawImage[] _slotIcons;
    Image _cooldownFill;
    Text _cooldownText;
    PenguinPowerUpController _controller;

    void Awake()
    {
        BuildUi();
        BottleScore.Finished += OnFinished;
    }

    void OnDestroy()
    {
        BottleScore.Finished -= OnFinished;
        if (_controller != null)
            _controller.Changed -= Refresh;
    }

    void Update()
    {
        Refresh();
    }

    public void Bind(PenguinPowerUpController controller)
    {
        if (_controller == controller)
            return;
        if (_controller != null)
            _controller.Changed -= Refresh;

        _controller = controller;
        if (_controller != null)
            _controller.Changed += Refresh;
        Refresh();
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("PowerUpCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 205;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _slotBgs = new Image[3];
        _slotIcons = new RawImage[3];
        for (int i = 0; i < 3; i++)
            CreateSlot(canvasGo.transform, i, out _slotBgs[i], out _slotIcons[i]);

        var back = CreateImage(canvasGo.transform, "PowerUpCooldownBack", new Color(0f, 0.08f, 0.12f, 0.72f));
        var rt = back.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 34f);
        rt.sizeDelta = new Vector2(620f, 34f);

        _cooldownFill = CreateImage(back.transform, "PowerUpCooldownFill", new Color(0.9f, 0.95f, 1f, 0.92f));
        var fillRt = _cooldownFill.rectTransform;
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = Vector2.zero;

        var textGo = new GameObject("PowerUpCooldownText");
        textGo.transform.SetParent(back.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        _cooldownText = textGo.AddComponent<Text>();
        _cooldownText.font = CreateUiFont();
        _cooldownText.fontSize = 22;
        _cooldownText.fontStyle = FontStyle.Bold;
        _cooldownText.color = Color.white;
        _cooldownText.alignment = TextAnchor.MiddleCenter;
        _cooldownText.text = "";
    }

    void CreateSlot(Transform parent, int index, out Image bg, out RawImage icon)
    {
        const float slotSize = 92f;
        const float slotSpacing = 98f;
        const float slotX = 30f;
        const float slotTopY = 120f;

        var go = new GameObject("PowerUpSlot_" + index);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(slotX, slotTopY - index * slotSpacing);
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        bg = go.AddComponent<Image>();
        bg.color = new Color(0f, 0.08f, 0.12f, 0.65f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.08f, 0.08f);
        iconRt.anchorMax = new Vector2(0.92f, 0.92f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        icon = iconGo.AddComponent<RawImage>();
        icon.color = Color.clear;

        var fitter = iconGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1f;
    }

    void Refresh()
    {
        if (_slotBgs == null || _controller == null)
            return;

        var stored = _controller.StoredPowerUps();
        for (int i = 0; i < _slotBgs.Length; i++)
        {
            if (i < stored.Length)
            {
                _slotBgs[i].gameObject.SetActive(true);
                var c = ColorFor(stored[i]);
                _slotBgs[i].color = new Color(c.r, c.g, c.b, 0.65f);
                var tex = IconTexture(stored[i]);
                _slotIcons[i].texture = tex;
                _slotIcons[i].color = tex != null ? Color.white : Color.clear;
                if (tex != null)
                    _slotIcons[i].GetComponent<AspectRatioFitter>().aspectRatio =
                        (float)tex.width / tex.height;
            }
            else
            {
                _slotBgs[i].gameObject.SetActive(false);
            }
        }

        float ratio = _controller.Cooldown01;
        if (_cooldownFill != null)
            _cooldownFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        if (_cooldownText != null)
        {
            _cooldownText.text = _controller.HasActivePowerUp
                ? Label(_controller.ActivePowerUp) + " " + _controller.CooldownRemaining.ToString("0.0") + "s"
                : "Shift : utiliser power-up";
        }
    }

    void OnFinished(int score)
    {
        gameObject.SetActive(false);
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static Texture2D IconTexture(PenguinPowerUpType type)
    {
        string name = type switch
        {
            PenguinPowerUpType.Red   => "piment-icon",
            PenguinPowerUpType.Blue  => "giga-icon",
            PenguinPowerUpType.Green => "clone-icon",
            PenguinPowerUpType.Gray  => "aimant-icon",
            PenguinPowerUpType.Pink  => "bouclier-icon",
            _                        => null
        };
        return name != null ? Resources.Load<Texture2D>("UI/PowerUpIcons/" + name) : null;
    }

    static string Label(PenguinPowerUpType type)
    {
        switch (type)
        {
            case PenguinPowerUpType.Red: return "Piment";
            case PenguinPowerUpType.Blue: return "Giga";
            case PenguinPowerUpType.Green: return "Clone";
            case PenguinPowerUpType.Gray: return "Aimant";
            case PenguinPowerUpType.Pink: return "Bouclier";
            default: return "-";
        }
    }

    static Color ColorFor(PenguinPowerUpType type)
    {
        switch (type)
        {
            case PenguinPowerUpType.Red: return new Color(1f, 0.18f, 0.12f);
            case PenguinPowerUpType.Blue: return new Color(0.25f, 0.45f, 1f);
            case PenguinPowerUpType.Green: return new Color(0.15f, 0.85f, 0.24f);
            case PenguinPowerUpType.Gray: return new Color(0.72f, 0.72f, 0.75f);
            case PenguinPowerUpType.Pink: return new Color(1f, 0.35f, 0.85f);
            default: return Color.white;
        }
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
