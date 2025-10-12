using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BehaviorButtonHoverable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public GameObject lockOverlay; // assign in prefab inspector
    public BehaviorData behaviorData;
    private TooltipPanel tooltip;
    private BehaviorPanel parentPanel;
    private bool isLocked;

    public void Configure(BehaviorData data, TooltipPanel tooltipPanel, BehaviorPanel panel)
    {
        behaviorData = data;
        tooltip = tooltipPanel;
        parentPanel = panel;

        // Check global unlock manager
        bool globallyUnlocked = BehaviorUnlockManager.Instance?.IsUnlocked(behaviorData.behaviorName) ?? false;
        isLocked = behaviorData.startsLocked && !globallyUnlocked;

        label.text = isLocked ? "" : behaviorData.behaviorName;

        if (lockOverlay != null)
            lockOverlay.SetActive(isLocked);

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    // Used for RefreshButtonLocks (doesn't overwrite data)
    public void Reconfigure(TooltipPanel tooltipPanel, BehaviorPanel panel)
    {
        Configure(behaviorData, tooltipPanel, panel);
    }


    private void OnClicked()
    {
        if (isLocked)
        {
            tooltip?.ShowBusyMessage("This behavior is locked!");
            return;
        }

        parentPanel.OnBehaviorClicked(behaviorData);
    }

    public void Unlock()
    {
        isLocked = false;
        label.text = behaviorData.behaviorName;

        if (lockOverlay != null)
            lockOverlay.SetActive(false);

        BehaviorUnlockManager.Instance?.Unlock(behaviorData.behaviorName);
    }

    public string GetBehaviorName() => behaviorData?.behaviorName ?? "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isLocked && behaviorData != null)
            tooltip?.Show(behaviorData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide();
    }
}
