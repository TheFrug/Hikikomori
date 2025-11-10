#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Yarn.Unity;

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

        [Header("Timing & Motion")]
        [SerializeField] private float defaultLifetime = 3f;
        [SerializeField] private float riseDistance = 80f;
        [SerializeField] private float fadeEdgeTime = 0.35f;
        [SerializeField] private float stackingSpacing = 20f;
        [SerializeField] private float spawnBaseY = 0f;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 6;
        [SerializeField] private int maxPoolSize = 40;
        [SerializeField] private int maxSimultaneous = 12;

        private readonly Queue<GameObject> pool = new();
        private readonly List<GameObject> activeBubbles = new();

        [SerializeField] private OptionItem optionButtonPrefab;
        [SerializeField] private RectTransform optionContainer;
        private List<OptionItem> activeOptions = new();
        private YarnTaskCompletionSource<DialogueOption?> optionSelectionSource;

        [SerializeField] private Thought debugAutomaticText;
        [SerializeField] private Thought debugInteractiveText;
        
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
                Debug.LogError("ThoughtBubbleView requires prefab and container.");
                enabled = false;
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
                PoolReturn(CreateBubbleInstance());
        }

        private void Update()
        {
            DebugThoughtBubble();
        }

        #region Pooling
        private GameObject CreateBubbleInstance()
        {
            var go = Instantiate(thoughtBubblePrefab, bubbleContainer);
            go.SetActive(false);
            if (!go.TryGetComponent(out CanvasGroup _))
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
                RecycleImmediate(oldest);
                return oldest;
            }

            return CreateBubbleInstance();
        }

        private void PoolReturn(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(bubbleContainer, false);
            if (pool.Count < maxPoolSize)
                pool.Enqueue(go);
            else
                Destroy(go);
        }

        private void RecycleImmediate(GameObject instance)
        {
            activeBubbles.Remove(instance);

            var rt = instance.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            var cg = instance.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            PoolReturn(instance);
        }

        public void RecycleBubble(GameObject instance) => RecycleImmediate(instance);
        #endregion

        #region Public API
        public void SetMode(ThoughtMode mode) => currentMode = mode;
        public ThoughtMode GetMode() => currentMode;

        public void SpawnThought(string speakerKey, string text, float? lifetime = null, float? rise = null)
        {
            if (!isActiveAndEnabled) return;

            if (activeBubbles.Count >= maxSimultaneous)
                RecycleImmediate(activeBubbles[0]);

            var instance = PoolGet();
            ConfigureAndStart(instance, speakerKey, text, lifetime ?? defaultLifetime, rise ?? riseDistance);
        }

        public void SpawnThought(Thought thought)
        {
            if (thought == null) return;
            SpawnThought(
                thought.speakerKey,
                thought.previewText,
                thought.lifetime,
                thought.riseDistance
            );
        }

        [YarnCommand("SpawnThought")]
        public static void YarnSpawnThought(string speakerKey, string text, float? lifetime = null, float? rise = null)
            => Instance?.SpawnThought(speakerKey, text, lifetime, rise);
        #endregion

        #region Configure & Animate
        private void ConfigureAndStart(GameObject instance, string speakerKey, string text, float lifetime, float rise)
        {
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
            float startX = UnityEngine.Random.Range(-12f, 12f);
            float startY = spawnBaseY + activeBubbles.Count * stackingSpacing;
            rt.anchoredPosition = new Vector2(startX, startY);

            var bubble = instance.GetComponent<ThoughtBubble>();
            if (bubble != null)
            {
                bubble.InitializeFloating(
                    text, bubbleColor, font, name,
                    lifetime, rise, fadeEdgeTime, this
                );
                activeBubbles.Add(instance);
            }
        }
        #endregion

        #region Yarn Overrides
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            string speakerKey = line.CharacterName ?? string.Empty;
            string text = line.TextWithoutCharacterName.Text;

            SpawnThought(speakerKey, text);

            if (currentMode == ThoughtMode.Automatic)
            {
                float timer = 0f;
                while (timer < defaultLifetime && !token.IsNextLineRequested)
                {
                    timer += Time.deltaTime;
                    await YarnTask.Yield();
                }
                return;
            }
            else
            {
                while (!token.IsNextLineRequested)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                        return;
                    await YarnTask.Yield();
                }
            }
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
        {
            if (currentMode == ThoughtMode.Automatic)
                return null;

            foreach (var item in activeOptions) Destroy(item.gameObject);
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
                foreach (var item in activeOptions) Destroy(item.gameObject);
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

        private void DebugThoughtBubble()
        {
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                currentMode = ThoughtMode.Automatic;
                SpawnThought(debugAutomaticText);
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                currentMode = ThoughtMode.Interactive;
                SpawnThought(debugInteractiveText);
            }
        }
    }
}
