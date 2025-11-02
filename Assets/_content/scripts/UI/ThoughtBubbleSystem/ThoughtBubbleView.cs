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
    [HelpURL("internal://project-hiki/ThoughtBubbleView")]
    public class ThoughtBubbleView : DialoguePresenterBase
    {
        public static ThoughtBubbleView Instance { get; private set; } = null!;

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

        /// <summary>
        /// Public wrapper so ThoughtBubble instances can return themselves safely.
        /// </summary>
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

        #region Public API
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

            // assign vertical stack offset
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                float y = spawnBaseY - (activeBubbles.Count * (rt.rect.height + stackingSpacing));
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
            }

            // initialize its self-managed ThoughtBubble animation
            var bubble = instance.GetComponent<ThoughtBubble>();
            if (bubble != null)
            {
                bubble.Initialize(text, bubbleColor, font, name, lifetime, rise, fadeEdgeTime, this);
            }
            else
            {
                // fallback to local coroutine if prefab has no ThoughtBubble script
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
            //StartCoroutine(RelayoutActiveBubbles());
        }

        private IEnumerator RelayoutActiveBubbles()
        {
            float animDur = 0.18f;
            float elapsed = 0f;
            var starts = new List<Vector2>();
            var targets = new List<Vector2>();

            foreach (var b in activeBubbles)
            {
                var rt = b.GetComponent<RectTransform>();
                if (rt != null)
                {
                    starts.Add(rt.anchoredPosition);
                    float y = -(activeBubbles.IndexOf(b) * (rt.rect.height + stackingSpacing));
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
                        rt.anchoredPosition = Vector2.Lerp(starts[i], targets[i], t);
                }
                yield return null;
            }

            for (int i = 0; i < activeBubbles.Count; i++)
            {
                var rt = activeBubbles[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    float y = -(i * (rt.rect.height + stackingSpacing));
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
                }
            }
        }
        #endregion

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
                SpawnThought("Goblin", "This is a test thought line!");
            if (Input.GetKeyDown(KeyCode.Y))
                SpawnThought("Lady", "This is a test thought line!", 5, 700);
        }
#endif

        #region Yarn presenter overrides
        public override YarnTask RunLineAsync(LocalizedLine localizedLine, LineCancellationToken token)
        {
            string speakerKey = localizedLine.CharacterName ?? string.Empty;
            string text = localizedLine.TextWithoutCharacterName.Text;
            SpawnThought(speakerKey, text);
            return YarnTask.CompletedTask;
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, System.Threading.CancellationToken cancellationToken)
            => YarnTask.FromResult<DialogueOption?>(null);

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            foreach (var b in new List<GameObject>(activeBubbles))
                RecycleImmediate(b);
            return YarnTask.CompletedTask;
        }
        #endregion

        #region Yarn Commands

        [YarnCommand("spawn_thought")]
        public void SpawnThoughtCommand(string speakerKey, string text)
        {
            SpawnThought(speakerKey, text);
        }
        #endregion
    }  
}
