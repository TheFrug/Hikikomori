using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

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
    public Transform gridParent;
    public GameObject behaviorButtonPrefab;

    [Header("Data")]
    public List<BehaviorData> bedroomBehaviors;
    public List<BehaviorData> kitchenBehaviors;
    public List<BehaviorData> hallwayBehaviors;

    [Header("UI helpers")]
    public TooltipPanel tooltip;

    [HideInInspector] public RoomType currentRoom = RoomType.Bedroom;

    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        if (bedroomButton != null) bedroomButton.onClick.AddListener(OnBedroomClicked);
        if (kitchenButton != null) kitchenButton.onClick.AddListener(OnKitchenClicked);
        if (hallwayButton != null) hallwayButton.onClick.AddListener(OnHallwayClicked);

        SelectRoom(RoomType.Bedroom);
    }

    // Room button click handlers
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
                bedroomButton.image.color = activeColor;
                break;
            case RoomType.Kitchen:
                kitchenButton.image.color = activeColor;
                break;
            case RoomType.Hallway:
                hallwayButton.image.color = activeColor;
                break;
        }
    }

    void PopulateGridForRoom(RoomType room)
    {
        foreach (var go in spawnedButtons) Destroy(go);
        spawnedButtons.Clear();
        tooltip?.Hide();

        List<BehaviorData> list = GetListForRoom(room);
        if (list == null) list = new List<BehaviorData>();

        int maxSlots = 6;
        for (int i = 0; i < Mathf.Min(list.Count, maxSlots); i++)
        {
            var data = list[i];
            var go = Instantiate(behaviorButtonPrefab, gridParent, false);
            var bb = go.GetComponent<BehaviorButtonHoverable>();
            if (bb != null)
            {
                bb.Configure(data, tooltip, this);
                spawnedButtons.Add(go);
            }
        }
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

    public void OnBehaviorClicked(BehaviorData data)
    {
        if (behaviorManager == null)
        {
            Debug.LogError("BehaviorManager reference missing on BehaviorPanel!");
            return;
        }

        Debug.Log($"Behavior clicked: {data?.behaviorName ?? "null"}");
        behaviorManager.QueueBehavior(data);
    }

    /// <summary>
    /// Reconfigures all visible buttons (use after a global state change).
    /// </summary>
    public void RefreshButtonLocks()
    {
        foreach (var go in spawnedButtons)
        {
            var bb = go.GetComponent<BehaviorButtonHoverable>();
            if (bb != null)
                bb.Reconfigure(tooltip, this); // NEW safe reconfigure call
        }
    }

    // --- Utility --- //
    public BehaviorButtonHoverable FindButtonByName(string name)
    {
        foreach (var go in spawnedButtons)
        {
            var bb = go.GetComponent<BehaviorButtonHoverable>();
            if (bb != null && bb.GetBehaviorName().Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return bb;
        }
        return null;
    }

    // --- YARN COMMAND: UnlockBehavior --- //
    [YarnCommand("UnlockBehavior")]
    public void UnlockBehavior(string behaviorName)
    {
        Debug.Log($"[Yarn] Attempting to unlock behavior: {behaviorName}");

        // First: see if button is currently visible in the active room
        var btn = FindButtonByName(behaviorName);
        if (btn != null)
        {
            btn.Unlock();
            Debug.Log($"[Yarn] Unlocked behavior '{behaviorName}' (visible in {currentRoom}).");
            return;
        }

        // Otherwise: update BehaviorData directly (for persistence)
        foreach (var list in new[] { bedroomBehaviors, kitchenBehaviors, hallwayBehaviors })
        {
            var data = list.Find(d => d.behaviorName.Equals(behaviorName, System.StringComparison.OrdinalIgnoreCase));
            if (data != null)
            {
                data.startsLocked = false;
                BehaviorUnlockManager.Instance?.Unlock(data.behaviorName);
                Debug.Log($"[Yarn] Unlocked '{behaviorName}' in data; will appear unlocked when room is revisited.");
                return;
            }
        }

        Debug.LogWarning($"[Yarn] No behavior named '{behaviorName}' found to unlock.");
    }
}
