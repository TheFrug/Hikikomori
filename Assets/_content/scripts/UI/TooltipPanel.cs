using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class TooltipPanel : MonoBehaviour
{
    [Header("UI")]
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
        if (panel != null)
        {
            panel.SetActive(false);

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
        if (panel == null || !panel.activeSelf) return;

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
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel == null) return;
        panel.SetActive(false);
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

        StopAllCoroutines(); // cancel any previous animations
        messageText.text = msg;
        messageText.alpha = 1f;

        // Position near mouse
        Vector3 mousePos = Input.mousePosition;
        messageText.transform.position = mousePos + (Vector3)offset;

        messageRoutine = StartCoroutine(FadeAndFloat(msg));
    }

    private IEnumerator FadeAndFloat(string msg)
    {
        messageText.text = msg;
        messageGroup.alpha = 1f;

        Vector2 startPos = (Vector2)Input.mousePosition + offset;
        Vector2 endPos = startPos + new Vector2(0, floatDistance);
        messageText.transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            messageText.transform.position = Vector2.Lerp(startPos, endPos, t);
            messageGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        messageText.text = "";
        messageGroup.alpha = 0f;
    }
}
