using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BehaviorChoice : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text timeText;
    public TMP_Text spoonsText;
    public TMP_Text hopeText;
    public TMP_Text behaviorTypeText;
    public Image iconImage;

    [Header("Hook-ins")]
    public Button selectButton;
    public BehaviorManager behaviorManager;
    [SerializeField] public GameObject spoonPanelPrefab;
    [SerializeField] public Transform panelAnchor;

    [Header("One-shot UI")]
    public Slider oneShotProgressBar;
    public float defaultOneShotSeconds = 0.6f;

    // optional: if you want a dedicated grey overlay object, assign it here (optional)
    [Header("Optional visuals")]
    [SerializeField] private GameObject greyOverlay;

    private BehaviorData data;
    public SpoonPanel currentSpoonPanel;
    private Coroutine progressRoutine;
    private ThoughtData myThought;

    // removed: per-instance usedToday flag -- persistence is centralized in BehaviorManager

    public BehaviorData BehaviorData { get { return data; } }

    public void Configure(BehaviorData behaviorData, BehaviorManager mgr)
    {
        data = behaviorData;
        behaviorManager = mgr;
        myThought = data.thought;

        titleText.text = behaviorData.behaviorName;
        descText.text = behaviorData.behaviorDescription;

        spoonsText.text = behaviorData.hideSpoonsCost
            ? "Spoons: ???"
            : $"Spoons: {behaviorData.spoonsCost}";

        hopeText.text = $"Hope: {behaviorData.hopeImpact}";

        if (data.repeatable)
        {
            behaviorTypeText.text = "-Repeatable Action-";
        }
        else
        {
            behaviorTypeText.text = "-Critical Action-";
        }

        if (iconImage != null)
            iconImage.sprite = behaviorData.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelected);

        // Initialize one-shot bar
        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }

        // Ensure grey overlay state reset if present
        if (greyOverlay != null) greyOverlay.SetActive(false);

        // Refresh UI based on central state
        RefreshState();
    }

    // Refresh UI interactability/visuals from BehaviorManager central record
    public void RefreshState()
    {
        if (behaviorManager == null || data == null)
            return;

        bool alreadyUsed = behaviorManager.IsBehaviorUsedToday(data);
        bool canSelect = data.repeatable || !alreadyUsed;

        selectButton.interactable = canSelect;

        // If the behavior is a one-shot already used today, visually disable it
        if (alreadyUsed && !data.repeatable)
        {
            // change button text to indicate completed (assumes there's a TMP child)
            var tmp = selectButton.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = "Completed";

            ApplyGreyOut();
        }
        else
        {
            // restore normal label if available (use behavior name or "Select")
            var tmp = selectButton.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = "Select";
            RemoveGreyOut();
        }
    }

    private void ApplyGreyOut()
    {
        // If a dedicated overlay GameObject provided, use that
        if (greyOverlay != null)
        {
            greyOverlay.SetActive(true);
            return;
        }

        // Otherwise tint images/texts to look disabled
        foreach (var img in GetComponentsInChildren<Image>())
        {
            // skip overlay or other UI that shouldn't be tinted if necessary
            if (img == iconImage) // keep small icon slightly visible
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0.6f);
            else
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0.4f);
        }

        foreach (var tmp in GetComponentsInChildren<TMP_Text>())
        {
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0.5f);
        }
    }

    private void RemoveGreyOut()
    {
        if (greyOverlay != null)
        {
            greyOverlay.SetActive(false);
            return;
        }

        // Attempt to restore to fully opaque; if you need to preserve original colors,
        // consider caching them on Configure()
        foreach (var img in GetComponentsInChildren<Image>())
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        }

        foreach (var tmp in GetComponentsInChildren<TMP_Text>())
        {
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f);
        }
    }

    private void OnSelected()
    {
        if (behaviorManager == null || data == null)
            return;

        // prevent opening spoon panel / running behavior if one-shot already used today
        if (!data.repeatable && behaviorManager.IsBehaviorUsedToday(data))
        {
            behaviorManager?.ShowTooltip("You can't do that again today.");
            // Make sure the UI reflects the state
            RefreshState();
            return;
        }

        // If zero-cost behavior, run immediately
        if (data.spoonsCost <= 0)
        {
            StartBehaviorConfirm(null);
            return;
        }

        // If this choice already has an open panel, do nothing
        if (currentSpoonPanel != null)
            return;

        // Only one global SpoonPanel allowed at a time
        if (SpoonPanel.ActivePanel != null)
        {
            var old = SpoonPanel.ActivePanel;

            // Hard close instantly, skip animations + delays
            old.ForceCloseImmediate();

            // Now the static is definitely clear
            SpoonPanel.ActivePanel = null;
        }

        // Spawn new SpoonPanel
        if (spoonPanelPrefab == null)
        {
            Debug.LogWarning("BehaviorChoice: spoonPanelPrefab is null.");
            return;
        }

        var parent = panelAnchor != null ? panelAnchor : transform;
        var panelGO = Instantiate(spoonPanelPrefab, parent);
        var panel = panelGO.GetComponent<SpoonPanel>();

        if (panel == null)
        {
            Debug.LogWarning("BehaviorChoice: spawned spoonPanel prefab missing SpoonPanel component.");
            Destroy(panelGO);
            return;
        }

        // Setup panel
        panel.Setup(data, behaviorManager, this);
        currentSpoonPanel = panel;
    }

    // Called when panel confirms (“Do the Thing”)
    public void StartBehaviorConfirm(SpoonPanel panel)
    {
        if (progressRoutine != null)
            return;

        bool isInteractive = (data.thought != null &&
                              data.thought.type == ThoughtData.ThoughtType.Interactive);

        float seconds = Mathf.Max(
            0.2f,
            defaultOneShotSeconds * (data.durationMinutes > 0
                ? (data.durationMinutes / 30f)
                : 1f)
        );

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(true);
            progressRoutine = StartCoroutine(
                RunConfirmBarThenRun(seconds, isInteractive, panel)
            );
        }
        else
        {
            behaviorManager.RunBehavior(this);

            // Immediately reflect used state in UI so player cannot re-open SpoonPanel
            behaviorManager.MarkBehaviorUsed(data);
            RefreshState();

            if (panel != null)
                panel.ClosePanel();

            currentSpoonPanel = null;
        }
    }

    private IEnumerator RunConfirmBarThenRun(float seconds, bool isInteractive, SpoonPanel panel)
    {
        float elapsed = 0f;
        oneShotProgressBar.value = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            oneShotProgressBar.value = Mathf.Clamp01(elapsed / seconds);
            yield return null;
        }

        behaviorManager.RunBehavior(this);

        // Immediately reflect used state in UI so player cannot re-open SpoonPanel
        behaviorManager.MarkBehaviorUsed(data);
        RefreshState();

        if (panel != null)
            panel.ClosePanel();

        currentSpoonPanel = null;

        progressRoutine = null;
        oneShotProgressBar.value = 0f;
        oneShotProgressBar.gameObject.SetActive(false);
    }

    public void NotifyPanelClosed()
    {
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }

        currentSpoonPanel = null;
    }

    public void StartProgressFromPanel(SpoonPanel panel)
    {
        StartBehaviorConfirm(panel);
    }
}
