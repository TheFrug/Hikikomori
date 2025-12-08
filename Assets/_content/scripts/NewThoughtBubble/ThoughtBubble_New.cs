using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThoughtBubble_New : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text bodyText = null!;
    [SerializeField] private GameObject namePanel = null!;      // panel (enable/disable)
    [SerializeField] private TMP_Text nameText = null!;         // text inside namePanel
    [SerializeField] private Image background = null!;
    [SerializeField] private CanvasGroup canvasGroup = null!;

    [Header("Auto-sizing")]
    [SerializeField] private bool autoSize = true;
    [SerializeField] private Vector2 padding = new Vector2(30f, 20f);
    [SerializeField] private float minWidth = 100f;
    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float minHeight = 75f;
    [SerializeField] private float maxHeight = 300f;


    public RectTransform RectTransform { get; private set; } = null!;
    [Header("Set at Runtime")]
    public bool HasSpeaker;
    public float TopTimer;
    public float CenterX;
    public float SwayTimer;
    public float Duration;
    public bool Done;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    // Initialize for automatic-floating mode (manager will position/activate)
    // Initialize for automatic-floating mode (manager will position/activate)
    public void InitializeAutomatic(string text, Color bubbleColor, TMP_FontAsset font, string speakerKey, Color textColor)
    {
        // set text + font + color immediately
        ApplyText(text, font);
        ApplyColor(bubbleColor);

        if (bodyText != null)
        {
            bodyText.color = textColor; // <-- apply textColor here
        }

        // Name logic: consult FamilyManager to determine whether to show name panel
        var fm = FamilyManager.Instance;
        bool showName = false;
        string displayName = string.Empty;

        if (fm != null)
        {
            var part = fm.parts.Find(p => p.key == speakerKey);
            if (part != null && part.nameRevealed)
            {
                showName = true;
                displayName = part.realName;
            }
        }

        if (namePanel != null)
        {
            namePanel.SetActive(showName);
            if (showName && nameText != null)
            {
                nameText.text = displayName;
                if (font != null) nameText.font = font;
                if (fm != null)
                {
                    var part = fm.parts.Find(p => p.key == speakerKey);
                    if (part != null)
                        nameText.color = part.textColor; // optional: match name text color too
                }
            }
        }

        // autosize (forces TMP mesh updates)
        if (autoSize)
            ResizeToFitText();

        // runtime state init
        HasSpeaker = namePanel != null && namePanel.activeSelf;
        TopTimer = 0f;
        SwayTimer = 0f;
        Done = false;

        if (Duration <= 0f) Duration = 3f;

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }


    // Called by controller when returning to pool
    public void ResetBubble()
    {
        TopTimer = 0f;
        Done = false;
        SwayTimer = 0f;
        Duration = 0f;
        HasSpeaker = false;
        gameObject.SetActive(false);
    }

    private void ApplyText(string text, TMP_FontAsset font)
    {
        if (bodyText != null)
        {
            bodyText.text = text ?? string.Empty;
            if (font != null) bodyText.font = font;
            bodyText.ForceMeshUpdate();
        }
    }

    private void ApplyColor(Color c)
    {
        if (background != null) background.color = c;
        if (nameText != null) nameText.color = c;
    }

    public CanvasGroup CanvasGroup => canvasGroup;

    // Non-public helper but accessible for controller to know how tall the name area is
    public float SpeakerHeight
    {
        get
        {
            if (namePanel == null) return 0f;
            var rt = namePanel.GetComponent<RectTransform>();
            return rt != null ? rt.rect.height : 0f;
        }
    }

    // Resize function (keeps the same algorithm you already had)
    private void ResizeToFitText()
    {
        if (bodyText == null || RectTransform == null) return;

        bodyText.enableWordWrapping = true;
        bodyText.enableAutoSizing = false;
        bodyText.overflowMode = TextOverflowModes.Overflow;

        RectTransform textRect = bodyText.rectTransform;

        bodyText.ForceMeshUpdate();
        Vector2 fullSize = bodyText.GetPreferredValues(bodyText.text, Mathf.Infinity, Mathf.Infinity);

        float targetContentWidth = Mathf.Clamp(fullSize.x, minWidth - padding.x, maxWidth - padding.x);

        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetContentWidth);
        bodyText.ForceMeshUpdate();

        Vector2 wrappedSize = bodyText.GetPreferredValues(bodyText.text, targetContentWidth, Mathf.Infinity);
        float contentWidth = wrappedSize.x;
        float contentHeight = wrappedSize.y;

        float finalWidth = Mathf.Clamp(contentWidth + padding.x, minWidth, maxWidth);
        float finalHeight = Mathf.Clamp(contentHeight + padding.y, minHeight, maxHeight);

        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

        if (background != null)
        {
            var bgRect = background.rectTransform;
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
            bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        }

        float innerW = Mathf.Max(8f, finalWidth - padding.x);
        float innerH = Mathf.Max(8f, finalHeight - padding.y);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerW);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, innerH);
        textRect.anchoredPosition = Vector2.zero;

        bodyText.ForceMeshUpdate();
    }
}
