using UnityEngine;
using UnityEngine.EventSystems;

public class SpoonSlot : MonoBehaviour
{
    public int spoonCount = 0;

    private SpoonPanel parentPanel;

    private Transform originalParent;
    private Vector2 originalAnchoredPos;

    public void Initialize(SpoonPanel panel)
    {
        parentPanel = panel;
    }

    // We don't use OnDrop anymore, but Unity complains if the interface is missing
    // so it's here empty.
    public void OnDrop(PointerEventData eventData) { }

    public void TryAcceptSpoon(spoonBehavior spoon)
    {
        if (spoon == null) return;
        if (!IsSpoonInside(spoon))
            return;

        var spoonRect = spoon.GetComponent<RectTransform>();

        // Save return data locally on slot (slot may hold multiple, but we only have one visual slot)
        originalParent = spoonRect.parent;
        originalAnchoredPos = spoonRect.anchoredPosition;

        // Snap spoon to slot visually (we keep it so the player sees it before it fades)
        spoonRect.SetParent(transform, true);
        spoonRect.anchoredPosition = Vector2.zero;

        spoon.insideDrawer = false;

        spoonCount++;

        // Let the panel register/track it as a spent spoon (panel will call spoon.Spend())
        parentPanel?.RegisterSpentSpoon(spoon);
    }

    public bool IsSpoonInside(spoonBehavior spoon)
    {
        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform spoonRect = spoon.GetComponent<RectTransform>();

        Vector3[] slotCorners = new Vector3[4];
        Vector3[] spoonCorners = new Vector3[4];

        slotRect.GetWorldCorners(slotCorners);
        spoonRect.GetWorldCorners(spoonCorners);

        Rect slotBounds = new Rect(
            slotCorners[0].x, slotCorners[0].y,
            slotCorners[2].x - slotCorners[0].x,
            slotCorners[2].y - slotCorners[0].y
        );

        Rect spoonBounds = new Rect(
            spoonCorners[0].x, spoonCorners[0].y,
            spoonCorners[2].x - spoonCorners[0].x,
            spoonCorners[2].y - spoonCorners[0].y
        );

        return slotBounds.Overlaps(spoonBounds, true);
    }

    public void ForceReturnSpoon()
    {
        foreach (Transform child in transform)
        {
            var rt = child as RectTransform;
            if (rt == null) continue;

            var spoon = rt.GetComponent<spoonBehavior>();
            if (spoon == null) continue;

            spoon.insideDrawer = true;
            rt.SetParent(originalParent, false);
            rt.anchoredPosition = originalAnchoredPos;
        }

        spoonCount = 0;
    }
}
