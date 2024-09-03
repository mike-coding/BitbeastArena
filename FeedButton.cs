using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FeedButton : UIButton
{
    private BeastState _loadedState;

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
        _iconObject = transform.Find("ICON").gameObject;
        _icon = _iconObject.GetComponent<Image>();
    }

    public void LoadBeast(BeastState state)
    {
        if (!gameObject.activeInHierarchy) return;
        _loadedState = state;
        ButtonState nextState = ButtonState.B_State;
        if (_loadedState!=null && state.StatDict[Stat.CurrentHP] != state.StatDict[Stat.MaxHP])
        {
            nextState = ButtonState.A_State;
        }
        UpdateButtonState(nextState);
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

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (_state == ButtonState.B_State) return;
        EndBattleManager.Instance.ToggleFeederWidget(_loadedState);
    }
}
