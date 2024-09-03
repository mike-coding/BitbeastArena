using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

public class ItemChoiceController : UIButton
{
    private int _selfID;
    private Item _item;
    private int _itemQuanity;
    private Image _displayImage;
    private GameObject _displayObject;
    private GameObject _selectorIndicator;

    public override void OnEnable()
    {
        base.OnEnable();
        GetComponents();
        GetDefaultValues();
    }
    public void Init(Item item, int itemQuantity)
    {
        if (!_bg) OnEnable();
        _item = item;
        _itemQuanity = itemQuantity;
        _displayImage.sprite = Item.GetDexByType(item.Type)[item.ID].Icon;
    }

    public override void GetComponents()
    {
        //sprite info
        _displayObject = transform.Find("Display").gameObject;
        _displayImage = _displayObject.GetComponent<Image>();
        _bg = GetComponent<Image>();
        _iconObject = _displayObject;
        _icon = _displayImage;
        _selfID = int.Parse(gameObject.name[gameObject.name.Length - 1].ToString());
        _selectorIndicator = gameObject.transform.Find("SelectorIndicator").gameObject;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        EventSystemController mainEventController = GameManager.EventManager;
        if (mainEventController.LoadedEventType==EventStyle.StarterLoot) mainEventController.DeactivateStarterLootSelectors();
        else mainEventController.DeactivateItemChoiceSelectors();
        _selectorIndicator.SetActive(true);
        mainEventController.SelectItem(_item, _itemQuanity);
    }

    public void TurnSelectorOff() { if (_selectorIndicator != null)_selectorIndicator.SetActive(false); }
}
