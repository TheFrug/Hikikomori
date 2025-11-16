using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public BehaviorIconRoomController roomCtrl;

    private List<BehaviorChoice> activePanels = new();
    private bool isOpen = false;
    private CanvasGroup group;

    void Start()
    {
        // The back button is hidden by default
        roomCtrl = FindObjectOfType<BehaviorIconRoomController>();
    }

    void Update()
    {

    }

    public void SetVisible(bool visible)
    {
        StopAllCoroutines();
        StartCoroutine(Fade(visible ? 1 : 0));
    }

    IEnumerator Fade(float target)
    {
        if (group == null)
            group = gameObject.GetComponent<CanvasGroup>() 
                ?? gameObject.AddComponent<CanvasGroup>();

        float start = group.alpha;
        float t = 0;

        while (t < 0.2f)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / 0.2f);
            yield return null;
        }

        group.alpha = target;
        group.blocksRaycasts = target > 0.9f;
    }

    public void OnClick()
    {
        if (isOpen) return;

        BehaviorIconRoomController roomCtrl = FindObjectOfType<BehaviorIconRoomController>();
        roomCtrl.Focus(this);
        // Fade out all icons INCLUDING this one
        foreach (var icon in roomCtrl.icons)
            icon.SetVisible(false);

        // Camera zoom-in goes here — not implemented yet but placeholder:
        // cameraManager.ZoomTo(anchorObject);

        // After zoom, open choices
        StartCoroutine(OpenAfterCamera());
    }

    IEnumerator OpenAfterCamera()
    {
        yield return new WaitForSeconds(0.2f); // replace later with actual camera event
        OpenChoices();
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
        roomCtrl.backButton.gameObject.SetActive(true);
    }

    public void ForceCloseChoices()
    {
        CloseChoices();
    }

    private void CloseChoices()
    {
        Debug.Log("Closing Choices");
        isOpen = false;

        foreach (var c in activePanels)
        {
            Debug.Log("Destroying: " + c.name + " @ " + c.transform.position);
            Destroy(c.gameObject);
        }

        activePanels.Clear();
    }
}
