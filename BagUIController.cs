using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BagState
{
    Default,
    Selection
}
public class BagUIController : MonoBehaviour
{
    public static BagUIController Instance;
    private Dictionary<int, ItemPanelController> _itemPanelDict = new Dictionary<int, ItemPanelController>();
    private List<BeastStatusPanel> _beastSelectorPanels = new List<BeastStatusPanel>();
    private ItemType OpenTab = ItemType.Food;
    private Item _selectedItem;
    private bool _initComplete=false;

    // ancillary button components
    private UIButton _bagButton;
    private BagTabButton _heldItemsButton;
    private BagTabButton _foodItemsButton;
    private BagTabButton _consumableItemsButton;
    private BagTabButton _exitButton;

    // details panel components
    private FeedEquipButton _feedEquipButton;
    private Text _feedEquipText;
    private Text _itemNameText;
    private Text _itemTypeText;
    private Text _itemRarityText;
    private Text _feedValueText;
    private Text _itemDescriptionText;
    private Image _itemPortrait;
    private Text _selectionPanelText;
    private Scrollbar _panelScrollBar;

    // page references
    private GameObject _mainPanel;
    private GameObject _detailsPanel;
    private GameObject _selectionPanel;

    //BSP shit
    private List<BeastStatusPanel> _profilePanels = new List<BeastStatusPanel>();
    private GameObject _profilePanelPrefab;
    private GameObject _selectorPanelBSPGrid;

    // item panel prefab
    private GameObject _itemPanelPrefab;
    //item panel placement location
    private GameObject _content;
    private ContentSizeFitter _contentFitter;

    void OnEnable() 
    {
        Instance=this;
        _bagButton = GameManager.MainCamera.transform.Find("Canvas/UI/SidePanelUI/BAGBUTTON").gameObject.GetComponent<UIButton>();
        _bagButton.UpdateButtonState(ButtonState.B_State);
        if (!_initComplete) GetReferences();
        if (_selectionPanel.activeInHierarchy) ToggleSelectionPanel();
        UpdateOpenTab();
        _panelScrollBar.value = 1;
    }

    void OnDisable()
    {
        _bagButton.UpdateButtonState(ButtonState.A_State);
    }

    private void GetReferences()
    {
        _profilePanelPrefab = Resources.Load<GameObject>("gameObjects/UI/BeastStatusPanel");
        _mainPanel = transform.Find("MainPanel").gameObject;
        _detailsPanel = transform.Find("DetailsPanel").gameObject;
        _selectionPanel = transform.Find("BeastSelectorPanel").gameObject;
        GameObject scrollArea = _mainPanel.transform.Find("ScrollArea").gameObject;
        _content = scrollArea.transform.Find("Content").gameObject;
        _contentFitter = _content.GetComponent<ContentSizeFitter>();

        //for (int i=0;i<_numItemPanels;i++) _itemPanelDict[i] = content.transform.Find($"ItemPanel ({i})").gameObject.GetComponent<ItemPanelController>();
        _itemPanelPrefab = Resources.Load<GameObject>("gameObjects/UI/ItemPanel-MainBag").gameObject;
        _consumableItemsButton = transform.Find("MainPanel/ConsumableTab").gameObject.GetComponent<BagTabButton>();
        _consumableItemsButton.TryInit(this);
        _foodItemsButton = transform.Find("MainPanel/FoodTab").gameObject.GetComponent<BagTabButton>();
        _foodItemsButton.TryInit(this);
        _heldItemsButton = transform.Find("MainPanel/HeldItemTab").gameObject.GetComponent<BagTabButton>();
        _heldItemsButton.TryInit(this);
        _exitButton = transform.Find("MainPanel/Exit").gameObject.GetComponent<BagTabButton>();
        _exitButton.TryInit(this);
        _itemNameText = transform.Find("DetailsPanel/ItemName").gameObject.GetComponent<Text>();
        _itemTypeText = transform.Find("DetailsPanel/ItemType").gameObject.GetComponent<Text>();
        _feedValueText = transform.Find("DetailsPanel/FeedValue").gameObject.GetComponent<Text>();
        _itemRarityText = transform.Find("DetailsPanel/ItemRarity").gameObject.GetComponent<Text>();
        _itemDescriptionText = transform.Find("DetailsPanel/ItemDescription").gameObject.GetComponent<Text>();
        _itemPortrait = transform.Find("DetailsPanel/ItemPortrait/Body/Item").gameObject.GetComponent<Image>();
        _panelScrollBar = transform.Find("MainPanel/ScrollArea/Scrollbar").gameObject.GetComponent<Scrollbar>();
        _feedEquipButton = _detailsPanel.transform.Find("FeedEquipButton").gameObject.GetComponent<FeedEquipButton>();
        _feedEquipText = _detailsPanel.transform.Find("FeedEquipButton/TEXT/textMain").gameObject.GetComponent<Text>();
        _selectionPanelText = _selectionPanel.transform.Find("Title").gameObject.GetComponent<Text>();
        _selectorPanelBSPGrid = _selectionPanel.transform.Find("BSPGrid").gameObject;
        _initComplete = true;
    }

    private void UpdatePanels()
    {
        Debug.Log("Update Panels called");
        List<KeyValuePair<int, int>> itemList = GameManager.PlayerInventory.GetListByItemType(OpenTab);
        itemList.RemoveAll(item => item.Value < 1);
        WipeItemPanels();
        for (int i = 0; i < itemList.Count; i++)
        {
            //instantiate itemPanel instance
            ItemPanelController currentPanel = GameObject.Instantiate(_itemPanelPrefab, _content.transform).GetComponent<ItemPanelController>();
            //place in 'content'?
            //then try init
            currentPanel.TryInit(i,this);
            currentPanel.ClearSelf();
            currentPanel.LoadItem(Item.GetInstanceByKey(itemList[i].Key, OpenTab), itemList[i].Value);
            _itemPanelDict.Add(i, currentPanel);
        }
        _itemDescriptionText.text = "";
        _panelScrollBar.value = 1;
        Debug.Log($"panel scrollbar value: {_panelScrollBar.value}");
    }

    private void WipeItemPanels()
    {
        for (int i=0; i<_itemPanelDict.Count; i++)
        {
            GameObject.Destroy(_itemPanelDict[i].gameObject);
        }
        _itemPanelDict.Clear();
    }

    public void UpdateOpenTab(ItemType bagPartition=ItemType.Null)
    {
        for (int i=0;i<2;i++)
        {
            _panelScrollBar.value = 1;
            if (bagPartition != ItemType.Null) OpenTab = bagPartition;
            if (OpenTab == ItemType.Food)
            {
                _foodItemsButton.UpdateButtonState(ButtonState.A_State);
                _heldItemsButton.UpdateButtonState(ButtonState.B_State);
                _consumableItemsButton.UpdateButtonState(ButtonState.B_State);
            }
            else if (OpenTab == ItemType.Held)
            {
                _foodItemsButton.UpdateButtonState(ButtonState.B_State);
                _heldItemsButton.UpdateButtonState(ButtonState.A_State);
                _consumableItemsButton.UpdateButtonState(ButtonState.B_State);
            }
            else if (OpenTab == ItemType.Consumable)
            {
                _foodItemsButton.UpdateButtonState(ButtonState.B_State);
                _heldItemsButton.UpdateButtonState(ButtonState.B_State);
                _consumableItemsButton.UpdateButtonState(ButtonState.A_State);
            }
            UpdatePanels();
            UpdateSelectedItemPanel(-1);
            //LayoutRebuilder.ForceRebuildLayoutImmediate(_content.GetComponent<RectTransform>());
            _panelScrollBar.value = 1;
            StartCoroutine(ScrollBarEnforcer());
        }
    }

    public void UpdateSelectedItemPanel(int positionIndex)
    {
        // turn selected item panel to B_state
        // turn every other item panel to A_state if not already
        ToggleDetailsPanel(null);
        for (int i = 0; i < _itemPanelDict.Count; i++)
        {
            if (i == positionIndex & _itemPanelDict[i].TestState(ButtonState.A_State)) 
            {
                ItemPanelController selectedPanel = _itemPanelDict[positionIndex];
                if (selectedPanel.LoadedItem.ID > 0 & selectedPanel.LoadedItem.Type != ItemType.Null)
                {
                    _itemPanelDict[i].ToggleStateStyle();
                    ToggleDetailsPanel(selectedPanel.LoadedItem);
                    _selectedItem = selectedPanel.LoadedItem;
                }
            }
            else if (_itemPanelDict[i].TestState(ButtonState.B_State)) _itemPanelDict[i].ToggleStateStyle();
        }
    }

    private void ToggleDetailsPanel(Item loadingItem)
    {
        if (loadingItem == null) // this is the 'OFF' switch. pass -1 or any negative value to toggle panel off.
        {
            LoadItemDescription(null);

            // flip states
            _detailsPanel.SetActive(false);
        }
        else // turning panel on -> update description and Feed/Equip button
        {
            // flip states
            _detailsPanel.SetActive(true);
            LoadItemDescription(loadingItem);
            //do this next line only for equip for now
            if (loadingItem.Type == ItemType.Held)
            {
                _feedValueText.gameObject.SetActive(false);
                _feedEquipButton.gameObject.SetActive(true);
                _feedEquipText.text = "EQUIP";
            }
            else if (loadingItem.Type == ItemType.Food)
            {
                _feedValueText.gameObject.SetActive(true);
                _feedValueText.text = $"FEED VALUE: [ {loadingItem.StatModificationIncrement} ]";
                _feedEquipButton.gameObject.SetActive(false);
            }
            else if (loadingItem.Type == ItemType.Consumable)
            {
                _feedValueText.gameObject.SetActive(false);
                _feedEquipButton.gameObject.SetActive(true);
                _feedEquipText.text = "FEED";
            }
            //load up beastStatusPanels here
        }
    }

    private void LoadItemDescription(Item loadingItem)
    {
        Color heldItemColor = new Color(217 / 255f, 175 / 255f, 1);
        Color foodItemColor = new Color(1, 204 / 255f, 179 / 255f);
        Sprite defaultSprite = Resources.Load<Sprite>("Sprites/UI/heldItem");
        if (loadingItem!=null)
        {
            string typeLabel = loadingItem.Type.ToString();
            if (typeLabel.Contains("Held")) typeLabel += " Item";
            _itemNameText.text = loadingItem.Name;
            _itemTypeText.text = $"[ {typeLabel} ]";
            _itemTypeText.color = loadingItem.Type == ItemType.Held ? heldItemColor : foodItemColor;
            _itemRarityText.text = $"[ {loadingItem.RarityLevel} ]";
            _itemRarityText.color = Item.RarityColors[loadingItem.RarityLevel];
            _itemDescriptionText.text = loadingItem.Description;
            _itemPortrait.sprite = loadingItem.Icon; //this might not actually get the icon. If it's not, revise how Item stores/retrieves icons
        }
        else
        {
            _itemNameText.text = "";
            _itemTypeText.text = "";
            _itemRarityText.text = "";
            _itemDescriptionText.text = "";
            _itemPortrait.sprite = defaultSprite;
        }
        
    }

    public void ToggleSelectionPanel()
    {
        if (_selectionPanel.activeInHierarchy) // returning to default bag panel
        {
            _selectionPanel.SetActive(false);
            _mainPanel.SetActive(true);
            _feedEquipButton.UpdateButtonState(ButtonState.A_State);
            //UpdateSelectedItemPanel(-1);  -> reactivate this to deselect on cancel
        }
        else // activating selection panel
        {
            _selectionPanel.SetActive(true);
            _mainPanel.SetActive(false);
            Debug.Log("setting feed equip to B state");
            _feedEquipButton.UpdateButtonState(ButtonState.B_State);
            _selectionPanelText.text = _selectedItem.Type == ItemType.Held ? "EQUIP:" : "FEED:";
            LoadBeastSelectionPanels();
        }
    }

    public void LoadBeastSelectionPanels()
    {
        BSPStyle style = _selectedItem.Type == ItemType.Held ? BSPStyle.BAG_Equip : BSPStyle.BAG_Feed;

        //make sure that thang empty
        foreach (Transform child in _selectorPanelBSPGrid.transform) GameObject.Destroy(child.gameObject);
        _profilePanels.Clear();

        //Spawn new profilePanels, store references, Init()
        for (int i = 0; i < GameManager.PartyBeastStates.Count; i++)
        {
            GameObject newPanel = GameObject.Instantiate(_profilePanelPrefab, _selectorPanelBSPGrid.transform);
            BeastStatusPanel panelComponent = newPanel.GetComponent<BeastStatusPanel>();
            _profilePanels.Add(panelComponent);
            panelComponent.Init(GameManager.PartyBeastStates[i], style, _selectedItem);
        }
    }

    public void FinishSelection()
    {
        UpdatePanels();
        UpdateSelectedItemPanel(-1);
        ToggleSelectionPanel();
    }

    public IEnumerator ScrollBarEnforcer()
    {
        float duration = 0.1f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            if (_panelScrollBar.value != 1) { _panelScrollBar.value = 1; break; }
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
}
