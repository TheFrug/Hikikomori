using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BehaviorIconUI : MonoBehaviour
{
    [Header("Settings")]
    public RoomType roomType;

    [Header("Behavior Data")]
    public List<BehaviorData> behaviors = new();

    [Header("UI References")]
    public BehaviorChoice choicePrefab;
    public Transform behaviorGridParent;
    public BehaviorManager behaviorManager;
    public Button backButton;

    private List<BehaviorChoice> activePanels = new();
    private bool isOpen = false;

    void Start()
    {
        // The back button is hidden by default
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void OnClick()
    {
        Debug.Log("Clicked");
        if (isOpen)
        {
            CloseChoices();
        }
        else
        {
            OpenChoices();
        }
    }

    private void OpenChoices()
    {
        isOpen = true;

        float radius = 1.5f;
        float step = 360f / behaviors.Count;

        for (int i = 0; i < behaviors.Count; i++)
        {
            float angle = i * step * Mathf.Deg2Rad;
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

        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    private void CloseChoices()
    {
        isOpen = false;

        foreach (var c in activePanels)
            if (c != null) Destroy(c.gameObject);

        activePanels.Clear();

        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }
}
