using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectHiki.UI
{
    /// <summary>
    /// Handles one bubble's lifetime: text, color, font, rising and fading.
    /// Notifies owner (ThoughtBubbleView) when finished so the instance can be pooled.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ThoughtBubble : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI bodyText = null!;
        [SerializeField] private Image background = null!;
        [Tooltip("The full nameplate panel (toggle this on/off). Should have a TMP_Text child for the speaker name.")]
        [SerializeField] private GameObject namePanel = null!;
        private TMP_Text? nameText;

        private CanvasGroup canvasGroup = null!;
        private RectTransform rect = null!;

        private float lifetime = 3f;
        private float riseDistance = 60f;
        private float fadeEdge = 0.35f;
        private ThoughtBubbleView? owner = null;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (namePanel != null)
                nameText = namePanel.GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// Initialize and start the bubble’s animation. Safe to call multiple times.
        /// </summary>
        public void Initialize(string text, Color color, TMP_FontAsset? font, string speakerKey,
                               float lifetime, float riseDistance, float fadeEdge, ThoughtBubbleView owner)
        {
            EnsureComponents();

            this.lifetime = lifetime > 0f ? lifetime : this.lifetime;
            this.riseDistance = riseDistance;
            this.fadeEdge = fadeEdge;
            this.owner = owner;

            // Body text setup
            bodyText.text = text ?? string.Empty;
            if (font != null) bodyText.font = font;
            if (background != null) background.color = color;

            // Name setup (depends on FamilyManager)
            bool showName = false;
            string displayName = string.Empty;

            var fm = FamilyManager.Instance;
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
                    nameText.color = color;
                    if (font != null) nameText.font = font;
                }
            }

            // Start at zero alpha, position set by the view
            canvasGroup.alpha = 0f;

            // Begin lifetime animation
            StopAllCoroutines();
            StartCoroutine(FloatAndFadeCoroutine());
        }

        private void EnsureComponents()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (bodyText == null)
                bodyText = GetComponentInChildren<TextMeshProUGUI>(true)
                    ?? throw new System.InvalidOperationException("ThoughtBubble requires a TextMeshProUGUI child for bodyText.");
            if (background == null)
                background = GetComponentInChildren<Image>(true);
            if (namePanel != null && nameText == null)
                nameText = namePanel.GetComponentInChildren<TMP_Text>(true);
        }

        private IEnumerator FloatAndFadeCoroutine()
        {
            EnsureComponents();

            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * riseDistance;

            float elapsed = 0f;
            float total = Mathf.Max(0.01f, lifetime);
            float edge = Mathf.Clamp01(fadeEdge);

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime; // UI ignores Time.timeScale
                float t = Mathf.Clamp01(elapsed / total);

                // Smooth rise
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // Fade in/out curve
                float alpha;
                if (elapsed < edge) alpha = Mathf.Clamp01(elapsed / edge);
                else if (elapsed > (total - edge)) alpha = Mathf.Clamp01((total - elapsed) / edge);
                else alpha = 1f;
                canvasGroup.alpha = alpha;

                yield return null;
            }

            // Return to pool via owner
            owner?.RecycleBubble(this.gameObject);
        }

        /// <summary>
        /// Immediately stop animation and return to owner pool.
        /// </summary>
        public void StopAndReturn()
        {
            StopAllCoroutines();
            owner?.RecycleBubble(this.gameObject);
        }
    }
}
