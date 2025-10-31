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

    private Coroutine returnRoutine;
    private Vector2 dragOffset;

    [Header("Visuals")]
    public float dragScale = 1.2f;
    public float dragBrightness = 1.15f;

    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        image = GetComponent<Image>();

        if (canvas == null)
            Debug.LogError("SpoonBehavior: No parent Canvas found — drag will not work correctly!");

        if (image != null)
            originalColor = image.color;

        originalScale = rectTransform.localScale;
    }

    private void Start()
    {
        restPosition = rectTransform.anchoredPosition;
    }

    #region Drag Handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;

        // Move spoon to topmost layer
        rectTransform.SetAsLastSibling();

        // Visual feedback: brighten and enlarge
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

        // Reset visuals
        rectTransform.localScale = originalScale;
        if (image != null)
            image.color = originalColor;

        if (IsInsideDrawer())
        {
            insideDrawer = true;
            restPosition = rectTransform.anchoredPosition; // save new organized position
        }
        else
        {
            insideDrawer = false;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = StartCoroutine(ReturnToDrawer());
        }
    }
    #endregion

    #region Drawer Check
    private bool IsInsideDrawer()
    {
        if (SpoonDrawer.Instance?.drawerArea == null)
            return false;

        RectTransform drawerRect = SpoonDrawer.Instance.drawerArea;

        Vector3[] drawerCorners = new Vector3[4];
        Vector3[] spoonCorners = new Vector3[4];

        drawerRect.GetWorldCorners(drawerCorners);
        rectTransform.GetWorldCorners(spoonCorners);

        Rect drawerBounds = new Rect(drawerCorners[0].x, drawerCorners[0].y,
                                     drawerCorners[2].x - drawerCorners[0].x,
                                     drawerCorners[2].y - drawerCorners[0].y);

        Rect spoonBounds = new Rect(spoonCorners[0].x, spoonCorners[0].y,
                                    spoonCorners[2].x - spoonCorners[0].x,
                                    spoonCorners[2].y - spoonCorners[0].y);

        return drawerBounds.Overlaps(spoonBounds, true);
    }
    #endregion

    #region Return Coroutine
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
        returnRoutine = null;
    }
    #endregion
}
