using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorIconRoomController : MonoBehaviour
{
    public spatialCameraManager camManager;
    public List<BehaviorIconUI> icons;

    void Update()
    {
        foreach (var icon in icons)
        {
            bool shouldShow = ((int)icon.roomType == camManager.currentCamIndex);
            icon.SetVisible(shouldShow);
        }
    }
}