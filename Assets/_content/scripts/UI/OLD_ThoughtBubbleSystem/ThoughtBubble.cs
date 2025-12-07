using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace ProjectHiki.UI
{
    /// <summary>
    /// Handles one bubble's lifetime: text, color, font, rising, holding at a ceiling, and fading away.
    /// Notifies owner (ThoughtBubbleView) when finished so the instance can be pooled.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ThoughtBubble : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _dialogue = null!;
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

        // NEW: target ceiling Y (in bubbleContainer local coordinates).
        // The bubble's top edge should not exceed this value.
        private float ceilingY = float.PositiveInfinity;

        // Movement tuning
        [Header("Movement / Timing (tweak to taste)")]
        [SerializeField] private float floatToCeilingSpeed = 120f; // units/sec
        [SerializeField] private float floatAwaySpeed = 40f; // units/sec while leaving
        [SerializeField] private float fadeOutDuration = 0.6f; // fade out after lifetime ends (seconds)
        [SerializeField] private float fadeInDuration = 0.2f;

        [Header("Auto-sizing")]
        [SerializeField] private bool autoSize;
        [SerializeField] private Vector2 padding = new Vector2(30f, 20f);
        [SerializeField] private float minWidth = 100f;
        [SerializeField] private float maxWidth = 400f;
        [SerializeField] private float minHeight = 75f;
        [SerializeField] private float maxHeight = 300f;

        private ThoughtBubble previousThought;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (namePanel != null)
                nameText = namePanel.GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// Initialize and start the bubble’s animation. Safe to call multiple times.
        /// After Initialize, the presenter should call SetCeiling(...) before expecting it to hold.
        /// </summary>
        public void Initialize(string text, Color color, TMP_FontAsset? font, string speakerKey,
                               float lifetime, float riseDistance, float fadeEdge, ThoughtBubbleView owner, ThoughtBubble previousThought)
        {
            EnsureComponents();

            this.lifetime = lifetime > 0f ? lifetime : this.lifetime;
            this.riseDistance = riseDistance;
            this.fadeEdge = fadeEdge;
            this.owner = owner;
            this.previousThought = previousThought;

            // Body text setup
            _dialogue.text = text ?? string.Empty;
            if (font != null) _dialogue.font = font;
            if (background != null) background.color = color;
            
            if (autoSize)
                ResizeToFitText();

            // Name setup (depends on FamilyManager)
            bool showName = false;
            string displayName = string.Empty;

            var familyManager = FamilyManager.Instance;
            if (familyManager != null)
            {
                var part = familyManager.parts.Find(p => p.key == speakerKey);
                if (part != null && part.nameRevealed)
                {
                    showName = true;
                    displayName = part.realName;
                }
            }

            if (namePanel != null)
            {
                namePanel.SetActive(showName);
                nameText.color = color;
                if (showName && nameText != null)
                {
                    nameText.text = displayName;
                    //nameText.color = color;
                    if (font != null) nameText.font = font;
                }
            }

            // Start invisible; fade-in while moving to ceiling
            canvasGroup.alpha = 0f;

            // Reset any previous coroutine
            StopAllCoroutines();
            StartCoroutine(FloatToCeilingAndHoldCoroutine());
        }

        private void EnsureComponents()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (_dialogue == null)
                _dialogue = GetComponentInChildren<TextMeshProUGUI>(true)
                    ?? throw new System.InvalidOperationException("ThoughtBubble requires a TextMeshProUGUI child for bodyText.");
            if (background == null)
                background = GetComponentInChildren<Image>(true);
            if (namePanel != null && nameText == null)
                nameText = namePanel.GetComponentInChildren<TMP_Text>(true);
        }

        public void InitializeInteractive(string text, Color color, TMP_FontAsset font, string speakerKey, ThoughtBubbleView owner)
        {
            EnsureComponents();

            this.owner = owner;

            // Text setup
            _dialogue.text = text ?? string.Empty;
            if (font != null) _dialogue.font = font;
            if (background != null) background.color = color;

            if (autoSize)
                ResizeToFitText();

            // Name setup
            bool showName = false;
            string displayName = string.Empty;

            var familyManager = FamilyManager.Instance;
            if (familyManager != null)
            {
                var part = familyManager.parts.Find(p => p.key == speakerKey);
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

            canvasGroup.alpha = 1f;

            // Don’t float or fade — interactive bubbles stay static
            StopAllCoroutines();
        }

        /// <summary>
        /// Sets the ceiling Y (in the same local coordinate space as rect.anchoredPosition).
        /// Bubble will float upward until its top edge is <= ceilingY (i.e., top edge touches ceilingY).
        /// </summary>
        public void SetCeiling(float ceilingY)
        {
            this.ceilingY = ceilingY;
        }

        /// <summary>
        /// Returns the anchored Y of this bubble (center).
        /// </summary>
        public float GetAnchoredY() => rect != null ? rect.anchoredPosition.y : 0f;

        /// <summary>
        /// Top edge Y in local coords (anchoredPosition.y + half height)
        /// </summary>
        public float GetTopEdgeY() => rect != null ? rect.anchoredPosition.y + rect.rect.height * 0.5f : 0f;

        /// <summary>
        /// Bottom edge Y in local coords (anchoredPosition.y - half height)
        /// </summary>
        public float GetBottomEdgeY() => rect != null ? rect.anchoredPosition.y - rect.rect.height * 0.5f : 0f;

        private IEnumerator FloatToCeilingAndHoldCoroutine()
        {
            EnsureComponents();

            Vector2 startPos = rect.anchoredPosition;

            // Compute target Y for the center so that top edge equals ceilingY.
            float halfHeight = rect.rect.height * 0.5f;
            float targetCenterY = float.PositiveInfinity;
            if (float.IsInfinity(ceilingY))
            {
                // no ceiling defined — just float by riseDistance and behave like before
                targetCenterY = startPos.y + riseDistance;
            }
            else
            {
                targetCenterY = ceilingY - halfHeight;
            }
            RectTransform previousRect = previousThought != null ? previousThought.GetComponent<RectTransform>() : null;
            float spacing = 10;

            // If the target is below current y (rare), clamp to current + small offset so it still animates.
            if (targetCenterY < startPos.y)
                targetCenterY = startPos.y + Mathf.Min(10f, riseDistance * 0.25f);

            // Randomize lateral sway params (kept similar to prior behavior)
            float swayAmplitude = UnityEngine.Random.Range(5f, 10f);
            float swayFrequency = UnityEngine.Random.Range(1f, 2f);
            float swayPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            // Fade-in
            float fadeTimer = 0f;
            float timer = 0f;
            while (timer < lifetime)
            {
                if (previousRect != null)
                    targetCenterY = Mathf.Min(ceilingY - halfHeight, previousRect.position.y - previousRect.rect.height * 0.5f - rect.rect.height * 0.5f - spacing);
                float delta = Time.unscaledDeltaTime;
                // Move toward target center Y
                float newY = Mathf.MoveTowards(rect.anchoredPosition.y, targetCenterY, floatToCeilingSpeed * delta);

                // horizontal sway
                float swayX = Mathf.Sin((Time.unscaledTime * swayFrequency) + swayPhase) * swayAmplitude;
                rect.anchoredPosition = new Vector2(startPos.x + swayX, newY);

                // fade-in smoothly
                if (fadeInDuration > 0f)
                {
                    fadeTimer += delta;
                    canvasGroup.alpha = Mathf.Clamp01(fadeTimer / fadeInDuration);
                }
                else canvasGroup.alpha = 1f;

                if (Mathf.Abs(rect.anchoredPosition.y - targetCenterY) <= 0.5f)
                {
                    // Snap exactly to target (stabilize)
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetCenterY);
                    canvasGroup.alpha = 1f;
                    timer += Time.unscaledDeltaTime;
                }

                yield return null;
            }

            // Wait while pinned until lifetime expires (infinite lifetime means wait until externally cancelled)
            if (float.IsInfinity(lifetime))
            {
                // Interactive — do nothing; bubble will be held until StopAndReturn or owner triggers RecycleImmediate
                yield break;
            }

            // Lifetime ended — float away upward and fade out
            float fadeElapsed = 0f;
            Vector2 leaveStart = rect.anchoredPosition;
            Vector2 leaveEnd = leaveStart + Vector2.up * riseDistance; // leave by same riseDistance
            float leaveDistance = Mathf.Abs(leaveEnd.y - leaveStart.y);

            // If fadeOutDuration is zero or negative, use fadeEdge as fallback
            float fadeDuration = fadeOutDuration > 0f ? fadeOutDuration : Mathf.Max(0.01f, fadeEdge);

            float leaveTime = Mathf.Max(0.01f, leaveDistance / floatAwaySpeed);
            float leaveTimer = 0f;

            while (leaveTimer < leaveTime)
            {
                float delta = Time.unscaledDeltaTime;
                leaveTimer += delta;
                float t = Mathf.Clamp01(leaveTimer / leaveTime);
                float newY = Mathf.Lerp(leaveStart.y, leaveEnd.y, t);

                // continue small sway while leaving
                float swayX = Mathf.Sin((Time.unscaledTime * swayFrequency) + swayPhase) * swayAmplitude * (1f - t * 0.8f); // diminish sway as it leaves
                rect.anchoredPosition = new Vector2(leaveStart.x + swayX, newY);

                // fade start after 25% of leaveTime so fade overlaps leaving movement
                float fadeStart = 0.25f;
                if (t >= fadeStart)
                {
                    float fadeT = Mathf.Clamp01((t - fadeStart) / (1f - fadeStart));
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
                }

                yield return null;
            }

            // Ensure fully invisible before returning
            canvasGroup.alpha = 0f;

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

        public void OnPointerClick(PointerEventData eventData)
        {
            // Tell the view that this bubble was clicked. The view will only set the advance
            // flag if currentMode == Interactive.
            owner?.NotifyBubbleClicked(this);
        }

        private void ResizeToFitText()
        {
            if (_dialogue == null || rect == null)
                return;

            _dialogue.enableWordWrapping = true;
            _dialogue.enableAutoSizing = false;
            _dialogue.overflowMode = TextOverflowModes.Overflow;

            RectTransform textRect = _dialogue.rectTransform;

            // Pass 1: measure text unconstrained to find natural width
            _dialogue.ForceMeshUpdate();
            Vector2 fullSize = _dialogue.GetPreferredValues(_dialogue.text, Mathf.Infinity, Mathf.Infinity);

            // Clamp that width by our max bubble width
            float targetContentWidth = Mathf.Clamp(fullSize.x, minWidth - padding.x, maxWidth - padding.x);

            // Apply that width so TMP can wrap properly
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetContentWidth);
            _dialogue.ForceMeshUpdate();

            // Pass 2: measure again with wrapping now enforced
            Vector2 wrappedSize = _dialogue.GetPreferredValues(_dialogue.text, targetContentWidth, Mathf.Infinity);

            float contentWidth = wrappedSize.x;
            float contentHeight = wrappedSize.y;

            // Add padding and clamp
            float finalWidth = Mathf.Clamp(contentWidth + padding.x, minWidth, maxWidth);
            float finalHeight = Mathf.Clamp(contentHeight + padding.y, minHeight, maxHeight);

            // Apply to bubble and background
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);

            if (background != null)
            {
                var bgRect = background.rectTransform;
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
            }

            // Apply final text box size
            float innerW = Mathf.Max(8f, finalWidth - padding.x);
            float innerH = Mathf.Max(8f, finalHeight - padding.y);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerW);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, innerH);
            textRect.anchoredPosition = Vector2.zero;

            _dialogue.ForceMeshUpdate();
        }

         public void SetOwnerView(ThoughtBubbleView newOwner)
        {
            owner = newOwner;
        }
    }
}
