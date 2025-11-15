using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorIconRoomController : MonoBehaviour
{
    public spatialCameraManager camManager;
    public List<BehaviorIconUI> icons;
    public BehaviorIconUI focusedIcon = null;

    void Awake()
    {
        icons = new List<BehaviorIconUI>(FindObjectsOfType<BehaviorIconUI>());
        camManager.OnCameraChanged += UpdateIcons;
    }

    void Start()
    {
        UpdateIcons(camManager.currentCamIndex);
    }

    void UpdateIcons(int activeRoom)
    {
        foreach (var icon in icons)
        {
            bool shouldShow = ((int)icon.roomType + 1 == activeRoom); // +1 converts roomType enum to camIndex int
            icon.SetVisible(shouldShow);
        }
    }

    public void FadeAllIconsOut()
    {
        foreach (var icon in icons)
            icon.SetVisible(false);
    }

    public void Focus(BehaviorIconUI icon)
    {
        focusedIcon = icon;
    }

    public void Unfocus()
    {
        focusedIcon = null;
    }
}