using System.Collections.Generic;
using UnityEngine;

public class ThoughtBubbleController : MonoBehaviour
{
    public static ThoughtBubbleController Instance;

    [SerializeField] private RectTransform _bubbleParent;
    [SerializeField] private ThoughtBubble _thoughtBubblePrefab;
    [SerializeField] private int _poolSize = 10;
    [SerializeField] private RectTransform _top;
    [SerializeField] private RectTransform _spawn;
    [SerializeField] private float _spacingBetweenBubbles = 40;
    [SerializeField] private float _bubblePaddingLeftRight = 10;
    [SerializeField] private float _moveSpeed = 100;
    [SerializeField] private float _swaySpeed = 0.5f;
    [SerializeField] private float _swayAmplitude = 20;

    private List<ThoughtBubble> _bubblePool;
    private List<ThoughtBubble> _activeBubbles;

    private void Awake()
    {
        Instance = this;
        InitPool();
    }

    private void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowThoughtBubble("Hiki","Heya!",5);
    }

    private void Update()
    {
        DebugControls();

        for (int i = 0; i < _activeBubbles.Count; i++)
        {
            ThoughtBubble bubble = _activeBubbles[i];
            float top = _top.position.y;
            if (i >= 1)
            {
                var prev = _activeBubbles[i - 1];
                float prevTop = prev.RectTransform.position.y - _spacingBetweenBubbles;
                top = Mathf.Min(_top.position.y, prevTop);
            }
            if (bubble.HasSpeaker)
            {
                top -= bubble.SpeakerHeight;
            }
            top -= bubble.RectTransform.rect.height * 0.5f;

            var pos = bubble.RectTransform.position;
            float y = pos.y + _moveSpeed * Time.deltaTime;
            // Hasnt reached top, continue
            if (y < top)
            {
                pos.y = y;
            }
            // Done moving, move off screen and kill
            else if (bubble.Done)
            {
                pos.y = y;
                if (y > _top.position.y + bubble.RectTransform.rect.height * 2)
                {
                    ReturnBubbleToPool(bubble);
                    continue;
                }
            }
            // Reached top, run duration timer
            else
            {
                bubble.TopTimer += Time.deltaTime;
                if (bubble.TopTimer >= bubble.Duration)
                {
                    bubble.Done = true;
                }
            }

            bubble.SwayTimer += Time.deltaTime * _swaySpeed;
            pos.x = bubble.CenterX + Mathf.Sin(bubble.SwayTimer) * _swayAmplitude;

            bubble.RectTransform.position = pos;
        }
    }

    private void InitPool()
    {
        _bubblePool = new List<ThoughtBubble>(_poolSize);
        _activeBubbles = new List<ThoughtBubble>();
        for (int i = 0; i < _poolSize; i++)
        {
            var bubble = Instantiate(_thoughtBubblePrefab, _bubbleParent, false);
            bubble.gameObject.SetActive(false);
            _bubblePool.Add(bubble);
        }
    }

    private ThoughtBubble GetFromPool()
    {
        if (_bubblePool.Count > 0)
        {
            var bubble = _bubblePool[0];
            _bubblePool.RemoveAt(0);
            _activeBubbles.Add(bubble);
            return bubble;
        }
        else
        {
            // Or could add more to pool if you want
            var bubble = _activeBubbles[0];
            _activeBubbles.Remove(bubble);
            bubble.ResetBubble();
            _activeBubbles.Add(bubble);
            return bubble;
        }
    }

    public static void ReturnBubbleToPoolStatic(ThoughtBubble bubble) => Instance.ReturnBubbleToPool(bubble);
    public void ReturnBubbleToPool(ThoughtBubble bubble)
    {
        bubble.ResetBubble();
        if (_activeBubbles.Contains(bubble))
        {
            _activeBubbles.Remove(bubble);
        }
        if (!_bubblePool.Contains(bubble))
        {
            _bubblePool.Add(bubble);
        }
    }
    
    public void ShowThoughtBubble(string speaker, string message, float duration)
    {
        var bubble = GetFromPool();
        var pos = _spawn.position;
        pos.y -= bubble.RectTransform.rect.height * 1f + (bubble.HasSpeaker ? bubble.SpeakerHeight : 0);
        bubble.RectTransform.position = pos;
        bubble.CenterX = bubble.RectTransform.position.x;
        bubble.ShowBubble(speaker, message, duration);
    }

    public void CreateInteractiveBubbleSet()
    {
        ShowThoughtBubble("Michael", "Question 1: What is your favorite color?", float.PositiveInfinity);
        ShowThoughtBubble("", "Answer: Blue", float.PositiveInfinity);
        ShowThoughtBubble("", "Answer: Green", float.PositiveInfinity);
        ShowThoughtBubble("", "Answer: Red", float.PositiveInfinity);
    }
}
