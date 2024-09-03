using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum Team
{
    Team1,
    Team2,
    None
}

public enum BattleMap
{
    Woods,
    Desert,
    Island,
    Test
}

public class BattleManager : MonoBehaviour 
{
    public bool CampaignBattle;

    public static Dictionary<int, BeastBattleProfile> Team1 = new Dictionary<int, BeastBattleProfile>
    {
        { 0, new BeastBattleProfile() },
        { 1, new BeastBattleProfile() },
        { 2, new BeastBattleProfile() }
    };
    public static int Team1Count { get { return Team1.Count(x => !x.Value.IsEmpty); } }
    public static bool Team1Alive {  get { return Team1.Any(kvp => kvp.Value.IsAlive); } }
    public static string Team1Name = "";

    public static Dictionary<int, BeastBattleProfile> Team2 = new Dictionary<int, BeastBattleProfile>
    {
        { 0, new BeastBattleProfile() },
        { 1, new BeastBattleProfile() },
        { 2, new BeastBattleProfile() }
    };
    public static int Team2Count { get { return Team2.Count(x => !x.Value.IsEmpty); } }
    public static bool Team2Alive { get { return Team2.Any(kvp => kvp.Value.IsAlive); } }
    public static string Team2Name = "";

    public List<BeastState> DefeatedEnemies = new List<BeastState>();

    private GameObject BeastPrefab;

    private static BattleManager _instance;
    public static BattleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerObject = GameObject.Find("BattleManager");
                if (managerObject != null) _instance = managerObject.GetComponent<BattleManager>();
            }
            return _instance;
        }
    }

    public static bool BattleActive = false;

    public static Dictionary<BattleMap, string> MapToScene = new Dictionary<BattleMap, string>()
    {
        { BattleMap.Woods, "WildsBattle" },
        { BattleMap.Desert, "CustomBattle" }
    };

    //public access battle effect prefabs
    public static GameObject SnareEffect;
    public static GameObject BurnEffect;
    public static GameObject StunEffect;
    public static GameObject PoisonEffect;
    public static Color SnareColor;
    public static Color PoisonColor;

    public static void PrepareBattle(List<BeastState> team1, List<BeastState> team2, string team1Name="PLAYER", string team2Name="ENEMY", BattleMap map = BattleMap.Woods)
    {
        GameManager.GetInstance().UpdateScene(MapToScene[map]);
        BattleActive = false;
        Team1Name = team1Name;
        Team2Name = team2Name;

        ResetTeams();
        // Store the state lists directly into the dictionaries for later use
        for (int i = 0; i < team1.Count; i++)
            Team1[i].Preset(team1[i]);

        for (int i = 0; i < team2.Count; i++)
            Team2[i].Preset(team2[i]);
    }

    private static void ResetTeams()
    {
        Team1.Clear();
        Team2.Clear();
        for (int i =0; i<3; i++)
        {
            Team1.Add(i, new BeastBattleProfile());
            Team2.Add(i, new BeastBattleProfile());
        }
    }

    public static List<BeastState> GetWinningTeam()
    {
        if (Team1Alive && !Team2Alive)
            return Team1.Where(kvp => !kvp.Value.IsEmpty)
                        .Select(kvp => kvp.Value.State)
                        .ToList();
        else if (Team2Alive && !Team1Alive)
            return Team2.Where(kvp => !kvp.Value.IsEmpty)
                        .Select(kvp => kvp.Value.State)
                        .ToList();
        else
            return new List<BeastState>();
    }

    void Start()
    {
        LoadReferences(); // Load all necessary resources
        UpdateTeamNames();
        SpawnBeastTeam(Team.Team1);
        SpawnBeastTeam(Team.Team2);
        GameManager.UImanager.SpawnProfilePanels(BSPStyle.BATTLE_ProfilePanel);
    }

    private void UpdateTeamNames()
    {
        GameManager.UImanager.Team1NameText.text = Team1Name;
        GameManager.UImanager.Team2NameText.text = Team2Name;
    }

    private void LoadReferences()
    {
        BeastPrefab = Resources.Load<GameObject>("gameObjects/BeastEntity");
        SnareEffect = Resources.Load<GameObject>("gameObjects/BattleEffects/SnareEffect");
        BurnEffect = Resources.Load<GameObject>("gameObjects/BattleEffects/BurnEffect");
        StunEffect = Resources.Load<GameObject>("gameObjects/BattleEffects/StunEffect");
        PoisonEffect = Resources.Load<GameObject>("gameObjects/BattleEffects/PoisonEffect");

        SnareColor = GameManager.CalculateAverageColor(Resources.Load<Sprite>("Sprites/AbilityEffects/SnaredEffect2"), 4, 4);
        PoisonColor = GameManager.CalculateAverageColor(Resources.Load<Sprite>("Sprites/AbilityEffects/PoisonEffect"), 4, 4);
    }

    public void SpawnBeastTeam(Team team)
    {
        Dictionary<int, BeastBattleProfile> teamDictionary = team == Team.Team1 ? Team1 : Team2;
        float baseY = team == Team.Team1 ? -1.4f : 1.4f;
        bool isEnemy = team == Team.Team2;

        Vector3 baseStartingPosition = new Vector3(0, baseY, -0.37f);
        float jitterAmount = 0.5f; // Adjust this value as needed
        System.Random random = new System.Random();

        for (int i=0; i < teamDictionary.Count; i++)
        {
            BeastState beastState = teamDictionary[i].State;
            if (teamDictionary[i].IsEmpty) continue;

            Vector3 jitter = new Vector3(
                (float)(random.NextDouble() * 2 - 1) * jitterAmount,
                (float)(random.NextDouble() * 2 - 1) * jitterAmount,
                0);
            Vector3 startingPosition = baseStartingPosition + jitter;

            GameObject beastObject = GameObject.Instantiate(BeastPrefab, startingPosition, Quaternion.identity);
            BeastController beastController = beastObject.GetComponent<BeastController>();
            beastController.LoadBeastState(beastState, false, isEnemy);

            // Update the BeastBattleProfile in the dictionary
            teamDictionary[i].FinishLoad(beastObject, beastController);
        }
    }

    public void BeginBattle()
    {
        BattleActive = true;
        foreach (BeastBattleProfile profile in Team1.Values) if (profile.IsLoaded) profile.Controller.ActivateBattleRoutine();
        foreach (BeastBattleProfile profile in Team2.Values) if (profile.IsLoaded) profile.Controller.ActivateBattleRoutine();
    }

    public void RemoveBeast(BeastController beastController, Team team)
    {
        Dictionary<int, BeastBattleProfile> teamDictionary = team == Team.Team1 ? Team1 : Team2;

        foreach (var kvp in teamDictionary)
        {
            if (kvp.Value.Controller == beastController)
            {
                GameObject.Destroy(kvp.Value.BeastObject);  // Destroy the GameObject
                if (team == Team.Team2) DefeatedEnemies.Add(kvp.Value.State);
                break;
            }
        }
        if (!teamDictionary.Any(kvp => kvp.Value.IsAlive) && BattleActive) EndBattle();
    }

    public void EndBattle()
    {
        BattleActive = false;
        GameObject EndBattleUI = GameManager.UImanager.EndBattlePage;
        EndBattleUI.SetActive(true);
        GameManager.UImanager.StartBattleButton.SetActive(false);
        GameManager.MainCamera.transform.Find("Canvas/UI/SidePanelUI").gameObject.SetActive(false);
        GameManager.ToggleBlur(true);
        bool team1Win = Team1.Any(profile => profile.Value.IsAlive);

        //handle text
        Text[] endBattleTexts = new Text[]
        {
            EndBattleUI.transform.Find("TEXT").gameObject.GetComponent<Text>(),
            EndBattleUI.transform.Find("TEXT/textMain").gameObject.GetComponent<Text>()
        };
        if (GameManager.PlayingCampaign)
        {
            if (team1Win) endBattleTexts[0].text = "Your beast(s) won the battle!";
            else endBattleTexts[0].text = "Your beast(s) were defeated!";
        }
        else
        {
            string winningTeamName = team1Win ? Team1Name : Team2Name;
            endBattleTexts[0].text = $"Team {winningTeamName} won\nthe battle!";
            if (Team1Alive == Team2Alive) endBattleTexts[0].text = "What the heck!!! They tied.";
        }

        endBattleTexts[1].text = endBattleTexts[0].text;

        if (!GameManager.PlayingCampaign) return;
        // Reset battle-dependent stats
        foreach (BeastState state in GameManager.PartyBeastStates) state.StatDict[Stat.CurrentHP] = state.StatDict[Stat.MaxHP];

        if (!team1Win) GameManager.Morale -= 1;
        else GameManager.Morale +=1;
    }

    public void ForcePlayerLoss()
    {
        foreach (var key in Team1.Keys.ToList())  // Use ToList to modify collection while iterating
        {
            if (Team1[key].IsAlive) RemoveBeast(Team1[key].Controller, Team.Team1);
        }
    }

    public void ForceEndBattle(Team setWinner = Team.None)
    {
        GameObject beginBattleButton = GameManager.UImanager.StartBattleButton;
        if (beginBattleButton.activeInHierarchy) beginBattleButton.SetActive(false);
        if (!BattleActive) BeginBattle();
        if (setWinner == Team.Team2 || setWinner == Team.None)
        {
            foreach (var key in Team1.Keys.ToList())
            {
                BeastBattleProfile currentProfile = Team1[key];
                if (currentProfile.IsLoaded && currentProfile.IsAlive) currentProfile.State.StatDict[Stat.CurrentHP] = 0;
            }
        }

        if (setWinner == Team.Team1 || setWinner == Team.None)
        {
            foreach (var key in Team2.Keys.ToList())
            {
                BeastBattleProfile currentProfile = Team2[key];
                if (currentProfile.IsLoaded && currentProfile.IsAlive) currentProfile.State.StatDict[Stat.CurrentHP] = 0;
            }
        }

    }

    public void DistributeXP()
    {
        // Calculate the total XP threshold for the next level for all beasts in the party
        int totalNextLevelXP = GameManager.PartyBeastStates.Sum(beast => BeastBlueprint.XPLadder[Mathf.Min(beast.Level, BeastBlueprint.XPLadder.Count - 1)]);
        int xpPerBeast = (totalNextLevelXP / 2) / GameManager.PartyBeastStates.Count;

        // Evenly distribute the XP among all party beasts
        foreach (BeastState beast in GameManager.PartyBeastStates) beast.AddXP(xpPerBeast);
    }
}
