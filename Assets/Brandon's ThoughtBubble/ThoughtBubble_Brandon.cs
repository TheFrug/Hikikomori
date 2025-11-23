using TMPro;
using UnityEngine;

public class ThoughtBubble_Brandon : MonoBehaviour
{
    [SerializeField] private TMP_Text _dialogue;
    [SerializeField] private GameObject _speakerPanel;
    [SerializeField] private TMP_Text _speaker;
    public float SpeakerHeight = 20;

    [Header("Auto size")]
    [SerializeField] private bool _autoSize;
    [SerializeField] private float _minWidth = 150;
    [SerializeField] private float _maxWidth = 400;
    [SerializeField] private float _padding = 40;

    [Header("Set at Runtime")]
    public RectTransform RectTransform;
    public bool HasSpeaker;
    public float TopTimer;
    public float CenterX;
    public float SwayTimer;
    public float Duration;
    public bool Done;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    public void ShowBubble(string speaker, string message, float duration)
    {
        _dialogue.text = message;
        HasSpeaker = !string.IsNullOrEmpty(speaker);
        _speakerPanel.SetActive(HasSpeaker);
        _speaker.text = speaker;
        Duration = duration;
        gameObject.SetActive(true);
        if (_autoSize) ResizeToFitText();
    }

    public void ResetBubble()
    {
        TopTimer = 0;
        Done = false;
        gameObject.SetActive(false);
    }

    private void ResizeToFitText()
    {
        if (_dialogue == null || RectTransform == null)
            return;

        _dialogue.enableWordWrapping = true;
        _dialogue.enableAutoSizing = false;
        _dialogue.overflowMode = TextOverflowModes.Overflow;

        RectTransform textRect = _dialogue.rectTransform;

        // Pass 1: measure text unconstrained to find natural width
        _dialogue.ForceMeshUpdate();
        Vector2 fullSize = _dialogue.GetPreferredValues(_dialogue.text, Mathf.Infinity, Mathf.Infinity);

        // Clamp that width by our max bubble width
        float targetContentWidth = Mathf.Clamp(fullSize.x, _minWidth - _padding, _maxWidth - _padding);

        // Apply that width so TMP can wrap properly
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetContentWidth);
        _dialogue.ForceMeshUpdate();

        // Pass 2: measure again with wrapping now enforced
        Vector2 wrappedSize = _dialogue.GetPreferredValues(_dialogue.text, targetContentWidth, Mathf.Infinity);

        float contentWidth = wrappedSize.x;

        // Add padding and clamp
        float finalWidth = Mathf.Clamp(contentWidth + _padding, _minWidth, _maxWidth);

        // Apply to bubble and background
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);

        // Apply final text box size
        float innerW = Mathf.Max(8f, finalWidth - _padding);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerW);
        textRect.anchoredPosition = Vector2.zero;

        _dialogue.ForceMeshUpdate();
    }
}
