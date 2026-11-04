using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only UI factory used by <see cref="Assignment3Builder"/>. Builds every Assignment 3
/// interface element with TextMeshPro in one consistent visual language:
///   dark translucent panels, warm amber accent, off-white text, one font.
/// (Lecture UI principles: visual hierarchy, contrast, consistency, minimal clutter.)
/// </summary>
public static class Assignment3UI
{
    public static readonly Color Panel = new Color(.05f, .06f, .07f, .86f);
    public static readonly Color PanelLight = new Color(.12f, .13f, .15f, .92f);
    public static readonly Color Accent = new Color(.95f, .71f, .35f);
    public static readonly Color Text = new Color(.93f, .92f, .88f);
    public static readonly Color Muted = new Color(.62f, .62f, .6f);
    public static readonly Color TrustColor = new Color(.42f, .74f, .93f);
    public static readonly Color HeatColor = new Color(.93f, .42f, .32f);

    public static TMP_FontAsset Font;
    static Sprite uiSprite, background, knob, checkmark, dropdownArrow, mask;

    public static void Init()
    {
        Font = TMP_Settings.defaultFontAsset;
        if (!Font) Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        dropdownArrow = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
    }

    // ------------------------------------------------------------------ primitives
    public static Canvas Canvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = .5f;
        return c;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name, float inset = 0)
    {
        var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    public static Image Img(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, bool sliced = true)
    {
        var rt = Rect(parent, name, anchorMin, anchorMax, pivot, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (sliced) { img.sprite = uiSprite; img.type = Image.Type.Sliced; }
        return img;
    }

    public static TextMeshProUGUI Txt(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, FontStyles style = FontStyles.Normal)
    {
        var rt = Rect(parent, name, anchorMin, anchorMax, pivot, pos, sizeDelta);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font) t.font = Font;
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal; t.richText = true; t.raycastTarget = false;
        return t;
    }

    public static TextMeshProUGUI FillText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, float inset = 0, FontStyles style = FontStyles.Normal)
    {
        var t = Txt(parent, name, text, size, color, align, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, style);
        t.rectTransform.offsetMin = new Vector2(inset, inset); t.rectTransform.offsetMax = new Vector2(-inset, -inset);
        return t;
    }

    public static CanvasGroup Group(GameObject go, float alpha = 1)
    {
        var g = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        g.alpha = alpha; return g;
    }

    public static Button Btn(Transform parent, string name, string label, Vector2 size, float fontSize = 28)
    {
        var img = Img(parent, name, PanelLight, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, size);
        var b = img.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.12f, .9f);
        colors.selectedColor = new Color(1.25f, 1.12f, .9f); colors.pressedColor = new Color(.8f, .8f, .8f);
        b.colors = colors;
        var le = img.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = size.y; le.preferredWidth = size.x;
        var t = FillText(img.transform, "Label", label, fontSize, Text, TextAlignmentOptions.Center, 12);
        t.fontStyle = FontStyles.Bold;
        // Thin amber accent on the left edge, a recurring motif of the UI.
        Img(img.transform, "Accent", Accent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(5, 0), false);
        return b;
    }

    public static VerticalLayoutGroup Column(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float spacing)
    {
        var rt = Rect(parent, name, anchor, anchor, pivot, pos, size);
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing; v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = false; v.childControlWidth = false; v.childForceExpandHeight = false; v.childForceExpandWidth = false;
        return v;
    }

    public static Image Bar(Transform parent, string name, string label, Color color, Vector2 pos, float width)
    {
        Txt(parent, name + "Label", label, 17, Muted, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(width, 22), FontStyles.Bold);
        var bg = Img(parent, name + "Back", new Color(1, 1, 1, .08f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos + new Vector2(0, -24), new Vector2(width, 12));
        var fill = Img(bg.transform, "Fill", color, Vector2.zero, Vector2.one, new Vector2(0, .5f), Vector2.zero, Vector2.zero, false);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = .4f;
        fill.sprite = uiSprite;
        return fill;
    }

    public static Slider SliderRow(Transform parent, string label, float y)
    {
        Txt(parent, label + "Label", label, 24, Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(300, 40));
        var go = DefaultControls.CreateSlider(Res());
        go.name = label + "Slider";
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(340, y - 10); rt.sizeDelta = new Vector2(360, 24);
        foreach (var img in go.GetComponentsInChildren<Image>()) if (img.name == "Fill") img.color = Accent;
        return go.GetComponent<Slider>();
    }

    public static Toggle ToggleRow(Transform parent, string label, float y)
    {
        Txt(parent, label + "Label", label, 24, Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(300, 40));
        var bg = Img(parent, label + "Toggle", PanelLight, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(340, y - 4), new Vector2(34, 34));
        var check = Img(bg.transform, "Checkmark", Accent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26), false);
        check.sprite = checkmark;
        var t = bg.gameObject.AddComponent<Toggle>(); t.targetGraphic = bg; t.graphic = check;
        return t;
    }

    public static TMP_Dropdown DropdownRow(Transform parent, string label, float y)
    {
        Txt(parent, label + "Label", label, 24, Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, y), new Vector2(300, 40));
        var go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources
        { standard = uiSprite, background = background, inputField = background, knob = knob, checkmark = checkmark, dropdown = dropdownArrow, mask = mask });
        go.name = label + "Dropdown";
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(340, y); rt.sizeDelta = new Vector2(360, 40);
        foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) { if (Font) t.font = Font; t.fontSize = 20; t.color = Color.black; }
        return go.GetComponent<TMP_Dropdown>();
    }

    static DefaultControls.Resources Res() => new DefaultControls.Resources
    { standard = uiSprite, background = background, inputField = background, knob = knob, checkmark = checkmark, dropdown = dropdownArrow, mask = mask };

    /// <summary>Auto-sizing dark backing behind a single line of text (prompts, toasts, subtitles).</summary>
    public static TextMeshProUGUI Pill(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float fontSize, float maxWidth, out GameObject root)
    {
        var img = Img(parent, name, Panel, anchor, anchor, pivot, pos, new Vector2(400, 50));
        root = img.gameObject;
        var h = img.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(22, 22, 10, 10); h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false;
        var fit = img.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var t = Txt(img.transform, "Text", "", fontSize, Text, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        t.textWrappingMode = TextWrappingModes.Normal;
        // Limit width: wrap long lines instead of growing past maxWidth.
        var limiter = t.gameObject.AddComponent<MaxWidthText>(); limiter.maxWidth = maxWidth;
        return t;
    }
}
