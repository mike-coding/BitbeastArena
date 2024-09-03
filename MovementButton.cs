using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovementButton : UIButton
{
    [SerializeField] OverworldManager.Direction controlDirection;


    public override void OnEnable()
    {
        _gameObjectName = gameObject.name;
        _UImanager = GameManager.UImanager;
        GetComponents();
        GetDefaultValues();
        UpdateButtonState(ButtonState.A_State);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        OverworldManager.MoveMons(controlDirection);
    }
}
