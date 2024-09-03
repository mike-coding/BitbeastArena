using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StatAllocatorButton : UIButton
{
    public bool increments;
    public bool initComplete=false;
    public string parentStat;
    public ButtonState CurrentState
    {
        get { return _state; }
        set { _state = value; }
    }
    public StatWidgetController controller;
    public BeastSummaryController SummaryController;
    private Dictionary<string, Stat> stringNameToStatDict = new Dictionary<string, Stat>() { { "HP", Stat.MaxHP },{"STR", Stat.STR },{"DEX",Stat.DEX },{"SPD",Stat.SPD } };

    public override void OnEnable()
    {
        base.OnEnable();
        GetComponents();
        GetDefaultValues();
        initComplete= true;
    }

    public override void GetComponents()
    {
        _bg = gameObject.GetComponent<Image>();
        _iconObject = transform.Find("Sprite").gameObject;
        _icon = _iconObject.GetComponent<Image>();
    }

    public void TryInit(BeastSummaryController summaryController = null)
    {
        if (summaryController != null) { SummaryController = summaryController; }
        if (initComplete) return;
        else OnEnable();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        parentStat = transform.parent.gameObject.name;
        controller = transform.parent.parent.parent.gameObject.GetComponent<StatWidgetController>();
        if (SummaryController!=null) SummaryController.ModifyStatValue(stringNameToStatDict[parentStat], increments);
        else controller.ModifyStatValue(stringNameToStatDict[parentStat], increments);
    }

    public override void ToggleClickedStyle()
    {
        if (_state == ButtonState.B_State) return;
        if (_isBeingPressed)
        {
            if (_textObject != null) { _textObject.transform.localScale = _defaultTextScale * _stateToDimensions[ButtonState.Clicked][2]; }
            if (_iconObject != null) { _iconObject.transform.localScale = _defaultIconScale * _stateToDimensions[ButtonState.Clicked][2]; }
        }
        else
        {
            if (_textObject != null) { _textObject.transform.localScale = _defaultTextScale * _stateToDimensions[ButtonState.A_State][2]; }
            if (_iconObject != null) { _iconObject.transform.localScale = _defaultIconScale * _stateToDimensions[ButtonState.A_State][2]; }
        }
    }
}
