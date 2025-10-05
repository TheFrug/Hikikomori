using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BehaviorButtonHoverable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    private BehaviorData behaviorData;
    private TooltipPanel tooltip;
    private BehaviorPanel parentPanel;

    public void Configure(BehaviorData data, TooltipPanel tooltipPanel, BehaviorPanel panel)
    {
        behaviorData = data;
        tooltip = tooltipPanel;
        parentPanel = panel;
        label.text = behaviorData.behaviorName;

        // Hook up button click to parentPanel
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => parentPanel.OnBehaviorClicked(behaviorData));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && behaviorData != null)
        {
            tooltip.Show(behaviorData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
        {
            tooltip.Hide();
        }
    }
}
