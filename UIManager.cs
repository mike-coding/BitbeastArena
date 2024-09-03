using UnityEngine;
using Cinemachine;
using System.Collections.Generic;
using UnityEngine.UI;

public enum ExpandableUIs
{
    Summary,
    Bag,
    Event,
    None
}

public enum MainMenuArenaState
{
    None,
    StyleSelect,
    QuickBattle,
    CustomBattle,
}

public class UIManager : ScriptableObject
{
    private LayerMask _uiLayerMask;
    public static Camera MainCam;

    public int BeginGameProgression = -1;
    public MainMenuArenaState MenuArenaState = MainMenuArenaState.None;

    //Main Menu page references
    public Canvas TitleCanvas;
    public Canvas Page1;
    public Canvas Page2;    
    public Canvas Page3;
    public Canvas ArenaPage;
    public Canvas ArenaStyleSelector;
    public Canvas ArenaQuickBattleInput;
    public Canvas ArenaCustomBattleSelector;
    public Canvas ArenaTeamLoadOuts;
    public StatWidgetController statWidgetController;
    public RunStartAbilityWidget runStartAbilityWidget;
    public QuickBattleMenuController quickBattleMenuController;

    //General References
    public List<BeastStatusPanel> PartyProfilePanels = new List<BeastStatusPanel>();
    public List<BeastStatusPanel> EnemyProfilePanels = new List<BeastStatusPanel>();
    public Text Team1ProfilePanelHeader;
    public Text Team2ProfilePanelHeader;
    public GameObject ProfilePanelPrefab;

    //Battle Scene references
    public GameObject EndBattlePage;
    public GameObject StartBattleButton;
    public Text Team1NameText;
    public Text Team2NameText;

    //Overworld Scene References
    public Text levelDepthText;
    public BagUIController InventoryController;
    public GameObject EndGameObject;
    public GameObject InventoryUI;
    public BeastSummaryController BeastSummaryPanelController;
    public Canvas BeastSummaryCanvas;

    public static Camera CinemachineCam;

    //open closed state tracking
    public bool bagUIOpen { get { return InventoryUI.activeInHierarchy; } }
    public bool EndGameUIOpen { get { return EndGameObject.activeInHierarchy; } }
    public bool SummaryUIOpen {  get { return BeastSummaryCanvas.enabled; } }
    private bool _eventUIOpen = false;
    public bool EventUIOpen
    {
        get => _eventUIOpen;
        set { 
            _eventUIOpen = value; 
            if (_eventUIOpen && bagUIOpen) ToggleInventoryUI();
            if (_eventUIOpen && SummaryUIOpen) ToggleBeastSummary();
            }
    }
    public bool UIOpen {  get { return bagUIOpen || EventUIOpen || SummaryUIOpen || EndGameUIOpen; } }

    public void Init()
    {
        MainCam = GameManager.MainCamera;
        CinemachineCam = MainCam.GetComponent<CinemachineBrain>().OutputCamera;
        _uiLayerMask = 1 << LayerMask.NameToLayer("UI");

        string currentSceneName = GameManager.CurrentScene.name;
        switch(currentSceneName)
        {
            case "Main Menu":
                GetMenuPages();
                statWidgetController = Page3.transform.Find("BeastStatUpdateWidget").gameObject.GetComponent<StatWidgetController>();
                runStartAbilityWidget = Page3.transform.Find("AbilityPickerWidget").gameObject.GetComponent<RunStartAbilityWidget>();
                quickBattleMenuController = ArenaQuickBattleInput.gameObject.GetComponent<QuickBattleMenuController>();
                IncrementBeginGameProgression();
                break;
            case "WildsMap":
                if (BeastSummaryController.Instance == null) GameManager.MainCamera.transform.Find("Canvas/UI/BeastSummary").gameObject.GetComponent<BeastSummaryController>().Init();
                SpawnProfilePanels(BSPStyle.OVERWORLD_ProfilePanel);
                GetBagUI();
                levelDepthText = MainCam.transform.Find("Canvas/UI/SidePanelUI/ProgressionLevel").gameObject.GetComponent<Text>();
                BeastSummaryPanelController = MainCam.transform.Find("Canvas/UI/BeastSummary").gameObject.GetComponent<BeastSummaryController>();
                BeastSummaryCanvas = BeastSummaryPanelController.gameObject.GetComponent<Canvas>();
                EndGameObject = MainCam.transform.Find("Canvas/UI/EndGame").gameObject;
                break;
            case "WildsBattle":
                //SpawnProfilePanels(BSPStyle.BATTLE_ProfilePanel);
                GetProfilePanelHeaderReferences();
                EndBattlePage = MainCam.transform.Find("Canvas/UI/ENDBATTLE").gameObject;
                StartBattleButton = MainCam.transform.Find("Canvas/UI/STARTBATTLE").gameObject;
                Team1NameText = MainCam.transform.Find("Canvas/UI/SidePanelUI/Team1_Name").gameObject.GetComponent<Text>();
                Team2NameText = MainCam.transform.Find("Canvas/UI/SidePanelUI/Team2_Name").gameObject.GetComponent<Text>();
                if (!GameManager.PlayingCampaign) SetForfeitButtonToExit();
                break;
            case "CustomBattle":
                //SpawnProfilePanels(BSPStyle.BATTLE_ProfilePanel);
                GetProfilePanelHeaderReferences();
                //plug in name entries
                EndBattlePage = MainCam.transform.Find("Canvas/UI/ENDBATTLE").gameObject;
                StartBattleButton = MainCam.transform.Find("Canvas/UI/STARTBATTLE").gameObject;
                //do some other processing here so that the systems know we're not in a campaign battle
                break;
        }
    }

    #region ProfilePanels
    private void GetProfilePanelHeaderReferences()
    {
        Team1ProfilePanelHeader = MainCam.transform.Find("Canvas/UI/SidePanelUI/Team1_Name").gameObject.GetComponent<Text>();
        Team2ProfilePanelHeader = MainCam.transform.Find("Canvas/UI/SidePanelUI/Team2_Name").gameObject.GetComponent<Text>();
    }

    public void SpawnProfilePanels(BSPStyle style) //replace GetBeastPanels
    {
        ProfilePanelPrefab = Resources.Load<GameObject>("gameObjects/UI/BeastStatusPanel");
        GameObject Team1BeastPanelParent = GameObject.Find("TEAM1_BEASTPANELS");
        
        //make sure that thang empty
        foreach (Transform child in Team1BeastPanelParent.transform) GameObject.Destroy(child.gameObject);       
        PartyProfilePanels.Clear();
        //Spawn new profilePanels, store references, Init()
        Debug.Log($"Playing Campaign: {GameManager.PlayingCampaign}");
        int stateCount = GameManager.PlayingCampaign ? GameManager.PartyBeastStates.Count : BattleManager.Team1Count;
        Debug.Log($"statecount: {stateCount}");
        for (int i=0; i<stateCount; i++)
        {
            BeastState state = GameManager.PlayingCampaign ? GameManager.PartyBeastStates[i] : BattleManager.Team1[i].State;
            if (state == null) Debug.Log("huh .... !");
            GameObject newPanel = GameObject.Instantiate(ProfilePanelPrefab, Team1BeastPanelParent.transform);
            BeastStatusPanel panelComponent = newPanel.GetComponent<BeastStatusPanel>();
            PartyProfilePanels.Add(panelComponent);
            panelComponent.Init(state, style);
        }

        if (!BattleManager.Instance) { Debug.Log("No battle manager found. spawning party profile panels only."); return; }
        GameObject Team2BeastPanelParent = GameObject.Find("TEAM2_BEASTPANELS");
        foreach (Transform child in Team2BeastPanelParent.transform) GameObject.Destroy(child.gameObject);
        EnemyProfilePanels.Clear();
        for (int i=0; i<BattleManager.Team2Count; i++)
        {
            if (BattleManager.Team2[i].State == null) { Debug.Log("Null team1State ?!?"); continue; }
            GameObject newPanel = GameObject.Instantiate(ProfilePanelPrefab, Team2BeastPanelParent.transform);
            BeastStatusPanel panelComponent = newPanel.GetComponent<BeastStatusPanel>();
            EnemyProfilePanels.Add(panelComponent); //FIX
            panelComponent.Init(BattleManager.Team2[i].State, style);
        }
    }

    public void RefreshProfilePanels()
    {
        Debug.Log("refreshing profile panels");
        //check if party count matches number of active panels
        //check if campaign, use it to determine party source
        //if campaign : use PartyBeastStates
        //if !campaign: use BattleManager.Team1[i]
        int stateCount = GameManager.PlayingCampaign ? GameManager.PartyBeastStates.Count : BattleManager.Team1Count;
        if (stateCount != PartyProfilePanels.Count) SpawnProfilePanels(PartyProfilePanels[0].Style);
        if (BattleManager.Instance && BattleManager.Team2Count != EnemyProfilePanels.Count) SpawnProfilePanels(PartyProfilePanels[0].Style);
        //if not, spawn more
        //then update existing panels
        for (int i = 0; i < PartyProfilePanels.Count; i++)
        {
            BeastState team1State = GameManager.PlayingCampaign ? GameManager.PartyBeastStates[i] : BattleManager.Team1[i].State;
            if (team1State!=null) PartyProfilePanels[i].RefreshPanel(team1State);
        }
        if (BattleManager.Instance) for (int i = 0; i < EnemyProfilePanels.Count; i++) EnemyProfilePanels[i].RefreshPanel(BattleManager.Team2[i].State);
    }
    #endregion

    #region Overworld
    private void GetBagUI()
    {
        InventoryUI = MainCam.transform.Find("Canvas/UI/BAG_UI").gameObject;
        InventoryController = InventoryUI.GetComponent<BagUIController>();
    }

    public void ToggleInventoryUI()
    {
        if ((EventUIOpen & !InventoryUI.activeInHierarchy) || EndGameObject.activeInHierarchy) return;
        if (SummaryUIOpen) ToggleBeastSummary();
        // Check if EventUI is open, only allow closing the inventory, not opening it
        if (InventoryUI.activeInHierarchy)
        {
            InventoryUI.SetActive(false);
        }
        else// Toggle Inventory UI normally if EventUI is not open
        {
            InventoryUI.SetActive(!InventoryUI.activeInHierarchy);
        }
        GameManager.ToggleBlur(InventoryUI.activeInHierarchy);
    }

    public void ToggleBeastSummary(BeastState beast = null)
    {
        if ((EventUIOpen & !BeastSummaryCanvas.enabled)||EndGameObject.activeInHierarchy) return;
        if (bagUIOpen) ToggleInventoryUI();
        if (BeastSummaryCanvas.enabled)
        {
            if (beast == BeastSummaryPanelController.LoadedState || beast == null) BeastSummaryCanvas.enabled = false;
            else
            {
                BeastSummaryPanelController.LoadBeast(beast);
            }
        }
        else
        {
            BeastSummaryCanvas.enabled = true;
            BeastSummaryPanelController.LoadPage(SummaryTab.Beast);
            BeastSummaryPanelController.LoadBeast(beast);
        }
        GameManager.ToggleBlur(BeastSummaryCanvas.enabled);
    }

    public void UpdateProgressionDisplay()
    {
        levelDepthText.text = $"Misty Woods\nDepth: {OverworldManager.ProgressionLevel}";
    }

    public void EndTheGame()
    {
        EndGameObject.SetActive(true);
        GameManager.ToggleBlur(true);
    }
    #endregion

    #region Menu
    private void GetMenuPages()
    {
        Page1 = MainCam.transform.Find("Canvas/page1").gameObject.GetComponent<Canvas>();
        Page2 = MainCam.transform.Find("Canvas/page2").gameObject.GetComponent<Canvas>();
        Page3 = MainCam.transform.Find("Canvas/page3").gameObject.GetComponent<Canvas>();

        ArenaPage = MainCam.transform.Find("Canvas/Arena").gameObject.GetComponent<Canvas>();
        ArenaStyleSelector = MainCam.transform.Find("Canvas/Arena/ArenaStyleSelector").gameObject.GetComponent<Canvas>();
        ArenaQuickBattleInput = MainCam.transform.Find("Canvas/Arena/QUICKBATTLEINPUT").gameObject.GetComponent<Canvas>();
        ArenaCustomBattleSelector = MainCam.transform.Find("Canvas/Arena/CUSTOMBATTLE").gameObject.GetComponent<Canvas>();
        ArenaTeamLoadOuts = MainCam.transform.Find("Canvas/Arena/TeamLoadOut").gameObject.GetComponent<Canvas>();

    }

    public void IncrementBeginGameProgression()
    {
        Debug.Log($"Incrementing. Current Level: {BeginGameProgression}");
        switch (BeginGameProgression)
        {
            case -1:
                BeginGameProgression += 1;
                Page1.enabled = true;
                Page2.enabled = false;
                Page3.enabled = false;
                break;
            case 0:
                BeginGameProgression += 1;
                Page1.enabled = false;
                Page2.enabled = true;
                Page3.enabled = false;
                break;
            case 1:
                if (GameManager.PartyBeastStates.Count < 1) return;
                RunStartAbilityWidget.CurrentInstance.Init();
                BeginGameProgression += 1;
                Page1.enabled = false;
                Page2.enabled = false;
                Page3.enabled = true;
                
                break;
            case 2:
                if (!runStartAbilityWidget.AbilitiesSelected) return;
                BeginGameProgression = -1;
                EggChoiceButton.ClearControllerDict();
                GameManager.PlayerInventory = new Inventory();
                GameManager.Morale = 3;
                GameManager.SaveData();
                GameManager.PlayingCampaign = true;
                GameManager.GetInstance().UpdateScene("WildsMap"); //Use the right way to update the scene motherfucker
                break;
        }
    }

    public void DecrementBeginGameProgression()
    {
        Debug.Log($"Decrementing. Current Level: {BeginGameProgression}");
        switch (BeginGameProgression)
        {
            case 1:
                BeginGameProgression -= 1;
                Page1.enabled = true;
                Page2.enabled = false;
                Page3.enabled = false;
                break;
            case 2:
                GameManager.ClearStartingBeast();
                BeginGameProgression -= 1;
                Page1.enabled = false;
                Page2.enabled = true;
                Page3.enabled = false;
                RunStartAbilityWidget.CurrentInstance.ClearBeastAbilities();
                break;
        }
    }

    public void UpdateMainMenuArenaState(bool goBack = false, MainMenuArenaState nextState = MainMenuArenaState.None)
    {
        if (goBack)
        {
            if (MenuArenaState == MainMenuArenaState.StyleSelect) // Go back to main menu proper
            {
                MenuArenaState = MainMenuArenaState.None;
                ArenaPage.enabled = false;
                Page1.enabled = true;
            }
            else //Return to Style Select
            {
                MenuArenaState = MainMenuArenaState.StyleSelect;
                ArenaStyleSelector.enabled = true;
                ArenaCustomBattleSelector.enabled = false;
                ArenaQuickBattleInput.enabled = false;
                ArenaTeamLoadOuts.enabled = false;
                if (quickBattleMenuController.IsOn) quickBattleMenuController.ShutDown();

            }
        }
        else // forward to next arena state
        {
            ArenaPage.enabled = true;
            switch (nextState)
            {
                case MainMenuArenaState.StyleSelect:
                    MenuArenaState = MainMenuArenaState.StyleSelect;
                    ArenaStyleSelector.enabled = true;
                    Page1.enabled = false;
                    break;
                case MainMenuArenaState.QuickBattle:
                    MenuArenaState = MainMenuArenaState.QuickBattle;
                    ArenaStyleSelector.enabled = false;
                    ArenaQuickBattleInput.enabled = true;
                    quickBattleMenuController.TurnOn();
                    break;
                case MainMenuArenaState.CustomBattle:
                    MenuArenaState = MainMenuArenaState.CustomBattle;
                    ArenaStyleSelector.enabled = false;
                    ArenaCustomBattleSelector.enabled = true;
                    break;
            }
        }
    }

    public int GetActivePageIndex()
    {
        int activePageIndex = -1;
        if (Page1.enabled) activePageIndex = 1;
        else if (Page2.enabled) activePageIndex = 2;
        else if (Page3.enabled) activePageIndex = 3;
        return activePageIndex;
    }
    #endregion

    #region Battle
    public void SetTeamNames(string team1="PLAYER", string team2="ENEMY")
    {
        Team1NameText.text = team1;
        Team2NameText.text = team2;
    }

    public void SetForfeitButtonToExit()
    {
        GameObject ForfeitText = GameManager.MainCamera.transform.Find("Canvas/UI/SidePanelUI/FORFEITBUTTON/TEXT").gameObject;
        UIButton forfeitButton = ForfeitText.GetComponentInParent<UIButton>();
        forfeitButton.Function = "HOME";
        Text forfeitButtonText1 = ForfeitText.GetComponent<Text>();
        Text forfeitButtonText2 = ForfeitText.transform.Find("textMain").gameObject.GetComponent<Text>();
        forfeitButtonText1.text = "EXIT";
        forfeitButtonText2.text = "EXIT";
    }
    #endregion

}
