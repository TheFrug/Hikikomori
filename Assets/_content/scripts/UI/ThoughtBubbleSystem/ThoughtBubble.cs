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

        [Header("Auto-sizing")]
        [SerializeField] private Vector2 padding = new Vector2(30f, 20f);
        [SerializeField] private float minWidth = 250f;
        [SerializeField] private float maxWidth = 500f;
        [SerializeField] private float minHeight = 75f;
        [SerializeField] private float maxHeight = 300f;

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
            ResizeToFitText();

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

            // randomize lateral drift amplitude and direction
            float swayAmplitude = UnityEngine.Random.Range(10f, 25f);
            float swayFrequency = UnityEngine.Random.Range(0.8f, 1.4f);
            float swayPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime; // UI unaffected by time scale
                float t = Mathf.Clamp01(elapsed / total);

                // vertical motion
                float newY = Mathf.Lerp(startPos.y, endPos.y, t);

                // side-to-side float (sinusoidal)
                float swayX = Mathf.Sin((elapsed * swayFrequency) + swayPhase) * swayAmplitude;

                rect.anchoredPosition = new Vector2(startPos.x + swayX, newY);

                // fade in/out curve
                float alpha;
                if (elapsed < edge) alpha = Mathf.Clamp01(elapsed / edge);
                else if (elapsed > (total - edge)) alpha = Mathf.Clamp01((total - elapsed) / edge);
                else alpha = 1f;
                canvasGroup.alpha = alpha;

                yield return null;
            }
            owner?.RecycleBubble(gameObject);
        }


        /// <summary>
        /// Immediately stop animation and return to owner pool.
        /// </summary>
        public void StopAndReturn()
        {
            StopAllCoroutines();
            owner?.RecycleBubble(this.gameObject);
        }

        private void ResizeToFitText()
        {
            if (bodyText == null || rect == null)
                return;

            // Force layout and ensure TMP knows what area it's wrapping inside
            bodyText.enableWordWrapping = true;
            bodyText.ForceMeshUpdate();

            // Measure with an approximate width limit to calculate multiline height correctly
            float availableWidth = Mathf.Clamp(rect.rect.width, minWidth, maxWidth);
            Vector2 preferred = bodyText.GetPreferredValues(bodyText.text, availableWidth, Mathf.Infinity);

            // Apply padding and clamp final dimensions
            float width = Mathf.Clamp(preferred.x + padding.x, minWidth, maxWidth);
            float height = Mathf.Clamp(preferred.y + padding.y, minHeight, maxHeight);

            // Apply to rects
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            if (background != null)
            {
                var bgRect = background.rectTransform;
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }

            // If the nameplate is visible, push it slightly below the top
            if (namePanel != null && namePanel.activeSelf)
            {
                RectTransform nameRect = namePanel.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    // center it horizontally
                    nameRect.anchoredPosition = new Vector2(0, 0);

                    // then move it slightly above or below the bubble depending on your layout
                    float offsetY = height * 0.5f + nameRect.rect.height * 0.5f + 8f; // 8px padding
                    // if you anchored namePanel to bottom center, flip the sign:
                    offsetY *= -1f;

                    nameRect.anchoredPosition = new Vector2(0, offsetY);
                }
            }
        }
    }
}
