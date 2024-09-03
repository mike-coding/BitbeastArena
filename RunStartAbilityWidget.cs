using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunStartAbilityWidget : MonoBehaviour
{
    public static RunStartAbilityWidget CurrentInstance;
    private Image _beastPortrait;
    private Image _skillLoadOutIcon;
    public BeastState SelectedBeast;
    public bool AbilitiesSelected=false;
    private AbilityButton[] abilityButtons = new AbilityButton[4];

    private void OnEnable()
    {
        CurrentInstance = this;
    }

    public void Init()
    {
        SetComponentReferences();
        StopAllCoroutines();
        StartCoroutine(AnimationRoutine());
        ResetWidget();
    }

    private void SetComponentReferences()
    {
        SelectedBeast = GameManager.PartyBeastStates[0];
        _beastPortrait = transform.Find("PortraitFrame/Display").gameObject.GetComponent<Image>();
        _skillLoadOutIcon = transform.Find("LoadOut/Ability 1/Body/IconArt").gameObject.GetComponent<Image>();
        for (int i = 1; i < 3; i++) abilityButtons[i - 1] = transform.Find($"AbilityButtons/AbilityButton{i}").gameObject.GetComponent<AbilityButton>();
        abilityButtons[0].LoadAbility(Ability.GetInstance("Headbutt"), AbilityButtonType.RunStartWidget);
        abilityButtons[1].LoadAbility(Ability.GetInstance("Screech"), AbilityButtonType.RunStartWidget);
    }


    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        while (true)
        {
            _beastPortrait.sprite = SelectedBeast.AnimationDictionary["F"][currentFrame];
            if (currentFrame < SelectedBeast.AnimationDictionary["F"].Count - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ModifyAbilities(string ability)
    {
        SelectedBeast.SkillAbilities[0] = ability;
        _skillLoadOutIcon.sprite = Ability.GetInstance(ability).GetSplashArt();
        SelectedBeast.MovementAbility = "Hop";
        SelectedBeast.LearnedAbilityNames = new List<string> { "Hop", SelectedBeast.SkillAbilities[0] };
        if (SelectedBeast.SkillAbilities[0] !="None" && SelectedBeast.MovementAbility!="None") AbilitiesSelected = true;
    }

    public void ResetWidget()
    {
        _skillLoadOutIcon.sprite = Ability.GetInstance("None").GetSplashArt();
        AbilitiesSelected = false;
    }

    public void ClearBeastAbilities()
    {
        Debug.Log("ClearBeastAbilitiesCalled");
        SelectedBeast.MovementAbility = "None";
        SelectedBeast.SkillAbilities = new string[] { "None", "None" };
        SelectedBeast.LearnedAbilityNames = new List<string>() { };
    }
}
