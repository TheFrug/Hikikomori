using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
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
    private int stressThreshold = 40;
    private bool stressedOut = false;

    [Header("Spoons")]
    public Slider spoonBar;
    public TMP_Text spoonText;
    public int maxSpoons = 10;
    private int currentSpoons;

    void Start()
    {
        // Initialize
        currentHunger = maxHunger;
        currentStress = 0;
        currentSpoons = Random.Range(3, maxSpoons + 1);

        SetupBars();
    }

    private void Update()
    {
        DebugControls();
        UpdateUI();
        checkStress();
    }

    void SetupBars()
    {
        hungerBar.maxValue = maxHunger;
        hungerBar.value = currentHunger;

        stressBar.maxValue = maxStress;
        stressBar.value = currentStress;

        spoonBar.maxValue = maxSpoons;
        spoonBar.value = currentSpoons;
    }

    void UpdateUI()
    {
        hungerBar.value = currentHunger;
        stressBar.value = currentStress;
        spoonBar.value = currentSpoons;

        if (hungerText) hungerText.text = $"{currentHunger}/{maxHunger}";
        if (stressText) stressText.text = $"{currentStress}/{maxStress}";
        if (spoonText) spoonText.text = $"{currentSpoons}/{maxSpoons}";

        // Stress threshold tinting
        Image stressFill = stressBar.fillRect.GetComponent<Image>();
        float stressPct = (float)currentStress / maxStress;
        if (stressPct >= 1f) stressFill.color = Color.black; // catastrophic
        else if (stressPct >= 0.8f) stressFill.color = Color.red;
        else if (stressPct >= 0.6f) stressFill.color = new Color(1f, 0.5f, 0f); // orange
        else if (stressPct >= 0.4f) stressFill.color = Color.yellow;
        else stressFill.color = Color.green;
    }

    void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentHunger = Mathf.Max(0, currentHunger - 10);
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentHunger = Mathf.Min(maxHunger, currentHunger + 10);

        if (Input.GetKeyDown(KeyCode.Alpha3)) currentStress = Mathf.Max(0, currentStress - 10);
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentStress = Mathf.Min(maxStress, currentStress + 10);

        if (Input.GetKeyDown(KeyCode.Alpha5)) currentSpoons = Mathf.Max(0, currentSpoons - 1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) currentSpoons = Mathf.Min(maxSpoons, currentSpoons + 1);
    }

    void checkStress()
    {
        if ((currentStress >= stressThreshold) && (!stressedOut))
        {
            Debug.Log("Hiki is stressed out! Current stress levels (" + currentStress + ") excedes Hiki's functional limit of 40.");
            stressedOut = true;
        }

        if ((stressedOut) && (currentStress < stressThreshold))
        {
            Debug.Log("Hiki is no longer stressed out");
            stressedOut = false;
        }
    }
}
