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

    [Header("Unlock")]
    public bool startsUnlocked = true; // can be set in inspector
    [HideInInspector] public bool unlocked = false;
    public string iconID;

    private List<BehaviorChoice> activePanels = new();
    private bool isOpen = false;
    private CanvasGroup group;

    void Start()
    {
        roomCtrl = FindObjectOfType<BehaviorIconRoomController>();
        ApplyUnlockState();
    }

    public void ApplyUnlockState()
    {
        if (!unlocked)
            return;

        if (roomCtrl != null &&
            roomCtrl.CurrentRoom == roomType && // use the helper
            !IsAnyChoiceOpen())
        {
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }


    // Helper to check if any BehaviorChoice panel is open
    private bool IsAnyChoiceOpen()
    {
        return activePanels.Count > 0 || isOpen;
    }

    public void SetVisible(bool visible)
    {
        // Locked icons cannot appear
        if (!unlocked)
            visible = false;

        StopAllCoroutines();
        StartCoroutine(Fade(visible ? 1f : 0f));
    }

    IEnumerator Fade(float target)
    {
        if (group == null)
            group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

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
        if (!unlocked) return; // cannot interact if locked

        if (UIStateController.Instance != null && !UIStateController.Instance.CanOpenIcon)
            return;

        if (isOpen) return;

        roomCtrl.Focus(this);
        UIStateController.Instance?.EnterIconOpen(this);

        foreach (var icon in roomCtrl.icons)
            icon.SetVisible(false);

        StartCoroutine(OpenAfterCamera());
    }

    IEnumerator OpenAfterCamera()
    {
        yield return new WaitForSeconds(0.2f);
        OpenChoices();
    }

    private void OpenChoices()
    {
        isOpen = true;
        float radius = 1.5f;
        float step = behaviors.Count > 0 ? 360f / behaviors.Count : 360f;

        for (int i = 0; i < behaviors.Count; i++)
        {
            float angle = i * step * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            // Instantiate card
            BehaviorChoice card = Instantiate(
                choicePrefab,
                transform.position + offset,
                Quaternion.identity,
                behaviorGridParent
            );

            // Configure card with data and manager
            card.Configure(behaviors[i], behaviorManager);

            // Immediately refresh state to reflect unlocked/locked status
            card.RefreshState();

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
        isOpen = false;
        foreach (var c in activePanels)
            Destroy(c.gameObject);

        activePanels.Clear();
        UIStateController.Instance?.ExitIconOpen();
    }
}
