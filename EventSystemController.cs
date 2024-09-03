using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSystemController : MonoBehaviour
{
    GameObject BG;
    GameObject EventPlaceHolder;
    GameObject StarterLoot;
    ItemChoiceController StarterLootController1;
    ItemChoiceController StarterLootController2;

    GameObject ItemChoiceEvent;
    ItemChoiceController ItemEventController1;
    ItemChoiceController ItemEventController2;
    ItemChoiceController ItemEventController3;

    GameObject VendingmachineEvent;
    GameObject WishingWellEvent;

    public EventStyle LoadedEventType=EventStyle.None;
    private Item _selectedItem;
    private int _selectedItemQuantity;


    private void Awake()
    {
        GetReferences();
    }

    void GetReferences()
    {
        BG = transform.Find("BG").gameObject;
        if (BG == null) Debug.Log("What the fuck!!!!!!!!!!");
        EventPlaceHolder = transform.Find("EventPlaceHolder").gameObject;
        StarterLoot = transform.Find("StarterLoot").gameObject;
        StarterLootController1 = transform.Find("StarterLoot/ItemWindow1").gameObject.GetComponent<ItemChoiceController>();
        StarterLootController2 = transform.Find("StarterLoot/ItemWindow2").gameObject.GetComponent<ItemChoiceController>();
        ItemChoiceEvent = transform.Find("ItemChoiceEvent").gameObject;
        ItemEventController1 = transform.Find("ItemChoiceEvent/ItemWindow1").gameObject.GetComponent<ItemChoiceController>();
        ItemEventController2 = transform.Find("ItemChoiceEvent/ItemWindow2").gameObject.GetComponent<ItemChoiceController>();
        ItemEventController3 = transform.Find("ItemChoiceEvent/ItemWindow3").gameObject.GetComponent<ItemChoiceController>();
    }

    public void DeactivateEvent()
    {
        //turn off respective item selectors
        if (LoadedEventType == EventStyle.StarterLoot) DeactivateStarterLootSelectors();
        else DeactivateItemChoiceSelectors();

        //clear loaded Event record
        LoadedEventType = EventStyle.None;

        //turn off all event UI interfaces
        StarterLoot.SetActive(false);
        EventPlaceHolder.SetActive(false);
        ItemChoiceEvent.SetActive(false);
        //common background gets turned off
        BG.SetActive(false);

        //set UIOpen flag off
        GameManager.UImanager.EventUIOpen = false;
        GameManager.ToggleBlur(false);
    }

    public void ActivateEvent(EventStyle eventType)
    {
        BG.SetActive(true);
        GameManager.UImanager.EventUIOpen = true;
        GameManager.ToggleBlur(true);

        switch (eventType)
        {
            case EventStyle.StarterLoot:
                ActivateStarterLootEvent();
                LoadedEventType = eventType;
                break;
            case EventStyle.Picnic:
                ActivateItemChoiceEvent(ItemType.Food);
                LoadedEventType = eventType;
                break;
            case EventStyle.Loot:
                ActivateItemChoiceEvent(ItemType.Held);
                LoadedEventType = eventType;
                break;
            case EventStyle.GumballMachine:
                ActivateItemChoiceEvent(ItemType.Consumable);
                LoadedEventType = eventType;
                break;
            default:
                EventPlaceHolder.SetActive(true);
                LoadedEventType = eventType;
                break;
        }
    }

    private void ActivateItemChoiceEvent(ItemType type)
    {
        ItemChoiceEvent.SetActive(true);
        Item[] eventSet = new Item[3];

        Dictionary<int, Item> targetDex = Item.GetDexByType(type);

        // Create a list of items weighted by their rarity
        List<Item> weightedItems = new List<Item>();
        foreach (var item in targetDex.Values)
        {
            int weight = Item.GetWeightByRarity(item.RarityLevel);
            for (int i = 0; i < weight; i++)
            {
                weightedItems.Add(item);
            }
        }

        // Select 3 items from the weighted list
        for (int i = 0; i < eventSet.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, weightedItems.Count);
            eventSet[i] = weightedItems[randomIndex];
            weightedItems.RemoveAt(randomIndex); // Remove selected item to avoid duplicates
        }

        ItemEventController1.Init(eventSet[0], 1);
        ItemEventController2.Init(eventSet[1], 1);
        ItemEventController3.Init(eventSet[2], 1);
    }

    private void ActivateStarterLootEvent()
    {
        StarterLoot.SetActive(true);
        StarterLootController1.Init(Item.HeldDex[10],1);
        StarterLootController2.Init(Item.HeldDex[1],1);
    }

    public void DeactivateStarterLootSelectors()
    {
        StarterLootController1.TurnSelectorOff();
        StarterLootController2.TurnSelectorOff();
    }

    public void DeactivateItemChoiceSelectors()
    {
        ItemEventController1.TurnSelectorOff();
        ItemEventController2.TurnSelectorOff();
        ItemEventController3.TurnSelectorOff();
    }

    public void SelectItem(Item item, int quantity)
    {
        _selectedItem = item;
        _selectedItemQuantity = quantity;
    }

    public void ConfirmItemSelectionChoice()
    {
        if (_selectedItem==null) return;
        if (_selectedItem.Type == ItemType.Null) return;
        GameManager.PlayerInventory.DepositItem(_selectedItem, _selectedItemQuantity);
        ResetItemSelection();
        DeactivateEvent();
        OverworldManager.LoadedMapData.ClearPassedEvents();
    }

    private void ResetItemSelection()
    {
        _selectedItem = null;
        _selectedItemQuantity = -1;
    }
}
