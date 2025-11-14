using UnityEngine;
using UnityEngine.UI;

public class BehaviorIconUI : MonoBehaviour
{
    public BehaviorAnchor anchor;
    public RectTransform rectTransform;
    public Camera worldCamera;

    void Awake()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        if (!worldCamera) worldCamera = Camera.main;
    }

    public Canvas uiCanvas; // assign from BehaviorAnchor on spawn

    void Update()
    {
        if (!anchor || !rectTransform || !worldCamera || !uiCanvas) return;

        Vector2 screenPoint = worldCamera.WorldToScreenPoint(anchor.transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rectTransform.parent,
            screenPoint,
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : uiCanvas.worldCamera,
            out Vector2 localPos
        );

        rectTransform.localPosition = localPos;
    }


    public void OnClick()
    {
        anchor.OnWorldClicked();
    }
}
