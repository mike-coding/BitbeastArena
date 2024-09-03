using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SummaryItemPicker : UIButton
{
    public override void OnEnable()
    {
        GetComponents();
        GetDefaultValues();
    }

    public override void GetComponents()
    {
        _bg = GetComponent<Image>();
        _iconObject = transform.Find("Body").gameObject;
        _icon = _iconObject.GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!BeastSummaryController.Instance.CurrentlyEvolving)
        {
            ManualHoverShutoff();
            BeastSummaryController.Instance.LoadPage(SummaryTab.HeldItemPicker);
        }
    }
}
