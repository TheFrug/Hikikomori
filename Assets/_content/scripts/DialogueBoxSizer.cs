using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System;

public class DialogueBoxSizer : MonoBehaviour
{
    //Comment
    [Header("References")]
    public RectTransform backgroundRect;   // the background image rect (sibling of text)
    public RectTransform textRect;         // the TMP text rect (sibling)
    public TMP_Text tmpText;               // the TMP component

    [Header("Layout constraints (px)")]
    public float minWidth = 300f;          // smallest default box width
    public float maxWidth = 600f;          // largest box width (after which text wraps)
    public float paddingHorizontal = 24f;  // background padding left+right
    public float paddingVertical = 12f;    // background padding top+bottom

    /// <summary>
    /// Call this immediately after setting tmpText.text.
    /// </summary>
    public void UpdateBackgroundSizeForText()
    {
        if (tmpText == null || backgroundRect == null || textRect == null) return;

        // Force TMP to recompute layout
        Canvas.ForceUpdateCanvases();

        // Get text's preferred values (unbounded width)
        // Passing a very large width gives us the width TMP would need on a single line.
        Vector2 preferredUnbounded = tmpText.GetPreferredValues(tmpText.text, 10000f, 10000f);

        // Now ask TMP how big it would be if constrained to maxWidth (so we can measure wrapped)
        float wrapWidth = Mathf.Max(minWidth - paddingHorizontal, maxWidth - paddingHorizontal);
        Vector2 preferredWrapped = tmpText.GetPreferredValues(tmpText.text, wrapWidth, 10000f);

        // If the unbounded width fits within our maxWidth, use that (no wrapping). Else clamp to maxWidth.
        float desiredTextWidth = Mathf.Clamp(preferredUnbounded.x, minWidth - paddingHorizontal, maxWidth - paddingHorizontal);

        // But if preferredUnbounded is larger than (maxWidth - padding), the resulting rendered height will be preferredWrapped.y
        float desiredTextHeight;
        if (preferredUnbounded.x > (maxWidth - paddingHorizontal))
        {
            // text will wrap: height is the wrapped height
            desiredTextHeight = preferredWrapped.y;
            desiredTextWidth = maxWidth - paddingHorizontal;
        }
        else
        {
            desiredTextHeight = preferredUnbounded.y;
        }

        // Apply sizes: textRect should be the size available for text (account for padding inside background)
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredTextWidth);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredTextHeight);

        // Background target size includes padding
        float bgWidth = desiredTextWidth + paddingHorizontal;
        float bgHeight = desiredTextHeight + paddingVertical;

        bgWidth = Mathf.Clamp(bgWidth, minWidth, maxWidth);
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgWidth);
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bgHeight);

        // If you use a LayoutGroup on the parent, force rebuild:
        var parent = backgroundRect.parent as RectTransform;
        if (parent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }
}
