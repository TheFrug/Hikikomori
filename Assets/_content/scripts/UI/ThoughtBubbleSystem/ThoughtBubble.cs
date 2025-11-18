using TMPro;
using UnityEngine;

namespace ProjectHiki.UI
{
    public class ThoughtBubble : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _dialogue;
        [SerializeField] private GameObject _speakerPanel;
        [SerializeField] private TMP_Text _speaker;

        [Header("Auto Size")]
        [SerializeField] private bool _autoSize = true;
        [SerializeField] private float _minWidth = 150f;
        [SerializeField] private float _maxWidth = 400f;
        [SerializeField] private float _padding = 40f;

        [Header("Runtime")]
        public RectTransform RectTransform;
        public bool HasSpeaker;
        public float Duration;
        public float TopTimer;
        public float CenterX;
        public float SwayTimer;
        public bool Done;

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        // Called by ThoughtBubbleController when spawning
        public void ShowBubble(string speaker, string message, float duration)
        {
            _dialogue.text = message;
            Duration = duration;

            HasSpeaker = !string.IsNullOrEmpty(speaker);
            _speakerPanel.SetActive(HasSpeaker);

            if (HasSpeaker)
                _speaker.text = speaker;

            gameObject.SetActive(true);

            if (_autoSize)
                ResizeToFitText();
        }

        // Called by controller when recycling
        public void ResetBubble()
        {
            Done = false;
            TopTimer = 0f;
            SwayTimer = 0f;
            CenterX = 0f;

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

            // Force initial measure
            _dialogue.ForceMeshUpdate();
            Vector2 fullSize = 
                _dialogue.GetPreferredValues(
                    _dialogue.text, Mathf.Infinity, Mathf.Infinity);

            float targetContentWidth =
                Mathf.Clamp(fullSize.x, _minWidth - _padding, _maxWidth - _padding);

            textRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, targetContentWidth);

            _dialogue.ForceMeshUpdate();

            Vector2 wrappedSize =
                _dialogue.GetPreferredValues(
                    _dialogue.text, targetContentWidth, Mathf.Infinity);

            float finalWidth =
                Mathf.Clamp(wrappedSize.x + _padding, _minWidth, _maxWidth);

            RectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, finalWidth);

            float innerW = Mathf.Max(8f, finalWidth - _padding);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerW);
            textRect.anchoredPosition = Vector2.zero;

            _dialogue.ForceMeshUpdate();
        }

        public float SpeakerHeight
        {
            get
            {
                var rt = _speakerPanel != null ? _speakerPanel.GetComponent<RectTransform>() : null;
                return rt != null ? rt.rect.height : 0f;
            }
        }

        public float GetAnchoredY()
        {
            return RectTransform != null ? RectTransform.anchoredPosition.y : 0f;
        }

        public float GetBottomEdgeY()
        {
            if (RectTransform == null)
                return 0f;

            float y = RectTransform.anchoredPosition.y;
            float halfHeight = RectTransform.rect.height * 0.5f;
            return y - halfHeight;
        }


        public void InitializeInteractive(
            string text,
            Color bubbleColor,
            TMP_FontAsset font,
            string speakerName,
            ThoughtBubbleView owner
        ) 
        {
            // Fill the bubble
            _dialogue.text = text;
            if (!string.IsNullOrEmpty(speakerName))
            {
                HasSpeaker = true;
                _speakerPanel.SetActive(true);
                _speaker.text = speakerName;
            }
            else
            {
                HasSpeaker = false;
                _speakerPanel.SetActive(false);
            }

            // Color + font (if provided)
            if (font != null)
                _dialogue.font = font;
            _dialogue.color = bubbleColor;

            gameObject.SetActive(true);
            if (_autoSize) ResizeToFitText();
        }


        public void Initialize(
            string text,
            Color bubbleColor,
            TMP_FontAsset font,
            string speakerName,
            float lifetime,
            float riseDistance,
            float fadeTime,
            ThoughtBubbleView view,
            ThoughtBubble previous
        ) {
            _dialogue.text = text;
            Duration = lifetime;

            HasSpeaker = !string.IsNullOrEmpty(speakerName);
            _speakerPanel.SetActive(HasSpeaker);

            if (HasSpeaker)
                _speaker.text = speakerName;

            if (font != null)
                _dialogue.font = font;
            _dialogue.color = bubbleColor;

            gameObject.SetActive(true);

            if (_autoSize)
                ResizeToFitText();

            // No animation logic yet — your controller handles the movement.
        }


        public void SetOwnerView(MonoBehaviour view)
        {
            // Stub: ThoughtBubbleView wants to keep a link, but ThoughtBubble no longer stores one
        }

        public void SetCeiling(float ceilingY)
        {
            // Stub: You may not need this anymore; controller handles ceiling entirely
        }
    }
    
}
