using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectHiki.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ThoughtBubble : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI bodyText = null!;
        [SerializeField] private Image background = null!;
        [SerializeField] private GameObject namePanel = null!;
        private TMP_Text? nameText;

        private CanvasGroup canvasGroup = null!;
        private RectTransform rect = null!;
        private ThoughtBubbleView? owner = null;

        private float lifetime = 3f;
        private float riseDistance = 60f;
        private float fadeEdgeTime = 0.35f;
        private Coroutine? moveCoroutine;

        [Header("Auto-sizing")]
        [SerializeField] private Vector2 padding = new(30f, 20f);
        [SerializeField] private float minWidth = 250f, maxWidth = 500f, minHeight = 75f, maxHeight = 300f;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rect = GetComponent<RectTransform>();
            if (namePanel != null) nameText = namePanel.GetComponentInChildren<TMP_Text>(true);
        }

        public void InitializeFloating(
            string text, Color color, TMP_FontAsset? font,
            string speakerName, float lifetime, float riseDistance,
            float fadeEdgeTime, ThoughtBubbleView owner)
        {
            this.lifetime = lifetime;
            this.riseDistance = riseDistance;
            this.fadeEdgeTime = fadeEdgeTime;
            this.owner = owner;

            bodyText.text = text;
            if (font != null) bodyText.font = font;
            if (background != null) background.color = color;
            if (namePanel != null)
            {
                namePanel.SetActive(!string.IsNullOrEmpty(speakerName));
                nameText?.SetText(speakerName);
            }

            ResizeToFitText();

            canvasGroup.alpha = 0f;
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(FloatAndFadeCoroutine());
        }

        private IEnumerator FloatAndFadeCoroutine()
        {
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * riseDistance;

            float elapsed = 0f;
            float swayAmp = Random.Range(10f, 25f);
            float swayFreq = Random.Range(0.8f, 1.4f);
            float swayPhase = Random.Range(0f, Mathf.PI * 2f);

            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                float newY = Mathf.Lerp(startPos.y, endPos.y, t);
                float swayX = Mathf.Sin((elapsed * swayFreq) + swayPhase) * swayAmp;

                rect.anchoredPosition = new Vector2(startPos.x + swayX, newY);

                float edge = fadeEdgeTime;
                float alpha = 1f;
                if (elapsed < edge) alpha = Mathf.Clamp01(elapsed / edge);
                else if (elapsed > lifetime - edge) alpha = Mathf.Clamp01((lifetime - elapsed) / edge);
                canvasGroup.alpha = alpha;

                yield return null;
            }

            owner?.RecycleBubble(gameObject);
        }

        private void ResizeToFitText()
        {
            bodyText.ForceMeshUpdate();
            float w = Mathf.Clamp(bodyText.preferredWidth + padding.x, minWidth, maxWidth);
            float h = Mathf.Clamp(bodyText.preferredHeight + padding.y, minHeight, maxHeight);

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            if (background != null)
            {
                var bgRect = background.rectTransform;
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                bgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }
        }
    }
}
