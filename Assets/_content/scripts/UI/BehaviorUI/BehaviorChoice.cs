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
    public SpoonPanel currentSpoonPanel;
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
        if (behaviorManager == null || data == null)
            return;

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
