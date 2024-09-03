using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;
using UnityEngine.UI;
using System;

public class StatWidgetController : MonoBehaviour
{
    private Image beastPortrait;
    private Text remainingPointsToAllocate;
    public BeastState selectedBeast;
    private Dictionary<Stat, (Text statValue, StatAllocatorButton incrementButton, StatAllocatorButton decrementButton)> statComponentsDictionary = new Dictionary<Stat,(Text statValue, StatAllocatorButton incrementButton, StatAllocatorButton decrementButton)>();
    private Dictionary<Stat, int> statMinimumDict= new Dictionary<Stat, int>() { {Stat.MaxHP,60 }, { Stat.STR, 1 }, { Stat.DEX, 1 }, { Stat.SPD, 1 } };
    private Stat[] allStats = new Stat[] { Stat.MaxHP, Stat.STR, Stat.DEX, Stat.SPD };

    private void OnEnable()
    {
        SetComponentReferences();
        StartCoroutine(AnimationRoutine());
        ResetWidget();
        UpdateWidget();
    }

    private void SetComponentReferences()
    {
        selectedBeast = GameManager.PartyBeastStates[0];
        beastPortrait =transform.Find("PortraitFrame/Display").gameObject.GetComponent<Image>();

        remainingPointsToAllocate = transform.Find("PointsLeftToAllocate/PointValue").gameObject.GetComponent<Text>();

        foreach (Stat stat in allStats)
        {
            string statName = stat.ToString();
            if (statName == "MaxHP") statName = "HP";
            Text statValueText = transform.Find($"StatPanels/{statName}/StatValue").gameObject.GetComponent<Text>();
            StatAllocatorButton incrementImage = transform.Find($"StatPanels/{statName}/IncrementUp").GetComponent<StatAllocatorButton>();
            StatAllocatorButton decrementImage = transform.Find($"StatPanels/{statName}/IncrementDown").GetComponent<StatAllocatorButton>();

            statComponentsDictionary[stat] = (statValueText,incrementImage,decrementImage);
        }
    }

    public void UpdateWidget()
    {
        foreach (Stat stat in allStats)
        {
            StatAllocatorButton incrementButton = statComponentsDictionary[stat].incrementButton;
            StatAllocatorButton decrementButton = statComponentsDictionary[stat].decrementButton;
            incrementButton.TryInit();
            decrementButton.TryInit();
            
            statComponentsDictionary[stat].statValue.text = selectedBeast.StatDict[stat].ToString();
            //handle increment up opacity
            if (selectedBeast.StatDict[Stat.Unspent] == 0) incrementButton.UpdateButtonState(ButtonState.B_State);
            else incrementButton.UpdateButtonState(ButtonState.A_State);
            //handle increment down opacity
            if (selectedBeast.StatDict[stat] == statMinimumDict[stat]) decrementButton.UpdateButtonState(ButtonState.B_State);
            else decrementButton.UpdateButtonState(ButtonState.A_State);
        }
        remainingPointsToAllocate.text = selectedBeast.StatDict[Stat.Unspent].ToString();
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        while (true)
        {
            beastPortrait.sprite = selectedBeast.AnimationDictionary["F"][currentFrame];
            if (currentFrame < selectedBeast.AnimationDictionary["F"].Count - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ModifyStatValue(Stat stat, bool adding)
    {
        if ((selectedBeast.StatDict[stat] == statMinimumDict[stat] && !adding)||
            (selectedBeast.StatDict[Stat.Unspent] == 4&!adding)||
            (selectedBeast.StatDict[Stat.Unspent] == 0 & adding)) return;
        int differenceToApply;
        if (adding) differenceToApply = 1;
        else differenceToApply = -1;
        if (stat == Stat.MaxHP)
        {
            selectedBeast.StatDict[stat] += differenceToApply * 10;
            selectedBeast.StatDict[Stat.CurrentHP] = selectedBeast.StatDict[stat];
        }

        else selectedBeast.StatDict[stat] += differenceToApply;
        selectedBeast.StatDict[Stat.Unspent] -= differenceToApply;

        UpdateWidget();
    }

    public void ResetWidget()
    {
        selectedBeast.StatDict[Stat.Unspent] = 4;
        selectedBeast.ResetStats();
    }
}

