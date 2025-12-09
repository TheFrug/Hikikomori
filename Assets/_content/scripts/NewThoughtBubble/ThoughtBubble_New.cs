using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ThoughtBubble_New : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text bodyText = null!;
    [SerializeField] private GameObject namePanel = null!;
    [SerializeField] private TMP_Text nameText = null!;
    [SerializeField] private Image background = null!;
    [SerializeField] private CanvasGroup canvasGroup = null!;

    [Header("Option UI (optional)")]
    [SerializeField] private Button optionButton = null;
    [SerializeField] private TMP_Text optionNumberText = null;

    [Header("Auto-sizing")]
    [SerializeField] private bool autoSize = true;
    [SerializeField] private Vector2 padding = new Vector2(30f, 20f);
    [SerializeField] private float minWidth = 100f;
    [SerializeField] private float maxWidth = 400f;
    [SerializeField] private float minHeight = 75f;
    [SerializeField] private float maxHeight = 300f;

    public RectTransform RectTransform { get; private set; } = null!;
    public bool HasSpeaker;
    public float TopTimer;
    public float CenterX;
    public float SwayTimer;
    public float Duration;
    public bool Done;

    public bool IsOption { get; private set; } = false;
    private Action<int>? onOptionSelected;
    private int optionIndex = -1;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Always hide option-specific UI unless manually enabled
        if (optionNumberText != null) optionNumberText.gameObject.SetActive(false);
        if (optionButton != null)
        {
            optionButton.gameObject.SetActive(false);
            optionButton.onClick.RemoveAllListeners();
            optionButton.onClick.AddListener(OnOptionButtonClickedInternal);
        }
    }

    public void InitializeAutomatic(
        string text, Color bubbleColor, TMP_FontAsset font,
        string speakerKey, Color textColor)
    {
        // ensure option UI is disabled for non-option bubbles
        IsOption = false;
        onOptionSelected = null;
        optionIndex = -1;

        if (optionNumberText != null) optionNumberText.gameObject.SetActive(false);
        if (optionButton != null) optionButton.gameObject.SetActive(false);

        ApplyText(text, font);
        ApplyColor(bubbleColor);

        if (bodyText != null)
            bodyText.color = textColor;

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
                        nameText.color = part.textColor;
                }
            }
        }

        if (autoSize)
            ResizeToFitText();

        HasSpeaker = namePanel != null && namePanel.activeSelf;
        TopTimer = 0f;
        SwayTimer = 0f;
        Done = false;
        Duration = Duration <= 0f ? 3f : Duration;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public void InitializeOption(
        string text, Color bubbleColor, TMP_FontAsset font,
        int optionNumber, Action<int> onSelected)
    {
        ApplyText(text, font);
        ApplyColor(bubbleColor);

        IsOption = true;
        optionIndex = optionNumber;
        onOptionSelected = onSelected;

        if (namePanel != null)
            namePanel.SetActive(false);

        // Show number bubble ONLY for options
        if (optionNumberText != null)
        {
            optionNumberText.gameObject.SetActive(true);
            optionNumberText.text = (optionNumber + 1).ToString();
        }

        if (optionButton != null)
            optionButton.gameObject.SetActive(true);

        ResizeToFitText();

        TopTimer = 0f;
        SwayTimer = 0f;
        Done = false;
        Duration = Mathf.Infinity;
        HasSpeaker = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void OnOptionButtonClickedInternal()
    {
        if (IsOption && onOptionSelected != null)
            onOptionSelected.Invoke(optionIndex);
    }

    public void ResetBubble()
    {
        TopTimer = 0f;
        Done = false;
        SwayTimer = 0f;
        Duration = 0f;
        HasSpeaker = false;
        IsOption = false;
        onOptionSelected = null;
        optionIndex = -1;

        if (optionNumberText != null) optionNumberText.gameObject.SetActive(false);
        if (optionButton != null) optionButton.gameObject.SetActive(false);

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

    public float SpeakerHeight
    {
        get
        {
            if (namePanel == null) return 0f;
            var rt = namePanel.GetComponent<RectTransform>();
            return rt != null ? rt.rect.height : 0f;
        }
    }

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
