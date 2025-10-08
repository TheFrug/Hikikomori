using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RoomType { Bedroom, Kitchen, Hallway }

public class BehaviorPanel : MonoBehaviour
{
    [Header("References")]
    public BehaviorManager behaviorManager;

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

    // These methods run SelectRoom
    public void OnBedroomClicked() => SelectRoom(RoomType.Bedroom);
    public void OnKitchenClicked() => SelectRoom(RoomType.Kitchen);
    public void OnHallwayClicked() => SelectRoom(RoomType.Hallway);

    public void SelectRoom(RoomType room)
    {
        currentRoom = room; 
        UpdateRoomHighlights();
        PopulateGridForRoom(room);
    }

    void UpdateRoomHighlights() // Sets selected room button to highlight color, resets other two room buttons
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

        // limit to the number of cells you have (2x3 = 6). Grid layout handles layout.
        int maxSlots = 6;
        for (int i = 0; i < Mathf.Min(list.Count, maxSlots); i++) // For each BehaviorData in the list
        {
            var data = list[i];
            var go = Instantiate(behaviorButtonPrefab, gridParent, false); // Creates BehaviorButton
            var bb = go.GetComponent<BehaviorButtonHoverable>(); // Drills to accesses BehaviorButtonHoverable component
            if (bb != null) bb.Configure(data, tooltip, this); // Run Configure() on each button to fill with relevant data
            spawnedButtons.Add(go);
        }

        // If you want empty placeholders for remaining cells, instantiate disabled placeholders here.
    }

    List<BehaviorData> GetListForRoom(RoomType r) // I don't entirely know how this works
    {                                             // I think this chooses which list of behaviors is loaded based on the room
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
    public void OnBehaviorClicked(BehaviorData data) // I don't know how this is getting added to the buttons, but the debug message appears so ???
    {
        if (behaviorManager == null)
        {
            Debug.LogError("BehaviorManager reference missing on BehaviorPanel!");
            return;
        }

        Debug.Log($"Behavior clicked: {data?.behaviorName ?? "null"}");
        behaviorManager.QueueBehavior(data);
    }
}
