using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum BSPStyle
{
    BAG_Feed,
    BAG_Equip,
    OVERWORLD_ProfilePanel,
    BATTLE_ProfilePanel,
    POSTBATTLE_TeamXP,
    POSTBATTLE_Recruit
}

public class BeastStatusPanel : UIButton
{
    private Slider _sliderBar;
    private Text _levelText;
    private Image _beastPortrait;

    //loadout
    private AbilityButton _movementAbility;
    private AbilityButton _skillAbility1;
    private AbilityButton _skillAbility2;
    private Image _heldItemDisplay;
    private Sprite _heldItemPlaceHolder;

    private BeastState _loadedState;
    private int[] _loadedEvolID;
    private Item _affectingItem;
    private bool _clickable = true;
    private bool _initComplete = false;

    //indicator icon objects
    private GameObject _unspentIndicator;
    private GameObject _evolutionIndicator;
    private Vector3[] _indicatorTransforms = new Vector3[2];

    //sliderpanel fixins
    private int _initalXP;
    private int _initialLevel;
    public BSPStyle Style;

    //Colors

    public override void GetComponents()
    {
        //UIButton
        _bgObject = transform.Find("BeastPortrait/BG").gameObject;
        _bg = _bgObject.GetComponent<Image>();
        _iconObject = transform.Find("BeastPortrait/BeastAnimation").gameObject;
        _icon = _iconObject.GetComponent<Image>();
        _beastPortrait = _icon;

        //Action Indicator Icons
        _unspentIndicator = transform.Find("UnspentIndicator").gameObject;
        _evolutionIndicator = transform.Find("EvolutionIndicator").gameObject;
        _indicatorTransforms = new Vector3[] { _unspentIndicator.transform.localPosition, _evolutionIndicator.transform.localPosition};

        _levelText = transform.Find("LevelDisplay/Level").gameObject.GetComponent<Text>();
        GameObject xpBarObject = transform.Find("XPSlider")?.gameObject;
        if (xpBarObject != null) _sliderBar = xpBarObject.GetComponent<Slider>();
        //loadout
        _movementAbility = transform.Find("LoadOut/Ability 0").gameObject.GetComponent<AbilityButton>();
        _skillAbility1 = transform.Find("LoadOut/Ability 1").gameObject.GetComponent<AbilityButton>();
        _skillAbility2 = transform.Find("LoadOut/Ability 2").gameObject.GetComponent<AbilityButton>();
        _heldItemDisplay = transform.Find("LoadOut/HeldItemDisplay/Body/Item").gameObject.GetComponent<Image>();
        _heldItemPlaceHolder = Resources.Load<Sprite>("Sprites/UI/heldItem");
    }

    public override void OnEnable()
    {
        GetComponents();
        GetDefaultValues();
        base.OnEnable();
        _initComplete = true;
    }

    public void Init(BeastState toLoad, BSPStyle style, Item affectingItem=null)
    {
        _loadedState = toLoad;
        _affectingItem = affectingItem;
        Style = style;
        if (style == BSPStyle.BATTLE_ProfilePanel|| style==BSPStyle.POSTBATTLE_TeamXP) _clickable = false;

        _initalXP = toLoad.StatDict[Stat.XP]; //only necessary for BSPStyle.POSTBATTLE_TeamXP
        _initialLevel = toLoad.Level; //only necessary for BSPStyle.POSTBATTLE_TeamXP
        _levelText.text = _loadedState.Level.ToString();
        _loadedEvolID = _loadedState.EvolutionID;
        StopAllCoroutines();
        StartCoroutine(AnimationRoutine());
        UpdateSliderBar();
        UpdateLoadOut();
        UpdateIndicators(toLoad);
    }

    private void UpdateSliderBar()
    {
        if (_sliderBar)
        {
            switch (Style)
            {
                case BSPStyle.POSTBATTLE_Recruit:
                    _sliderBar.gameObject.transform.Find("FilledBar").gameObject.GetComponent<Image>().color = new Color(67 / 255f, 226 / 255f, 141 / 255f, 137 / 255f);
                    _sliderBar.value = (float)_loadedState.StatDict[Stat.CurrentHP] / _loadedState.StatDict[Stat.MaxHP] + 0.02f;
                    break;
                default:
                    _sliderBar.value = (float)_loadedState.StatDict[Stat.XP] / _loadedState.XPToNextLevel + 0.02f;
                    break;
            }

        }
    }

    public void RefreshPanel(BeastState toLoad)
    {
        _loadedState = toLoad;
        _levelText.text = _loadedState.Level.ToString();
        if (_loadedEvolID != _loadedState.EvolutionID)
        {
            StopAllCoroutines();
            StartCoroutine(AnimationRoutine());
            _loadedEvolID = _loadedState.EvolutionID;
        }
        if (_sliderBar) _sliderBar.value = (float)_loadedState.StatDict[Stat.CurrentHP] / _loadedState.StatDict[Stat.MaxHP] + 0.02f;
        UpdateLoadOut();
        UpdateIndicators(toLoad);
        UpdateSliderBar();
    }

    private void UpdateIndicators(BeastState toLoad)
    {
        if (toLoad==null || Style!=BSPStyle.OVERWORLD_ProfilePanel)
        {
            _unspentIndicator.SetActive(false);
            _evolutionIndicator.SetActive(false);
            return;
        }
        _unspentIndicator.SetActive(toLoad.StatDict[Stat.Unspent] > 0);
        _evolutionIndicator.SetActive(false);
        if (_evolutionIndicator.activeInHierarchy)
        {
            if (_unspentIndicator.activeInHierarchy) _evolutionIndicator.transform.localPosition = _indicatorTransforms[1];
            else _evolutionIndicator.transform.localPosition = _indicatorTransforms[0];
        }
    }

    private void UpdateLoadOut()
    {
        if (!_movementAbility.enabled) ToggleAbilityButtons();
        _movementAbility.LoadAbility(Ability.GetInstance(_loadedState.MovementAbility), AbilityButtonType.LoadOutSlot);
        _skillAbility1.LoadAbility(Ability.GetInstance(_loadedState.SkillAbilities[0]), AbilityButtonType.DisplayOnly);
        _skillAbility2.LoadAbility(Ability.GetInstance(_loadedState.SkillAbilities[1]), AbilityButtonType.DisplayOnly);
        if (_loadedState.HeldItem != null) _heldItemDisplay.sprite = Item.HeldDex[_loadedState.HeldItem.ID].Icon;
        else _heldItemDisplay.sprite = _heldItemPlaceHolder;
        ToggleAbilityButtons();
    }

    private void ToggleAbilityButtons()
    {
        _movementAbility.enabled = !_movementAbility.enabled;
        _skillAbility1.enabled = !_skillAbility1.enabled;
        _skillAbility2.enabled = !_skillAbility2.enabled;
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        int spriteCount = 0;
        try { spriteCount = _loadedState.AnimationDictionary["F"].Count; }
        catch 
        {
            Debug.Log("====Key 'F' not found in animation dict====");
            Debug.Log($"beast null? {_loadedState==null}");
            Debug.Log($"loaded EVOL-ID: {_loadedState.EvolutionID[0]}-{_loadedState.EvolutionID[1]}");
            Debug.Log($"loaded level: {_loadedState.Level}");
            Debug.Log($"animation dict length: {_loadedState.AnimationDictionary.Count}");
        }

        while (true)
        {
            _beastPortrait.sprite = _loadedState.AnimationDictionary["F"][currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!_clickable || _loadedState == null) return;
        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!_clickable || _loadedState == null) return;
        base.OnPointerExit(eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!_clickable || _loadedState == null) return;
        _isBeingPressed = true;
        ToggleClickedStyle();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!_clickable || _loadedState == null) return;
        _isBeingPressed = false;
        ToggleClickedStyle();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!_clickable || _loadedState == null || BeastSummaryController.Instance.CurrentlyEvolving) { Debug.Log("click declined"); return; }
        if (Style != BSPStyle.POSTBATTLE_Recruit)
        {
            if (_affectingItem == null) { Debug.Log("ToggleSummary"); GameManager.UImanager.ToggleBeastSummary(_loadedState); }//open beast summary
                                                                                                                               //feed or equip
            else
            {
                Debug.Log("attempting to feed OR equip");
                if (_affectingItem.Type == ItemType.Held)
                {
                    Debug.Log("equip attempt");
                    if (_loadedState.HeldItem != null) GameManager.PlayerInventory.DepositItem(_loadedState.HeldItem);
                    _loadedState.HeldItem = _affectingItem;
                    GameManager.PlayerInventory.WithdrawItem(_affectingItem, 1);
                }
                else //eat da food
                {
                    Debug.Log("eat :-)");
                    _loadedState.StatDict[_affectingItem.StatToModify] += _affectingItem.StatModificationIncrement;
                    GameManager.PlayerInventory.WithdrawItem(_affectingItem, 1);
                }
                GameManager.UImanager.RefreshProfilePanels();
                BagUIController.Instance.FinishSelection();
            }
        }
        else EndBattleManager.Instance.ToggleFeederWidget(_loadedState);

    }

    private float CalculateSliderValue(int xp, int level)
    {
        float xpToNextLevel = _loadedState.XPToNextLevel; // You might need to adjust this based on the level
        return (float)xp / xpToNextLevel;
    }

    public void AnimateSliderBar()
    {
        if (!(_loadedState.StatDict[Stat.XP] != _initalXP || _initialLevel != _loadedState.Level)) return;
        int levelDifference = _loadedState.Level - _initialLevel;

        StartCoroutine(AnimateSliderAndLevel(levelDifference));
    }

    private IEnumerator AnimateSliderAndLevel(int levelDifference)
    {
        float totalAnimationTime = 1.5f; // Total time to fill the slider from 0 to 1

        if (levelDifference > 0)
        {
            for (int i = 0; i < levelDifference; i++)
            {
                // Calculate the XP needed to fill the bar for the current level
                int xpToFill = _loadedState.XPToNextLevel - _initalXP;
                // Calculate fill duration based on the proportion of the bar being filled
                float fillDuration = (float)xpToFill / _loadedState.XPToNextLevel * totalAnimationTime;

                // Animate to full for the current level
                yield return StartCoroutine(FillSliderBar(_initalXP, _loadedState.XPToNextLevel, fillDuration));

                // Level up
                _initialLevel++;
                _levelText.text = _initialLevel.ToString();
                _sliderBar.value = 0; // Reset slider for the new level
                _initalXP = 0; // Reset initial XP for calculations for the next iteration
            }
        }

        // Animate any remaining XP for the final level
        int finalXpToFill = _loadedState.StatDict[Stat.XP] - _initalXP;
        float finalFillDuration = (float)finalXpToFill / _loadedState.XPToNextLevel * totalAnimationTime;
        yield return StartCoroutine(FillSliderBar(_initalXP, _loadedState.StatDict[Stat.XP], finalFillDuration));

        // Update the slider to the final value directly to ensure it matches exactly
        _sliderBar.value = (float)_loadedState.StatDict[Stat.XP] / _loadedState.XPToNextLevel;
        _levelText.text = _loadedState.Level.ToString();
    }

    // Helper coroutine to animate the XP bar filling
    private IEnumerator FillSliderBar(int startXP, int endXP, float duration)
    {
        float startTime = Time.time;
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            int currentXP = (int)Mathf.Lerp(startXP, endXP, t);
            _sliderBar.value = CalculateSliderValue(currentXP, _initialLevel);
            yield return null;
        }
        // Ensure the bar reaches the end value
        _sliderBar.value = CalculateSliderValue(endXP, _initialLevel);
    }

    public void ConvertToStyle(SliderPanelStyle style) //move some of this shit into init
    {
        if (!_initComplete) GetComponents();
        StopAllCoroutines();
        Color HPGreen = new Color(67 / 255f, 226 / 255f, 141 / 255f, 137 / 255f);
        Color XPBlue = new Color(72 / 255f, 177 / 255f, 229 / 255f, 204 / 255f);
        Image sliderImage = _sliderBar.gameObject.transform.Find("FilledBar").gameObject.GetComponent<Image>();

        if (style == SliderPanelStyle.XP)
        {
            sliderImage.color = XPBlue;
            if (_loadedState!=null) _sliderBar.value = (_loadedState.StatDict[Stat.XP] / _loadedState.XPToNextLevel) + 0.02f;
        }
        else
        {
            sliderImage.color = HPGreen;
            if (_loadedState != null) _sliderBar.value = (_loadedState.StatDict[Stat.CurrentHP] / _loadedState.StatDict[Stat.MaxHP]) + 0.02f;
        }

        //_panelStyle = style;
    }
}
