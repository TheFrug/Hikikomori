using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ProjectHiki.UI;
using Yarn.Unity;
using UnityEngine.Events;

public class ThoughtBubbleManager_New : MonoBehaviour
{
    public static ThoughtBubbleManager_New Instance { get; private set; }
    public static UnityEvent BubbleFinished = new UnityEvent();

    [Header("References")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform topPoint;
    [SerializeField] private RectTransform container;

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

    [Header("Debug")]
    [SerializeField] private ThoughtData debugThought;

    public List<ThoughtBubble_New> _pool;
    public List<ThoughtBubble_New> _active;

    private float currentTravelMultiplier = 1.0f;
    public float CurrentTravelMultiplier => currentTravelMultiplier;

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

        for (int i = 0; i < _active.Count; i++)
        {
            var bubble = _active[i];
            float target = CalculateTargetTopPosition(i, bubble);
            UpdateBubbleMovement(bubble, target);
            ApplySway(bubble);
        }

        if (!allAtTop && AreAllBubblesAtTarget())
            allAtTop = true;
    }

    // -------------------------
    // HELPER METHODS
    // -------------------------

    private float CalculateTargetTopPosition(int index, ThoughtBubble_New bubble)
    {
        float top = topPoint.position.y;

        if (index >= 1)
        {
            var prev = _active[index - 1];
            float prevTop = prev.RectTransform.position.y - spacingBetweenBubbles;
            top = Mathf.Min(topPoint.position.y, prevTop);
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

                if (allAtTop && bubble.TopTimer >= bubble.Duration)
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
                foreach (var b in _active)
                    b.Done = true;
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
        for (int i = 0; i < _active.Count; i++)
        {
            var b = _active[i];
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
        ClearAll();
        if (thought == null)
        {
            Debug.LogWarning("[ThoughtBubbleManager] Tried to spawn null Thought asset!");
            return;
        }

        SetSpeedFromThought(thought);

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

    public void ClearAll()
    {
        allAtTop = false;
        for (int i = _active.Count - 1; i >= 0; i--)
            ReturnBubbleToPool(_active[i]);
        _active.Clear();
    }
}
