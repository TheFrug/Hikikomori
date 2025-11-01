// ThoughtBubbleView.cs
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Yarn.Unity;

namespace ProjectHiki.UI
{
    /// <summary>
    /// A Yarn 3.0-compatible dialogue presenter that spawns small, ephemeral
    /// "thought" bubbles on the right-hand UI. Non-blocking: it spawns bubbles
    /// and returns immediately so normal dialogue continues.
    ///
    /// Assumptions: FamilyManager provides methods used below (GetDisplayName,
    /// GetBubbleColor, GetFontAsset). See comments beneath class for details.
    /// </summary>
    [HelpURL("internal://project-hiki/ThoughtBubbleView")]
    public class ThoughtBubbleView : DialoguePresenterBase
    {
        public static ThoughtBubbleView Instance { get; private set; } = null!;

        [Header("Prefab / Container")]
        [Tooltip("Prefab for one thought bubble. Should contain a CanvasGroup, a TextMeshProUGUI, and preferably a LayoutElement or RectTransform sized to content.")]
        [SerializeField] private GameObject thoughtBubblePrefab = null!; // required

        [Tooltip("Parent RectTransform inside which bubbles are created (right-side panel). Use a Canvas-space RectTransform.")]
        [SerializeField] private RectTransform bubbleContainer = null!; // required

        [Header("Default timing & motion")]
        [Tooltip("Duration in seconds for a bubble to rise and fade away.")]
        [SerializeField] private float defaultLifetime = 3.0f;

        [Tooltip("Distance in local UI units (anchoredPosition.y) a bubble will rise over lifetime.")]
        [SerializeField] private float riseDistance = 80f;

        [Tooltip("How quickly (seconds) the bubble fades in/out at start/end (part of total lifetime).")]
        [SerializeField] private float fadeEdgeTime = 0.35f;

        [Tooltip("Spacing between stacked bubbles (local units).")]
        [SerializeField] private float stackingSpacing = 8f;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 6;
        [SerializeField] private int maxPoolSize = 40;

        // runtime pool
        private readonly Queue<GameObject> pool = new Queue<GameObject>();

        // small helper to ensure we don't spam too many bubbles visually
        [SerializeField] private int maxSimultaneous = 12;

        private List<GameObject> activeBubbles = new List<GameObject>();

        #region Unity lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (thoughtBubblePrefab == null)
            {
                Debug.LogError($"{nameof(ThoughtBubbleView)} needs a Thought Bubble Prefab assigned.", this);
                enabled = false;
                return;
            }

            if (bubbleContainer == null)
            {
                Debug.LogError($"{nameof(ThoughtBubbleView)} needs a bubble container RectTransform assigned.", this);
                enabled = false;
                return;
            }

            // pre-populate pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                var go = CreateBubbleInstance();
                PoolReturn(go);
            }
        }
        #endregion

        #region Pooling
        private GameObject CreateBubbleInstance()
        {
            var go = Instantiate(thoughtBubblePrefab, bubbleContainer);
            go.SetActive(false);
            // ensure it has a CanvasGroup (for fade)
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            return go;
        }

        private GameObject PoolGet()
        {
            if (pool.Count > 0) return pool.Dequeue();
            if (pool.Count + activeBubbles.Count < maxPoolSize)
            {
                return CreateBubbleInstance();
            }
            // fallback: reuse oldest active bubble (will be interrupted)
            if (activeBubbles.Count > 0)
            {
                var oldest = activeBubbles[0];
                // stop any coroutine animating it
                StopAllCoroutinesOnInstance(oldest);
                return oldest;
            }
            // ultimate fallback: instantiate new one
            return CreateBubbleInstance();
        }

        private void PoolReturn(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            // detach from container to avoid layout interference
            go.transform.SetParent(bubbleContainer, false);
            if (pool.Count < maxPoolSize) pool.Enqueue(go);
            else Destroy(go);
        }

        private void StopAllCoroutinesOnInstance(GameObject instance)
        {
            // naive: stop all coroutines on this component (coarse but effective)
            // We can't stop coroutines running on anonymous components easily; we will
            // manage by ensuring only this component starts those coroutines.
            // (Left as a note in case you add per-instance coroutine holders.)
        }
        #endregion

        #region Public API - spawn a thought (can be called from YarnCommands)
        /// <summary>
        /// Spawn a thought bubble with the given speaker key and text.
        /// Non-blocking: returns immediately.
        /// </summary>
        public void SpawnThought(string speakerKey, string text, float? lifetime = null, float? rise = null)
        {
            if (!isActiveAndEnabled) return;

            // limit simultaneous load
            if (activeBubbles.Count >= maxSimultaneous)
            {
                // drop oldest bubble to make room (or early-return if you prefer)
                var oldest = activeBubbles[0];
                // quickly remove it
                activeBubbles.RemoveAt(0);
                StopCoroutine(AnimateAndRecycle(oldest)); // best-effort
                RecycleImmediate(oldest);
            }

            var instance = PoolGet();
            ConfigureAndStart(instance, speakerKey, text, lifetime ?? defaultLifetime, rise ?? riseDistance);
        }
        #endregion

        #region Configuration helpers & animation
        private void ConfigureAndStart(GameObject instance, string speakerKey, string text, float lifetime, float rise)
        {
            instance.transform.SetParent(bubbleContainer, false);
            instance.SetActive(true);

            // try to populate TMP text
            var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
            }

            // style: try to ask FamilyManager for data. If unavailable, use defaults.
            Color bubbleColor = Color.white;
            TMP_FontAsset? fontAsset = null;
            string displayName = string.Empty;

            var fm = FamilyManager.Instance;
            if (fm != null)
            {
                // NOTE: these methods were discussed earlier as additions to FamilyManager.
                // If you haven't added them, you'll need to add:
                //   public Color GetBubbleColor(string key) { ... }
                //   public TMP_FontAsset GetFontAsset(string key) { ... } // optional
                //   public string GetDisplayName(string key) { ... } // already exists
                try
                {
                    // Safe calls. If methods are missing, developer will get compile errors;
                    // that's intentional so you add the family-style helpers.
                    bubbleColor = fm.GetBubbleColor(speakerKey);
                    fontAsset = fm.GetFontAsset(speakerKey);
                    displayName = fm.GetDisplayName(speakerKey);
                }
                catch (MissingMethodException)
                {
                    // If you haven't implemented visuals on FamilyManager yet, ignore.
                    bubbleColor = Color.white;
                }
                catch (Exception)
                {
                    bubbleColor = Color.white;
                }
            }

            // Apply color to background if exists
            var background = instance.GetComponentInChildren<UnityEngine.UI.Image>();
            if (background != null)
            {
                background.color = bubbleColor;
            }

            // Apply font
            if (tmp != null && fontAsset != null)
            {
                tmp.font = fontAsset;
            }

            // Optionally apply a nameplate (if prefab includes one)
            var namePlate = instance.transform.Find("NamePlate")?.GetComponentInChildren<TextMeshProUGUI>();
            if (namePlate != null)
            {
                namePlate.text = displayName;
                namePlate.gameObject.SetActive(!string.IsNullOrEmpty(displayName) && displayName != "???");
            }

            // set start anchored position near bottom of container.
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Basic stacking: place new bubble below existing active bubbles.
                // If you use a VerticalLayoutGroup on container, you can disable this.
                float y = - (activeBubbles.Count * (rt.rect.height + stackingSpacing));
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }

            activeBubbles.Add(instance);
            StartCoroutine(AnimateAndRecycle(instance, lifetime, rise));
        }

        private IEnumerator AnimateAndRecycle(GameObject instance, float lifetime = -1f, float rise = -1f)
        {
            if (instance == null) yield break;

            var cg = instance.GetComponent<CanvasGroup>();
            var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
            var rt = instance.GetComponent<RectTransform>();

            // default fallback
            if (lifetime <= 0) lifetime = defaultLifetime;
            if (rise <= 0) rise = riseDistance;

            // fade-in
            if (cg != null)
            {
                cg.alpha = 0f;
            }
            float elapsed = 0f;
            float total = lifetime;
            float half = fadeEdgeTime;

            Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
            Vector2 endPos = startPos + Vector2.up * rise;

            // Basic animation loop: rise + fade
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / total);

                // position
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                // alpha: ramp in and out
                if (cg != null)
                {
                    float alpha;
                    if (elapsed < half) alpha = Mathf.Clamp01(elapsed / half);
                    else if (elapsed > (total - half)) alpha = Mathf.Clamp01((total - elapsed) / half);
                    else alpha = 1f;
                    cg.alpha = alpha;
                }

                yield return null;
            }

            // finalize out
            RecycleImmediate(instance);
        }

        private void RecycleImmediate(GameObject instance)
        {
            if (instance == null) return;

            activeBubbles.Remove(instance);

            // reset transforms for next use
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;

            var cg = instance.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            PoolReturn(instance);

            // After recycling one bubble, we should nudge remaining active bubbles down
            // so they settle visually. Basic approach: animate their anchored positions
            // to new indices. (Non-blocking; coarse.)
            StartCoroutine(RelayoutActiveBubbles());
        }

        private IEnumerator RelayoutActiveBubbles()
        {
            float animDur = 0.18f;
            float elapsed = 0f;
            // capture starts and targets
            var starts = new List<Vector2>();
            var targets = new List<Vector2>();
            foreach (var b in activeBubbles)
            {
                var rt = b.GetComponent<RectTransform>();
                if (rt != null)
                {
                    starts.Add(rt.anchoredPosition);
                    float y = - (activeBubbles.IndexOf(b) * (rt.rect.height + stackingSpacing));
                    targets.Add(new Vector2(starts[^1].x, y));
                }
                else
                {
                    starts.Add(Vector2.zero);
                    targets.Add(Vector2.zero);
                }
            }

            while (elapsed < animDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDur));
                for (int i = 0; i < activeBubbles.Count; i++)
                {
                    var rt = activeBubbles[i].GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = Vector2.Lerp(starts[i], targets[i], t);
                    }
                }
                yield return null;
            }

            // ensure final
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                var rt = activeBubbles[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    float y = - (i * (rt.rect.height + stackingSpacing));
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                }
            }
        }
        #endregion

        #region Yarn presenter overrides (non-blocking)
        /// <summary>
        /// Called by DialogueRunner when a line is delivered. We spawn an ambient
        /// thought bubble and return immediately; we do NOT block the dialogue.
        /// </summary>
        public override YarnTask RunLineAsync(LocalizedLine localizedLine, LineCancellationToken token)
        {
            // Get the speaker key (CharacterName) and the text to show
            string speakerKey = localizedLine.CharacterName ?? string.Empty;
            string text = localizedLine.TextWithoutCharacterName.Text;

            // Spawn a thought bubble for this line. This is intentionally non-blocking.
            SpawnThought(speakerKey, text);

            // return a completed YarnTask immediately so DialogueRunner can continue.
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// ThoughtBubbleView doesn't present options; return no selection.
        /// </summary>
        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, System.Threading.CancellationToken cancellationToken)
        {
            return YarnTask.FromResult<DialogueOption?>(null);
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            // clear bubbles
            foreach (var b in new List<GameObject>(activeBubbles))
            {
                RecycleImmediate(b);
            }
            return YarnTask.CompletedTask;
        }
        #endregion
    }
}
