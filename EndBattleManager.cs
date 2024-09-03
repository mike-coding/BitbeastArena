using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EndBattleManager : MonoBehaviour
{
    public static EndBattleManager Instance;
    private List<BeastStatusPanel> _profilePanels = new List<BeastStatusPanel>();
    private Dictionary<int, ItemPanelController> _itemPanelDict = new Dictionary<int, ItemPanelController>();
    private int _progressionLevel = 0;
    private GameObject _levelUpPanels;
    private GameObject _feederWidget;
    public BeastState ToFeedBeast;
    private GameObject _profilePanelPrefab;
    private GameObject _profilePanelParent;

    // item panel prefab
    private GameObject _itemPanelPrefab;
    //item panel placement location
    private GameObject _content;

    private void OnEnable()
    {
        PerformSetup();
        if (GameManager.PlayingCampaign)
        {
            SpawnBeastPanels(BSPStyle.POSTBATTLE_TeamXP);
            BattleManager.Instance.DistributeXP();
        }
        else SpawnBeastPanels(BSPStyle.BATTLE_ProfilePanel);
        
        AnimatePanelXPBars();
        UpdateItemPanels();
    }
    
    private void PerformSetup()
    {
        GetReferences();
        Instance = this;
    }

    private void GetReferences()
    {
        _levelUpPanels = transform.Find("LevelUpPanels").gameObject;
        _feederWidget = transform.Find("FeederWidget").gameObject;
        _profilePanelPrefab = Resources.Load<GameObject>("gameObjects/UI/BeastStatusPanel");
        _profilePanelParent = transform.Find("LevelUpPanels/GridLayout").gameObject;

        _content = transform.Find($"FeederWidget/ScrollArea/Content").gameObject;


        //for (int i=0;i<_numItemPanels;i++) _itemPanelDict[i] = content.transform.Find($"ItemPanel ({i})").gameObject.GetComponent<ItemPanelController>();
        _itemPanelPrefab = Resources.Load<GameObject>("gameObjects/UI/FeedWidgetItemPanel").gameObject;
    }

    private void AnimatePanelXPBars()
    {
        for (int i = 0; i < _profilePanels.Count; i++)
        {
            if (i < GameManager.PartyBeastStates.Count) _profilePanels[i].AnimateSliderBar();
        }
    }

    public void SpawnBeastPanels(BSPStyle style)
    {
        List<BeastState> beastList= new List<BeastState>();
        switch (style)
        {
            case BSPStyle.POSTBATTLE_TeamXP:
                beastList = GameManager.PartyBeastStates;
                break;
            case BSPStyle.POSTBATTLE_Recruit:
                beastList = BattleManager.Instance.DefeatedEnemies;
                break;
            case BSPStyle.BATTLE_ProfilePanel:
                beastList = BattleManager.GetWinningTeam();
                break;
        }

        //make sure that thang empty
        foreach (Transform child in _profilePanelParent.transform) GameObject.Destroy(child.gameObject);
        _profilePanels.Clear();

        //Spawn new profilePanels, store references, Init()
        for (int i = 0; i < beastList.Count; i++)
        {
            GameObject newPanel = GameObject.Instantiate(_profilePanelPrefab, _profilePanelParent.transform);
            BeastStatusPanel panelComponent = newPanel.GetComponent<BeastStatusPanel>();
            _profilePanels.Add(panelComponent);
            panelComponent.Init(beastList[i], style);
        }
    }

    public void IncrementProgressionLevel()
    {
        Debug.Log($"GameManager.PlayingCampaign: {GameManager.PlayingCampaign}");
        if (!GameManager.PlayingCampaign) GameManager.GetInstance().UpdateScene("Main Menu");
        else
        {
            if (_progressionLevel == 0) _progressionLevel++;
            if (_progressionLevel == 1)
            {
                bool noDefeatedEnemies = BattleManager.Instance.DefeatedEnemies.Count < 1;
                bool partyMaxedOut = GameManager.PartyBeastStates.Count > 2;
                bool notEnoughFood = false;
                if (!noDefeatedEnemies)
                {
                    int minDefeatedEnemyLevel = BattleManager.Instance.DefeatedEnemies.Min(enemy => enemy.Level);
                    notEnoughFood = GameManager.PlayerInventory.GetFoodValueTotal() < minDefeatedEnemyLevel;
                }

                if (noDefeatedEnemies || partyMaxedOut || notEnoughFood) _progressionLevel++;
                else
                {
                    SpawnBeastPanels(BSPStyle.POSTBATTLE_Recruit);
                    UpdateText("Revive downed\nenemies?");
                }
            }
            if (_progressionLevel == 2) GameManager.GetInstance().UpdateScene("WildsMap");
            _progressionLevel++;
        }
    }

    public void ResumeFromProgressionLevel()
    {
        if (_progressionLevel == 1)
        {
            //SpawnPanels(BSPStyle.POS);
            //UpdateText("Revive downed\nparty members?");
            Debug.Log("lost state?");

        }
        if (_progressionLevel == 2)
        {
            SpawnBeastPanels(BSPStyle.POSTBATTLE_Recruit);
            UpdateText("Revive downed\nenemies?");
        }
    }

    public void UpdateText(string newText)
    {
        Text[] endBattleTexts = new Text[]
        {
            transform.Find("TEXT").gameObject.GetComponent<Text>(),
            transform.Find("TEXT/textMain").gameObject.GetComponent<Text>()
        };
        for (int i = 0; i < 2; i++) endBattleTexts[i].text = newText;
    }

    public void ToggleFeederWidget(BeastState beast)
    {
        if (_levelUpPanels.activeInHierarchy) //turn on feeder widget
        {
            ToFeedBeast = beast;
            _levelUpPanels.SetActive(false);
            _feederWidget.SetActive(true);
            UpdateText("Feed which item?");
        }
        else //turn off feeder widget
        {
            _levelUpPanels.SetActive(true);
            _feederWidget.SetActive(false);
            ResumeFromProgressionLevel();
        }
    }

    private void UpdateItemPanels()
    {
        List<KeyValuePair<int, int>> itemList = GameManager.PlayerInventory.FoodList;
        itemList.RemoveAll(item => item.Value < 1);
        WipeItemPanels();
        for (int i = 0; i < itemList.Count; i++)
        {
            ItemPanelController currentPanel = GameObject.Instantiate(_itemPanelPrefab, _content.transform).GetComponent<ItemPanelController>();
            currentPanel.TryInit(i,null,this);
            currentPanel.ClearSelf();
            currentPanel.LoadItem(Item.GetInstanceByKey(itemList[i].Key, ItemType.Food), itemList[i].Value); //write GetInstanceByItem method instead man
            _itemPanelDict.Add(i, currentPanel);
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

    public void ProcessBeastFeeding(Item feedItem)
    {
        if (ToFeedBeast.StatDict[Stat.CurrentHP] == ToFeedBeast.StatDict[Stat.MaxHP]) return;

        bool recruited = false;
        float hpPortion = feedItem.StatModificationIncrement / (float)ToFeedBeast.Level;
        int hpIncrement = Mathf.CeilToInt(hpPortion * (float)ToFeedBeast.StatDict[Stat.MaxHP]);
        Debug.Log($"HP Increment: {hpIncrement}");
        Debug.Log($"hpPortion: {hpPortion}");
        ToFeedBeast.StatDict[Stat.CurrentHP] += hpIncrement;
        if (ToFeedBeast.StatDict[Stat.CurrentHP] >= ToFeedBeast.StatDict[Stat.MaxHP])
        {
            ToFeedBeast.StatDict[Stat.CurrentHP] = ToFeedBeast.StatDict[Stat.MaxHP];
            recruited = true;
        }


        GameManager.PlayerInventory.WithdrawItem(feedItem, 1);
        if (_progressionLevel == 2 & recruited) GameManager.PartyBeastStates.Add(ToFeedBeast);
        ToggleFeederWidget(null);
        UpdateItemPanels();
    }

}
