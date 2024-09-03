using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EvolveButton : UIButton
{
    public override void OnEnable()
    {
        GetComponents();
        GetDefaultValues();
    }

    public override void GetComponents()
    {
        _bgObject = transform.Find("BG").gameObject;
        _bg = _bgObject.GetComponent<Image>();

        _iconObject = transform.Find("ICON").gameObject;
        _icon = _iconObject.GetComponent<Image>();

        _textObject = transform.Find("TEXT").gameObject;
        _textMain = transform.Find("TEXT/textMain").gameObject.GetComponent<Text>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (_state==ButtonState.A_State && !BeastSummaryController.Instance.CurrentlyEvolving) GameManager.UImanager.BeastSummaryPanelController.TryEvolution();
    }
}
