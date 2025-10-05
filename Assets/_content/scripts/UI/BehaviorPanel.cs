using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RoomType { Bedroom, Kitchen, Hallway }

public class BehaviorPanel : MonoBehaviour
{
    [Header("Room buttons")]
    public Button bedroomButton;
    public Button kitchenButton;
    public Button hallwayButton;

    [Header("Highlighting")]
    public Color defaultColor = Color.white;
    public Color activeColor = new Color(0.0f, 0.8f, 0.2f); // Sims-like green

    [Header("Button grid")]
    public Transform gridParent;                   // content transform for GridLayoutGroup
    public GameObject behaviorButtonPrefab;        // prefab with BehaviorButton component

    [Header("Data")]
    public List<BehaviorData> bedroomBehaviors;
    public List<BehaviorData> kitchenBehaviors;
    public List<BehaviorData> hallwayBehaviors;

    [Header("UI helpers")]
    public TooltipPanel tooltip;

    [HideInInspector]
    public RoomType currentRoom = RoomType.Bedroom;

    List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        // wire room buttons (you can also assign these in inspector OnClick)
        if (bedroomButton != null) bedroomButton.onClick.AddListener(OnBedroomClicked);
        if (kitchenButton != null) kitchenButton.onClick.AddListener(OnKitchenClicked);
        if (hallwayButton != null) hallwayButton.onClick.AddListener(OnHallwayClicked);

        SelectRoom(RoomType.Bedroom); // default start
    }

    public void OnBedroomClicked() => SelectRoom(RoomType.Bedroom);
    public void OnKitchenClicked() => SelectRoom(RoomType.Kitchen);
    public void OnHallwayClicked() => SelectRoom(RoomType.Hallway);

    public void SelectRoom(RoomType room)
    {
        currentRoom = room;
        UpdateRoomHighlights();
        PopulateGridForRoom(room);
    }

    void UpdateRoomHighlights()
    {
        if (bedroomButton != null) bedroomButton.image.color = defaultColor;
        if (kitchenButton != null) kitchenButton.image.color = defaultColor;
        if (hallwayButton != null) hallwayButton.image.color = defaultColor;

        switch (currentRoom)
        {
            case RoomType.Bedroom:
                if (bedroomButton != null) bedroomButton.image.color = activeColor;
                break;
            case RoomType.Kitchen:
                if (kitchenButton != null) kitchenButton.image.color = activeColor;
                break;
            case RoomType.Hallway:
                if (hallwayButton != null) hallwayButton.image.color = activeColor;
                break;
        }
    }

    void PopulateGridForRoom(RoomType room)
    {
        // clear existing
        foreach (var go in spawnedButtons) Destroy(go);
        spawnedButtons.Clear();
        tooltip?.Hide();

        List<BehaviorData> list = GetListForRoom(room);
        if (list == null) list = new List<BehaviorData>();

        // limit to the number of cells you have (2x6 = 12). Grid layout handles layout.
        int maxSlots = 12;
        for (int i = 0; i < Mathf.Min(list.Count, maxSlots); i++)
        {
            var data = list[i];
            var go = Instantiate(behaviorButtonPrefab, gridParent, false);
            var bb = go.GetComponent<BehaviorButtonHoverable>();
            if (bb != null) bb.Configure(data, tooltip, this);
            spawnedButtons.Add(go);
        }

        // If you want empty placeholders for remaining cells, instantiate disabled placeholders here.
    }

    List<BehaviorData> GetListForRoom(RoomType r)
    {
        return r switch
        {
            RoomType.Bedroom => bedroomBehaviors,
            RoomType.Kitchen => kitchenBehaviors,
            RoomType.Hallway => hallwayBehaviors,
            _ => bedroomBehaviors,
        };
    }

    /// <summary>
    /// Called when a behavior button is clicked.
    /// Right now it just logs — replace with applying behavior effects to resources.
    /// </summary>
    public void OnBehaviorClicked(BehaviorData data)
    {
        Debug.Log($"Behavior clicked: {data?.behaviorName ?? "null"}");
        // TODO: Set the active behavior, start running it, apply costs etc.
    }
}
