using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FeedEquipButton : UIButton
{
    public override void OnEnable()
    {
        base.OnEnable();
        GetComponents();
        GetDefaultValues();
    }

    public override void GetComponents()
    {
        _bgObject = transform.Find("BG").gameObject;
        _bg = _bgObject.GetComponent<Image>();
        _textObject = transform.Find("TEXT/textMain").gameObject;
        _textMain = _textObject.GetComponent<Text>();
    }

    public override void OnPointerClick(PointerEventData eventData) { if (_state != ButtonState.B_State) BagUIController.Instance.ToggleSelectionPanel(); }
}
