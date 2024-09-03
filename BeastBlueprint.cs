using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeastBlueprint 
{
    #region Variables
    //static data dex shit
    public static Dictionary<string, BeastBlueprint> Dex = new Dictionary<string, BeastBlueprint>();
    public static Dictionary<int, int> XPLadder = new Dictionary<int, int>();
    public static Dictionary<int, int> EvolutionLadder = new Dictionary<int, int>();
    
    public int[] EvolutionID; //evolution level, ID (within level)
    public List<string> AvailableAbilityNames
    {
        // returns a list of the .name attribute of every ability in availableAbilities
        get { return AvailableAbilities.Select(a => a.Name).ToList(); }

        //clone ability instances from dex matching each name in AvailableAbilityNames into AvailableAbilities
        set { AvailableAbilities = value.Select(abilityName => Ability.GetInstance(abilityName)).ToList(); } 
    }
    public List<Ability> AvailableAbilities = new List<Ability>();

    public List<string> LearnedAbilityNames
    {
        get { return LearnedAbilities.Select(a => a.Name).ToList(); }
        set
        {
            // Filter out the name "None" and remove duplicates using HashSet.
            var filteredNames = new HashSet<string>(value.Where(name => name != "None"));

            // Clone abilities from the dex, assuming if an ability name is not found, it will throw an error.
            LearnedAbilities = filteredNames.Select(abilityName => Ability.Dex[abilityName].Clone()).ToList();
        }
    }
    public List<Ability> LearnedAbilities = new List<Ability>();

    public List<string> LoadOutAbilityNames
    {
        // returns a list of the .name attribute of every ability in availableAbilities
        get { return LoadOutAbilities.Select(a => a.Name).ToList(); }

        //clone ability instances from dex matching each name in AvailableAbilityNames into AvailableAbilities
        set { LoadOutAbilities = value.Select(abilityName => Ability.Dex[abilityName].Clone()).ToList(); }
    }
    public List<Ability> LoadOutAbilities = new List<Ability>();

    public string MovementAbility = "None";
    public string[] SkillAbilities = { "None", "None" };
    public float YoffsetToCenter;
    public float YoffsetToTop;

    [JsonIgnore]
    public Dictionary<string, List<Sprite>> AnimationDictionary= new Dictionary<string, List<Sprite>>();
    #endregion
    //==========================================================

    public class TempBeastBlueprint
    {
        public int[] EvolutionID;
        public List<string> AvailableAbilityNames;
        public List<string> LearnedAbilityNames;
    }

    static public void LoadDex()
    {
        if (Dex.Count > 0) return;
        TextAsset dexAsText = Resources.Load<TextAsset>("GameData/TamaDex");
        TempBeastBlueprint[] tempBlueprints = JsonConvert.DeserializeObject<TempBeastBlueprint[]>(dexAsText.ToString());

        foreach (TempBeastBlueprint tempBlueprint in tempBlueprints)
        {
            BeastBlueprint blueprint = new BeastBlueprint
            {
                EvolutionID = tempBlueprint.EvolutionID,
                AvailableAbilityNames = tempBlueprint.AvailableAbilityNames ?? new List<string>(), // Use empty list if null
                LearnedAbilityNames = tempBlueprint.LearnedAbilityNames ?? new List<string>() // Use empty list if null
            };

            // Load sprites
            foreach (string directionKey in new string[] { "F", "U", "R", "L" })
            {
                List<Sprite> sprites = Resources.LoadAll<Sprite>($"Sprites/mons/{blueprint.EvolutionID[0]}/{blueprint.EvolutionID[1]}/{directionKey}").ToList();
                if (sprites.Count > 0) blueprint.AnimationDictionary.Add(directionKey, sprites);
            }
            blueprint.SetSpriteOffsets();
            Dex.Add(GenerateStringDexKey(blueprint.EvolutionID), blueprint);
        }
    }

    static public void LoadXPLadder()
    {
        if (XPLadder.Count > 0) return;
        TextAsset ladderData = Resources.Load<TextAsset>("GameData/xpLadder");
        if (ladderData != null)
        {
            string[] lines = ladderData.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    int level = int.Parse(parts[0].Trim());
                    int xpRequired = int.Parse(parts[1].Trim());
                    XPLadder.Add(level, xpRequired);
                }
            }
        }
    }

    static public void LoadEvolutionLadder()
    {
        if (EvolutionLadder.Count > 0) return;
        TextAsset evolData = Resources.Load<TextAsset>("GameData/EvolutionLadder");
        if (evolData != null)
        {
            string[] lines = evolData.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    int evolID = int.Parse(parts[0].Trim());
                    int levelRequired = int.Parse(parts[1].Trim());
                    EvolutionLadder.Add(evolID, levelRequired);
                }
            }
        }
    }

    static public string GenerateStringDexKey(int[] evolutionID)
    {
        string DexKey = $"{evolutionID[0]}-{evolutionID[1]}";
        return DexKey;
    }

    public void SetSpriteOffsets()
    {
        Sprite sprite = AnimationDictionary["F"][0]; // Assuming the forward facing sprite is representative.
        if (sprite == null) Debug.Log("no Sprite found from GetSpriteCenterPosition");
        Texture2D texture = sprite.texture;
        int minY = texture.height, maxY = 0;

        // Get pixel data from the sprite's texture
        Color[] pixels = texture.GetPixels((int)sprite.textureRect.x,
                                           (int)sprite.textureRect.y,
                                           (int)sprite.textureRect.width,
                                           (int)sprite.textureRect.height);
        // Find the bounds of the non-transparent pixels
        for (int y = 0; y < (int)sprite.textureRect.height; y++)
        {
            for (int x = 0; x < (int)sprite.textureRect.width; x++)
            {
                Color pixel = pixels[y * (int)sprite.textureRect.width + x];
                if (pixel.a > 0) // alpha value greater than 0 means the pixel is not fully transparent
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        float centerOffsetY = (maxY + minY) / 2.0f - 1; // Calculate the sprite's visual center offset in pixels
        float worldUnitOffset = centerOffsetY / sprite.pixelsPerUnit; // Convert pixel offset to Unity world units, considering the sprite's pixels per unit
        YoffsetToCenter = worldUnitOffset; // Return the new position with adjusted Y
        YoffsetToTop = (maxY - 1)/ sprite.pixelsPerUnit;
    }

    public float GetMaximumMaxRange(float proportion=1)
    {
        float maxMaxRange=0;
        if (Ability.GetInstance(SkillAbilities[0]).MaxRange > maxMaxRange) maxMaxRange = Ability.GetInstance(SkillAbilities[0]).MaxRange;
        if (Ability.GetInstance(SkillAbilities[1]).MaxRange > maxMaxRange) maxMaxRange = Ability.GetInstance(SkillAbilities[1]).MaxRange;
        return maxMaxRange*proportion;
    }

    public float GetMinimumMaxRange(float proportion = 1)
    {
        float minMaxRange = float.MaxValue; // Initialize with a very large number

        if (SkillAbilities[0] != "None")
        {
            float maxRange0 = Ability.GetInstance(SkillAbilities[0]).MaxRange;
            if (maxRange0 < minMaxRange) minMaxRange = maxRange0;
        }

        if (SkillAbilities[1] != "None")
        {
            float maxRange1 = Ability.GetInstance(SkillAbilities[1]).MaxRange;
            if (maxRange1 < minMaxRange) minMaxRange = maxRange1;
        }

        return minMaxRange == float.MaxValue ? 0 : minMaxRange*proportion; // Return 0 if no valid abilities are found
    }
}
 