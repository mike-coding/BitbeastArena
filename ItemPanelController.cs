using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemPanelController : UIButton
{
    // ControllerTypeDeterminants
    // One of these three will be a valid reference
    // the other two will be null
    private BagUIController _bagUIController;
    private bool _isBagInstance {  get { return _bagUIController != null; } }
    private EndBattleManager _endBattleManager;
    private bool _isEndBattleInstance { get { return _endBattleManager!= null; } }
    private BeastSummaryController _beastSummaryController;
    private bool _isSummaryInstance { get { return _beastSummaryController!=null; } }


    private int _positionIndex;
    private Text _quantityText;
    private Text _nameText;
    bool _initComplete;

    public Item LoadedItem;
    public int LoadedItemQuantity;

    private bool _itemPanelComponentsCollected = false;

    //bg alpha (as proportion of orginal), icon alpha, icon scale (as proportion of original scale)
    new protected Dictionary<ButtonState, float[]> _stateToDimensions = new Dictionary<ButtonState, float[]>()
    {
        { ButtonState.A_State, new float[3] { 35/255f, 1, 1 } },
        { ButtonState.B_State, new float[3] { 100/255f, 1f, 1 } },
        { ButtonState.Hovered, new float[3] { 55/255f, 1, 1 } },
        { ButtonState.Clicked, new float[3] { 55/ 255f, 1, 0.9f } },
    };

    public override void GetComponents() //UIButton
    {
        _bg = transform.Find("BG").gameObject.GetComponent<Image>();
        _iconObject = transform.Find("Content").gameObject;
        _icon = _iconObject.transform.Find("Icon").gameObject.GetComponent<Image>();
    }

    private void GetItemPanelComponents() //ItemPanel-Specific
    {
        if (_itemPanelComponentsCollected) return;
        _quantityText = transform.Find("Content/Quantity").gameObject.GetComponent<Text>();
        _nameText = transform.Find("Content/ItemName").gameObject.GetComponent<Text>();
        _itemPanelComponentsCollected = true;
    }

    public void TryInit(int indexPosition, BagUIController parent = null, EndBattleManager endBattleManager = null, BeastSummaryController summaryParent=null)
    {
        if (_initComplete) return;
        _positionIndex = indexPosition;
        _bagUIController = parent;
        _endBattleManager = endBattleManager;
        _beastSummaryController = summaryParent;
        GetComponents();
        GetItemPanelComponents();
        GetDefaultValues();
        _initComplete = true;
    }

    public void LoadItem(Item item, int quantity)
    {
        GetItemPanelComponents();
        if (quantity < 1) { ClearSelf(); return; }
        _icon.enabled = true;
        _icon.sprite = Resources.Load<Sprite>($"Sprites/Items/{item.Type}/{item.ID}");
        if (item.Type != ItemType.Null) _quantityText.text = "x" + quantity.ToString();
        else _quantityText.text = "";
        _nameText.text = item.Name;

        LoadedItem = item;
        LoadedItemQuantity = quantity;
    }

    public void ClearSelf()
    {
        GetItemPanelComponents();
        LoadedItem = null;
        _icon.enabled = false;
        _quantityText.text = "";
        _nameText.text = "";
        if (_state == ButtonState.B_State) ToggleStateStyle();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        //if button is deselected, pass its index to be the new selected panel
        //if the button is already selected, update to -1 to indicate no selection (reset selection)
        if (LoadedItem == null) return;
        int indexToPass = _state == ButtonState.A_State ? _positionIndex : -1; 

        //below is hacky. discern better
        if (_bagUIController!=null) _bagUIController.UpdateSelectedItemPanel(indexToPass);
        if (GameManager.CurrentScene.name != "WildsMap") EndBattleManager.Instance.ProcessBeastFeeding(LoadedItem);
        if (_beastSummaryController != null) HandleSummaryClick();

    }

    //discern equipping and feeding
    private void HandleSummaryClick()
    {
        if (LoadedItem.Type == ItemType.Held || LoadedItem.Type ==ItemType.Null)
        {
            //return currently held item to inventory if it's equipped
            Item currentlyHeldItem = _beastSummaryController.LoadedState.HeldItem;
            if (currentlyHeldItem != null && currentlyHeldItem.Type != ItemType.Null) GameManager.PlayerInventory.DepositItem(currentlyHeldItem);

            //equip new item
            if (LoadedItem.Type != ItemType.Null)
            {
                _beastSummaryController.LoadedState.HeldItem = LoadedItem;
                GameManager.PlayerInventory.WithdrawItem(LoadedItem, 1);
            }
            else _beastSummaryController.LoadedState.HeldItem = null;
        }
        else if (LoadedItem.Type == ItemType.Consumable)
        {
            if (LoadedItem.Type != ItemType.Null)
            {
                _beastSummaryController.LoadedState.StatDict[LoadedItem.StatToModify] += LoadedItem.StatModificationIncrement;
                if (LoadedItem.StatToModify == Stat.MaxHP) _beastSummaryController.LoadedState.StatDict[Stat.CurrentHP] = _beastSummaryController.LoadedState.StatDict[Stat.MaxHP];
                GameManager.PlayerInventory.WithdrawItem(LoadedItem, 1);
            }
        }

        //return to beastpanel
        _beastSummaryController.LoadBeast(_beastSummaryController.LoadedState);
        _beastSummaryController.LoadLastPage();
        GameManager.UImanager.RefreshProfilePanels();
        ManualHoverShutoff();
    }

    public bool TestState(ButtonState desiredState)
    {
        return _state == desiredState;
    }

    public override void UpdateButtonState(ButtonState newState)
    {
        _state = newState;
        //bg alpha
        float newBGAlpha = _stateToDimensions[newState][0];
        _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        //icon / text alpha
        float newIconTextAlpha = _stateToDimensions[newState][1];
        if (_textMain != null) _textMain.color = new Color(_textMain.color.r, _textMain.color.g, _textMain.color.b, newIconTextAlpha * _defaultTextMainAlpha);
        if (_icon != null) { _icon.color = new Color(_icon.color.r, _icon.color.g, _icon.color.b, newIconTextAlpha); }
    }

    public override void ToggleHoverStyle()
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
}
