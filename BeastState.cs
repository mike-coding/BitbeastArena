using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Newtonsoft.Json;

public enum Stat
{
    Unspent,
    XP,
    CurrentHP,
    MaxHP,
    Shield,
    STR,
    DEX,
    SPD
}

public class BeastState : BeastBlueprint
{
    public int Level 
    { 
        get 
        { 
            int levelFromStats = StatDict[Stat.STR] + StatDict[Stat.DEX] + StatDict[Stat.SPD] + (StatDict[Stat.MaxHP] / 10 - 6) + StatDict[Stat.Unspent] - 2;
            int levelFromAbilities = 0;
            foreach (Ability ability in LearnedAbilities) levelFromAbilities += ability.LearnCost;
            return levelFromStats + levelFromAbilities -1;
        } 
    }
    public Dictionary<Stat, int> StatDict = new Dictionary<Stat, int> {
        {Stat.XP, 0 },
        {Stat.Unspent, 0},
        {Stat.CurrentHP, 60},
        {Stat.MaxHP, 60 },
        {Stat.STR, 1 },
        {Stat.DEX, 1 },
        {Stat.SPD, 1 },
    };
    public int XPToNextLevel { get { return XPLadder[Level]; } }
    public bool IsShiny = false;

    public Item HeldItem;
    public bool IsEgg { get { return EvolutionID.SequenceEqual(new int[] { 0, 0 }); } }
    public int MaxHP { get { return (StatDict[Stat.MaxHP]); } }

    [JsonIgnore]
    public new Dictionary<string, List<Sprite>> AnimationDictionary { get { return Dex[GenerateStringDexKey(EvolutionID)].AnimationDictionary; } }

    public static BeastState RollRandomState(int seed, int level, bool equipRandomItem=false)
    {
        System.Random random = new System.Random(seed);
        int remainingLevels = level;

        //state initialization, roll identity
        int evolutionIDcomponent = random.Next(3);
        BeastState workingState = new BeastState();
        int evolIDPrefix = level + 5 > 9 ? 2 : 1; //accommodate higher tiers at some point
        if (evolIDPrefix > 1)
        {
            int[] possibleIDs = new int[] { 0, 1, 2, 3, 4, 5 };
            int randomIndex = random.Next(0, possibleIDs.Length); // Generates a random index between 0 and the length of the array

            // Use the random index to select an ID from the array
            evolutionIDcomponent = possibleIDs[randomIndex];
        }
        workingState.LoadBlueprintData(new int[] { evolIDPrefix, evolutionIDcomponent });

        //ability allocations
        //LEARN 'FREE' ABILITIES, LOADOUT
        workingState.MovementAbility = "Hop";
        workingState.SkillAbilities[0] = random.Next(2) == 0 ? "Headbutt" : "Screech";
        workingState.LearnedAbilityNames = new List<string> { workingState.MovementAbility, workingState.SkillAbilities[0] };

        //LEARN ACE ABILITY (if possible)
        Ability aceAbility = Ability.GetAceAbilityByEvolID(workingState.EvolutionID);
        if (aceAbility!=null && remainingLevels >= aceAbility.LearnCost)
        {
            workingState.StatDict[Stat.Unspent] += aceAbility.LearnCost;
            remainingLevels -= aceAbility.LearnCost;
            workingState.LearnAbility(aceAbility);
        }

        //learn and equip other abilities
        var availableMovementAbilities = workingState.AvailableAbilities
            .Where(a => a.ThisAbilityType == AbilityType.Movement)
            .ToList();

        var availableSkillAbilities = workingState.AvailableAbilities
            .Where(a => a.ThisAbilityType == AbilityType.Skill)
            .ToList();

        // Handle movement abilities
        foreach (var movementAbility in availableMovementAbilities.OrderBy(x => random.Next()).Take(1))
        {
            if (!workingState.LearnedAbilityNames.Contains(movementAbility.Name) && remainingLevels >= movementAbility.LearnCost)
            {
                if (random.Next(2) == 0)  // 50/50 chance to learn
                {
                    workingState.StatDict[Stat.Unspent] += movementAbility.LearnCost;
                    remainingLevels -= movementAbility.LearnCost;
                    workingState.LearnAbility(movementAbility);
                }
            }
        }

        // Handle skill abilities
        int numSkillsToLearn = 2;
        foreach (var skillAbility in availableSkillAbilities.OrderBy(x => random.Next()).Take(numSkillsToLearn))
        {
            if (!workingState.LearnedAbilityNames.Contains(skillAbility.Name) && remainingLevels >= skillAbility.LearnCost)
            {
                if (random.NextDouble() < 0.65)
                {
                    workingState.StatDict[Stat.Unspent] += skillAbility.LearnCost;
                    remainingLevels -= skillAbility.LearnCost;
                    bool skillLoadOutFull = workingState.SkillAbilities[0] != "None" && workingState.SkillAbilities[1] != "None";
                    workingState.LearnAbility(skillAbility);

                    if (skillLoadOutFull)
                    {
                        workingState.SkillAbilities[1] = workingState.SkillAbilities[0];
                        workingState.SkillAbilities[0] = skillAbility.Name;
                    }
                }
            }
        }

        //stat point allocations
        List<Stat> statOptions = new List<Stat> { Stat.MaxHP, Stat.STR, Stat.DEX, Stat.SPD };
        while (remainingLevels > 0)
        {
            int boostBy = 1;
            int randomStatIndex = random.Next(statOptions.Count);
            Stat statToBoost = statOptions[randomStatIndex];
            if (statToBoost == Stat.MaxHP)
            {
                boostBy *= 10;
                workingState.StatDict[Stat.CurrentHP] += boostBy;
            }
            workingState.StatDict[statToBoost] += boostBy;
            remainingLevels -= 1;
        }

        //held item -> 1/6 chance
        if (equipRandomItem && random.Next(5) == 0) EquipRandomItem(workingState, seed);

        return workingState;
    }

    private static void EquipRandomItem(BeastState workingState, int seed)
    {
        System.Random random = new System.Random(seed);
        List<Item> items = new List<Item>(Item.HeldDex.Values);
        List<int> weights = new List<int>();

        foreach (var item in items) weights.Add(Item.GetWeightByRarity(item.RarityLevel));

        int totalWeight = weights.Sum();
        int randomWeightPoint = random.Next(totalWeight);
        int currentWeightSum = 0;

        for (int i = 0; i < items.Count; i++)
        {
            currentWeightSum += weights[i];
            if (randomWeightPoint < currentWeightSum)
            {
                workingState.HeldItem = items[i];
                return;
            }
        }
    }

    public void ModifyStats(Stat stat, int value) { StatDict[stat] += value; }

    public void LoadBlueprintData(int[] evolutionID, bool retainOldAvailableAbilities=false)
    {
        EvolutionID = evolutionID;
        BeastBlueprint blueprint = Dex[GenerateStringDexKey(evolutionID)];

        if (retainOldAvailableAbilities)
        {
            var newAbilityNames = blueprint.AvailableAbilityNames.Where(a => !AvailableAbilityNames.Any(b => b == a));
            List<string> updatedAbilityNames = new List<string>(AvailableAbilityNames);
            updatedAbilityNames.AddRange(newAbilityNames);
            AvailableAbilityNames = updatedAbilityNames;
        }
        else AvailableAbilityNames = blueprint.AvailableAbilityNames;
        YoffsetToCenter = blueprint.YoffsetToCenter;
        YoffsetToTop = blueprint.YoffsetToTop;
    }

    public void ResetStats()
    {
        StatDict = new Dictionary<Stat, int> {
        {Stat.XP, 0 },
        {Stat.Unspent, 4},
        {Stat.CurrentHP, 60},
        {Stat.MaxHP, 60 },
        {Stat.STR, 1 },
        {Stat.DEX, 1 },
        {Stat.SPD, 1 }};
    }

    public void ResetLoadOutAbilities()
    {
        //MovementAbility = Ability.GetInstance("None");
        //SkillAbilities = new Ability[2] { Ability.GetInstance("None"), Ability.GetInstance("None")};
    }

    public void AddXP(int value)
    {
        StatDict[Stat.XP] += value;
        while (StatDict[Stat.XP] >= XPToNextLevel)
        {
            int xpLeftover = StatDict[Stat.XP] - XPToNextLevel;
            StatDict[Stat.Unspent] += 1;
            StatDict[Stat.XP] = xpLeftover;
        }
    }

    public void EvolveSelf()
    {
        if (Level < EvolutionLadder[EvolutionID[0]] || Level == 2) return; //temporary stop measure until 3rd level forms are added
        LoadBlueprintData(GetEvolutionEvolID());
    }

    public int[] GetEvolutionEvolID()
    {
        foreach (string abilityName in LearnedAbilityNames)
        {
            Ability ability = Ability.GetInstance(abilityName);

            if (!ability.EvolutionProc.SequenceEqual(new int[] { 0, 0 }) && ability.EvolutionProc[0] > EvolutionID[0])
            {
                return ability.EvolutionProc;
            }
        }
        return new int[] { 0, 0 };
    }

    public void LearnAbility(Ability toLearn)
    {
        if (StatDict[Stat.Unspent]>=toLearn.LearnCost)
        {
            List<string> knownAbilityNames = LearnedAbilityNames;
            knownAbilityNames.Add(toLearn.Name);
            LearnedAbilityNames = knownAbilityNames;
            StatDict[Stat.Unspent]-=toLearn.LearnCost;
            if ((toLearn.ThisAbilityType==AbilityType.Skill && SkillAbilities.Any(a => a == "None"))|| toLearn.ThisAbilityType == AbilityType.Movement) LoadAbility(toLearn);
        }
    }

    public void LoadAbility(Ability toLoad)
    {
        if (toLoad.ThisAbilityType == AbilityType.Movement) MovementAbility = toLoad.Name;
        else if (toLoad.ThisAbilityType == AbilityType.Skill)
        {
            // Check for duplicates - ensure the ability isn't already loaded
            if (SkillAbilities.Any(a => a == toLoad.Name))
            {
                Debug.Log("Attempted to load a duplicate skill ability which is not allowed.");
                return; // Exit the method if the same ability is already in SkillAbilities
            }
            // Move the ability at index 0 to index 1
            SkillAbilities[1] = SkillAbilities[0];
            // Set index 0 to the new skill ability
            SkillAbilities[0] = toLoad.Name;
        }
        GameManager.SaveData();
    }

    public void UnloadAbility(Ability toUnload)
    {
        if (toUnload.Name == MovementAbility) MovementAbility = "None";
        else
        {
            if (toUnload.Name == SkillAbilities[0]) SkillAbilities[0] = SkillAbilities[1];
            SkillAbilities[1] = "None";
        }
    }
}
