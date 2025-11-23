using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectHiki.UI
{
    public class ThoughtBubble_New : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text bodyText = null!;
        [SerializeField] private TMP_Text speakerText = null!;
        [SerializeField] private Image background = null!;
        [SerializeField] private CanvasGroup canvasGroup = null!;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void InitializeInteractive(string text, Color bubbleColor, TMP_FontAsset font, string speakerName, object view)
        {
            ApplyText(text, font);
            ApplySpeaker(speakerName, font);
            ApplyColor(bubbleColor);
            canvasGroup.alpha = 1f;
        }

        public void InitializeAutomatic(string text, Color bubbleColor, TMP_FontAsset font, string speakerName, object view)
        {
            ApplyText(text, font);
            ApplySpeaker(speakerName, font);
            ApplyColor(bubbleColor);
            canvasGroup.alpha = 1f;
        }

        private void ApplyText(string text, TMP_FontAsset font)
        {
            bodyText.text = text;
            if (font != null) bodyText.font = font;
        }

        private void ApplySpeaker(string speakerName, TMP_FontAsset font)
        {
            speakerText.text = speakerName;
            if (font != null) speakerText.font = font;
        }

        private void ApplyColor(Color c)
        {
            if (background != null) background.color = c;
        }

        // Click handling now routes to the manager to avoid type coupling
        public void OnClick()
        {
            // Automatic mode: do nothing for now
            // mgr.NotifyBubbleClicked(this); // <-- remove
        }

        public RectTransform Rect => rectTransform;

        public float Height => rectTransform != null ? rectTransform.rect.height : 0f;
        public float Width => rectTransform != null ? rectTransform.rect.width : 0f;

        public float GetAnchoredY() => rectTransform != null ? rectTransform.anchoredPosition.y : 0f;
        public float GetBottomEdgeY() => rectTransform != null ? rectTransform.anchoredPosition.y - rectTransform.rect.height * 0.5f : 0f;
        public float GetTopEdgeY() => rectTransform != null ? rectTransform.anchoredPosition.y + rectTransform.rect.height * 0.5f : 0f;

        public CanvasGroup CanvasGroup => canvasGroup;
    }
}
