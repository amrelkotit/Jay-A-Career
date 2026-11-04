using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI helper: lets a TextMeshPro label grow with its content (for auto-sized prompt/toast/subtitle
/// backings) but wrap onto new lines once it reaches <see cref="maxWidth"/>.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class MaxWidthText : MonoBehaviour, ILayoutElement
{
    public float maxWidth = 900;
    TextMeshProUGUI text;
    TextMeshProUGUI Text => text ? text : text = GetComponent<TextMeshProUGUI>();

    public float minWidth => -1;
    public float preferredWidth => Mathf.Min(Text.GetPreferredValues(Text.text, float.PositiveInfinity, 0).x, maxWidth);
    public float flexibleWidth => -1;
    public float minHeight => -1;
    public float preferredHeight => Text.GetPreferredValues(Text.text, preferredWidth, 0).y;
    public float flexibleHeight => -1;
    public int layoutPriority => 2;
    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }
}
