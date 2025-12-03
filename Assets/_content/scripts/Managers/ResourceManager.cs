// ResourceManager.cs (REPLACEMENT / DROP-IN)
// Notes: paste this over your existing ResourceManager. It keeps your existing fields and behavior
// but adds clear public APIs, Yarn commands, and a Shutdown/Doomscroll coroutine.

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Hunger")]
    public Slider hungerBar;
    public TMP_Text hungerText;
    public int maxHunger = 100;
    private int currentHunger;

    [Header("Stress")]
    public Slider stressBar;
    public TMP_Text stressText;
    public int maxStress = 100;
    private int currentStress;
    [Tooltip("Soft threshold where you might show UI warnings, independent of maxStress.")]
    private int stressThreshold = 40;
    private bool stressedOut = false;

    [Header("Hope")]
    public Slider hopeBar;
    public TMP_Text hopeText;
    public TMP_Text hopeLevelText;
    public int maxHope = 100;
    private int currentHope;
    public int hopeLevel = 1;
    private int hopeLevelUpThreshold = 5; // number of XP to gain a hopeLevel (adjust to design)

    [Header("Spoons")]
    public SpoonDrawer spoonDrawer;
    public int maxSpoons = 10;
    public int currentSpoons;

    [Header("Cash")]
    public int cash = 20;
    public int cashNeededForRent = 100;

    [Header("Shutdown / Doomscroll settings")]
    [Tooltip("When stress == maxStress, enter shutdown mode.")]
    public bool isShutdownMode = false;
    public float doomscrollTickSeconds = 1.0f;
    public int doomscrollStressRecoveryPerTick = 1; // stress reduced per tick while doomscrolling
    public int doomscrollHopeDrainPerTick = 1;     // hope lost per tick while doomscrolling

    private bool uiInitialized = false;

    // --- Lifecycle ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // default starting values (tweak as you like)
        currentHunger = 70;
        currentStress = 0;
        currentHope = 0;
        hopeLevel = 0;

        SetupBars();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeSceneUI());
    }

    private IEnumerator InitializeSceneUI()
    {
        yield return null; // wait a frame for UI objects to exist

        // Rebind any missing UI references
        if (hungerBar == null || stressBar == null || hopeBar == null)
        {
            var sliders = FindObjectsOfType<Slider>();
            foreach (var s in sliders)
            {
                var name = s.name.ToLower();
                if (name.Contains("hunger")) hungerBar = s;
                else if (name.Contains("stress")) stressBar = s;
                else if (name.Contains("hope")) hopeBar = s;
            }
        }

        var texts = FindObjectsOfType<TMP_Text>();
        foreach (var t in texts)
        {
            var n = t.name.ToLower();
            if (n.Contains("hunger") && hungerText == null) hungerText = t;
            else if (n.Contains("stress") && stressText == null) stressText = t;
            else if (n.Contains("hope") && hopeText == null) hopeText = t;
        }

        if (spoonDrawer == null)
            spoonDrawer = FindObjectOfType<SpoonDrawer>();

        if (spoonDrawer == null)
        {
            Debug.LogWarning("ResourceManager: No SpoonDrawer found in this scene.");
            // continue — spoon drawer optional for scenes without it
        }

        uiInitialized = true;

        LoadDailySpoons();
        UpdateUI();
    }

    private void Update()
    {
        DebugControls();
        CheckStressThreshold();
    }

    // --- Setup & Update UI ---
    void SetupBars()
    {
        if (hungerBar != null)
        {
            hungerBar.maxValue = maxHunger;
            hungerBar.value = currentHunger;
        }

        if (stressBar != null)
        {
            stressBar.maxValue = maxStress;
            stressBar.value = currentStress;
        }

        if (hopeBar != null)
        {
            hopeBar.maxValue = maxHope;
            hopeBar.value = currentHope;
        }
    }

    void UpdateUI()
    {
        if (!uiInitialized) return;

        if (hungerBar != null)
        {
            hungerBar.value = currentHunger;
            if (hungerText) hungerText.text = $"{currentHunger}/{maxHunger}";
        }

        if (stressBar != null)
        {
            stressBar.value = currentStress;
            if (stressText) stressText.text = $"{currentStress}/{maxStress}";
        }

        if (hopeBar != null)
        {
            hopeBar.value = currentHope;
            if (hopeText) hopeText.text = $"{currentHope}/{hopeLevelUpThreshold}";
            if (hopeLevelText) hopeLevelText.text = $"{hopeLevel}";
        }

        // Stress threshold tinting (keeps your original colors)
        if (stressBar?.fillRect != null)
        {
            Image stressFill = stressBar.fillRect.GetComponent<Image>();
            if (stressFill)
            {
                float stressPct = (float)currentStress / maxStress;
                if (stressPct >= 1f) stressFill.color = Color.black;
                else if (stressPct >= 0.8f) stressFill.color = Color.red;
                else if (stressPct >= 0.6f) stressFill.color = new Color(1f, 0.5f, 0f);
                else if (stressPct >= 0.4f) stressFill.color = Color.yellow;
                else stressFill.color = Color.green;
            }
        }
    }

    // --- Core Logic / Public APIs ---

    // SPOONS
    /// <summary>
    /// Change the current spoons by delta. Positive adds spoons, negative consumes spoons.
    /// Clamped to [0, maxSpoons]. Updates the SpoonDrawer immediately if present.
    /// </summary>
    public void ModifySpoons(int delta)
    {
        currentSpoons = Mathf.Clamp(currentSpoons + delta, 0, maxSpoons);
        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);

        UpdateUI();
    }

    /// <summary>
    /// Backwards-compatible multi-purpose modifier. Use explicit ModifySpoons/ModifyStress/ModifyHope when possible.
    /// spoonDelta/hungerDelta/cashDelta are applied as additive deltas (positive = increase).
    /// </summary>
    public void ModifyResources(float spoonDelta, float hungerDelta, float cashDelta)
    {
        // Spoon: convert float to int delta
        if (spoonDelta != 0f)
            ModifySpoons(Mathf.RoundToInt(spoonDelta));

        if (hungerDelta != 0f)
        {
            currentHunger = Mathf.Clamp(currentHunger + Mathf.RoundToInt(hungerDelta), 0, maxHunger);
        }

        if (cashDelta != 0f)
        {
            cash = Mathf.Clamp(cash + Mathf.RoundToInt(cashDelta), int.MinValue, int.MaxValue);
        }

        UpdateUI();
    }

    // STRESS
    /// <summary>
    /// Change Stress by delta. Positive increases stress. If stress reaches maxStress, Shutdown mode triggers.
    /// </summary>
    public void ModifyStress(int delta)
    {
        if (isShutdownMode) return; // don't allow external increases while shutdown is running

        currentStress = Mathf.Clamp(currentStress + delta, 0, maxStress);
        UpdateUI();

        if (currentStress >= maxStress && !isShutdownMode)
        {
            StartShutdownMode();
        }
    }

    // HOPE
    /// <summary>
    /// Change Hope XP by delta. Positive increases XP. Will check for level-up.
    /// </summary>
    public void ModifyHope(int deltaXP)
    {
        currentHope = Mathf.Clamp(currentHope + deltaXP, 0, maxHope);
        UpdateUI();
        CheckHopeThreshold();
    }

    /// <summary>
    /// Check if currentHope meets the threshold for a level-up; if so increase hopeLevel and expand max spoons.
    /// </summary>
    public void CheckHopeThreshold()
    {
        while (currentHope >= hopeLevelUpThreshold)
        {
            currentHope -= hopeLevelUpThreshold;
            hopeLevel++;
            // Example effect on leveling: increase max spoons by 1 (tweak design as you like)
            maxSpoons = Mathf.Min(20, maxSpoons + 1);
            Debug.Log($"Hope level up! New level: {hopeLevel}. maxSpoons => {maxSpoons}");
            // Visual feedback hook: you can instantiate level-up VFX here, or signal UI
        }
        UpdateUI();
    }

    public int GetCurrentSpoons() => currentSpoons;

    /// <summary>
    /// Attempt to spend `amount` spoons. Returns true only if the player had enough spoons.
    /// If successful, deducts the spoons and refreshes SpoonDrawer.
    /// </summary>
    public bool TrySpendSpoons(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"TrySpendSpoons called with non-positive amount: {amount}");
            return false;
        }

        Debug.Log($"TrySpendSpoons: have {currentSpoons}, need {amount}");

        if (currentSpoons < amount)
        {
            // not enough spoons
            Debug.Log($"TrySpendSpoons: not enough spoons (have {currentSpoons}, need {amount})");
            return false;
        }

        // deduct and update UI/drawer
        currentSpoons = Mathf.Clamp(currentSpoons - amount, 0, maxSpoons);
        Debug.Log($"TrySpendSpoons: success — new currentSpoons = {currentSpoons}");

        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);

        UpdateUI();
        return true;
    }

    // DAILY SLOTS
    public void LoadDailySpoons()
    {
        float hungerPercent = (float)currentHunger / maxHunger;
        int baseSpoons = Mathf.RoundToInt(hungerPercent * maxSpoons);
        int randomVariance = Random.Range(-3, 2);
        currentSpoons = Mathf.Clamp(baseSpoons + randomVariance, 1, maxSpoons);

        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);

        UpdateUI();
    }

    // --- Shutdown / Doomscroll Mode ---
    private void StartShutdownMode()
    {
        if (isShutdownMode) return;
        isShutdownMode = true;
        Debug.Log("Entering Shutdown/Doomscroll mode due to max stress.");
        // Optional: lock inputs globally (your own InputManager or UI blocker should be used here).
        StartCoroutine(DoomscrollCoroutine());
    }

    private IEnumerator DoomscrollCoroutine()
    {
        // This simple implementation reduces stress and drains hope until stress is zero.
        while (currentStress > 0)
        {
            yield return new WaitForSeconds(doomscrollTickSeconds);

            // Recover a bit of stress each tick
            currentStress = Mathf.Max(0, currentStress - doomscrollStressRecoveryPerTick);

            // Drain hope as penalty
            currentHope = Mathf.Max(0, currentHope - doomscrollHopeDrainPerTick);

            UpdateUI();
        }

        // Fully recovered
        isShutdownMode = false;
        Debug.Log("Shutdown/Doomscroll ended; stress is zero.");
        UpdateUI();
    }

    // Simple threshold logging
    void CheckStressThreshold()
    {
        if ((currentStress >= stressThreshold) && (!stressedOut))
        {
            Debug.Log($"Hiki is stressed: {currentStress}/{maxStress} >= threshold {stressThreshold}.");
            stressedOut = true;
            // TODO: hook UI warning flash/effect
        }

        if (stressedOut && (currentStress < stressThreshold))
        {
            Debug.Log("Hiki is no longer in the stress warning region.");
            stressedOut = false;
        }
    }

    // --- Debug Keys (updated to use clear APIs) ---
    void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ModifyResources(0, -10, 0); // hunger -10
        if (Input.GetKeyDown(KeyCode.Alpha2)) ModifyResources(0, 10, 0);  // hunger +10
        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyStress(-10);          // stress -10
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyStress(10);           // stress +10
        if (Input.GetKeyDown(KeyCode.Alpha5)) ModifySpoons(-1);           // consume 1 spoon
        if (Input.GetKeyDown(KeyCode.Alpha6)) ModifySpoons(1);            // gain 1 spoon
    }

    // --- Optional helpers for debugging from other scripts ---
    public int GetCurrentStress() => currentStress;
    public int GetCurrentHope() => currentHope;
    public int GetHopeLevel() => hopeLevel;
}
