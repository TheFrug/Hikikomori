// BehaviorChoice.cs
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
    public Image iconImage;

    [Header("Hook-ins")]
    public Button selectButton;
    public BehaviorManager behaviorManager;
    [SerializeField] public GameObject spoonPanelPrefab;
    [SerializeField] public Transform panelAnchor;

    [Header("One-shot UI")]
    public Slider oneShotProgressBar;
    public float defaultOneShotSeconds = 0.6f;

    private BehaviorData data;
    private SpoonPanel currentSpoonPanel;
    private Coroutine progressRoutine;
    private ThoughtData myThought;

    public bool usedToday = false;

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

        if (iconImage != null)
            iconImage.sprite = behaviorData.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelected);

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }
    }

    private void OnSelected()
    {
        if (behaviorManager == null || data == null) return;

        // If no spoon cost, start immediately
        if (data.spoonsCost <= 0)
        {
            StartBehaviorConfirm(null);
            return;
        }

        // If a panel for this choice is already open, keep it (toggle close handled by panel)
        if (currentSpoonPanel != null)
        {
            // Panel already open — do nothing (panel has its own controls)
            return;
        }

        // Spawn a new panel anchored under panelAnchor or this transform
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

        // Panel will call back to this BehaviorChoice (NotifyPanelClosed / StartBehaviorConfirm)
        panel.Setup(data, behaviorManager, this);
        currentSpoonPanel = panel;
    }

    // Called by SpoonPanel when the player confirms/presses the "Do the Thing" button
    public void StartBehaviorConfirm(SpoonPanel panel)
    {
        if (progressRoutine != null) return;

        bool isInteractive = (data.thought != null && data.thought.type == ThoughtData.ThoughtType.Interactive);

        float seconds = Mathf.Max(
            0.2f,
            defaultOneShotSeconds * (data.durationMinutes > 0 ? (data.durationMinutes / 30f) : 1f)
        );

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(true);
            progressRoutine = StartCoroutine(RunConfirmBarThenRun(seconds, isInteractive, panel));
        }
        else
        {
            // Directly ask BehaviorManager to run the behavior (unified entry point)
            behaviorManager.RunBehavior(this);
            // close panel after requesting run (panel usually closes itself)
            if (panel != null) panel.ClosePanel();
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

        // After confirmation bar completes, tell BehaviorManager to run this BehaviorChoice
        behaviorManager.RunBehavior(this);

        // Close panel if present
        if (panel != null) panel.ClosePanel();
        currentSpoonPanel = null;

        progressRoutine = null;
        oneShotProgressBar.value = 0f;
        oneShotProgressBar.gameObject.SetActive(false);
    }

    // Called by SpoonPanel when it closes (user cancelled or finished)
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

    // Compatibility wrapper: old code calls StartProgressFromPanel(panel)
    public void StartProgressFromPanel(SpoonPanel panel)
    {
        // Delegate to the new method (StartBehaviorConfirm / StartProgressFromPanel semantics)
        StartBehaviorConfirm(panel); // if your method name is StartBehaviorConfirm
    }

    string FormatDuration(int minutes)
    {
        if (minutes <= 0) return "Time: <1m";
        int h = minutes / 60;
        int m = minutes % 60;
        if (h > 0) return $"Time: {h}h {m}m";
        return $"Time: {m}m";
    }
}
