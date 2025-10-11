using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class TooltipPanel : MonoBehaviour
{
    [Header("UI")]
    // NOTE: keep this GameObject as the root that holds this script.
    // 'panel' should be the visual tooltip card (child). If you assigned the same GameObject,
    // this code will handle that safely by toggling children instead of deactivating the host.
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text timeText;
    public TMP_Text spoonsText;
    public TMP_Text hungerText;
    public TMP_Text cashText;
    public Image iconImage;

    [Header("Message UI")]
    public TMP_Text messageText;
    public Vector2 offset = new Vector2(12f, -12f);
    public float floatDistance = 40f;
    public float fadeDuration = 1.5f;

    RectTransform panelRect;
    CanvasGroup messageGroup;
    Coroutine messageRoutine;

    void Awake()
    {
        // IMPORTANT: ensure we don't accidentally deactivate the GameObject that holds this component.
        // If 'panel' was set to the same GameObject, we won't deactivate that GameObject anymore.
        if (panel != null)
        {
            // If panel is the same object as this component's GameObject, we will toggle children instead.
            if (panel == this.gameObject)
            {
                // ensure children are initially inactive (visual content)
                SetPanelChildrenActive(false);
            }
            else
            {
                panel.SetActive(false);
            }

            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            panelRect = panel.GetComponent<RectTransform>();
        }

        if (messageText != null)
        {
            messageGroup = messageText.GetComponent<CanvasGroup>();
            if (messageGroup == null) messageGroup = messageText.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // only move the tooltip visual panel (not the message text) when it's visible
        if (panel == null)
            return;

        // if the visual panel is not active, nothing to update
        bool isVisualActive = IsPanelVisualActive();
        if (!isVisualActive) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 newPos = mousePos + offset;

        if (panelRect != null)
        {
            float width = panelRect.rect.width;
            float height = panelRect.rect.height;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (newPos.x + width > screenWidth)
                newPos.x = mousePos.x - width - Mathf.Abs(offset.x);
            if (newPos.y - height < 0)
                newPos.y = height;
        }

        panel.transform.position = newPos;
    }

    // === Tooltip Content ===
    public void Show(BehaviorData data)
    {
        if (panel == null || data == null) return;

        titleText.text = data.behaviorName;
        descText.text = data.behaviorDescription;

        if (data.isToggle)
            timeText.text = "Time: Toggle (player controlled)";
        else if (data.durationMinutes > 0)
            timeText.text = FormatDuration(data.durationMinutes);
        else
            timeText.text = "";

        spoonsText.text = data.hideSpoonsCost ? "Spoons: ???" : $"Spoons: {data.spoonsCost}";
        hungerText.text = data.hungerImpact != 0 ? FormatHunger(data.hungerImpact) : "";
        cashText.text = data.cashCost > 0 ? $"Cost: ${data.cashCost:0.00}" : "";

        if (iconImage != null) iconImage.sprite = data.icon;

        // show visuals without deactivating this GameObject
        SetPanelVisualActive(true);
    }

    public void Hide()
    {
        if (panel == null) return;

        // If a busy message is running, don't disable the host object (that would kill coroutines).
        // Instead hide visual children so the tooltip disappears but coroutine(s) can continue.
        SetPanelVisualActive(false);
    }

    string FormatDuration(int minutes)
    {
        if (minutes <= 0) return "Time: <1m";
        int h = minutes / 60;
        int m = minutes % 60;
        if (h > 0) return $"Time: {h}h {m}m";
        return $"Time: {m}m";
    }

    string FormatHunger(int delta)
    {
        if (delta == 0) return "Hunger: None";
        if (delta > 0) return $"Hunger: +{delta}";
        return $"Hunger: {delta}";
    }

    // === Busy Message ===
    public void ShowBusyMessage(string msg)
    {
        if (messageText == null) return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageText.text = msg;
        // ensure visible immediately
        messageGroup.alpha = 1f;

        // Position near mouse
        Vector3 mousePos = Input.mousePosition;
        messageText.transform.position = mousePos + (Vector3)offset;

        messageRoutine = StartCoroutine(FadeAndFloat(msg));
    }

    private IEnumerator FadeAndFloat(string msg)
    {
        messageText.enabled = true;
        messageText.text = msg;
        if (messageGroup != null)
        {
            messageGroup.alpha = 1f;
            messageGroup.blocksRaycasts = false;
        }

        Vector2 startPos = (Vector2)Input.mousePosition + offset;
        Vector2 endPos = startPos + new Vector2(0, floatDistance);
        messageText.transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            messageText.transform.position = Vector2.Lerp(startPos, endPos, t);
            messageGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        messageText.text = "";
        messageGroup.alpha = 0f;
        messageText.enabled = false;
        messageRoutine = null;
    }


    // ---- helper helpers ----
    bool IsPanelVisualActive()
    {
        if (panel == null) return false;
        if (panel != this.gameObject) return panel.activeSelf;
        // If panel == host, check if any child is active (visual)
        foreach (Transform t in panel.transform)
        {
            if (t.gameObject.activeSelf) return true;
        }
        return false;
    }

    void SetPanelVisualActive(bool active)
    {
        if (panel == null) return;

        // Toggle root Image (the background)
        var rootImage = panel.GetComponent<Image>();
        if (rootImage != null)
            rootImage.enabled = active;

        if (panel != this.gameObject)
        {
            panel.SetActive(active);
            return;
        }

        // If the user assigned the same GameObject to `panel` (common), avoid deactivating the host.
        // Toggle children instead, but do NOT touch messageText (which may be sibling/child).
        foreach (Transform child in panel.transform)
        {
            if (messageText != null && child.gameObject == messageText.gameObject)
                continue;

            // If the child is the background panel that you keep as a child of the message,
            // you might want to exclude it here too. Adjust as needed.
            child.gameObject.SetActive(active);
        }
    }

    void SetPanelChildrenActive(bool active)
    {
        if (panel == null) return;
        foreach (Transform child in panel.transform)
        {
            child.gameObject.SetActive(active);
        }
    }
}
