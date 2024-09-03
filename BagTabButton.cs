using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public class BagTabButton : UIButton
{
    bool _initComplete=false;
    BagUIController _bagController;
    ItemType _bagPartition;

    public void TryInit(BagUIController parentReference)
    {
        if (_initComplete) return;
        _bagController = parentReference;
        if (gameObject.name.Contains("Held")) _bagPartition = ItemType.Held;
        else if (gameObject.name.Contains("Food")) _bagPartition = ItemType.Food;
        else if (gameObject.name.Contains("Consumable")) _bagPartition = ItemType.Consumable;
        else if (gameObject.name.Contains("Key")) _bagPartition = ItemType.Key;
        else _bagPartition = ItemType.Null;
        GetComponents();
        GetDefaultValues();
        _initComplete = true;
    }

    public override void GetComponents()
    {
        _bg = transform.Find("BG").gameObject.GetComponent<Image>();
        _iconObject = transform.Find("Icon").gameObject;
        _icon = _iconObject.GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (_bagPartition != ItemType.Null) _bagController.UpdateOpenTab(_bagPartition);
        else
        {
            UpdateButtonState(ButtonState.A_State);
            GameManager.UImanager.ToggleInventoryUI();  //exit bag here
        }
    }

    public override void ToggleHoverStyle()
    {
        if (_state == ButtonState.A_State && _bagPartition!=ItemType.Null) return;
        if (_isBeingHovered)
        {
            float newBGAlpha = _stateToDimensions[ButtonState.Hovered][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
        else
        {
            float newBGAlpha = _stateToDimensions[ButtonState.B_State][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
    }

}
