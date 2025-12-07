using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using TMPro;
using Yarn.Unity;
using UnityEngine.UI;

using static System.ValueType;

namespace ProjectHiki.UI
{
    [HelpURL("internal://project-hiki/ThoughtBubbleView")]
    public class ThoughtBubbleView : DialoguePresenterBase
    {
        public static ThoughtBubbleView Instance { get; private set; } = null!;

        public enum ThoughtMode
        {
            Automatic,
            Interactive
        }

        [Header("Mode Control")]
        [SerializeField] private ThoughtMode currentMode = ThoughtMode.Automatic;

        [Header("Prefab / Container")]
        [SerializeField] private GameObject thoughtBubblePrefab = null!;
        [SerializeField] private RectTransform bubbleContainer = null!;

        [Header("Default timing & motion")]
        [SerializeField] private float defaultLifetime = 3.0f;
        [SerializeField] private float riseDistance = 80f;
        [SerializeField] private float fadeEdgeTime = 0.35f;
        [SerializeField] private float stackingSpacing = 20f;
        [SerializeField] private float spawnBaseY = -300f;
        [SerializeField] private float interactiveSpawnBaseY = -120f;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 6;
        [SerializeField] private int maxPoolSize = 40;
        private readonly Queue<GameObject> pool = new();
        private readonly List<GameObject> activeBubbles = new();
        [SerializeField] private int maxSimultaneous = 12;

        [Header("Interactive Layout (chat)")]
        [SerializeField] private ScrollRect bubbleScrollRect = null!;
        [SerializeField] private RectTransform bubbleContent = null!;
        [SerializeField] private RectTransform layoutContent = null!;


        [Header("Interactive Options")]
        [SerializeField] private OptionItem optionButtonPrefab = null!;
        [SerializeField] private RectTransform optionContainer = null!;
        private List<OptionItem> activeOptions = new();
        private YarnTaskCompletionSource<DialogueOption?> optionSelectionSource = null!;
        private bool interactiveAdvanceRequested = false;

        [Header("Debug Thought assets (assign in inspector)")]
        [SerializeField] private ThoughtData goblinThought = null!;
        [SerializeField] private ThoughtData innerThought = null!;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (thoughtBubblePrefab == null || bubbleContainer == null)
            {
                Debug.LogError($"{nameof(ThoughtBubbleView)} requires prefab and container assigned.", this);
                enabled = false;
                return;
            }

            if (bubbleScrollRect == null || bubbleContent == null)
            {
                Debug.LogWarning($"{nameof(ThoughtBubbleView)}: bubbleScrollRect or bubbleContent not assigned. Interactive mode layout will not scroll.", this);
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                var go = CreateBubbleInstance();
                PoolReturn(go);
            }
        }

        #region Mode Handling
        public void SetMode(ThoughtMode mode) => currentMode = mode;
        public ThoughtMode GetMode() => currentMode;
        #endregion

        #region Pooling
        private GameObject CreateBubbleInstance()
        {
            var go = Instantiate(thoughtBubblePrefab, bubbleContainer);
            go.SetActive(false);
            if (go.GetComponent<CanvasGroup>() == null)
                go.AddComponent<CanvasGroup>();
            return go;
        }

        private GameObject PoolGet()
        {
            if (pool.Count > 0) return pool.Dequeue();
            if (pool.Count + activeBubbles.Count < maxPoolSize)
                return CreateBubbleInstance();

            if (activeBubbles.Count > 0)
            {
                var oldest = activeBubbles[0];
                StopAllCoroutinesOnInstance(oldest);
                return oldest;
            }

            return CreateBubbleInstance();
        }

        private void PoolReturn(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            go.transform.SetParent(bubbleContainer, false);
            if (pool.Count < maxPoolSize)
                pool.Enqueue(go);
            else
                Destroy(go);
        }

        private void StopAllCoroutinesOnInstance(GameObject thoughtBubble) { }

        public void RecycleBubble(GameObject thoughtBubble)
        {
            if (thoughtBubble == null) return;
            if (!activeBubbles.Contains(thoughtBubble))
            {
                PoolReturn(thoughtBubble);
                return;
            }
            RecycleImmediate(thoughtBubble);
        }
        #endregion

        #region SpawnThought (string-based)
        [YarnCommand("SpawnThought")]
        public static void YarnSpawnThought(string speakerKey, string text, float? lifetime = null, float? rise = null)
        {
            if (Instance != null)
                Instance.SpawnThought(speakerKey, text, lifetime, rise);
            else
                Debug.LogWarning("[ThoughtBubbleView] No instance available for SpawnThought!");
        }

        public void SpawnThought(string speakerKey, string text, float? lifetime = null, float? rise = null)
        {
            if (!isActiveAndEnabled) return;

            if (activeBubbles.Count >= maxSimultaneous)
            {
                var oldest = activeBubbles[0];
                activeBubbles.RemoveAt(0);
                RecycleImmediate(oldest);
            }

            var instance = PoolGet();
            ConfigureAndStart(instance, speakerKey, text, lifetime ?? defaultLifetime, rise ?? riseDistance);
        }
        #endregion

        #region SpawnThought (ScriptableObject-based)
        // Uses only yarnNodeName from the Thought asset. DOES NOT touch YarnProject.
        public void SpawnThought(ThoughtData thought)
        {
            if (thought == null)
            {
                Debug.LogWarning("[ThoughtBubbleView] Tried to spawn null Thought asset!");
                return;
            }

            // Set mode based on type
            SetMode(thought.type == ThoughtData.ThoughtType.Automatic ? ThoughtMode.Automatic : ThoughtMode.Interactive);

            // If a Yarn node is defined, always run it — regardless of type
            if (!string.IsNullOrEmpty(thought.yarnNodeName))
            {
                var runner = FindObjectOfType<DialogueRunner>();
                if (runner == null)
                {
                    Debug.LogWarning("[ThoughtBubbleView] No DialogueRunner in scene!");
                    return;
                }

                // Ensure this presenter is added to the runner's presenters
                var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
                if (!presenters.Contains(this))
                {
                    presenters.Add(this);
                    runner.DialoguePresenters = presenters;
                }

                // Start the Yarn node
                runner.StartDialogue(thought.yarnNodeName);
                return; // Important: stop here so we don't also spawn a static bubble
            }
        }
        #endregion

        #region Configure & animate (delegated)
        private void ConfigureAndStart(GameObject thoughtBubble, string speakerKey, string text, float lifetime, float rise)
        {
            // Base setup
            thoughtBubble.SetActive(true);

            // Retrieve display info from FamilyManager
            var familyManager = FamilyManager.Instance;
            Color bubbleColor = Color.white;
            TMP_FontAsset? font = null;
            string name = string.Empty;

            if (familyManager != null)
            {
                bubbleColor = familyManager.GetBubbleColor(speakerKey);
                font = familyManager.GetFontAsset(speakerKey);
                name = familyManager.GetDisplayName(speakerKey);
            }

            // --- Step 2 integration: proper parenting + layout awareness ---
            var rt = thoughtBubble.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (currentMode == ThoughtMode.Interactive && bubbleContent != null)
                {
                    // Parent to layout-driven content object
                    rt.SetParent(bubbleContent, false);

                    // Clear any manual positioning; VerticalLayoutGroup will handle spacing
                    rt.anchoredPosition = Vector2.zero;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                else
                {
                    // Automatic or Interactive (non-layout) bubbles
                    float xOffset = UnityEngine.Random.Range(-12f, 12f);
                    float baseY = currentMode == ThoughtMode.Interactive ? interactiveSpawnBaseY : spawnBaseY;

                    rt.SetParent(bubbleContainer, false);
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + xOffset, baseY);
                }
            }

            // --- Initialize bubble depending on mode ---
            var bubble = thoughtBubble.GetComponent<ThoughtBubble>();
            if (bubble != null)
            {
                if (currentMode == ThoughtMode.Interactive)
                {
                    // Layout-based static bubble
                    bubble.InitializeInteractive(text, bubbleColor, font, name, this);
                    bubble.SetOwnerView(this);
                }
                else
                {
                    var previous = activeBubbles.Count > 0 ? activeBubbles[activeBubbles.Count - 1] : null;
                    var previousThought =  previous != null ? previous.GetComponent<ThoughtBubble>() : null;

                    // Floating automatic bubble
                    
                    bubble.Initialize(text, bubbleColor, font, name, lifetime, rise, fadeEdgeTime, this, previousThought);

                    bubble.SetCeiling(bubbleContainer.position.y + bubbleContainer.rect.height * 0.5f - (rt != null ? rt.rect.height * 0.5f : 0f));

                    //float ceilingY = ComputeCeilingForNewBubble(bubble);
                    //bubble.SetCeiling(ceilingY);
                }
            }

            // --- Bookkeeping ---
            activeBubbles.Add(thoughtBubble);

            // --- Layout rebuild and scroll management ---
            if (currentMode == ThoughtMode.Interactive && bubbleScrollRect != null && bubbleContent != null)
            {
                // Force Unity to recalc the layout sizes and push new content
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleContent);
                Canvas.ForceUpdateCanvases();

                // Now ensure scroll is pinned to bottom (0 = bottom)
                bubbleScrollRect.verticalNormalizedPosition = 0f;

                // Unity often needs one frame delay to stabilize scroll after rebuild
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }
        #endregion

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null; // Wait for next layout pass
            Canvas.ForceUpdateCanvases();

            if (bubbleScrollRect != null)
                bubbleScrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// Compute the ceiling Y (in bubbleContainer local coordinates) that the newly spawned bubble
        /// should stop at. This uses the top of the container as the initial ceiling, and then
        /// pushes it lower for each existing active bubble so they stack top-to-bottom with spacing.
        /// </summary>
        private float ComputeCeilingForNewBubble(ThoughtBubble newcomer)
        {
            // Top edge of the container (anchoredPosition y=0 is center)
            float containerHalfHeight = bubbleContainer.rect.height * 0.5f;
            float ceilingY = containerHalfHeight;

            // We will inspect existing active bubbles and move the ceiling downward to sit below their bottoms.
            // Sort active bubbles by anchored y descending (topmost first) so stacking is predictable.
            var ordered = activeBubbles
                .Select(go => go.GetComponent<ThoughtBubble>())
                .Where(b => b != null && b.gameObject.activeInHierarchy)
                .OrderByDescending(b => b.GetAnchoredY())
                .ToList();

            foreach (var b in ordered)
            {
                // get bottom edge of that bubble
                float otherBottom = b.GetBottomEdgeY();
                // place ceiling so that newcomer's top sits below that bottom by stackingSpacing
                float candidateCeiling = otherBottom - stackingSpacing;
                // choose the lower ceiling (so we keep pushing down)
                if (candidateCeiling < ceilingY)
                    ceilingY = candidateCeiling;
            }

            // If the newcomer has a height greater than available space we allow ceiling to be well below center.
            // Return the final computed ceiling.
            return ceilingY;
        }

        private void RecycleImmediate(GameObject thoughtBubble)
        {
            if (thoughtBubble == null) return;
            activeBubbles.Remove(thoughtBubble);

            var rt = thoughtBubble.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            var cg = thoughtBubble.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            PoolReturn(thoughtBubble);
        }

        #if UNITY_EDITOR
        private void Update()
        {
            // Restore your debug keys using the Thought assets you assign in inspector.
            if (Input.GetKeyDown(KeyCode.T))
                SpawnThought("Goblin", "This is a test thought line!");
            if (Input.GetKeyDown(KeyCode.Y))
                SpawnThought("Lady", "This is a test thought line!", 5, 700);

            // --- Debug Yarn triggers using Thought assets ---
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                var runner = FindObjectOfType<DialogueRunner>();
                if (runner != null)
                {
                    Debug.Log("[Debug] Running Goblin Thought (via Thought asset)");
                    SetMode(ThoughtMode.Automatic);

                    var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
                    if (!presenters.Contains(this))
                    {
                        presenters.Add(this);
                        runner.DialoguePresenters = presenters;
                    }

                    if (goblinThought != null)
                        SpawnThought(goblinThought);
                    else
                        Debug.LogWarning("[ThoughtBubbleView] goblinThought asset not assigned in inspector.");
                }
                else Debug.LogWarning("No DialogueRunner found in scene!");
            }

            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                var runner = FindObjectOfType<DialogueRunner>();
                if (runner != null)
                {
                    Debug.Log("[Debug] Running Inner Thought (Interactive) via Thought asset");
                    SetMode(ThoughtMode.Interactive);

                    var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
                    if (!presenters.Contains(this))
                    {
                        presenters.Add(this);
                        runner.DialoguePresenters = presenters;
                    }

                    if (innerThought != null)
                        SpawnThought(innerThought);
                    else
                        Debug.LogWarning("[ThoughtBubbleView] innerThought asset not assigned in inspector.");
                }
                else Debug.LogWarning("No DialogueRunner found in scene!");
            }
        }
        #endif

        // Put this somewhere in ThoughtBubbleView (public or internal as you like)
        public void NotifyBubbleClicked(ThoughtBubble bubble)
        {
            // Only honor bubble clicks while in Interactive mode
            if (currentMode == ThoughtMode.Interactive)
            {
                interactiveAdvanceRequested = true;
            }
        }

        #region Yarn presenter overrides
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            string speakerKey = line.CharacterName ?? string.Empty;
            string text = line.TextWithoutCharacterName.Text;
            SpawnThought(speakerKey, text);

            if (currentMode == ThoughtMode.Automatic)
            {
                float duration = defaultLifetime;
                float timer = 0f;
                while (timer < duration && !token.IsNextLineRequested)
                {
                    timer += Time.deltaTime;
                    await YarnTask.Yield();
                }
            }
            else
            {
                await WaitForPlayerContinue(token);
            }
        }

        private async YarnTask WaitForPlayerContinue(LineCancellationToken token)
        {
            // Only allow advance via SPACE (keyboard) or via clicking the bubble (bubble click
            // sets interactiveAdvanceRequested), NOT via general mouse clicks.
            while (!token.IsNextLineRequested)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // consume the space press as an explicit "advance"
                    return;
                }

                if (interactiveAdvanceRequested)
                {
                    // was set by a bubble click
                    interactiveAdvanceRequested = false;
                    return;
                }

                await YarnTask.Yield();
            }
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
        {
            if (currentMode == ThoughtMode.Automatic)
                return YarnTask.FromResult<DialogueOption?>(null);

            return RunInteractiveOptions(options, cancellationToken);
        }

        private async YarnTask<DialogueOption?> RunInteractiveOptions(DialogueOption[] options, CancellationToken cancellationToken)
        {
            foreach (var item in activeOptions)
                Destroy(item.gameObject);
            activeOptions.Clear();

            optionSelectionSource = new YarnTaskCompletionSource<DialogueOption?>();

            foreach (var opt in options)
            {
                var item = Instantiate(optionButtonPrefab, optionContainer);
                item.Option = opt;
                item.OnOptionSelected = optionSelectionSource;
                item.completionToken = cancellationToken;
                item.gameObject.SetActive(true);
                activeOptions.Add(item);
            }

            using (cancellationToken.Register(() => optionSelectionSource.TrySetResult(null)))
            {
                var result = await optionSelectionSource.Task;
                foreach (var item in activeOptions)
                    Destroy(item.gameObject);
                activeOptions.Clear();
                return result;
            }
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;
        public override YarnTask OnDialogueCompleteAsync()
        {
            // Don’t force immediate recycle — let bubbles end on their own
            return YarnTask.CompletedTask;
        }
        #endregion
    }
}
