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

    void Update()
    {
        if (!anchor) return;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(anchor.transform.position);

        rectTransform.position = screenPos;
    }

    public void OnClick()
    {
        anchor.OnWorldClicked();
    }
}
