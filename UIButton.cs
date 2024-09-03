using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ButtonState
{
    A_State,
    B_State,
    Hovered,
    Clicked
}

public class UIButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    protected string _gameObjectName;
    public string Function;
    protected ButtonState _state = ButtonState.A_State;
    protected UIManager _UImanager;
    protected bool _isBeingHovered;
    protected bool _isBeingPressed;
    protected bool _defaultValuesCollected = false;
    //bg alpha (as proportion of orginal), icon alpha, icon scale (as proportion of original scale)
    protected Dictionary<ButtonState, float[]> _stateToDimensions = new Dictionary<ButtonState, float[]>() 
    {
        { ButtonState.A_State, new float[3] { 35/255f, 1, 1 } },
        { ButtonState.B_State, new float[3] { 10/255f, 0.5f, 1 } },
        { ButtonState.Hovered, new float[3] { 70/255f, 1, 1 } },
        { ButtonState.Clicked, new float[3] { 70 / 255f, 1, 0.9f } },
    };

    //default values
    protected float _defaultBG_Alpha;
    protected float _defaultTextMainAlpha;
    protected float _defaultTextShadowAlpha;
    protected Vector3 _defaultIconScale;
    protected Vector3 _defaultTextScale;

    //components
    protected GameObject _bgObject;
    protected Image _bg;

    protected GameObject _iconObject;
    protected Image _icon;

    protected GameObject _textObject;
    protected Text _textMain;

    public virtual void OnEnable()
    {
        _gameObjectName = gameObject.name;
        _UImanager = GameManager.UImanager;
        if (this.GetType() == typeof(UIButton)) //is a UIbutton class ?
        {
            GetComponents();
            GetDefaultValues();
            UpdateButtonState(ButtonState.A_State);
        }
    }

    public virtual void GetComponents()
    {
        _bgObject = transform.Find("BG").gameObject;
        _bg = _bgObject.GetComponent<Image>();

        _iconObject = transform.Find("ICON").gameObject;
        _icon = _iconObject.GetComponent<Image>();

        _textObject = transform.Find("TEXT").gameObject;
        _textMain = transform.Find("TEXT/textMain").gameObject.GetComponent<Text>();
    }

    public virtual void GetDefaultValues()
    {
        if (_defaultValuesCollected) return;
        if (_bg!=null)_defaultBG_Alpha = _bg.color.a;
        if (_textMain != null ) _defaultTextMainAlpha = _textMain.color.a;
        if (_iconObject!= null) _defaultIconScale = _iconObject.transform.localScale;
        if (_textObject != null) _defaultTextScale = _textObject.transform.localScale;
        _defaultValuesCollected = true;
    }

    public virtual void UpdateButtonState(ButtonState newState)
    {
        _state = newState;
        //bg alpha
        float newBGAlpha = _stateToDimensions[newState][0];
        if (_bg != null) _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        //icon / text alpha
        float newIconTextAlpha = _stateToDimensions[newState][1];
        if (_textMain != null) _textMain.color = new Color(_textMain.color.r, _textMain.color.g, _textMain.color.b, newIconTextAlpha * _defaultTextMainAlpha);
        if (_icon != null) { _icon.color = new Color(_icon.color.r, _icon.color.g, _icon.color.b, newIconTextAlpha); }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (SceneManager.GetActiveScene().name == "Main Menu" && !((Function == "MainMenu_NEXT" & _UImanager.GetActivePageIndex() == 2) || _UImanager.GetActivePageIndex() == 3)) 
        {
            EggChoiceButton.TurnOffAllSelectorIndicators();
            GameManager.PartyBeastStates = new List<BeastState>() { };
        }
        if (Function == "PLAY") GameManager.GetInstance().UpdateScene("WildsMap"); 
        if (Function == "EXIT") Application.Quit(); 
        if (Function == "MainMenu_ARENA") _UImanager.UpdateMainMenuArenaState(false,MainMenuArenaState.StyleSelect);
        if (Function == "MainMenu_ARENA_BACK") _UImanager.UpdateMainMenuArenaState(true);
        if (Function == "MainMenu_ARENA_QUICK") _UImanager.UpdateMainMenuArenaState(false, MainMenuArenaState.QuickBattle);
        if (Function == "MainMenu_ARENA_CUSTOM") _UImanager.UpdateMainMenuArenaState(false, MainMenuArenaState.CustomBattle);
        if (Function == "MainMenu_NEXT") _UImanager.IncrementBeginGameProgression(); 
        if (Function == "MainMenu_BACK") _UImanager.DecrementBeginGameProgression();
        if (Function == "HOME") GameManager.GetInstance().RestartGame();
        if (Function == "PASSPLACEHOLDER") GameManager.EventManager.DeactivateEvent();
        if (Function == "ITEMCHOICECONFIRMATION") GameManager.EventManager.ConfirmItemSelectionChoice();
        if (Function == "STARTBATTLE")
        {
            BattleManager.Instance.BeginBattle();
            gameObject.SetActive(false);
        }
        if (Function == "INVENTORY" && !BeastSummaryController.Instance.CurrentlyEvolving) GameManager.UImanager.ToggleInventoryUI();
        if (Function == "ADVANCEENDBATTLE") EndBattleManager.Instance.IncrementProgressionLevel();
        if (Function == "SelectionCancel") BagUIController.Instance.ToggleSelectionPanel();
        if (Function == "EndBattleFeederCancel") EndBattleManager.Instance.ToggleFeederWidget(null);
        if (Function == "SummExit" && !BeastSummaryController.Instance.CurrentlyEvolving) GameManager.UImanager.ToggleBeastSummary();
        if (Function == "SummTabUp" && !BeastSummaryController.Instance.CurrentlyEvolving) BeastSummaryController.Instance.IncrementTab(1);
        if (Function == "SummTabDown" && !BeastSummaryController.Instance.CurrentlyEvolving) BeastSummaryController.Instance.IncrementTab(-1);
        if (Function == "Summ_Back") BeastSummaryController.Instance.LoadLastPage();
        if (Function == "SummaryFeeder" && !BeastSummaryController.Instance.CurrentlyEvolving){
            BeastSummaryController.Instance.LoadPage(SummaryTab.ConsumablePicker);}
        if (Function == "SummaryAbilityViewer") BeastSummaryController.Instance.LoadPage(SummaryTab.AbilityViewer);
        if (Function == "LearnAbility")
        {
            BeastState loadedState = BeastSummaryController.Instance.LoadedState;
            Ability toLearnAbility = BeastSummaryController.Instance.ExpandedAbility;
            if (loadedState.StatDict[Stat.Unspent]>=toLearnAbility.LearnCost)
            {
                BeastSummaryController.Instance.LoadedState.LearnAbility(BeastSummaryController.Instance.ExpandedAbility);
                BeastSummaryController.Instance.LoadPage(SummaryTab.AbilityViewer);
                BeastSummaryController.Instance.TryEvolution();
            }
        }
        if (Function == "EquipAbility")
        {
            BeastSummaryController.Instance.LoadedState.LoadAbility(BeastSummaryController.Instance.ExpandedAbility);
            BeastSummaryController.Instance.RefreshSharedElements();
            GameManager.UImanager.RefreshProfilePanels();
            BeastSummaryController.Instance.LoadPage(SummaryTab.AbilityViewer);
        }
        if (Function == "UnequipAbility")
        {
            Ability ability = BeastSummaryController.Instance.ExpandedAbility;
            if (ability.ThisAbilityType==AbilityType.Movement) BeastSummaryController.Instance.LoadedState.MovementAbility = "None";
            else
            {
                // Find the index of the ability to replace
                int index = Array.IndexOf(BeastSummaryController.Instance.LoadedState.SkillAbilities, ability.Name);
                if (index != -1) // Make sure the ability is found
                {
                    BeastSummaryController.Instance.LoadedState.SkillAbilities[index] = "None";
                }
            }
            BeastSummaryController.Instance.RefreshSharedElements();
            GameManager.UImanager.RefreshProfilePanels();
            BeastSummaryController.Instance.LoadPage(SummaryTab.AbilityViewer);
        }
        if (Function == "FORFEIT")
        {
            Team winningTeam = GameManager.PlayingCampaign ? Team.Team2 : Team.None;
            BattleManager.Instance.ForceEndBattle(winningTeam);
        }
        if (Function == "LoadQuickBattle") QuickBattleMenuController.Instance.StartBattle();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        //if (_state != ButtonState.B_State) UpdateButtonState(ButtonState.Clicked);
        _isBeingPressed = true;
        ToggleClickedStyle();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        //if (_state != ButtonState.B_State) UpdateButtonState(ButtonState.Hovered);
        _isBeingPressed = false;
        ToggleClickedStyle();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _isBeingHovered = true;
        ToggleHoverStyle();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _isBeingHovered = false;
        ToggleHoverStyle();
    }

    public virtual void ToggleHoverStyle()
    {
        if (_state == ButtonState.B_State) return;
        if (_isBeingHovered)
        {
            float newBGAlpha = _stateToDimensions[ButtonState.Hovered][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
        else
        {
            float newBGAlpha = _stateToDimensions[ButtonState.A_State][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
    }

    public virtual void ToggleClickedStyle()
    {
        if (_isBeingPressed)
        {
            if (_textObject != null) { _textObject.transform.localScale = _defaultTextScale * _stateToDimensions[ButtonState.Clicked][2]; }
            if (_iconObject != null) { _iconObject.transform.localScale = _defaultIconScale * _stateToDimensions[ButtonState.Clicked][2]; }
        }
        else
        {
            if (_textObject != null) { _textObject.transform.localScale = _defaultTextScale * _stateToDimensions[ButtonState.A_State][2]; }
            if (_iconObject != null) { _iconObject.transform.localScale = _defaultIconScale * _stateToDimensions[ButtonState.A_State][2]; }
        }
    }

    public virtual void ToggleStateStyle()
    {
        if (_state == ButtonState.A_State) UpdateButtonState(ButtonState.B_State);
        else UpdateButtonState(ButtonState.A_State);
    }

    public void ManualHoverShutoff()
    {
        _isBeingHovered = false;
        ToggleHoverStyle();
    }

    public void ModifyText(string input)
    {
        _textMain.text = input;
    }
}
