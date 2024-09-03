using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public enum SummaryTab
{
    Beast,
    AbilityViewer,
    AbilityDetails,
    HeldItemPicker,
    ConsumablePicker
}

public class BeastSummaryController : MonoBehaviour
{
    public static BeastSummaryController Instance;
    public BeastState LoadedState;
    private SummaryTab LastOpenTab = SummaryTab.Beast;
    public SummaryTab OpenTab = SummaryTab.Beast;

    //tabPageObjects
    private Canvas _beastPanel;
    private Canvas _abilitiesPanel;
    private Canvas _itemPickerPage;
    private GameObject _abilityViewer;
    private GameObject _abilityDetails;

    //tabTurnerButton Controllers
    private SummaryTabButton _beastPanelButton;
    private SummaryTabButton _abilitiesPanelButton;
    private SummaryItemPicker _heldItemPickerButton;
    private UIButton _feederButton;

    //SharedElements
    private Canvas _sharedElements;
    private Text _abilityInfoTitle;
    private Text _abilityInfoBody;
    private Text _abilityInfoType;
    private Text _abilityInfoLearnCost;
    

    //beastPanel
    private Stat[] allStats = new Stat[] { Stat.Unspent, Stat.MaxHP, Stat.STR, Stat.DEX, Stat.SPD };
    private Dictionary<Stat, (Text statValue, StatAllocatorButton incrementButton)> statComponentsDictionary = new Dictionary<Stat, (Text statValue, StatAllocatorButton incrementButton)>();
    private Text _levelText;
    private Slider _xpBar;
    private Text _xpValueText;
    private Image _beastPortrait;
    private Image _heldItemDisplay;
    private Light _evolLight;
    private Dictionary<string, List<Sprite>> evolDict;

    //abilityPanel
    private GameObject _learnedAbilityGrid;
    private GameObject _availableAbilityGrid;
    private AbilityButton[] _loadOutButtons = new AbilityButton[3]; //index 0 is movement ability
    private Text _growthPointDisplay;
    private GameObject _abilityButtonPrefab; //prefab
    private Sprite _heldItemPlaceHolder;
    private Image _expandedAbilityImage;
    private Image _expandedAbilityImageFrame;
    private UIButton _expandedAbilityActionButton;

    public Ability ExpandedAbility;

    //ItemPickerPanel
    private Dictionary<int, ItemPanelController> _itemPanelDict = new Dictionary<int, ItemPanelController>();
    private Text _pickerTitleText;
    private Scrollbar _pickerScrollbar;
    // item panel prefab
    private GameObject _itemPanelPrefab;
    //item panel placement location
    private GameObject _content;


    //bool flags
    private bool _initComplete=false;
    public bool CurrentlyEvolving;
    public bool IsHungry { get { if (LoadedState != null) return LoadedState.StatDict[Stat.CurrentHP] < LoadedState.StatDict[Stat.MaxHP]; else return false; } }

    public void Init()
    {
        Instance = this;
        GetReferences();
        _initComplete = true;
    }

    private void GetReferences()
    {
        //sharedElements
        _sharedElements = transform.Find("SharedElements").gameObject.GetComponent<Canvas>();
        _beastPortrait = transform.Find("SharedElements/PortraitFrame/Display").gameObject.GetComponent<Image>();
        _levelText = transform.Find("SharedElements/LevelDisplay/Level").gameObject.GetComponent<Text>();
        _xpBar = transform.Find("SharedElements/XPBar").gameObject.GetComponent<Slider>();
        _xpValueText = transform.Find("SharedElements/XPBar/ValueDisplay").gameObject.GetComponent<Text>();
        _heldItemDisplay = transform.Find("SharedElements/LoadOut/HeldItemDisplay/Body/Item").gameObject.GetComponent<Image>();
        _growthPointDisplay = transform.Find("SharedElements/PointsDisplay/Value").gameObject.GetComponent<Text>();
        for (int i = 0; i < 3; i++) _loadOutButtons[i] = transform.Find($"SharedElements/LoadOut/Ability {i}").gameObject.GetComponent<AbilityButton>();

        //beastPanel
        _evolLight = GameManager.MainCamera.transform.Find("PortraitSpotLight").gameObject.GetComponent<Light>();

        //abilityPanel shit
        _availableAbilityGrid = transform.Find("AbilitiesPanel/AbilityViewer/AvailableAbilityGrid").gameObject;
        _learnedAbilityGrid = transform.Find("AbilitiesPanel/AbilityViewer/LearnedAbilityGrid").gameObject;
        _abilityButtonPrefab = Resources.Load<GameObject>("gameObjects/UI/AbilityIconButton").gameObject;

        //expanded ability
        _abilityInfoTitle = transform.Find("AbilitiesPanel/AbilityDetails/Title").gameObject.GetComponent<Text>();
        _abilityInfoBody = transform.Find("AbilitiesPanel/AbilityDetails/Body").gameObject.GetComponent<Text>();
        _abilityInfoType = transform.Find("AbilitiesPanel/AbilityDetails/Type").gameObject.GetComponent<Text>();
        _abilityInfoLearnCost = transform.Find("AbilitiesPanel/AbilityDetails/LearnCost").gameObject.GetComponent<Text>();
        _expandedAbilityImage = transform.Find("AbilitiesPanel/AbilityDetails/AbilityIconDisplay/Body/IconArt").gameObject.GetComponent<Image>();
        _expandedAbilityImageFrame = transform.Find("AbilitiesPanel/AbilityDetails/AbilityIconDisplay/Body/Frame").gameObject.GetComponent<Image>();

        //get alternate page gameObjs
        _beastPanel = transform.Find("BeastPanel").gameObject.GetComponent<Canvas>();
        _abilitiesPanel = transform.Find("AbilitiesPanel").gameObject.GetComponent<Canvas>();
        _itemPickerPage = transform.Find("ItemPickerWidget").gameObject.GetComponent<Canvas>();
        _abilityViewer = transform.Find("AbilitiesPanel/AbilityViewer").gameObject;
        _abilityDetails = transform.Find("AbilitiesPanel/AbilityDetails").gameObject;

        _expandedAbilityActionButton = transform.Find("AbilitiesPanel/AbilityDetails/Learn").gameObject.GetComponent<UIButton>();

        //non-stat buttons -> tabs, heldItemPicker, feedOption
        _beastPanelButton = transform.Find("Tabs/SummaryPages/Beast").gameObject.GetComponent<SummaryTabButton>();
        _abilitiesPanelButton = transform.Find("Tabs/SummaryPages/Ability").gameObject.GetComponent<SummaryTabButton>();
        _heldItemPickerButton = transform.Find("SharedElements/LoadOut/HeldItemDisplay").gameObject.GetComponent<SummaryItemPicker>();
        _feederButton = transform.Find("SharedElements/PointsDisplay/FeedOption").gameObject.GetComponent<UIButton>();
        _pickerScrollbar = transform.Find("ItemPickerWidget/ScrollArea/Scrollbar").gameObject.GetComponent<Scrollbar>();
        _heldItemPlaceHolder = Resources.Load<Sprite>("Sprites/UI/heldItem");

        //get item panel references
        _pickerTitleText = transform.Find("ItemPickerWidget/Text").gameObject.GetComponent<Text>();
        _itemPanelPrefab = Resources.Load<GameObject>("gameObjects/UI/ItemPanel-MainBag").gameObject;
        _content = transform.Find($"ItemPickerWidget/ScrollArea/Content").gameObject;
        //for (int i = 0; i < 15; i++) _itemPanelDict[i] = transform.Find($"ItemPickerWidget/ScrollArea/Content/ItemPanel ({i})").gameObject.GetComponent<ItemPanelController>();

        foreach (Stat stat in allStats)
        {
            string statName = stat.ToString();
            if (statName == "MaxHP") statName = "HP";
            Text statValueText;
            if (statName != "Unspent") statValueText = transform.Find($"BeastPanel/StatPanels/{statName}/StatValue").gameObject.GetComponent<Text>();
            else statValueText = transform.Find($"SharedElements/PointsDisplay/Value").gameObject.GetComponent<Text>();
            StatAllocatorButton incrementButton = null;
            if (statName != "Unspent") { incrementButton = transform.Find($"BeastPanel/StatPanels/{statName}/IncrementUp").GetComponent<StatAllocatorButton>(); }
            statComponentsDictionary[stat] = (statValueText, incrementButton);
        }
    }

    public void LoadBeast(BeastState toLoad, bool skipPageUpdate=false)
    {
        if (!_initComplete) Init();
        LoadedState = toLoad;
        if (toLoad == null)
        {
            ClearSelf();
            return;
        }

        if (!skipPageUpdate) LoadPage(OpenTab);
        if (OpenTab == SummaryTab.AbilityDetails && !skipPageUpdate) LoadPage(SummaryTab.AbilityViewer);
        RefreshBeastPanel();
        RefreshAbilitiesPanel();
        RefreshSharedElements();
        GameManager.UImanager.RefreshProfilePanels();

        if (_itemPickerPage.enabled) LoadPage(SummaryTab.Beast);
    }

    #region RefreshBeastPanel
    private void RefreshBeastPanel()
    {
        //RefreshPortrait();
        RefreshStatWidget();
    }

    public void RefreshStatWidget()
    {
        foreach (Stat stat in allStats)
        {
            if (stat != Stat.Unspent)
            {
                StatAllocatorButton incrementButton = statComponentsDictionary[stat].incrementButton;
                incrementButton.TryInit(this);
                //handle increment up opacity
                if (LoadedState.StatDict[Stat.Unspent] == 0) incrementButton.UpdateButtonState(ButtonState.B_State);
                else incrementButton.UpdateButtonState(ButtonState.A_State);
            }
            statComponentsDictionary[stat].statValue.text = LoadedState.StatDict[stat].ToString();
        }
        statComponentsDictionary[Stat.Unspent].statValue.text = LoadedState.StatDict[Stat.Unspent].ToString();
    }
    #endregion

    #region RefreshAbilitiesPanel
    private void RefreshAbilitiesPanel()
    {
        RefreshAbilityButtonGrid();
    }

    private void RefreshAbilityButtonGrid()
    {
        // Clear the existing buttons
        foreach (Transform child in _learnedAbilityGrid.transform) Destroy(child.gameObject);
        foreach (Transform child in _availableAbilityGrid.transform) Destroy(child.gameObject);

        // Filter out abilities that are already learned
        var abilitiesToDisplay = LoadedState.AvailableAbilities
            .Where(available => !LoadedState.LearnedAbilities.Any(learned => learned.Name == available.Name))
            .ToList();

        // Iterate over filtered abilities and display them
        for (int i = 0; i < abilitiesToDisplay.Count; i++)
        {
            // Instantiate the prefab at the default position with no parent initially
            GameObject abilityButton = Instantiate(_abilityButtonPrefab, _availableAbilityGrid.transform.position, Quaternion.identity, _availableAbilityGrid.transform);

            abilityButton.transform.localRotation = Quaternion.identity;

            // Set the button properties or load the specific ability
            AbilityButton abilityButtonComponent = abilityButton.GetComponent<AbilityButton>();
            abilityButtonComponent.LoadAbility(abilitiesToDisplay[i], AbilityButtonType.CanLearn);
        }

        for (int i = 0; i < LoadedState.LearnedAbilities.Count; i++)
        {
            // Instantiate the prefab at the default position with no parent initially
            GameObject abilityButton = Instantiate(_abilityButtonPrefab, _learnedAbilityGrid.transform.position, Quaternion.identity, _learnedAbilityGrid.transform);

            abilityButton.transform.localRotation = Quaternion.identity;

            // Set the button properties or load the specific ability
            AbilityButton abilityButtonComponent = abilityButton.GetComponent<AbilityButton>();
            abilityButtonComponent.LoadAbility(LoadedState.LearnedAbilities[i], AbilityButtonType.Known);
        }
    }

    #endregion

    #region RefreshShared
    public void RefreshSharedElements()
    {
        RefreshLoadOutPanel();
        RefreshPortrait();
        RefreshXPBar();
        _levelText.text = LoadedState.Level.ToString();
        _growthPointDisplay.text = LoadedState.StatDict[Stat.Unspent].ToString();
    }

    private void RefreshHeldItemDisplay() //redirect this to HeldItemPicker
    {
        if (_heldItemDisplay == null) { Debug.Log("No valid Image component reference"); return; }
        if (LoadedState == null) { Debug.Log("No beastState in slot"); _heldItemDisplay.enabled = false; }
        else if (LoadedState.HeldItem != null)
        {
            _heldItemDisplay.enabled = true;
            _heldItemDisplay.sprite = Item.HeldDex[LoadedState.HeldItem.ID].Icon;

        }
        else
        {
            _heldItemDisplay.enabled = true;
            _heldItemDisplay.sprite = _heldItemPlaceHolder;
        }
    }

    private void RefreshXPBar()
    {
        if (_xpBar == null || LoadedState == null) return;
        _xpBar.value = (float)LoadedState.StatDict[Stat.XP] / LoadedState.XPToNextLevel + 0.02f;
        _xpValueText.text = $"{LoadedState.StatDict[Stat.XP]}/{LoadedState.XPToNextLevel}";
    }

    private void RefreshLoadOutPanel()
    {
        _loadOutButtons[0].LoadAbility(Ability.GetInstance(LoadedState.MovementAbility), AbilityButtonType.LoadOutSlot);
        int numSkillButtons = LoadedState.SkillAbilities.Length;
        for (int i = 1; i < numSkillButtons + 1; i++) _loadOutButtons[i].LoadAbility(Ability.GetInstance(LoadedState.SkillAbilities[i - 1]), AbilityButtonType.LoadOutSlot);
        RefreshHeldItemDisplay();
    }

    private void RefreshPortrait()
    {
        _beastPortrait.enabled = true;
        StopAllCoroutines();
        string dir = OpenTab == SummaryTab.AbilityViewer ? "R" : "F";
        StartCoroutine(AnimationRoutine(dir));
    }

    private void ClearSelf()
    {
        _levelText.text = "";
        _beastPortrait.enabled = false;
        if (_xpBar) _xpBar.value = 0;
    }
    #endregion

    #region Animations
    private IEnumerator AnimationRoutine(string direction)
    {
        int currentFrame = 0;
        int spriteCount = LoadedState.AnimationDictionary[direction].Count;
        while (true)
        {
            if (CurrentlyEvolving && evolDict!=null) _beastPortrait.sprite = evolDict[direction][currentFrame];
            else _beastPortrait.sprite = LoadedState.AnimationDictionary[direction][currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator EvolutionAnimationRoutine(Dictionary<string, List<Sprite>>[] animationDicts, float duration = 1)
    {
        // Define max intensity value for easy adjustment
        float startingIntensity = _evolLight.intensity;
        float maxIntensity = 3.5f; // Feel free to experiment with this value

        // Phase 1: Build Up
        StartCoroutine(EvolutionParticleRoutine(duration));
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            _evolLight.intensity = Mathf.Lerp(startingIntensity, maxIntensity, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        _evolLight.intensity = maxIntensity; // Ensure it hits the max intensity

        // Phase 2: Maintain and Animate
        float phase2Duration = duration * 3;
        int numFlips = 12;
        float flipDuration = phase2Duration / numFlips; // Split the phase duration into parts for flipping
        for (int i = 0; i < numFlips; i++)
        {
            evolDict = animationDicts[i % 2]; // Flip back and forth
            _beastPortrait.sprite = evolDict["F"][0];
            yield return new WaitForSeconds(flipDuration); // Wait for flipDuration before flipping again
        }

        // Phase 3: Cool Down
        evolDict = null;
        LoadedState.EvolveSelf();
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            _evolLight.intensity = Mathf.Lerp(maxIntensity, startingIntensity, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        _evolLight.intensity = startingIntensity; // Ensure it cools down to 0 intensity

        // Wrapping up
        LoadBeast(LoadedState);
        CurrentlyEvolving = false;
    }

    private IEnumerator EvolutionParticleRoutine(float duration)
    {
        yield return new WaitForSeconds(duration*0.6f);
        int numTotalBursts = 12;
        float waitBetweenBurst = duration*3 / numTotalBursts;
        for (int i=0; i<numTotalBursts;i++)
        {
            yield return new WaitForSeconds(waitBetweenBurst);
            GameObject emitter = GameManager.GetParticleEmitter();
            emitter.transform.position = _beastPortrait.transform.position;
            emitter.transform.rotation = Quaternion.identity;
            ParticleEmitterBrain emitterBrain = emitter.GetComponent<ParticleEmitterBrain>();
            emitterBrain.SetEvolutionBuildUp();
            emitterBrain.SetUI();
            emitterBrain.SetParticleSize(0.02f);
            emitter.SetActive(true);
        }
        yield return new WaitForSeconds(duration * 0.8f);

        // do final smoke puff
        GameObject puffEmitter = GameManager.GetParticleEmitter();
        puffEmitter.transform.position = _beastPortrait.transform.position;
        puffEmitter.transform.rotation = Quaternion.identity;
        ParticleEmitterBrain puffEmitterBrain = puffEmitter.GetComponent<ParticleEmitterBrain>();
        puffEmitterBrain.SetUI();
        puffEmitterBrain.SetEvolutionClimax();
        puffEmitterBrain.SetParticleSize(0.05f);
        puffEmitter.SetActive(true);
    }
    #endregion

    public void ExpandAbility(AbilityButton invokingButton)
    {
        Color movementTypeColor = new Color(255/ 255f, 190/ 255f, 88/ 255f);
        Color skillTypeColor = new Color(183/ 255f, 163/ 255f, 255/ 255f);

        ExpandedAbility = invokingButton.LoadedAbility;
        if (ExpandedAbility.Name == "None") { LoadPage(SummaryTab.AbilityViewer); return; }
        LoadPage(SummaryTab.AbilityDetails);
        _expandedAbilityImage.sprite = ExpandedAbility.GetSplashArt();
        AbilityFrameType frameStyle = ExpandedAbility.ThisAbilityType == AbilityType.Skill ? AbilityFrameType.Default : AbilityFrameType.Movement;
        _expandedAbilityImageFrame.sprite = AbilityButton.FrameStyleDict[frameStyle];
        _abilityInfoTitle.text = ExpandedAbility.Name;
        _abilityInfoBody.text = ExpandedAbility.Description;
        _abilityInfoType.text = $"[ {ExpandedAbility.ThisAbilityType.ToString().ToUpper()} ]";
        _abilityInfoType.color = ExpandedAbility.ThisAbilityType == AbilityType.Movement ? movementTypeColor : skillTypeColor;
        _abilityInfoLearnCost.text = $"LEARN COST: [ {ExpandedAbility.LearnCost} ]";
        _abilityInfoBody.text += $"\n\nCooldown: {ExpandedAbility.CoolDownTime}s";
        if (ExpandedAbility.ScalingStats.Count>0)_abilityInfoBody.text += $"\nScaling: [ {ExpandedAbility.ScalingStats[0]} ] x {ExpandedAbility.ScaleRates[0]}";

        Debug.Log("expandAbility called");
        Debug.Log($"loadedstate availableabilities count: {LoadedState.AvailableAbilities.Count}");
        Debug.Log($"loadedstate learnedabilities count: {LoadedState.LearnedAbilities.Count}");
        //available, not learned
        if (LoadedState.AvailableAbilityNames.Contains(ExpandedAbility.Name) && !LoadedState.LearnedAbilityNames.Contains(ExpandedAbility.Name))
        {
            Debug.Log("available, not learned");
            _expandedAbilityActionButton.Function = "LearnAbility";
            _expandedAbilityActionButton.ModifyText("LEARN");
            if (LoadedState.StatDict[Stat.Unspent] < ExpandedAbility.LearnCost) _expandedAbilityActionButton.UpdateButtonState(ButtonState.B_State);
            else _expandedAbilityActionButton.UpdateButtonState(ButtonState.A_State);
        }
        //learned, not equipped
        if (LoadedState.LearnedAbilityNames.Contains(ExpandedAbility.Name) && LoadedState.MovementAbility!=ExpandedAbility.Name && !LoadedState.SkillAbilities.Contains(ExpandedAbility.Name))
        {
            Debug.Log("learned, not equipped");
            _expandedAbilityActionButton.Function = "EquipAbility";
            _expandedAbilityActionButton.ModifyText("EQUIP");
            _expandedAbilityActionButton.UpdateButtonState(ButtonState.A_State);
        }

        //learned and equipped
        if (LoadedState.LearnedAbilityNames.Contains(ExpandedAbility.Name) && (LoadedState.MovementAbility == ExpandedAbility.Name || LoadedState.SkillAbilities.Contains(ExpandedAbility.Name)))
        {
            Debug.Log("learned and equipped");
            _expandedAbilityActionButton.Function = "UnequipAbility";
            _expandedAbilityActionButton.ModifyText("UNEQUIP");
            _expandedAbilityActionButton.UpdateButtonState(ButtonState.A_State);
        }
        //activate or deactivate the learn button on the basis of if the ability has been learned already, can afford cost, etc
    }

    public void TryEvolution()
    {
        //temporary stop measure until 3rd level forms are added
        int[] soupInt = LoadedState.GetEvolutionEvolID();
        Debug.Log($"checking evolution to ID: {soupInt[0]},{soupInt[1]}");
        if (LoadedState.EvolutionID[0] == 2 || LoadedState.GetEvolutionEvolID().SequenceEqual(new int[] { 0, 0 })) return;
        //create array of animation dictionaries. 0 -> pre-evol  :  1 -> post-evol
        CurrentlyEvolving = true;
        Dictionary<string, List<Sprite>>[] animationDicts = new Dictionary<string, List<Sprite>>[2];
        animationDicts[0] = BeastBlueprint.Dex[BeastBlueprint.GenerateStringDexKey(LoadedState.EvolutionID)].AnimationDictionary;
        animationDicts[1] = BeastBlueprint.Dex[BeastBlueprint.GenerateStringDexKey(LoadedState.GetEvolutionEvolID())].AnimationDictionary;
        StartCoroutine(EvolutionAnimationRoutine(animationDicts)); 
    }

    public void ModifyStatValue(Stat stat, bool adding)
    {
        if (LoadedState.StatDict[Stat.Unspent] == 0 & adding) return;
        int differenceToApply;
        if (adding) differenceToApply = 1;
        else differenceToApply = -1;
        if (stat == Stat.MaxHP)
        {
            LoadedState.StatDict[stat] += differenceToApply * 10;
            if (LoadedState.StatDict[Stat.CurrentHP]>0) LoadedState.StatDict[Stat.CurrentHP] += differenceToApply * 10;
        }

        else LoadedState.StatDict[stat] += differenceToApply;
        LoadedState.StatDict[Stat.Unspent] -= differenceToApply;

        RefreshStatWidget();
        GameManager.UImanager.RefreshProfilePanels();
    }

    private void UpdateItemPanels(ItemType type)
    {
        List<KeyValuePair<int, int>> itemList = GameManager.PlayerInventory.GetListByItemType(type);
        itemList.RemoveAll(item => item.Value < 1);
        WipeItemPanels();
        for (int i = 0; i < itemList.Count+1; i++)
        {
            ItemPanelController currentPanel = GameObject.Instantiate(_itemPanelPrefab, _content.transform).GetComponent<ItemPanelController>();
            currentPanel.TryInit(i, null, null, this);
            currentPanel.ClearSelf();
            if (i == 0) currentPanel.LoadItem(Item.NoneItem, 1);
            else currentPanel.LoadItem(Item.GetInstanceByKey(itemList[i - 1].Key, type), itemList[i - 1].Value);
            _itemPanelDict.Add(i, currentPanel);
        }
        StartCoroutine(ScrollBarEnforcer());
    }

    public IEnumerator ScrollBarEnforcer()
    {
        float duration = 0.1f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (_pickerScrollbar.value != 1) { _pickerScrollbar.value = 1; break; }
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    private void WipeItemPanels()
    {
        for (int i = 0; i < _itemPanelDict.Count; i++)
        {
            GameObject.Destroy(_itemPanelDict[i].gameObject);
        }
        _itemPanelDict.Clear();
    }

    #region Navigation
    public void IncrementTab(int direction)
    {
        // Ensure there are beasts to cycle through
        if (GameManager.PartyBeastStates.Count <2) return;

        // Find the current index of LoadedState in the party list
        int currentIndex = GameManager.PartyBeastStates.IndexOf(LoadedState);

        // Calculate the new index with wrapping
        int newIndex = (currentIndex + direction + GameManager.PartyBeastStates.Count) % GameManager.PartyBeastStates.Count;

        // Load the beast at the new index
        LoadBeast(GameManager.PartyBeastStates[newIndex]);
    }

    public void LoadPage(SummaryTab summaryPage)
    {
        if (OpenTab!=SummaryTab.ConsumablePicker & OpenTab!=SummaryTab.HeldItemPicker) LastOpenTab = OpenTab;
        OpenTab = summaryPage;

        // turn all pages off
        _sharedElements.enabled=false;
        _beastPanel.enabled = false;
        _abilitiesPanel.enabled = false;
        _itemPickerPage.enabled = false;
        _beastPanelButton.UpdateButtonState(ButtonState.B_State);
        _abilitiesPanelButton.UpdateButtonState(ButtonState.B_State);

        //turn on specified page. Switch?
        //now initialize elements on the respective pages bub
        switch (summaryPage)
        {
            case SummaryTab.Beast:
                _beastPanel.enabled = true;
                _sharedElements.enabled = true;
                _beastPanelButton.UpdateButtonState(ButtonState.A_State);
                LoadBeast(LoadedState,true);
                break;
            case SummaryTab.AbilityViewer:
                _abilitiesPanel.enabled = true;
                _sharedElements.enabled = true;
                _abilityViewer.SetActive(true);
                _abilityDetails.SetActive(false);
                _abilitiesPanelButton.UpdateButtonState(ButtonState.A_State);
                LoadBeast(LoadedState,true);
                break;
            case SummaryTab.AbilityDetails:
                _abilitiesPanel.enabled = true;
                _sharedElements.enabled = true;
                _abilityViewer.SetActive(false);
                _abilityDetails.SetActive(true);
                _abilitiesPanelButton.UpdateButtonState(ButtonState.A_State);
                LoadBeast(LoadedState, true);
                break;
            case SummaryTab.HeldItemPicker:
                _itemPickerPage.enabled = true;
                _beastPanelButton.UpdateButtonState(ButtonState.A_State);
                _heldItemPickerButton.ToggleHoverStyle();
                UpdateItemPanels(ItemType.Held);
                _pickerTitleText.text = "EQUIP?";
                break;
            case SummaryTab.ConsumablePicker:
                _itemPickerPage.enabled = true;
                _beastPanelButton.UpdateButtonState(ButtonState.A_State);
                _feederButton.ToggleHoverStyle();
                UpdateItemPanels(ItemType.Consumable);
                _pickerTitleText.text = "POWER UP WITH:";
                break;
        }

        //update the button states
    }

    public void LoadLastPage()
    {
        if (LastOpenTab == SummaryTab.AbilityDetails) LastOpenTab = SummaryTab.AbilityViewer;
        LoadPage(LastOpenTab);
    }
    #endregion
}
