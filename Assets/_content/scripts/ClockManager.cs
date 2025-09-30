using UnityEngine;
using TMPro;

public class ClockManager : MonoBehaviour
{
    [Header("Clock Settings")]
    public TMP_Text clockText;
    public int startHour = 14; // 2:00 PM
    public int startMinute = 0;
    public float realSecondsPerGameTick = 6f; // every 6 seconds
    public int minutesPerTick = 10;           // advance 10 minutes per tick

    private int currentHour;
    private int currentMinute;
    private float timer;

    private bool isPaused = false;
    private float speedMultiplier = 1f; // 1x = normal, 2x = fast-forward

    void Start()
    {
        currentHour = startHour;
        currentMinute = startMinute;
        UpdateClockUI();
    }

    void Update()
    {
        DebugControls(); // Always check input

        if (isPaused) return; // Only stop time progression

        timer += Time.deltaTime * speedMultiplier;

        if (timer >= realSecondsPerGameTick)
        {
            timer = 0f;
            AdvanceTime(minutesPerTick);
        }
    }


    void AdvanceTime(int minutesToAdd)
    {
        currentMinute += minutesToAdd;

        while (currentMinute >= 60)
        {
            currentMinute -= 60;
            currentHour++;
        }

        if (currentHour >= 24)
            currentHour -= 24; // wrap around midnight

        UpdateClockUI();
    }

    void UpdateClockUI()
    {
        if (clockText != null)
        {
            string suffix = currentHour >= 12 ? "PM" : "AM";
            int displayHour = currentHour % 12;
            if (displayHour == 0) displayHour = 12;

            // Four-digit formatting (e.g., 02:00 PM instead of 2:00 PM)
            clockText.text = string.Format("{0:00}:{1:00} {2}", displayHour, currentMinute, suffix);
        }
    }

    // Debug controls for testing
    void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.P)) PauseClock();
        if (Input.GetKeyDown(KeyCode.O)) PlayClock();
        if (Input.GetKeyDown(KeyCode.F)) FastForward(2f); // 2x speed
        if (Input.GetKeyDown(KeyCode.N)) NormalSpeed();
    }

    public void PauseClock()
    {
        isPaused = true;
        Debug.Log("Clock paused at " + GetFormattedTime());
    }

    public void PlayClock()
    {
        isPaused = false;
        Debug.Log("Clock resumed at " + GetFormattedTime());
    }

    public void FastForward(float multiplier)
    {
        isPaused = false;
        speedMultiplier = multiplier;
        Debug.Log("Clock fast-forwarding at " + multiplier + "x speed. Current time: " + GetFormattedTime());
    }

    public void NormalSpeed()
    {
        speedMultiplier = 1f;
        isPaused = false;
        Debug.Log("Clock running at normal speed. Current time: " + GetFormattedTime());
    }

    private string GetFormattedTime()
    {
        string suffix = currentHour >= 12 ? "PM" : "AM";
        int displayHour = currentHour % 12;
        if (displayHour == 0) displayHour = 12;

        return string.Format("{0:00}:{1:00} {2}", displayHour, currentMinute, suffix);
    }
}
