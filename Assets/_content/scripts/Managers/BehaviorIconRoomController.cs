// BehaviorIconRoomController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BehaviorIconRoomController : MonoBehaviour
{
    public spatialCameraManager camManager;
    public List<BehaviorIconUI> icons;
    public BehaviorIconUI focusedIcon = null;
    public Button backButton;

    void Awake()
    {
        icons = new List<BehaviorIconUI>(FindObjectsOfType<BehaviorIconUI>());
        if (camManager != null)
            camManager.OnCameraChanged += UpdateIcons;

        backButton.gameObject.SetActive(false);
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(BackPressed);
    }

    void Start()
    {
        UpdateIcons(camManager.currentCamIndex);
    }

    void Update()
    {
        // DEBUG: Close focused icon with keyboard
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            DebugCloseCommand();
        }
    }

    public RoomType CurrentRoom
    {
        get
        {
            // Convert camManager.currentCamIndex to RoomType
            // Assuming your existing UpdateIcons logic: (int)icon.roomType + 1 == activeRoom
            // Then current room = camManager.currentCamIndex - 1 as RoomType
            return (RoomType)(camManager.currentCamIndex - 1);
        }
    }

    public void DebugCloseCommand()
    {
        if (focusedIcon == null)
        {
            Debug.LogWarning("DebugCloseCommand called but no focused icon.");
            return;
        }

        Debug.Log("DebugCloseCommand: Forcing close of focused icon choices.");
        focusedIcon.ForceCloseChoices();
        Unfocus();
        UpdateIcons(camManager.currentCamIndex);
        backButton.gameObject.SetActive(false);
    }

    public void UpdateIcons(int activeRoom)
    {
        foreach (var icon in icons)
        {
            bool shouldShow = ((int)icon.roomType + 1 == activeRoom);
            icon.SetVisible(shouldShow);
        }
    }

    public void FadeAllIconsOut()
    {
        foreach (var icon in icons)
            icon.SetVisible(false);
    }

    public void BackPressed()
    {
        if (focusedIcon == null)
        {
            Debug.LogWarning("Back pressed but no focused icon.");
            return;
        }

        focusedIcon.ForceCloseChoices();
        Unfocus();
        UpdateIcons(camManager.currentCamIndex);
        backButton.gameObject.SetActive(false);
    }

    public void Focus(BehaviorIconUI icon)
    {
        // If there is an already-focused icon, force it closed first
        if (focusedIcon != null && focusedIcon != icon)
        {
            focusedIcon.ForceCloseChoices();
        }

        focusedIcon = icon;
    }

    public void Unfocus()
    {
        focusedIcon = null;
        // Ensure UIState is reset when unfocusing
        UIStateController.Instance?.SetState(UIState.Idle);
    }
}