using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum ItemType
{
    Held,
    Food,
    Consumable,
    Key,
    Null
}

public enum Enhancement
{
    Damage,
    ShieldStart,
    HealthRegen,
    CritChance,
    Accuracy,
    Speed,
    Sturdy,
    CooldownReduction,
    ApplyPoison,
    EvasionSneak,
    DampenStatusEffects,
    None
}

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Relic
}

public class Item
{
    public static Dictionary<int, Item> FoodDex = new Dictionary<int, Item>();
    public static Dictionary<int, Item> HeldDex = new Dictionary<int, Item>();
    public static Dictionary<int, Item> ConsumableDex = new Dictionary<int, Item>();
    public static Dictionary<int, Item> KeyDex = new Dictionary<int, Item>();
    public static bool DexLoaded { get { return FoodDex.Count > 0 || HeldDex.Count > 0; } }
    public static Item NoneItem;

    public ItemType Type;
    public int ID;
    public string Name;
    public string Description;
    public Rarity RarityLevel;

    //food variables
    public Stat StatToModify;
    public int StatModificationIncrement;

    //held variables
    public Enhancement EnhancementType = Enhancement.None;
    public float EnhancementMagnitude;

    [JsonIgnore] public Sprite Icon;

    public static Dictionary<Rarity, Color> RarityColors = new Dictionary<Rarity, Color>()
    {
        {Rarity.Common, new Color(134/255f,1,158/255f) },
        {Rarity.Uncommon, new Color(1,218/255f,134/255f) },
        {Rarity.Rare, new Color(1,93/255f,130/255f) },
        {Rarity.Relic, new Color(245/255f,127/255f,1) }
    };


    public static void LoadDex()
    {
        if (DexLoaded) return;
        TextAsset dexAsText = Resources.Load<TextAsset>("GameData/ItemDex");
        List<Item> allItems = JsonConvert.DeserializeObject<List<Item>>(dexAsText.ToString());
        foreach (Item item in allItems)
        {
            item.Icon = Resources.Load<Sprite>($"Sprites/Items/{item.Type}/{item.ID}");
            switch (item.Type)
            {
                case ItemType.Held:
                    HeldDex.Add(item.ID, item);
                    break;
                case ItemType.Food:
                    FoodDex.Add(item.ID, item);
                    break;
                case ItemType.Consumable:
                    ConsumableDex.Add(item.ID, item);
                    break;
                case ItemType.Key:
                    KeyDex.Add(item.ID, item);
                    break;
                case ItemType.Null:
                    NoneItem=item;
                    break;
            }
        }
    }

    public static Item GetInstanceByKey(int id, ItemType type)
    {
        Dictionary<int, Item> targetDex = new Dictionary<int, Item>();
        switch (type)
        {
            case ItemType.Held:
                targetDex=HeldDex;
                break;
            case ItemType.Food:
                targetDex = FoodDex;
                break;
            case ItemType.Consumable:
                targetDex = ConsumableDex;
                break;
            case ItemType.Key:
                targetDex = KeyDex;
                break;
        }
        return targetDex[id];
    }

    public static Dictionary<int, Item> GetDexByType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Held:
                return HeldDex;
            case ItemType.Food:
                return FoodDex;
            case ItemType.Consumable:
                return ConsumableDex;
            case ItemType.Key:
                return KeyDex;
            default:
                return null;
        }
    }

    public static int GetWeightByRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return 20; // Most common items have highest weight
            case Rarity.Uncommon:
                return 10;
            case Rarity.Rare:
                return 5;
            case Rarity.Relic:
                return 1; // Rarest items have lowest weight
            default:
                return 0;
        }
    }
}
