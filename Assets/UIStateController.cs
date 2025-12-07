// UIStateController.cs
using System;
using UnityEngine;

public enum UIState
{
    Idle,
    IconOpen,
    SpoonPanelOpen,
    BehaviorRunning
}

/// <summary>
/// Centralized UI state controller — single source of truth for simple UI rules.
/// Minimal for P1: camera buttons disabled while IconOpen or SpoonPanelOpen.
/// Extendable for later problems.
/// </summary>
public class UIStateController : MonoBehaviour
{
    public static UIStateController Instance { get; private set; }

    public UIState State { get; private set; } = UIState.Idle;

    /// <summary> Currently focused icon (if any) — optional helper. </summary>
    public MonoBehaviour ActiveIcon { get; private set; }

    /// <summary> Currently active spoon panel (if any) — optional helper. </summary>
    public MonoBehaviour ActiveSpoonPanel { get; private set; }

    public event Action<UIState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Query helpers used by other systems -------------------------

    /// <summary> Can the player click room/camera buttons? Only when idle. </summary>
    public bool CanClickRoomButtons => State == UIState.Idle;

    /// <summary> Can an icon be opened? Only when idle. </summary>
    public bool CanOpenIcon => State == UIState.Idle;

    /// <summary> Can a spoon panel open? Only when an icon is open (simple rule). </summary>
    public bool CanOpenSpoonPanel => State == UIState.IconOpen && ActiveSpoonPanel == null;

    // --- State transitions ------------------------------------------

    public void SetState(UIState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }

    public void EnterIconOpen(MonoBehaviour icon)
    {
        ActiveIcon = icon;
        ActiveSpoonPanel = null;
        SetState(UIState.IconOpen);
    }

    public void ExitIconOpen()
    {
        ActiveIcon = null;
        // If a spoon panel exists, prefer that state, otherwise go Idle
        if (ActiveSpoonPanel != null)
            SetState(UIState.SpoonPanelOpen);
        else
            SetState(UIState.Idle);
    }

    public void EnterSpoonPanel(MonoBehaviour panel)
    {
        ActiveSpoonPanel = panel;
        SetState(UIState.SpoonPanelOpen);
    }

    public void ExitSpoonPanel()
    {
        ActiveSpoonPanel = null;
        // Return to IconOpen if an icon is focused, else Idle
        if (ActiveIcon != null)
            SetState(UIState.IconOpen);
        else
            SetState(UIState.Idle);
    }

    public void EnterBehaviorRunning()
    {
        SetState(UIState.BehaviorRunning);
    }

    public void ExitBehaviorRunning()
    {
        SetState(UIState.Idle);
    }
}
