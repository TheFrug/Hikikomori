using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpoonPanel : MonoBehaviour
{
    public static SpoonPanel ActivePanel;

    [Header("Config")]
    public BehaviorData behaviorData;
    public BehaviorManager behaviorManager;
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Button cancelButton;
    public Button doThingButton;
    public TMP_Text spoonsUsedText;
    public Slider progressBar;
    public float oneShotBaseSeconds = 0.6f;

    [HideInInspector] public List<SpoonSlot> slots = new List<SpoonSlot>();

    private int requiredSpoons = 0;
    private bool behaviorTriggered = false;

    private List<spoonBehavior> stagedSpoons = new List<spoonBehavior>();

    private BehaviorChoice ownerChoice;
    private bool committed = false;

    private bool panelClosed = false;

    private bool closeInProgress = false;

    private BehaviorIconRoomController iconController;
    private UIStateController uiState;
    private Tab SpoonDrawerTab;

    void OnEnable()
    {
        ActivePanel = this;
        iconController = FindObjectOfType<BehaviorIconRoomController>();
        uiState = UIStateController.Instance;

        var drawer = GameObject.Find("p_SpoonDrawer"); // rename if needed
        if (drawer != null)
            SpoonDrawerTab = drawer.GetComponent<Tab>();
    }

    void OnDisable()
    {
        if (ActivePanel == this)
            ActivePanel = null;
    }

    void Start()
    {
        if (behaviorData != null && behaviorManager != null && ownerChoice != null)
        {
            Setup(behaviorData, behaviorManager, ownerChoice);
        }
    }

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
        panelClosed = false;
        closeInProgress = false;

        // clear any existing UI
        foreach (Transform t in slotContainer)
            Destroy(t.gameObject);

        slots.Clear();
        stagedSpoons.Clear();

        // Create exactly requiredSpoons slots (one per required spoon)
        // If requiredSpoons == 0, still create one slot visually so UI doesn't break
        int spawnCount = Mathf.Max(1, requiredSpoons);
        for (int i = 0; i < spawnCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            var slot = go.GetComponent<SpoonSlot>();
            if (slot == null)
            {
                Debug.LogWarning("SpoonPanel: slotPrefab missing SpoonSlot component. Using placeholder.");
                // create a placeholder SpoonSlot if needed
                slot = go.AddComponent<SpoonSlot>();
            }
            slot.Initialize(this);
            // ensure slot starts empty visually
            TrySetSlotFilledVisual(slot, false);
            slots.Add(slot);
        }

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

    // -------------------------------------------------------------
    // Slot & Spoon registration
    // -------------------------------------------------------------

    public void RegisterSpentSpoon(spoonBehavior spoon)
    {
        if (spoon == null) return;
        if (stagedSpoons.Contains(spoon)) return;

        int filled = CurrentFilledSpoons();

        // Prevent overfilling: if we've already enough, restore the spoon immediately
        if (filled >= requiredSpoons)
        {
            // Give immediate feedback by restoring spoon
            spoon.RestoreFromSpend();
            return;
        }

        // Accept this spoon
        spoon.Spend(); // fade + deactivate
        stagedSpoons.Add(spoon);

        // Update the slot visuals: mark the next empty slot filled
        UpdateSlotVisuals();

        OnSlotChanged();
    }

    // CurrentFilledSpoons is determined by accepted staged spoons (one spoon per slot)
    public int CurrentFilledSpoons()
    {
        return stagedSpoons != null ? stagedSpoons.Count : 0;
    }

    public void OnSlotChanged()
    {
        if (behaviorTriggered) return;

        int filled = CurrentFilledSpoons();

        if (spoonsUsedText != null)
            spoonsUsedText.text = $"Spoons Used: {filled} / {requiredSpoons}";

        if (doThingButton != null)
            doThingButton.interactable = (filled >= requiredSpoons);

        if (filled >= requiredSpoons)
            behaviorTriggered = true;
    }

    // -------------------------------------------------------------
    // Update slot visuals based on stagedSpoons count
    // -------------------------------------------------------------
    private void UpdateSlotVisuals()
    {
        int filled = CurrentFilledSpoons();

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            bool shouldBeFilled = i < filled;
            TrySetSlotFilledVisual(slot, shouldBeFilled);
        }
    }

    // Tries several strategies to set a slot's visual filled/unfilled.
    // - If SpoonSlot exposes SetFilled(bool) method, calls it.
    // - Otherwise tries to find child Image named "filled"/"img_filled"/"fill"/"img_fill" and enable/disable it.
    private void TrySetSlotFilledVisual(SpoonSlot slot, bool filled)
    {
        if (slot == null) return;

        // Preferred: SpoonSlot implements SetFilled(bool)
        var slotType = slot.GetType();
        var setFilledMethod = slotType.GetMethod("SetFilled");
        if (setFilledMethod != null)
        {
            setFilledMethod.Invoke(slot, new object[] { filled });
            return;
        }

        // Fallback: search for child images by name
        var images = slot.GetComponentsInChildren<Image>(true);
        if (images != null && images.Length > 0)
        {
            // look for explicit "filled" image
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null) continue;
                string n = img.gameObject.name.ToLowerInvariant();
                if (n.Contains("filled") || n.Contains("img_filled") || n.Contains("fill") || n.Contains("img_fill"))
                {
                    img.gameObject.SetActive(filled);
                    // Also try to find an "empty" image sibling and invert
                    var parent = img.transform.parent;
                    if (parent != null)
                    {
                        foreach (Transform child in parent)
                        {
                            var cimg = child.GetComponent<Image>();
                            if (cimg != null && child.gameObject != img.gameObject)
                            {
                                // If this child looks like an empty indicator, toggle opposite
                                string cn = child.gameObject.name.ToLowerInvariant();
                                if (cn.Contains("empty") || cn.Contains("silhouette") || cn.Contains("bg") || cn.Contains("outline"))
                                {
                                    child.gameObject.SetActive(!filled);
                                }
                            }
                        }
                    }
                    return;
                }
            }

            // If no named filled image found, as a last resort toggle the first child image's alpha
            var fallbackImg = images[0];
            if (fallbackImg != null)
            {
                var c = fallbackImg.color;
                c.a = filled ? 1f : 0.25f;
                fallbackImg.color = c;
            }
            return;
        }
    }

    // -------------------------------------------------------------
    // Confirm behavior
    // -------------------------------------------------------------

    private void OnDoTheThing()
    {
        if (!behaviorTriggered && CurrentFilledSpoons() < requiredSpoons)
        {
            Debug.LogWarning("DoTheThing pressed but not enough spoons.");
            return;
        }

        committed = true;

        // CLOSE SPOON DRAWER TAB IF FOUND
        if (SpoonDrawerTab != null)
            SpoonDrawerTab.CloseIfOpen();

        if (ownerChoice != null)
        {
            ownerChoice.StartProgressFromPanel(this);
            return;
        }

        StartBehaviorRun();
    }

    private void StartBehaviorRun()
    {
        if (behaviorManager == null)
        {
            Debug.LogError("BehaviorManager missing!");
            return;
        }

        bool isScene = (behaviorData.thought != null &&
                        behaviorData.thought.type == ThoughtData.ThoughtType.Interactive);

        if (isScene)
        {
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

    // -------------------------------------------------------------
    // Closing logic
    // -------------------------------------------------------------

    public void CancelPanel()
    {
        ClosePanel();
    }

    public void ClosePanel()
    {
        if (closeInProgress) return;
        closeInProgress = true;
        StartCoroutine(CloseSequence());
    }

    private IEnumerator CloseSequence()
    {
        if (panelClosed) yield break;
        panelClosed = true;

        bool hadSpentSpoons = (stagedSpoons != null && stagedSpoons.Count > 0);

        if (committed)
        {
            foreach (var spoon in stagedSpoons)
                if (spoon != null) Destroy(spoon.gameObject);
        }
        else
        {
            RestoreSpentSpoons();
        }

        // Skip the delay if no spoons spent
        if (!hadSpentSpoons)
        {
            behaviorManager?.ClearPanel(this);
            ownerChoice?.NotifyPanelClosed();
            ownerChoice.currentSpoonPanel = null;
            Destroy(gameObject);
            yield break;
        }

        // Otherwise do the normal visual delay
        yield return new WaitForSeconds(0.4f);

        behaviorManager?.ClearPanel(this);
        ownerChoice?.NotifyPanelClosed();
        ownerChoice.currentSpoonPanel = null;
        Destroy(gameObject);
    }

    private void RestoreSpentSpoons()
    {
        if (stagedSpoons == null || stagedSpoons.Count == 0) return;

        foreach (var spoon in stagedSpoons)
        {
            if (spoon == null) continue;
            spoon.RestoreFromSpend();
        }

        stagedSpoons.Clear();

        // Reset slot visuals
        foreach (var slot in slots)
            TrySetSlotFilledVisual(slot, false);
    }

    void OnDestroy()
    {
        if (!panelClosed)
        {
            // Only restore if we were NOT properly closed by CloseSequence
            if (!committed)
                RestoreSpentSpoons();
            // If committed, do nothing — spoons should remain destroyed
        }
    }

    // -------------------------------------------------------------
    // Utility options
    // -------------------------------------------------------------

    // NEW — purely visual hide, does NOT close the panel
    public void HideImmediately()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Immediate, full close — skips animations
    public void ForceCloseImmediate()
    {
        if (panelClosed) return;
        panelClosed = true;

        if (!committed)
        {
            foreach (var spoon in stagedSpoons)
                spoon?.RestoreFromSpend();
        }
        else
        {
            foreach (var spoon in stagedSpoons)
                if (spoon != null) Destroy(spoon.gameObject);
        }

        stagedSpoons.Clear();

        behaviorManager?.ClearPanel(this);
        ownerChoice?.NotifyPanelClosed();
        if (ownerChoice != null)
            ownerChoice.currentSpoonPanel = null;

        Destroy(gameObject);
    }
}
