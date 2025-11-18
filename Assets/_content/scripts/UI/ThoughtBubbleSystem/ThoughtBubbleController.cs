// ProjectHiki/UI/ThoughtBubbleController.cs
using System.Collections.Generic;
using UnityEngine;

namespace ProjectHiki.UI
{
    public class ThoughtBubbleController : MonoBehaviour
    {
        public static ThoughtBubbleController Instance;

        [Header("Prefabs & Parents")]
        [SerializeField] private RectTransform bubbleParent;
        [SerializeField] private ThoughtBubble thoughtBubblePrefab;
        [SerializeField] private int poolSize = 10;

        [Header("Spawn / Layout")]
        [SerializeField] private RectTransform top;   // top ceiling reference
        [SerializeField] private RectTransform spawn; // spawn origin
        [SerializeField] private float spacingBetweenBubbles = 20f;
        [SerializeField] private float moveSpeed = 50f;
        [Header("Sway")]
        [SerializeField] private float swaySpeed = 0.5f;
        [SerializeField] private float swayAmplitude = 20f;

        private List<ThoughtBubble> bubblePool;
        private List<ThoughtBubble> activeBubbles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitPool();
        }

        private void InitPool()
        {
            bubblePool = new List<ThoughtBubble>(poolSize);
            activeBubbles = new List<ThoughtBubble>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                var bubble = Instantiate(thoughtBubblePrefab, bubbleParent, false);
                bubble.gameObject.SetActive(false);
                bubblePool.Add(bubble);
            }
        }

        private ThoughtBubble GetFromPool()
        {
            if (bubblePool.Count > 0)
            {
                var b = bubblePool[0];
                bubblePool.RemoveAt(0);
                activeBubbles.Add(b);
                return b;
            }
            else
            {
                // fallback: recycle oldest active bubble
                if (activeBubbles.Count > 0)
                {
                    var oldest = activeBubbles[0];
                    activeBubbles.RemoveAt(0);
                    oldest.ResetBubble();
                    activeBubbles.Add(oldest);
                    return oldest;
                }
                // last resort: instantiate (should be rare)
                var bubble = Instantiate(thoughtBubblePrefab, bubbleParent, false);
                activeBubbles.Add(bubble);
                return bubble;
            }
        }

        public static void ReturnBubbleToPoolStatic(ThoughtBubble bubble)
        {
            Instance?.ReturnBubbleToPool(bubble);
        }

        public void ReturnBubbleToPool(ThoughtBubble bubble)
        {
            if (bubble == null) return;
            bubble.ResetBubble();
            if (activeBubbles.Contains(bubble))
                activeBubbles.Remove(bubble);
            if (!bubblePool.Contains(bubble))
                bubblePool.Add(bubble);
        }

        private void Update()
        {
            for (int i = 0; i < activeBubbles.Count; i++)
            {
                var bubble = activeBubbles[i];
                float ceiling = top.position.y;
                if (i >= 1)
                {
                    var prev = activeBubbles[i - 1];
                    float prevTop = prev.RectTransform.position.y - spacingBetweenBubbles;
                    ceiling = Mathf.Min(top.position.y, prevTop);
                }

                if (bubble.HasSpeaker)
                    ceiling -= bubble.SpeakerHeight;

                ceiling -= bubble.RectTransform.rect.height * 0.5f;

                var pos = bubble.RectTransform.position;
                float y = pos.y + moveSpeed * Time.deltaTime;

                // Not yet reached top: keep moving up
                if (y < ceiling)
                {
                    pos.y = y;
                }
                else if (bubble.Done)
                {
                    // if done, keep moving off screen; when far enough, recycle
                    pos.y = y;
                    if (y > top.position.y + bubble.RectTransform.rect.height * 2f)
                    {
                        ReturnBubbleToPool(bubble);
                        continue;
                    }
                }
                else
                {
                    // reached top and not Done: run duration timer
                    bubble.TopTimer += Time.deltaTime;
                    if (bubble.TopTimer >= bubble.Duration)
                        bubble.Done = true;
                }

                // sway
                bubble.SwayTimer += Time.deltaTime * swaySpeed;
                pos.x = bubble.CenterX + Mathf.Sin(bubble.SwayTimer) * swayAmplitude;

                bubble.RectTransform.position = pos;
            }
        }

        /// <summary>
        /// Public spawn call: speaker can be "" or null if no speaker name.
        /// Duration can be float.PositiveInfinity for interactive/permanent bubbles.
        /// </summary>
        public void ShowThoughtBubble(string speaker, string message, float duration)
        {
            var bubble = GetFromPool();
            var pos = spawn.position;
            pos.y -= bubble.RectTransform.rect.height * 1f + (bubble.HasSpeaker ? bubble.SpeakerHeight : 0f);
            bubble.RectTransform.position = pos;
            bubble.CenterX = bubble.RectTransform.position.x;
            bubble.ShowBubble(speaker, message, duration);
        }

        // simple debug helper
        public void CreateInteractiveBubbleSet()
        {
            ShowThoughtBubble("Michael", "Question 1: What is your favorite color?", float.PositiveInfinity);
            ShowThoughtBubble("", "Answer: Blue", float.PositiveInfinity);
            ShowThoughtBubble("", "Answer: Green", float.PositiveInfinity);
            ShowThoughtBubble("", "Answer: Red", float.PositiveInfinity);
        }
    }
}
