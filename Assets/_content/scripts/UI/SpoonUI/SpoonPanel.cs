using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpoonPanel : MonoBehaviour
{
    public static SpoonPanel ActivePanel;   // GLOBAL ACCESS

    [Header("Config")]
    public BehaviorData behaviorData;
    public BehaviorManager behaviorManager;
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Button cancelButton;
    public Button doThingButton;        // NEW: "Do the Thing" inside panel
    public TMP_Text spoonsUsedText;     // NEW: "Spoons Used: a / b"
    public Slider progressBar; // optional visual; mainly kept for legacy/visual feedback
    public float oneShotBaseSeconds = 0.6f;

    [HideInInspector] public List<SpoonSlot> slots = new List<SpoonSlot>();

    private int requiredSpoons = 0;
    private bool behaviorTriggered = false;

    // runtime: list of spoonBehavior objects that have been consumed/spent
    private List<spoonBehavior> spentSpoons = new List<spoonBehavior>();

    // owner
    private BehaviorChoice ownerChoice;

    void OnEnable()
    {
        ActivePanel = this;
    }

    void OnDisable()
    {
        if (ActivePanel == this)
            ActivePanel = null;
    }

    void Start()
    {
        if (behaviorData != null && behaviorManager != null)
            Setup(behaviorData, behaviorManager, ownerChoice);
    }

    // NOTE: setup now takes the owning BehaviorChoice so we can notify it
    public void Setup(BehaviorData data, BehaviorManager mgr, BehaviorChoice owner)
    {
        behaviorData = data;
        behaviorManager = mgr;
        ownerChoice = owner;

        requiredSpoons = Mathf.Max(0, data.spoonsCost);
        behaviorTriggered = false;

        foreach (Transform t in slotContainer)
            Destroy(t.gameObject);

        slots.Clear();
        spentSpoons.Clear();

        // Only ONE slot (this slot can accept multiple spoons)
        var go = Instantiate(slotPrefab, slotContainer);
        var slot = go.GetComponent<SpoonSlot>();
        slot.Initialize(this);
        slots.Add(slot);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(CancelPanel);

        if (doThingButton != null)
        {
            doThingButton.onClick.RemoveAllListeners();
            doThingButton.onClick.AddListener(OnDoTheThing);
            doThingButton.interactable = false;
        }

        if (spoonsUsedText != null)
            spoonsUsedText.text = $"Spoons Used: 0 / {requiredSpoons}";

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    // Called by a SpoonSlot when it accepts a spoon
    public void RegisterSpentSpoon(spoonBehavior spoon)
    {
        if (spoon == null) return;

        // Avoid double-register
        if (spentSpoons.Contains(spoon)) return;

        // Mark spoon spent (visual fade + deactivation)
        spoon.Spend();

        spentSpoons.Add(spoon);

        // Notify slot change logic
        OnSlotChanged();
    }

    public int CurrentFilledSpoons()
    {
        int count = 0;
        foreach (var s in slots)
            count += s.spoonCount;

        // Also include spent spoons if slotCount logic doesn't reflect them
        count += Mathf.Max(0, spentSpoons.Count - count);
        return count;
    }

    public void OnSlotChanged()
    {
        if (behaviorTriggered)
            return;

        int filled = CurrentFilledSpoons();

        // update UI text
        if (spoonsUsedText != null)
            spoonsUsedText.text = $"Spoons Used: {filled} / {requiredSpoons}";

        // enable do-the-thing once enough spoons present
        if (doThingButton != null)
            doThingButton.interactable = (filled >= requiredSpoons);

        if (filled >= requiredSpoons)
        {
            behaviorTriggered = true;
        }
    }

    // Do-the-Thing pressed inside the SpoonPanel
    private void OnDoTheThing()
    {
        if (ownerChoice != null)
        {
            ownerChoice.StartProgressFromPanel(this);
        }
        else
        {
            StartBehaviorRun();
        }
    }

    // Legacy fallback when ownerChoice isn't provided
    private void StartBehaviorRun()
    {
        if (behaviorManager == null)
        {
            Debug.LogError("SpoonPanel: BehaviorManager missing!");
            return;
        }

        bool isScene = behaviorData.isScene ||
                       (behaviorData.thought != null &&
                        behaviorData.thought.type == Thought.ThoughtType.Interactive);

        if (isScene)
        {
            behaviorManager.BeginSceneBehavior(behaviorData, this);
        }
        else
        {
            StartCoroutine(RunOneShot());
        }
    }

    private IEnumerator RunOneShot()
    {
        float seconds = oneShotBaseSeconds;

        if (behaviorData.durationMinutes > 0)
            seconds = Mathf.Max(0.2f, oneShotBaseSeconds * (behaviorData.durationMinutes / 30f));

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / seconds);

            yield return null;
        }

        behaviorManager.BeginOneShotBehavior(behaviorData, this);
    }

    // Called by user pressing Cancel
    public void CancelPanel()
    {
        // NEW — graceful closing, not instant destruction
        ClosePanel();
    }

    // NEW — called by BehaviorManager also
    public void ClosePanel()
    {
        StartCoroutine(CloseSequence());
    }

    // NEW — animates spoons back BEFORE destruction
    private IEnumerator CloseSequence()
    {
        RestoreSpentSpoons();

        // wait for spoon animations to finish (0.35–0.45s usually)
        yield return new WaitForSeconds(0.4f);

        behaviorManager?.ClearPanel(this);

        ownerChoice?.NotifyPanelClosed();

        Destroy(gameObject);
    }

    private void RestoreSpentSpoons()
    {
        if (spentSpoons == null || spentSpoons.Count == 0) return;

        foreach (var spoon in spentSpoons)
        {
            if (spoon == null) continue;

            // restore spoon: make it visible and animate it back into drawer
            spoon.RestoreFromSpend();    // This now handles reset-scale
        }

        spentSpoons.Clear();
    }

    void OnDestroy()
    {
        // extra failsafe so spoons never get stranded
        RestoreSpentSpoons();
    }
}
