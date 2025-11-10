#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using TMPro;
using Yarn.Unity;

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

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 6;
        [SerializeField] private int maxPoolSize = 40;
        private readonly Queue<GameObject> pool = new();
        private readonly List<GameObject> activeBubbles = new();
        [SerializeField] private int maxSimultaneous = 12;

        [Header("Interactive Options")]
        [SerializeField] private OptionItem optionButtonPrefab = null!;
        [SerializeField] private RectTransform optionContainer = null!;
        private List<OptionItem> activeOptions = new();
        private YarnTaskCompletionSource<DialogueOption?> optionSelectionSource = null!;

        [Header("Debug Thought assets (assign in inspector)")]
        [SerializeField] private Thought goblinThought = null!;
        [SerializeField] private Thought innerThought = null!;

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

        private void StopAllCoroutinesOnInstance(GameObject instance) { }

        public void RecycleBubble(GameObject instance)
        {
            if (instance == null) return;
            if (!activeBubbles.Contains(instance))
            {
                PoolReturn(instance);
                return;
            }
            RecycleImmediate(instance);
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
        public void SpawnThought(Thought thought)
        {
            if (thought == null)
            {
                Debug.LogWarning("[ThoughtBubbleView] Tried to spawn null Thought asset!");
                return;
            }

            // Set mode based on type
            SetMode(thought.type == Thought.ThoughtType.Automatic ? ThoughtMode.Automatic : ThoughtMode.Interactive);

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

            // If no Yarn node is defined, fall back to the simple preview bubble
            SpawnThought(thought.speakerKey, thought.previewText, thought.lifetime, thought.riseDistance);
        }
        #endregion

        #region Configure & animate
        private void ConfigureAndStart(GameObject instance, string speakerKey, string text, float lifetime, float rise)
        {
            instance.transform.SetParent(bubbleContainer, false);
            instance.SetActive(true);

            var fm = FamilyManager.Instance;
            Color bubbleColor = Color.white;
            TMP_FontAsset? font = null;
            string name = string.Empty;

            if (fm != null)
            {
                bubbleColor = fm.GetBubbleColor(speakerKey);
                font = fm.GetFontAsset(speakerKey);
                name = fm.GetDisplayName(speakerKey);
            }

            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Add a small randomized horizontal offset for floatiness (yOffset requested stored here as local var)
                float yOffset = UnityEngine.Random.Range(-12f, 12f);
                float y;
                if (currentMode == ThoughtMode.Interactive)
                {
                    // Stack upward from base (like a dialogue log)
                    y = spawnBaseY + (activeBubbles.Count * (rt.rect.height + stackingSpacing)) + yOffset;
                }
                else
                {
                    // Stack downward (floaty thought bubble style)
                    y = spawnBaseY - (activeBubbles.Count * (rt.rect.height + stackingSpacing)) + yOffset;
                }

                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }

            var bubble = instance.GetComponent<ThoughtBubble>();
            if (bubble != null)
            {
                if (currentMode == ThoughtMode.Interactive)
                    bubble.Initialize(text, bubbleColor, font, name, Mathf.Infinity, 0f, 0f, this);
                else
                    bubble.Initialize(text, bubbleColor, font, name, lifetime, rise, fadeEdgeTime, this);
            }

            activeBubbles.Add(instance);
        }

        private void RecycleImmediate(GameObject instance)
        {
            if (instance == null) return;
            activeBubbles.Remove(instance);

            var rt = instance.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            var cg = instance.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            PoolReturn(instance);
        }
        #endregion

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
            while (!token.IsNextLineRequested)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    return;
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
            foreach (var b in new List<GameObject>(activeBubbles))
                RecycleImmediate(b);
            return YarnTask.CompletedTask;
        }
        #endregion
    }
}
