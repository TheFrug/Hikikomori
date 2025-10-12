using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public class ClockManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text clockText;
    public Button pauseButton;
    public Button playButton;
    public Button fastForwardButton;

    [Header("Clock Settings")]
    public float realSecondsPerGameTick = 3f;
    public int minutesPerTick = 5;

    private System.TimeSpan currentTime;
    private float timer;
    private bool isPaused = false;
    private float speedMultiplier = 1f;

    private Color defaultColor = Color.white;
    private Color activeColor = Color.green;

    public enum ClockState { Paused, Normal, FastForward }

    public ClockState CurrentState { get; private set; } = ClockState.Normal;
    public float TimeScaleMultiplier => speedMultiplier;

    // Optional: Event that fires every tick
    public delegate void ClockTickEvent();
    public event ClockTickEvent OnTick;

    void Start()
    {
        currentTime = new System.TimeSpan(14, 0, 0); // start 2:00pm
        UpdateClockDisplay();
        OnTick?.Invoke();
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
        OnTick?.Invoke();
    }

    private void UpdateClockDisplay()
    {
        string amPm = currentTime.Hours >= 12 ? "pm" : "am";
        int displayHour = currentTime.Hours % 12;
        if (displayHour == 0) displayHour = 12;

        clockText.text = string.Format("{0:00}:{1:00}{2}", displayHour, currentTime.Minutes, amPm);
    }

    // ==== BUTTON METHODS ====
    [YarnCommand("PauseClock")]
    public void PauseClock()
    {
        isPaused = true;
        speedMultiplier = 1f; // ensure pause always clears fast-forward
        CurrentState = ClockState.Paused;
        HighlightButton(pauseButton);
        Debug.Log("Clock paused");
    }

    [YarnCommand("PlayClock")]
    public void PlayClock()
    {
        isPaused = false;
        speedMultiplier = 1f;
        CurrentState = ClockState.Normal;
        HighlightButton(playButton);
        Debug.Log("Clock playing at normal speed");
    }

    public void FastForwardClock()
    {
        isPaused = false;
        speedMultiplier = (speedMultiplier == 2f) ? 1f : 2f;
        CurrentState = (speedMultiplier == 2f) ? ClockState.FastForward : ClockState.Normal;
        HighlightButton(speedMultiplier == 2f ? fastForwardButton : playButton);
        Debug.Log($"Fast Forward {(speedMultiplier == 2f ? "ON" : "OFF")} (Speed x{speedMultiplier})");
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
