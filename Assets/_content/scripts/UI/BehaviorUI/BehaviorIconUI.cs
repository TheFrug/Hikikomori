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
        HandlePosition();
    }

    private void HandlePosition()
    {
        if (!anchor || !rectTransform || !worldCamera || !uiCanvas)
            return;

        Vector3 screenPoint3D = worldCamera.WorldToScreenPoint(anchor.transform.position);

        // === Offscreen / behind camera check ===
        bool offScreen =
            screenPoint3D.z < 0 ||
            screenPoint3D.x < 0 ||
            screenPoint3D.x > Screen.width ||
            screenPoint3D.y < 0 ||
            screenPoint3D.y > Screen.height;

        if (offScreen)
        {
            rectTransform.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!rectTransform.gameObject.activeSelf)
                rectTransform.gameObject.SetActive(true);
        }

        // Now it's safe to map to the RawImage

        Vector2 screenPoint = screenPoint3D;

        RectTransform rawRect = (RectTransform)rectTransform.parent;

        Vector2 rawPos = rawRect.anchoredPosition;
        Vector2 rawSize = rawRect.sizeDelta;

        Vector2 normalized = new Vector2(
            screenPoint.x / Screen.width,
            screenPoint.y / Screen.height
        );

        Vector2 rawLocal = new Vector2(
            (normalized.x * rawSize.x) - rawSize.x * 0.5f,
            (normalized.y * rawSize.y) - rawSize.y * 0.5f
        );

        rectTransform.localPosition = rawLocal;

    }


    public void OnClick()
    {
        anchor.OnWorldClicked();
    }
}
