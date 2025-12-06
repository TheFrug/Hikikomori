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
    public Button doThingButton;
    public TMP_Text spoonsUsedText;
    public Slider progressBar; // optional visual; mainly kept for legacy/visual feedback
    public float oneShotBaseSeconds = 0.6f;

    [HideInInspector] public List<SpoonSlot> slots = new List<SpoonSlot>();

    private int requiredSpoons = 0;
    private bool behaviorTriggered = false;

    // runtime: list of spoonBehavior objects that have been consumed/staged
    private List<spoonBehavior> stagedSpoons = new List<spoonBehavior>();

    // owner
    private BehaviorChoice ownerChoice;

    // Whether player confirmed (Do the Thing) — used to decide destroy vs restore
    private bool committed = false;

    // ----- Lifecycle -----
    void OnEnable()
    {
        ActivePanel = this;
    }

    void OnDisable()
    {
        if (ActivePanel == this)
            ActivePanel = null;
    }

    // Prevent relying on Start for setup. Setup must be explicitly called by owner.
    void Start()
    {
        // If data was pre-populated and ownerChoice set before Start, ensure it's initialized.
        if (behaviorData != null && behaviorManager != null && ownerChoice != null)
        {
            Setup(behaviorData, behaviorManager, ownerChoice);
        }
    }

    /// <summary>
    /// Initialize the panel. Must be called by the code that spawns this panel (BehaviorChoice).
    /// </summary>
    public void Setup(BehaviorData data, BehaviorManager mgr, BehaviorChoice owner)
    {
        if (data == null || mgr == null)
        {
            Debug.LogError("SpoonPanel.Setup called with null data or manager.");
            return;
        }

        behaviorData = data;
        behaviorManager = mgr;
        ownerChoice = owner;

        requiredSpoons = Mathf.Max(0, data.spoonsCost);
        behaviorTriggered = false;
        committed = false;

        // Clear old slots/spoons in the UI
        foreach (Transform t in slotContainer)
            Destroy(t.gameObject);

        slots.Clear();
        stagedSpoons.Clear();

        // Only ONE slot (this slot can accept multiple spoons)
        var go = Instantiate(slotPrefab, slotContainer);
        var slot = go.GetComponent<SpoonSlot>();
        if (slot == null)
        {
            Debug.LogError("SpoonPanel.Setup: slotPrefab does not contain SpoonSlot.");
        }
        else
        {
            slot.Initialize(this);
            slots.Add(slot);
        }

        // Wire buttons safely
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelPanel);
        }

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
        if (stagedSpoons.Contains(spoon)) return;

        // Mark spoon visually spent (fade + deactivate) — keep the GameObject for possible restore
        // This preserves the user's expectation that the spoon disappears at time of dragging.
        spoon.Spend();

        // Add to staged list for later commit/restore
        stagedSpoons.Add(spoon);

        // Notify slot change logic
        OnSlotChanged();
    }

    public int CurrentFilledSpoons()
    {
        int slotCount = 0;
        foreach (var s in slots)
            slotCount += s.spoonCount;

        // stagedSpoons holds the actual consumed/staged spoon objects.
        // Return the maximum of the two to handle any sync mismatches.
        return Mathf.Max(slotCount, stagedSpoons.Count);
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
            // Optionally we could auto-trigger UI animation / highlight here
        }
    }

    // Do-the-Thing pressed inside the SpoonPanel
    private void OnDoTheThing()
    {
        if (behaviorTriggered == false && CurrentFilledSpoons() < requiredSpoons)
        {
            Debug.LogWarning("SpoonPanel: DoTheThing pressed but not enough spoons.");
            return;
        }

        // mark committed so CloseSequence won't restore staged spoons
        committed = true;

        // If ownerChoice is present, let it drive the behavior (preferred)
        if (ownerChoice != null)
        {
            ownerChoice.StartProgressFromPanel(this);
            return;
        }

        // Fallback: start behavior run directly using stored BehaviorData and BehaviorManager
        StartBehaviorRun();
    }

    // Legacy fallback when ownerChoice isn't provided
    private void StartBehaviorRun()
    {
        if (behaviorManager == null)
        {
            Debug.LogError("SpoonPanel: BehaviorManager missing!");
            return;
        }

        bool isScene = (behaviorData.thought != null &&
                        behaviorData.thought.type == ThoughtData.ThoughtType.Interactive);

        if (isScene)
        {
            // If we don't have a real BehaviorChoice, we can't fully validate via BehaviorManager.
            // This mirrors the old fallback behavior but is fragile — better to call ownerChoice.
            behaviorManager.RunBehavior(null);
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

        behaviorManager.RunBehavior(ownerChoice);
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
        if (committed)
        {
            // If confirmed, destroy staged spoon GOs so they don't get restored and
            // duplicate with the newly spawned drawer spoons (spawned by ResourceManager).
            if (stagedSpoons != null && stagedSpoons.Count > 0)
            {
                foreach (var spoon in stagedSpoons)
                {
                    if (spoon == null) continue;
                    // destroy the GO immediately; SpoonDrawer.RefreshDrawer will recreate canonical visuals
                    Destroy(spoon.gameObject);
                }
                stagedSpoons.Clear();
            }
        }
        else
        {
            // Cancel flow — return staged spoons to drawer visually
            RestoreSpentSpoons();
        }

        // wait for spoon animations to finish (0.35–0.45s usually)
        yield return new WaitForSeconds(0.4f);

        behaviorManager?.ClearPanel(this);

        ownerChoice?.NotifyPanelClosed();

        Destroy(gameObject);
    }

    private void RestoreSpentSpoons()
    {
        if (stagedSpoons == null || stagedSpoons.Count == 0) return;

        foreach (var spoon in stagedSpoons)
        {
            if (spoon == null) continue;

            // restore spoon: make it visible and animate it back into drawer
            spoon.RestoreFromSpend();    // This now handles reset-scale
        }

        stagedSpoons.Clear();

        // Reset slot counts (slot visuals may have their own children — ForceReturnSpoon handles that)
        foreach (var slot in slots)
            slot.ForceReturnSpoon();
    }

    void OnDestroy()
    {
        // extra failsafe so spoons never get stranded
        RestoreSpentSpoons();
    }

    // Utility for tests / editor: immediate force close (skips animation)
    public void ForceCloseImmediate()
    {
        // restore spoons synchronously if not committed
        if (!committed)
        {
            if (stagedSpoons != null)
            {
                foreach (var spoon in stagedSpoons)
                {
                    if (spoon == null) continue;
                    spoon.RestoreFromSpend();
                }
                stagedSpoons.Clear();
            }
        }
        else
        {
            // committed -> destroy
            if (stagedSpoons != null)
            {
                foreach (var spoon in stagedSpoons)
                {
                    if (spoon == null) continue;
                    Destroy(spoon.gameObject);
                }
                stagedSpoons.Clear();
            }
        }

        behaviorManager?.ClearPanel(this);
        ownerChoice?.NotifyPanelClosed();
        Destroy(gameObject);
    }
}