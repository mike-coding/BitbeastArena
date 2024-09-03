using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public enum AbilityButtonType // help determine what to do when clicked on
{
    Known,
    CanLearn,
    LoadOutSlot,
    DisplayOnly,
    RunStartWidget
}

public enum AbilityFrameType
{
    Default,
    Movement,
    Locked
}

public class AbilityButton : UIButton
{
    // static data
    public static Dictionary<AbilityFrameType, Sprite> FrameStyleDict = new Dictionary<AbilityFrameType, Sprite>() { };
    private static bool _framesLoaded { get { return FrameStyleDict.Count > 0; } }
    private static float _defaultFilterAlpha = 20 / 255f; //originally 60

    // GameObjects, Components
    private GameObject _buttonBody;
    private Image _art;
    private Image _filter;
    private Image _frame;

    // State Tracking
    private BeastState _loadedState
    {
        get
        {
            if (_type != AbilityButtonType.RunStartWidget)
            {
                return BeastSummaryController.Instance.LoadedState;
            }
            else return RunStartAbilityWidget.CurrentInstance.SelectedBeast;
        }
    }
    private AbilityButtonType _type;
    public Ability LoadedAbility;
    private AbilityType _loadedAbilityType { get { return LoadedAbility.ThisAbilityType; } }
    private bool _initComplete = false;

    public void Init()
    {
        if (!_framesLoaded) LoadStaticFrameImages();
        GetComponents();
        GetDefaultValues();
        _initComplete = true;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (_type == AbilityButtonType.DisplayOnly) return;
        if (BeastSummaryController.Instance != null && BeastSummaryController.Instance.CurrentlyEvolving) return;
        if (eventData.button == PointerEventData.InputButton.Left) HandleLeftClick();
        else if (eventData.button == PointerEventData.InputButton.Right) HandleRightClick();
    }

    private void HandleLeftClick()
    {
        if (_type == AbilityButtonType.RunStartWidget) RunStartAbilityWidget.CurrentInstance.ModifyAbilities(LoadedAbility.Name);
        else BeastSummaryController.Instance.ExpandAbility(this);
    }

    private void HandleRightClick()
    {
        if (_type == AbilityButtonType.LoadOutSlot) //most of this should be moved to BeastState
        {
            _loadedState.UnloadAbility(LoadedAbility);
            BeastSummaryController.Instance.RefreshSharedElements();
            GameManager.UImanager.RefreshProfilePanels();
        }
    }

    public override void GetComponents()
    {
        _buttonBody = transform.Find("Body").gameObject;
        _art = transform.Find("Body/IconArt").gameObject.GetComponent<Image>();
        _filter = transform.Find("Body/Filter").gameObject.GetComponent<Image>();
        _frame = transform.Find("Body/Frame").gameObject.GetComponent<Image>();

        //UIButton vars
        _bgObject = _filter.gameObject;
        _bg = _filter;
        _iconObject = _buttonBody;

    }

    public void LoadStaticFrameImages()
    {
        // Load the entire sheet
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/UI/Tiles/tree");

        // Extract the specific sprites by name
        FrameStyleDict = new Dictionary<AbilityFrameType, Sprite>()
    {
        { AbilityFrameType.Default, sprites[0] },
        { AbilityFrameType.Movement, sprites[33] },
        { AbilityFrameType.Locked, sprites[34] }
    };
    }

    public void LoadAbility(Ability ability, AbilityButtonType type)
    {
        if (!_initComplete) Init();
        LoadedAbility = ability;
        _type = type;
        _art.sprite = LoadedAbility.GetSplashArt();
        _filter.color = new Color(_filter.color.r, _filter.color.g, _filter.color.b, _defaultFilterAlpha);

        RefreshFrameStyle();
    }

    public void RefreshFrameStyle()
    {
        if (_type == AbilityButtonType.LoadOutSlot || _type==AbilityButtonType.DisplayOnly) return;
        //if Not learned FIRST check if prereqs are met, and if NOT: locked
        if (!LoadedAbility.PrerequisitesMet(_loadedState))
        {
            _frame.sprite = FrameStyleDict[AbilityFrameType.Locked];
        }
        else
        {
            if (LoadedAbility.ThisAbilityType == AbilityType.Movement) _frame.sprite = FrameStyleDict[AbilityFrameType.Movement];
            else _frame.sprite = FrameStyleDict[AbilityFrameType.Default];
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (_type != AbilityButtonType.DisplayOnly) base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (_type != AbilityButtonType.DisplayOnly) base.OnPointerUp(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (_type != AbilityButtonType.DisplayOnly) base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (_type != AbilityButtonType.DisplayOnly) base.OnPointerExit(eventData);
    }
}
