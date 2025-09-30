using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClockSystem : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text clockText;
    public Button playButton;
    public Button pauseButton;
    public Button fastForwardButton;

    [Header("Clock Settings")]
    public float realSecondsPerGameTick = 6f;
    public int minutesPerTick = 10;

    private System.TimeSpan currentTime;
    private float timer;
    private bool isPaused = false;
    private float speedMultiplier = 1f;

    private Color defaultColor = Color.white;
    private Color activeColor = Color.green;

    void Start()
    {
        currentTime = new System.TimeSpan(14, 0, 0); // start 2:00pm
        UpdateClockDisplay();
        HighlightButton(playButton); // start as "playing"
    }

    void Update()
    {
        if (isPaused) return;

        timer += Time.deltaTime * speedMultiplier;
        if (timer >= realSecondsPerGameTick)
        {
            timer = 0f;
            AdvanceTime(minutesPerTick);
        }
    }

    private void AdvanceTime(int minutes)
    {
        currentTime = currentTime.Add(System.TimeSpan.FromMinutes(minutes));
        if (currentTime.Hours >= 24)
            currentTime = currentTime.Subtract(System.TimeSpan.FromHours(24));

        UpdateClockDisplay();
    }

    private void UpdateClockDisplay()
    {
        string amPm = currentTime.Hours >= 12 ? "pm" : "am";
        int displayHour = currentTime.Hours % 12;
        if (displayHour == 0) displayHour = 12;

        clockText.text = string.Format("{0:00}:{1:00}{2}", displayHour, currentTime.Minutes, amPm);
    }

    // ==== BUTTON METHODS ====
    public void PauseClock()
    {
        isPaused = true;
        speedMultiplier = 1f; // ensure pause always clears fast-forward
        Debug.Log("Clock paused");
        HighlightButton(pauseButton);
    }

    public void PlayClock()
    {
        isPaused = false;
        speedMultiplier = 1f;
        Debug.Log("Clock playing at normal speed");
        HighlightButton(playButton);
    }

    public void FastForwardClock()
    {
        if (!isPaused && speedMultiplier == 2f)
        {
            // Already in fast forward → back to normal play
            speedMultiplier = 1f;
            Debug.Log("Fast Forward OFF → Normal speed");
            HighlightButton(playButton);
        }
        else
        {
            // Enter fast forward
            isPaused = false;
            speedMultiplier = 2f;
            Debug.Log("Fast Forward ON");
            HighlightButton(fastForwardButton);
        }
    }

    // ==== HIGHLIGHT HANDLER ====
    private void HighlightButton(Button activeButton)
    {
        // reset all
        playButton.image.color = defaultColor;
        pauseButton.image.color = defaultColor;
        fastForwardButton.image.color = defaultColor;

        // set active
        activeButton.image.color = activeColor;
    }
}
