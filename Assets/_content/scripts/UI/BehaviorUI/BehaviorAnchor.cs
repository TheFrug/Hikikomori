using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviorAnchor : MonoBehaviour {
    public RoomType roomType;
    public BehaviorPanel behaviorPanelPrefab; // or reference to panel
    public Sprite icon; // icon to display on world-space anchor
    private GameObject uiIconInstance; // screen‐space representation
    // maybe:
    public List<BehaviorData> behaviors; // for this anchor
}
