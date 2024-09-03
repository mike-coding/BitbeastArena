using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class Inventory
{
    // key: itemID, value: quantity
    public Dictionary<int, int> FoodItems = new Dictionary<int, int>();
    public Dictionary<int, int> HeldItems = new Dictionary<int, int>();
    public Dictionary<int, int> ConsumableItems = new Dictionary<int, int>();
    public Dictionary<int, int> KeyItems = new Dictionary<int, int>();

    public List<KeyValuePair<int, int>> FoodList { get { return new List<KeyValuePair<int, int>>(FoodItems); } }
    public List<KeyValuePair<int, int>> HeldList { get { return new List<KeyValuePair<int, int>>(HeldItems); } }
    public List<KeyValuePair<int, int>> ConsumableList { get { return new List<KeyValuePair<int, int>>(ConsumableItems); } }
    public List<KeyValuePair<int, int>> KeyList { get { return new List<KeyValuePair<int, int>>(KeyItems); } }

    public bool HasFood { get { foreach (var item in FoodItems) if (item.Value > 0) return true; return false; } }

    public int Money = 0;

    public void DepositItem(Item item, int quantity=1)
    {
        Dictionary<int, int> targetDictionary = GetDictByItemType(item.Type);
        if (targetDictionary.TryGetValue(item.ID, out int currentQuantity))
            targetDictionary[item.ID] = currentQuantity + quantity;
        else
            targetDictionary.Add(item.ID, quantity);
    }

    public void WithdrawItem(Item item, int quantity)
    {
        Dictionary<int, int> targetDictionary = GetDictByItemType(item.Type);
        if (targetDictionary.TryGetValue(item.ID, out int currentQuantity))
            if (currentQuantity >= quantity) targetDictionary[item.ID] = currentQuantity - quantity;
            else
                Debug.Log($"ERROR: trying to withdraw ivalid item. ID: {item.ID}");
    }

    public int GetItemQuantity(Item item)
    {
        return GetDictByItemType(item.Type)[item.ID];
    }

    public int GetFoodValueTotal()
    {
        int foodValueTotal = 0;
        foreach (KeyValuePair<int, int> entry in FoodList)
        {
            if (entry.Value > 0) foodValueTotal += Item.GetInstanceByKey(entry.Key, ItemType.Food).StatModificationIncrement * entry.Value;
        }
        return foodValueTotal;
    }

    public List<KeyValuePair<int, int>> GetListByItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Held:
                return HeldList;
            case ItemType.Food:
                return FoodList;
            case ItemType.Consumable:
                return ConsumableList;
            case ItemType.Key:
                return KeyList;
            default:
                return null;
        }
    }

    public Dictionary<int, int> GetDictByItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Held:
                return HeldItems;
            case ItemType.Food:
                return FoodItems;
            case ItemType.Consumable:
                return ConsumableItems;
            case ItemType.Key:
                return KeyItems;
            default:
                return null;
        }
    }
}
