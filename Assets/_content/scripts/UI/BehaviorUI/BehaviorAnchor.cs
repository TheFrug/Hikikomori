using System.Collections.Generic;
using UnityEngine;

public class BehaviorAnchor : MonoBehaviour
{
    [Header("Room Settings")]
    public RoomType roomType;

    [Header("Behavior Data")]
    public List<BehaviorData> behaviors = new();

    [Header("UI Prefabs & References")]
    public BehaviorIconUI iconPrefab;            
    public RectTransform worldIconsParent;
    public Canvas uiCanvas;                      // Your main UI canvas
    public BehaviorChoice choicePrefab;
    public BehaviorManager behaviorManager;
    public Transform behaviorGridParent;

    // Runtime
    private BehaviorIconUI iconInstance;
    private List<BehaviorChoice> activePanels = new();
    private bool isZoomedIn = false;

    private void Start()
    {
        SpawnUIIcon();
    }

    private void SpawnUIIcon()
    {
        if (iconPrefab == null || worldIconsParent == null)
        {
            Debug.LogError($"BehaviorAnchor '{name}' missing iconPrefab or worldIconsParent.");
            return;
        }

        iconInstance = Instantiate(iconPrefab, worldIconsParent);  
        iconInstance.anchor = this;
        iconInstance.uiCanvas = uiCanvas;
    }

    public void OnWorldClicked()
    {
        Debug.Log($"[BehaviorAnchor] Click → {name}");

        if (!isZoomedIn)
        {
            ZoomCameraToAnchor();
            SpawnBehaviorPanels();
            isZoomedIn = true;
        }
        else
        {
            HideBehaviorPanels();
            ZoomCameraBack();
            isZoomedIn = false;
        }
    }

    private void ZoomCameraToAnchor()
    {
        // Hook into your room cam transition logic
    }

    private void ZoomCameraBack()
    {
        // Hook into your room cam transition logic
    }

    private void SpawnBehaviorPanels()
    {
        HideBehaviorPanels();

        if (behaviors == null || behaviors.Count == 0)
            return;

        float radius = 1.5f;
        float angleStep = 360f / behaviors.Count;

        for (int i = 0; i < behaviors.Count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            BehaviorChoice card = Instantiate(
                choicePrefab,
                transform.position + offset,
                Quaternion.identity,
                behaviorGridParent
            );

            card.Configure(behaviors[i], behaviorManager);
            activePanels.Add(card);
        }
    }

    private void HideBehaviorPanels()
    {
        foreach (var panel in activePanels)
            if (panel != null) Destroy(panel.gameObject);

        activePanels.Clear();
    }
}
