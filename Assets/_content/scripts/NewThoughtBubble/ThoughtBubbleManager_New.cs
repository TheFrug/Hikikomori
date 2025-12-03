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
    [SerializeField] private Thought debugThought;

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
        {
            ShowBubble(debugThought);
        }

        // MOVE ACTIVE BUBBLES
        for (int i = 0; i < _active.Count; i++)
        {
            var bubble = _active[i];

            float top = topPoint.position.y;

            if (i >= 1)
            {
                var prev = _active[i - 1];
                float prevTop = prev.RectTransform.position.y - spacingBetweenBubbles;
                top = Mathf.Min(topPoint.position.y, prevTop);
            }

            if (bubble.HasSpeaker)
            {
                top -= bubble.SpeakerHeight;
            }

            top -= bubble.RectTransform.rect.height * 0.5f;

            var pos = bubble.RectTransform.position;

            // apply speed multiplier
            float y = pos.y + (moveSpeed * currentTravelMultiplier) * Time.deltaTime;

            if (y < top)
            {
                pos.y = y;
            }
            else
            {
                // Bubble *has reached top* this frame
                pos.y = top;

                if (!bubble.Done)
                {
                    bubble.TopTimer += Time.deltaTime;

                    if (allAtTop && bubble.TopTimer >= bubble.Duration)
                        bubble.Done = true;
                }
                else
                {
                    // DONE bubbles keep moving past top
                    pos.y = y;
                    // If bubble reached its stacking position, lock it there
                    if (!bubble.Done && pos.y >= top)
                    {

                        // snap to the top slot
                        pos.y = top;

                        // If this is NOT the last bubble, no timer is used
                        if (bubble != lastBubbleSpawned)
                        {
                            bubble.TopTimer = 0f;       // ensure it doesn't accumulate
                                                        // Only becomes done when final bubble triggers the synchronized release
                        }
                        else
                        {
                            // This IS the final bubble
                            bubble.TopTimer += Time.deltaTime;

                            // When the last bubble finishes its duration:
                            if (bubble.TopTimer >= bubble.Duration)
                            {
                                allAtTop = true;

                                // Mark all active bubbles as done simultaneously
                                foreach (var b in _active)
                                {
                                    b.Done = true;
                                }
                            }
                        }
                    }
                }
            }


            bubble.SwayTimer += Time.deltaTime * swaySpeed;
            pos.x = bubble.CenterX + Mathf.Sin(bubble.SwayTimer) * swayAmplitude;

            bubble.RectTransform.position = pos;
        }

        // ------------------------------------------------------
        // STEP 2 — Detect when every bubble has reached its top
        // ------------------------------------------------------
        if (!allAtTop)
        {
            bool everyBubbleAtTop = true;

            for (int i = 0; i < _active.Count; i++)
            {
                var b = _active[i];

                float targetTop = topPoint.position.y;

                if (i >= 1)
                {
                    var prev = _active[i - 1];
                    float prevTop = prev.RectTransform.position.y - spacingBetweenBubbles;
                    targetTop = Mathf.Min(topPoint.position.y, prevTop);
                }

                if (b.HasSpeaker)
                    targetTop -= b.SpeakerHeight;

                targetTop -= b.RectTransform.rect.height * 0.5f;

                if (b.RectTransform.position.y < targetTop)
                {
                    everyBubbleAtTop = false;
                    break;
                }
            }

            if (everyBubbleAtTop)
                allAtTop = true;
        }
    }

    // -------------------------------------------
    // PUBLIC ENTRY (Thought asset)
    // -------------------------------------------
    public void ShowBubble(Thought thought)
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

    private void SetSpeedFromThought(Thought t)
    {
        switch (t.speed)
        {
            case Thought.ThoughtSpeed.Slow:
                CurrentSpawnDelay = slowSpawnDelay;
                currentTravelMultiplier = slowTravelMultiplier;
                break;

            case Thought.ThoughtSpeed.Fast:
                CurrentSpawnDelay = fastSpawnDelay;
                currentTravelMultiplier = fastTravelMultiplier;
                break;

            default:
                CurrentSpawnDelay = normalSpawnDelay;
                currentTravelMultiplier = normalTravelMultiplier;
                break;
        }
        //nextSpawnAllowedTime = Time.time + CurrentSpawnDelay;
    }

    // -------------------------------------------
    // PUBLIC ENTRY (string-based)
    // -------------------------------------------
    public void ShowBubble(string speakerKey, string text)
    {
        ShowBubbleInternal(speakerKey, text, 3f);
    }

    // -------------------------------------------
    // INTERNAL SPAWN LOGIC
    // -------------------------------------------
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

        bubble.InitializeAutomatic(
            text,
            FamilyManager.Instance != null ? FamilyManager.Instance.GetBubbleColor(speakerKey) : Color.white,
            FamilyManager.Instance != null ? FamilyManager.Instance.GetFontAsset(speakerKey) : null,
            speakerKey
        );

        bubble.Duration = lifetime > 0f ? lifetime : 3f;

        var pos = spawnPoint.position;

        float extra = bubble.HasSpeaker ? bubble.SpeakerHeight : 0f;
        pos.y -= bubble.RectTransform.rect.height + extra;

        bubble.RectTransform.position = pos;
        bubble.CenterX = bubble.RectTransform.position.x;

        if (bubble.CanvasGroup != null)
            bubble.CanvasGroup.alpha = 1f;

        lastBubbleSpawned = bubble;
    }

    public void ClearAll()
    {
        print("Clearing all bubbles");
        allAtTop = false;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ReturnBubbleToPool(_active[i]);
        }
        _active.Clear();
    }
}
