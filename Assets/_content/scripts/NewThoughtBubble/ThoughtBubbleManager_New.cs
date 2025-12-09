using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ProjectHiki.UI;
using Yarn.Unity;
using UnityEngine.Events;
using System;
using System.Linq;

public class ThoughtBubbleManager_New : MonoBehaviour
{
    public static ThoughtBubbleManager_New Instance { get; private set; }
    public static UnityEvent BubbleFinished = new UnityEvent();

    [Header("References")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform topPoint;
    [SerializeField] private RectTransform container;

    [Header("Options")]
    // Panel (RectTransform) where option bubbles will be arranged (near bottom). Create and assign in inspector.
    [SerializeField] private RectTransform optionsPanel;
    [SerializeField] private float optionsSpacing = 8f;
    [SerializeField] private float optionsStartYOffset = -20f; // relative to optionsPanel pivot

    [Header("Pool Settings")]
    [SerializeField] private ThoughtBubble_New bubblePrefab;
    [SerializeField] private int poolSize = 20;

    [Header("Float Settings (Base Values)")]
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float swaySpeed = 0.5f;
    [SerializeField] private float swayAmplitude = 20f;
    [SerializeField] private float spacingBetweenBubbles = 40f;

    [Header("General")]
    [SerializeField] private int maxSimultaneous = 20;
    private bool allAtTop = false;

    [Header("Speed Multipliers")]
    public float slowSpawnDelay = 1.2f;
    public float normalSpawnDelay = 0.75f;
    public float fastSpawnDelay = 0.35f;

    private ThoughtBubble_New lastBubbleSpawned;

    public float CurrentSpawnDelay { get; private set; } = 0.75f; // default
    public float slowTravelMultiplier = 0.75f;
    public float normalTravelMultiplier = 1.0f;
    public float fastTravelMultiplier = 1.4f;

    // Interactive mode flag
    private bool isInteractiveSession = false;
    public bool IsInteractiveSession => isInteractiveSession;

    [Header("Debug")]
    [SerializeField] private ThoughtData debugThought;

    public List<ThoughtBubble_New> _pool;
    public List<ThoughtBubble_New> _active;

    private float currentTravelMultiplier = 1.0f;
    public float CurrentTravelMultiplier => currentTravelMultiplier;

    // --- OPTION STATE ---
    private bool awaitingOptionSelection = false;
    private int selectedOptionIndex = -1;
    private List<ThoughtBubble_New> currentOptionBubbles = new List<ThoughtBubble_New>();
    private DialogueOption[] lastPresentedOptions = null;

    private void Awake()
    {
        Instance = this;
        InitPool();
    }

    private void InitPool()
    {
        _pool = new List<ThoughtBubble_New>(poolSize);
        _active = new List<ThoughtBubble_New>();

        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(bubblePrefab, container, false);
            go.gameObject.SetActive(false);
            _pool.Add(go);
        }
    }

    private ThoughtBubble_New GetFromPool()
    {
        if (_pool.Count > 0)
        {
            var b = _pool[0];
            _pool.RemoveAt(0);
            _active.Add(b);
            return b;
        }

        if (_active.Count > 0)
        {
            var oldest = _active[0];
            _active.RemoveAt(0);
            oldest.ResetBubble();
            _active.Add(oldest);
            return oldest;
        }

        var inst = Instantiate(bubblePrefab, container, false);
        _active.Add(inst);
        return inst;
    }

    private void ReturnBubbleToPool(ThoughtBubble_New bubble)
    {
        bubble.ResetBubble();
        if (_active.Contains(bubble)) _active.Remove(bubble);
        if (!_pool.Contains(bubble)) _pool.Add(bubble);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha5) && debugThought != null)
            StartThought(debugThought);

        // Update regular active bubbles (skip options in floating logic)
        int indexForFloating = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            var bubble = _active[i];

            // Skip options for stacking/floating; they'll be positioned separately
            if (bubble.IsOption)
                continue;

            float target = CalculateTargetTopPosition(indexForFloating, bubble);
            UpdateBubbleMovement(bubble, target);
            ApplySway(bubble);
            indexForFloating++;
        }

        if (!allAtTop && AreAllBubblesAtTarget())
            allAtTop = true;

        // Position option bubbles inside optionsPanel if any
        if (currentOptionBubbles.Count > 0 && optionsPanel != null)
            LayoutOptions();
    }

    // -------------------------
    // HELPER METHODS
    // -------------------------

    private float CalculateTargetTopPosition(int index, ThoughtBubble_New bubble)
    {
        float top = topPoint.position.y;

        if (index >= 1)
        {
            // find previous non-option bubble in _active list by index mapping (we pass index based on non-option ordering)
            var prev = _active.Where(b => !b.IsOption).ElementAtOrDefault(index - 1);
            if (prev != null)
            {
                float prevTop = prev.RectTransform.position.y - spacingBetweenBubbles;
                top = Mathf.Min(topPoint.position.y, prevTop);
            }
        }

        if (bubble.HasSpeaker)
            top -= bubble.SpeakerHeight;

        top -= bubble.RectTransform.rect.height * 0.5f;

        return top;
    }

    private void UpdateBubbleMovement(ThoughtBubble_New bubble, float targetTop)
    {
        var pos = bubble.RectTransform.position;
        float y = pos.y + (moveSpeed * currentTravelMultiplier) * Time.deltaTime;

        if (y < targetTop)
        {
            pos.y = y;
        }
        else
        {
            pos.y = targetTop;

            if (!bubble.Done)
            {
                bubble.TopTimer += Time.deltaTime;

                // Only auto-complete bubbles when NOT in an interactive session.
                if (!isInteractiveSession && allAtTop && bubble.TopTimer >= bubble.Duration)
                    bubble.Done = true;
            }
            else
            {
                pos.y = y;

                if (!bubble.Done && pos.y >= targetTop)
                    HandleBubbleAtTop(bubble);
            }
        }

        bubble.RectTransform.position = pos;
    }

    private void HandleBubbleAtTop(ThoughtBubble_New bubble)
    {
        bubble.RectTransform.position = new Vector3(bubble.RectTransform.position.x, CalculateTargetTopPosition(_active.IndexOf(bubble), bubble), bubble.RectTransform.position.z);

        if (bubble != lastBubbleSpawned)
        {
            bubble.TopTimer = 0f;
        }
        else
        {
            bubble.TopTimer += Time.deltaTime;

            if (bubble.TopTimer >= bubble.Duration)
            {
                allAtTop = true;

                // If this is NOT an interactive session, mark all as Done so they float out.
                if (!isInteractiveSession)
                {
                    foreach (var b in _active)
                        b.Done = true;
                }
                // If interactive, do NOT auto-mark Done. Wait for Yarn/BehaviorManager to end session.
            }
        }
    }

    private void ApplySway(ThoughtBubble_New bubble)
    {
        bubble.SwayTimer += Time.deltaTime * swaySpeed;
        var pos = bubble.RectTransform.position;
        pos.x = bubble.CenterX + Mathf.Sin(bubble.SwayTimer) * swayAmplitude;
        bubble.RectTransform.position = pos;
    }

    private bool AreAllBubblesAtTarget()
    {
        // Only check non-option bubbles
        var activeNonOptions = _active.Where(b => !b.IsOption).ToList();
        for (int i = 0; i < activeNonOptions.Count; i++)
        {
            var b = activeNonOptions[i];
            float target = CalculateTargetTopPosition(i, b);
            if (b.RectTransform.position.y < target)
                return false;
        }
        return true;
    }

    private void SpawnBubbleAtStartPosition(ThoughtBubble_New bubble)
    {
        var pos = spawnPoint.position;
        float extra = bubble.HasSpeaker ? bubble.SpeakerHeight : 0f;
        pos.y -= bubble.RectTransform.rect.height + extra;

        bubble.RectTransform.position = pos;
        bubble.CenterX = pos.x;

        if (bubble.CanvasGroup != null)
            bubble.CanvasGroup.alpha = 1f;
    }

    // -------------------------
    // PUBLIC SPAWN METHODS
    // -------------------------

    public void StartThought(ThoughtData thought)
    {
        if (thought == null)
        {
            Debug.LogWarning("[ThoughtBubbleManager] Tried to spawn null Thought asset!");
            return;
        }

        // Set interactive flag for this session so presenter/manager know how to behave.
        isInteractiveSession = (thought.type == ThoughtData.ThoughtType.Interactive);

        // Reset state for a new session
        allAtTop = false;
        lastBubbleSpawned = null;

        if (!string.IsNullOrEmpty(thought.yarnNodeName))
        {
            var runner = FindObjectOfType<DialogueRunner>();
            if (runner == null)
            {
                Debug.LogWarning("[ThoughtBubbleManager] No DialogueRunner in scene!");
                return;
            }

            runner.StartDialogue(thought.yarnNodeName);
            return;
        }

        Debug.LogWarning($"[ThoughtBubbleManager] Thought '{thought.name}' has no Yarn node defined, skipping spawn.");
    }


    private void SetSpeedFromThought(ThoughtData t)
    {
        switch (t.speed)
        {
            case ThoughtData.ThoughtSpeed.Slow:
                CurrentSpawnDelay = slowSpawnDelay;
                currentTravelMultiplier = slowTravelMultiplier;
                break;
            case ThoughtData.ThoughtSpeed.Fast:
                CurrentSpawnDelay = fastSpawnDelay;
                currentTravelMultiplier = fastTravelMultiplier;
                break;
            default:
                CurrentSpawnDelay = normalSpawnDelay;
                currentTravelMultiplier = normalTravelMultiplier;
                break;
        }
    }

    public void ShowBubble(string speakerKey, string text)
    {
        ShowBubbleInternal(speakerKey, text, 3f);
    }

    private void ShowBubbleInternal(string speakerKey, string text, float lifetime)
    {
        if (_active.Count >= maxSimultaneous)
        {
            var oldest = _active[0];
            _active.RemoveAt(0);
            ReturnBubbleToPool(oldest);
        }

        var bubble = GetFromPool();
        bubble.gameObject.SetActive(true);

        Color bubbleColor = FamilyManager.Instance != null ? FamilyManager.Instance.GetBubbleColor(speakerKey) : Color.white;
        TMP_FontAsset font = FamilyManager.Instance != null ? FamilyManager.Instance.GetFontAsset(speakerKey) : null;
        Color textColor = Color.white;

        if (FamilyManager.Instance != null)
        {
            var part = FamilyManager.Instance.parts.Find(p => p.key == speakerKey);
            if (part != null)
                textColor = part.textColor;
        }

        bubble.InitializeAutomatic(
            text,
            bubbleColor,
            font,
            speakerKey,
            textColor // <-- pass textColor here
        );

        bubble.Duration = lifetime > 0f ? lifetime : 3f;

        SpawnBubbleAtStartPosition(bubble);

        lastBubbleSpawned = bubble;
    }

    // -------------------------
    // OPTIONS: Presentation & Selection
    // -------------------------

    /// <summary>
    /// Present Yarn options as clickable bubbles in the optionsPanel. This will block (set internal state) until the player selects one.
    /// </summary>
    public void PresentOptionsAsBubbles(DialogueOption[] options, Action<int> onSelected)
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("[ThoughtBubbleManager] No optionsPanel assigned. Returning first option automatically.");
            onSelected?.Invoke(0);
            return;
        }

        // Clear any existing option bubbles first
        ClearCurrentOptions();

        awaitingOptionSelection = true;
        selectedOptionIndex = -1;
        lastPresentedOptions = options;

        // Spawn bubbles for each option and place them under optionsPanel (we keep them in _active so pooling works)
        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            var b = GetFromPool();
            b.gameObject.SetActive(true);

            // Use a neutral bubble color for options (or pick something like FamilyManager.Instance.GetBubbleColor for narrator)
            Color bubbleColor = FamilyManager.Instance != null ? FamilyManager.Instance.GetBubbleColor(string.Empty) : Color.gray;
            TMP_FontAsset font = FamilyManager.Instance != null ? FamilyManager.Instance.GetFontAsset(string.Empty) : null;

            b.InitializeOption(
                opt.Line.TextWithoutCharacterName.Text
                ?? opt.Line.RawText
                ?? string.Empty,
                bubbleColor,
                font,
                i,
                OnOptionClickedInternal
            );


            // Parent to optionsPanel so it's positioned by LayoutOptions()
            b.RectTransform.SetParent(optionsPanel, false);

            currentOptionBubbles.Add(b);
        }
    }

    private void OnOptionClickedInternal(int optionIdx)
    {
        if (!awaitingOptionSelection) return;

        selectedOptionIndex = optionIdx;
        awaitingOptionSelection = false;

        // Fade out the other options immediately
        for (int i = currentOptionBubbles.Count - 1; i >= 0; i--)
        {
            var b = currentOptionBubbles[i];
            if (b == null) continue;
            if (b.IsOption && currentOptionBubbles.IndexOf(b) != optionIdx)
            {
                // simple fade and return - instant for now
                if (b.CanvasGroup != null)
                {
                    b.CanvasGroup.alpha = 0f;
                }
                ReturnBubbleToPool(b);
                currentOptionBubbles.RemoveAt(i);
            }
        }

        // Keep the selected bubble visible for a moment, then return it as well (or let Yarn handle showing subsequent lines)
        // We'll just return it immediately to the pool so dialogue continues cleanly.
        var selectedBubble = currentOptionBubbles.FirstOrDefault(b => b.IsOption && currentOptionBubbles.IndexOf(b) == optionIdx);
        if (selectedBubble != null)
        {
            // leave a tiny delay could be added; for now return immediately
            ReturnBubbleToPool(selectedBubble);
            currentOptionBubbles.Remove(selectedBubble);
        }

        // Clear option list
        lastPresentedOptions = null;
    }

    private void LayoutOptions()
    {
        // Vertical stack from top-down within optionsPanel (bottom anchored) — place them starting from optionsStartYOffset
        float y = optionsStartYOffset;
        for (int i = 0; i < currentOptionBubbles.Count; i++)
        {
            var b = currentOptionBubbles[i];
            if (b == null) continue;
            var rt = b.RectTransform;
            // local anchored position relative to optionsPanel
            // we position downward stacking (each subsequent bubble below previous)
            float halfHeight = rt.rect.height * 0.5f;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y - halfHeight);
            y -= (rt.rect.height + optionsSpacing);
        }
    }

    private void ClearCurrentOptions()
    {
        for (int i = currentOptionBubbles.Count - 1; i >= 0; i--)
        {
            var b = currentOptionBubbles[i];
            if (b != null)
                ReturnBubbleToPool(b);
        }
        currentOptionBubbles.Clear();
        awaitingOptionSelection = false;
        selectedOptionIndex = -1;
        lastPresentedOptions = null;
    }

    public void EndInteractiveSession()
    {
        isInteractiveSession = false;

        // Once interactive session ends, allow bubbles to auto-complete:
        allAtTop = false; // reset so manager will recalc and eventually set allAtTop and mark Done
        // Optionally mark lastBubbleSpawned TopTimer large to force completion:
        if (lastBubbleSpawned != null)
            lastBubbleSpawned.TopTimer = lastBubbleSpawned.Duration + 0.1f;
    }

    /// <summary>
    /// Public accessor for RunOptionsAsync to check selection state and read index.
    /// </summary>
    public bool HasOptionSelection => selectedOptionIndex >= 0;
    public int SelectedOptionIndex => selectedOptionIndex;

    // -------------------------
    // Cleanup / utility
    // -------------------------

    public void ClearAll()
    {
        allAtTop = false;
        ClearCurrentOptions();
        for (int i = _active.Count - 1; i >= 0; i--)
            ReturnBubbleToPool(_active[i]);
        _active.Clear();
    }
}
