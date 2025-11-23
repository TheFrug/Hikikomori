using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using ProjectHiki.UI;

public class ThoughtBubbleManager_New : MonoBehaviour
{
    public static ThoughtBubbleManager_New Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform topPoint;
    [SerializeField] private RectTransform container;

    [Header("Pool Settings")]
    [SerializeField] private ThoughtBubble_New bubblePrefab;
    [SerializeField] private int poolSize = 20;

    [Header("Float Settings")]
    [SerializeField] private float riseSpeed = 40f;
    [SerializeField] private float swayAmplitude = 8f;
    [SerializeField] private float swaySpeed = 2f;
    [SerializeField] private float bubbleSpacing = 24f;
    [SerializeField] private float ceilingBuffer = 10f;

    [Header("Debug")]
    [SerializeField] private Thought debugThought;

    private readonly Queue<ThoughtBubble_New> _pool = new();
    private readonly List<BubbleRuntime> _active = new();

    private float _swayTimer;

    public System.Action? OnBubbleFinished;

    private class BubbleRuntime
    {
        public ThoughtBubble_New bubble;
        public float originalX;
        public float ceilingY;
        public bool atCeiling;
        public float lingerTimer;
        public float fadeTimer;
        public float lifetime;
        public bool fading;
    }

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            var bubble = Instantiate(bubblePrefab, container);
            bubble.gameObject.SetActive(false);
            _pool.Enqueue(bubble);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha5) && debugThought != null)
        {
            ShowBubble(debugThought);
        }

        if (_active.Count == 0)
            return;

        UpdateBubblePositions();
    }

    private void UpdateBubblePositions()
    {
        _swayTimer += Time.deltaTime * swaySpeed;
        float sway = Mathf.Sin(_swayTimer) * swayAmplitude;

        float ceiling = topPoint.anchoredPosition.y - ceilingBuffer;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            BubbleRuntime rt = _active[i];
            var b = rt.bubble;
            var cg = b.CanvasGroup;

            Vector2 pos = b.Rect.anchoredPosition;

            if (!rt.atCeiling)
            {
                pos.y += riseSpeed * Time.deltaTime;
                pos.x = Mathf.Lerp(rt.originalX, rt.originalX + sway, 0.5f);

                if (pos.y >= ceiling)
                {
                    pos.y = ceiling;
                    rt.atCeiling = true;
                    rt.lingerTimer = 0f;
                }
            }
            else if (!rt.fading)
            {
                rt.lingerTimer += Time.deltaTime;
                if (rt.lingerTimer >= rt.lifetime)
                {
                    rt.fading = true;
                    rt.fadeTimer = 0f;
                }
            }
            else
            {
                rt.fadeTimer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, rt.fadeTimer / 0.5f);

                if (cg.alpha <= 0.01f)
                {
                    ReturnToPool(rt.bubble);
                    OnBubbleFinished?.Invoke();
                    _active.RemoveAt(i);
                    continue;
                }
            }

            b.Rect.anchoredPosition = pos;
        }
    }

    private ThoughtBubble_New GetBubbleFromPool()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        // fallback: recycle oldest
        var oldest = _active[0];
        _active.RemoveAt(0);
        return oldest.bubble;
    }

    // --------------------------------------------------------
    // PUBLIC ENTRY POINTS
    // --------------------------------------------------------

    public void ShowBubble(string speakerKey, string text)
    {
        // GET VISUAL STYLE
        Color color = FamilyManager.Instance.GetBubbleColor(speakerKey);
        TMP_FontAsset font = FamilyManager.Instance.GetFontAsset(speakerKey);
        string displayName = FamilyManager.Instance.GetDisplayName(speakerKey);

        SpawnBubbleInternal(text, speakerKey, displayName, color, font, 3f);
    }

    public void ShowBubble(Thought thought)
    {
        Color color = FamilyManager.Instance.GetBubbleColor(thought.speakerKey);
        TMP_FontAsset font = FamilyManager.Instance.GetFontAsset(thought.speakerKey);
        string displayName = FamilyManager.Instance.GetDisplayName(thought.speakerKey);

        SpawnBubbleInternal(
            thought.previewText,
            thought.speakerKey,
            displayName,
            color,
            font,
            thought.lifetime
        );
    }

    private void SpawnBubbleInternal(
        string text,
        string speakerKey,
        string displayName,
        Color color,
        TMP_FontAsset font,
        float lifetime)
    {
        var bubble = GetBubbleFromPool();

        bubble.InitializeAutomatic(
            text,
            color,
            font,
            displayName,
            null
        );

        var rt = bubble.Rect;
        rt.SetParent(container, false);
        rt.anchoredPosition = spawnPoint.anchoredPosition;
        bubble.CanvasGroup.alpha = 1f;

        float origX = rt.anchoredPosition.x;
        float ceiling = topPoint.anchoredPosition.y - ceilingBuffer;

        bubble.gameObject.SetActive(true);

        _active.Add(new BubbleRuntime
        {
            bubble = bubble,
            originalX = origX,
            ceilingY = ceiling,
            atCeiling = false,
            lingerTimer = 0f,
            fadeTimer = 0f,
            lifetime = lifetime,
            fading = false
        });
    }

    private void ReturnToPool(ThoughtBubble_New bubble)
    {
        bubble.gameObject.SetActive(false);
        bubble.CanvasGroup.alpha = 1f;
        _pool.Enqueue(bubble);
    }

    public void ClearAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ReturnToPool(_active[i].bubble);
        }
        _active.Clear();
    }
}
