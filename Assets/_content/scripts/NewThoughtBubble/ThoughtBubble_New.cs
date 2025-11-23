using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectHiki.UI
{
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

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        // NOTE: speakerKey is the key used by FamilyManager (so bubble controls whether to show a name panel).
        public void InitializeAutomatic(string text, Color bubbleColor, TMP_FontAsset font, string speakerKey, object view)
        {
            ApplyText(text, font);
            ApplyColor(bubbleColor);

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
                }
            }

            if (autoSize)
                ResizeToFitText();

            canvasGroup.alpha = 1f;
        }

        private void ApplyText(string text, TMP_FontAsset font)
        {
            if (bodyText != null)
            {
                bodyText.text = text ?? string.Empty;
                if (font != null) bodyText.font = font;
            }
        }

        private void ApplyColor(Color c)
        {
            if (background != null) background.color = c;
            if (nameText != null) nameText.color = c;
        }

        public RectTransform Rect => rectTransform;

        public CanvasGroup CanvasGroup => canvasGroup;

        private void ResizeToFitText()
        {
            if (bodyText == null || rectTransform == null) return;

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

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

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
}
