using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class Tab : MonoBehaviour
{
    [Header("Settings")]
    public bool inFrame = false;             // Whether the panel is currently visible
    public float slideDuration = 0.5f;       // How long the animation takes

    [Header("Offsets")]
    public float xOffset = 300f;             // Positive = right, negative = left
    public float yOffset = 0f;               // Positive = up, negative = down

    [Header("References")]
    public Button button;                    // The toggle button

    private RectTransform rectTransform;
    private Vector2 startingPos;
    private Vector2 targetPos;
    private bool isSliding = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startingPos = rectTransform.anchoredPosition;

        // If button wasn’t manually assigned, look for one among children
        if (button == null)
            button = GetComponentInChildren<Button>();

        if (button != null)
            button.onClick.AddListener(ToggleTab);
    }

    public void ToggleTab()
    {
        if (isSliding) return;
        StartCoroutine(SlideCoroutine());
    }

    private IEnumerator SlideCoroutine()
    {
        isSliding = true;
        Vector2 start = rectTransform.anchoredPosition;
        Vector2 end = inFrame
            ? startingPos                   // Move back to start
            : startingPos + new Vector2(xOffset, yOffset);  // Slide out

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        rectTransform.anchoredPosition = end;
        inFrame = !inFrame;
        isSliding = false;
    }
}
