using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class spoonBehavior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image image;

    [Header("Resting")]
    public Vector2 restPosition;
    public float returnDelay = 0.25f;
    public float returnDuration = 0.4f;
    public AnimationCurve returnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("State")]
    public bool insideDrawer = true;

    // saved rest position used when restoring from "spent" state
    private Vector2 savedRestPosition;

    private Coroutine returnRoutine;
    private Vector2 dragOffset;

    [Header("Visuals")]
    public float dragScale = 1.2f;
    public float dragBrightness = 1.15f;

    private Color originalColor;
    private Vector3 originalScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        image = GetComponent<Image>();

        originalScale = rectTransform.localScale;
        if (image != null)
            originalColor = image.color;
    }

    void Start()
    {
        restPosition = rectTransform.anchoredPosition;
        savedRestPosition = restPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;

        rectTransform.SetAsLastSibling();

        rectTransform.localScale = originalScale * dragScale;
        if (image != null)
            image.color = originalColor * dragBrightness;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            null,
            out Vector2 localMousePos
        );

        dragOffset = rectTransform.anchoredPosition - localMousePos;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            null,
            out Vector2 localMousePos))
        {
            rectTransform.anchoredPosition = localMousePos + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        rectTransform.localScale = originalScale;
        if (image != null)
            image.color = originalColor;

        // Try to accept into slot
        if (SpoonPanel.ActivePanel != null)
        {
            foreach (var slot in SpoonPanel.ActivePanel.slots)
            {
                slot.TryAcceptSpoon(this);
                if (!insideDrawer) return; // accepted -> stop further processing
            }
        }

        // If dropped in drawer, update its new rest position
        if (IsInsideDrawer())
        {
            insideDrawer = true;
            restPosition = rectTransform.anchoredPosition;
            savedRestPosition = restPosition;
        }
        else
        {
            insideDrawer = false;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = StartCoroutine(ReturnToDrawer());
        }
    }

    private bool IsInsideDrawer()
    {
        if (SpoonDrawer.Instance?.drawerArea == null)
            return false;

        RectTransform drawerRect = SpoonDrawer.Instance.drawerArea;

        Vector3[] drawerCorners = new Vector3[4];
        Vector3[] spoonCorners = new Vector3[4];

        drawerRect.GetWorldCorners(drawerCorners);
        rectTransform.GetWorldCorners(spoonCorners);

        Rect drawerBounds = new Rect(
            drawerCorners[0].x, drawerCorners[0].y,
            drawerCorners[2].x - drawerCorners[0].x,
            drawerCorners[2].y - drawerCorners[0].y
        );

        Rect spoonBounds = new Rect(
            spoonCorners[0].x, spoonCorners[0].y,
            spoonCorners[2].x - spoonCorners[0].x,
            spoonCorners[2].y - spoonCorners[0].y
        );

        return drawerBounds.Overlaps(spoonBounds, true);
    }

    private IEnumerator ReturnToDrawer()
    {
        yield return new WaitForSeconds(returnDelay);

        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = returnEase.Evaluate(elapsed / returnDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, restPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = restPosition;
        insideDrawer = true;
        savedRestPosition = restPosition;
        returnRoutine = null;
    }

    // Called by SpoonPanel to mark this spoon as consumed/spent
    public void Spend()
    {
        // Save the rest position in case we need to restore
        savedRestPosition = restPosition;

        // Visual fade and deactivate
        StartCoroutine(FadeOutAndDeactivate());
    }

    private IEnumerator FadeOutAndDeactivate()
    {
        float fadeTime = 0.25f;
        float elapsed = 0f;

        // Try to fade via CanvasGroup
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        cg.alpha = 0f;

        // Deactivate the GO (we keep the component to restore later)
        gameObject.SetActive(false);
    }

    // Restore a spent spoon back to the drawer using its savedRestPosition
    public void RestoreFromSpend()
    {
        // Make sure Go is active and parented under the drawer
        gameObject.SetActive(true);

        if (SpoonDrawer.Instance != null && SpoonDrawer.Instance.spoonParent != null)
        {
            rectTransform.SetParent(SpoonDrawer.Instance.spoonParent, false);
        }

        // re-enable visuals
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        if (image != null)
        {
            // reset color immediately
            image.color = originalColor;
        }

        // Animate from current anchored position to savedRestPosition
        StartCoroutine(AnimateRestoreTo(savedRestPosition));
    }

    private IEnumerator AnimateRestoreTo(Vector2 targetAnchored)
    {
        // start visible
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 0f;

        // Optionally set starting position near the spoon panel/slot.
        Vector2 start = rectTransform.anchoredPosition;
        float dur = Mathf.Max(0.25f, returnDuration);

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = returnEase.Evaluate(Mathf.Clamp01(elapsed / dur));
            rectTransform.anchoredPosition = Vector2.Lerp(start, targetAnchored, t);
            if (cg != null) cg.alpha = Mathf.Lerp(0f, 1f, elapsed / dur);
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchored;
        if (cg != null) cg.alpha = 1f;

        insideDrawer = true;
        restPosition = targetAnchored;
        savedRestPosition = targetAnchored;
    }
}
