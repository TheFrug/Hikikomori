using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ProjectHiki.UI;

public class ThoughtBubbleManager_New : MonoBehaviour
{
    public static ThoughtBubbleManager_New Instance { get; private set; }
    public System.Action OnBubbleFinished;

    [Header("References")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform topPoint;
    [SerializeField] private RectTransform container;

    [Header("Pool Settings")]
    [SerializeField] private ThoughtBubble_New bubblePrefab;
    [SerializeField] private int poolSize = 20;

    [Header("Float Settings (Brandon-style)")]
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float swaySpeed = 0.5f;
    [SerializeField] private float swayAmplitude = 20f;
    [SerializeField] private float spacingBetweenBubbles = 40f;

    [Header("General")]
    [SerializeField] private int maxSimultaneous = 20;

    [Header("Debug")]
    [SerializeField] private Thought debugThought;

    private List<ThoughtBubble_New> _pool;
    private List<ThoughtBubble_New> _active;

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

        // recycle oldest
        if (_active.Count > 0)
        {
            var oldest = _active[0];
            _active.RemoveAt(0);
            oldest.ResetBubble();
            _active.Add(oldest);
            return oldest;
        }

        // fallback (shouldn't happen)
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
        // debug spawn
        if (Input.GetKeyDown(KeyCode.Alpha5) && debugThought != null)
        {
            ShowBubble(debugThought);
        }

        // update active bubbles
        for (int i = 0; i < _active.Count; i++)
        {
            var bubble = _active[i];
            // world-space positions like Brandon's controller
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

            // want top to be the Y where bubble's top edge should sit, convert to center Y
            top -= bubble.RectTransform.rect.height * 0.5f;

            var pos = bubble.RectTransform.position;
            float y = pos.y + moveSpeed * Time.deltaTime;

            if (y < top)
            {
                // still moving to top
                pos.y = y;
            }
            else if (bubble.Done)
            {
                // already finished waiting at top; continue moving off-screen then remove
                pos.y = y;
                if (y > topPoint.position.y + bubble.RectTransform.rect.height * 2f)
                {
                    ReturnBubbleToPool(bubble);
                    OnBubbleFinished.Invoke();
                    // adjust index because pool removed one element in list update? We remove from _active inside ReturnBubbleToPool
                    i--;
                    continue;
                }
            }
            else
            {
                // reached top and not yet done: run the top timer
                bubble.TopTimer += Time.deltaTime;
                if (bubble.TopTimer >= bubble.Duration)
                {
                    bubble.Done = true;
                }
            }

            // sway and horizontal movement
            bubble.SwayTimer += Time.deltaTime * swaySpeed;
            pos.x = bubble.CenterX + Mathf.Sin(bubble.SwayTimer) * swayAmplitude;

            bubble.RectTransform.position = pos;
        }
    }

    // Public entry points (string-based)
    public void ShowBubble(string speakerKey, string text)
    {
        // default lifetime pulled from Thought or fallback
        float lifetime = 3f;
        ShowBubbleInternal(speakerKey, text, lifetime);
    }

    // Scriptable object entry
    public void ShowBubble(Thought thought)
    {
        if (thought == null) return;
        ShowBubbleInternal(thought.speakerKey, thought.previewText ?? string.Empty, thought.lifetime);
    }

    private void ShowBubbleInternal(string speakerKey, string text, float lifetime)
    {
        if (_active.Count >= maxSimultaneous)
        {
            // recycle oldest immediately
            var oldest = _active[0];
            _active.RemoveAt(0);
            ReturnBubbleToPool(oldest);
        }

        var bubble = GetFromPool();

        // Activate first so TMP/rect size calculations are reliable
        bubble.gameObject.SetActive(true);

        // Initialize visuals & autosize
        bubble.InitializeAutomatic(text, FamilyManager.Instance != null ? FamilyManager.Instance.GetBubbleColor(speakerKey) : Color.white,
                                   FamilyManager.Instance != null ? FamilyManager.Instance.GetFontAsset(speakerKey) : null,
                                   speakerKey);

        // set duration
        bubble.Duration = lifetime > 0f ? lifetime : 3f;

        // position at spawn
        var pos = spawnPoint.position;
        // adjust spawn downwards by bubble height + optional speaker height so it starts below spawn like Brandon
        float extra = bubble.HasSpeaker ? bubble.SpeakerHeight : 0f;
        pos.y -= bubble.RectTransform.rect.height * 1f + extra;
        bubble.RectTransform.position = pos;

        // record center X for sway
        bubble.CenterX = bubble.RectTransform.position.x;

        // ensure visible
        if (bubble.CanvasGroup != null) bubble.CanvasGroup.alpha = 1f;
    }

    // external clear
    public void ClearAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ReturnBubbleToPool(_active[i]);
        }
        _active.Clear();
    }
}
