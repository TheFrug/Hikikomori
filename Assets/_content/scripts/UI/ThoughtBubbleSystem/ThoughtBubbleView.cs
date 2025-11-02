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

        // Two distinct Yarn dialogue modes
        public enum ThoughtMode
        {
            Automatic,   // timed fades, <<wait>> pacing
            Interactive  // player input and options
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
        [SerializeField] private int maxSimultaneous = 12;
        private readonly List<GameObject> activeBubbles = new();

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

            for (int i = 0; i < initialPoolSize; i++)
            {
                var go = CreateBubbleInstance();
                PoolReturn(go);
            }
        }

        #region Mode Handling
        public void SetMode(ThoughtMode mode)
        {
            currentMode = mode;
            Debug.Log($"[ThoughtBubbleView] Mode set to {mode}");
        }

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

        private void StopAllCoroutinesOnInstance(GameObject instance)
        {
            // placeholder for later per-instance coroutine control
        }

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

        #region Public API + Yarn Command
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
                StopCoroutine(AnimateAndRecycle(oldest));
                RecycleImmediate(oldest);
            }

            var instance = PoolGet();
            ConfigureAndStart(instance, speakerKey, text, lifetime ?? defaultLifetime, rise ?? riseDistance);
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
                float y = spawnBaseY - (activeBubbles.Count * (rt.rect.height + stackingSpacing));
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }

            var bubble = instance.GetComponent<ThoughtBubble>();
            if (bubble != null)
            {
                bubble.Initialize(text, bubbleColor, font, name, lifetime, rise, fadeEdgeTime, this);
            }
            else
            {
                StartCoroutine(AnimateAndRecycle(instance, lifetime, rise));
            }

            activeBubbles.Add(instance);
        }

        private IEnumerator AnimateAndRecycle(GameObject instance, float lifetime = -1f, float rise = -1f)
        {
            if (instance == null) yield break;

            var cg = instance.GetComponent<CanvasGroup>();
            var rt = instance.GetComponent<RectTransform>();

            if (lifetime <= 0) lifetime = defaultLifetime;
            if (rise <= 0) rise = riseDistance;

            if (cg != null) cg.alpha = 0f;
            float elapsed = 0f;
            float total = lifetime;
            float half = fadeEdgeTime;

            Vector2 startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
            Vector2 endPos = startPos + Vector2.up * rise;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / total);

                if (rt != null)
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

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

            RecycleImmediate(instance);
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
                    if (Input.GetKeyDown(KeyCode.T))
                        SpawnThought("Goblin", "This is a test thought line!");
                    if (Input.GetKeyDown(KeyCode.Y))
                        SpawnThought("Lady", "This is a test thought line!", 5, 700);

                    // --- Debug Yarn triggers ---
                    if (Input.GetKeyDown(KeyCode.Alpha7))
                    {
                        var runner = FindObjectOfType<DialogueRunner>();
                        if (runner != null)
                        {
                            Debug.Log("[Debug] Running Yarn script: GoblinThought (Automatic)");
                            SetMode(ThoughtMode.Automatic);

                            var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
                            if (!presenters.Contains(this))
                            {
                                presenters.Add(this);
                                runner.DialoguePresenters = presenters;
                            }

                            runner.StartDialogue("GoblinThought");
                        }
                        else Debug.LogWarning("No DialogueRunner found in scene!");
                    }

                    if (Input.GetKeyDown(KeyCode.Alpha8))
                    {
                        var runner = FindObjectOfType<DialogueRunner>();
                        if (runner != null)
                        {
                            Debug.Log("[Debug] Running Yarn script: InnerDialogue_Truth (Interactive)");
                            SetMode(ThoughtMode.Interactive);

                            var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
                            if (!presenters.Contains(this))
                            {
                                presenters.Add(this);
                                runner.DialoguePresenters = presenters;
                            }

                            runner.StartDialogue("InnerDialogue_Truth");
                        }
                        else Debug.LogWarning("No DialogueRunner found in scene!");
                    }
                }
        #endif

        #region Yarn presenter overrides
        public override YarnTask RunLineAsync(LocalizedLine localizedLine, LineCancellationToken token)
        {
            string speakerKey = localizedLine.CharacterName ?? string.Empty;
            string text = localizedLine.TextWithoutCharacterName.Text;

            SpawnThought(speakerKey, text);

            if (currentMode == ThoughtMode.Automatic)
                return YarnTask.CompletedTask;
            else
                return WaitForPlayerContinue(token);
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

            Debug.Log("[ThoughtBubbleView] Displaying options (interactive mode)");
            return YarnTask.FromResult<DialogueOption?>(options.Length > 0 ? options[0] : null);
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
