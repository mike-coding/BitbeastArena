using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/*
public class BeastPanelManager : MonoBehaviour
{
    Slider HPBar;
    Slider XPBar;
    Text Level;
    Image BeastPortrait;

    Image[] AbilityIcons = new Image[3];
    BeastState LoadedState;

    private static Dictionary<int, Vector3> beastDisplayScales = new Dictionary<int, Vector3>();

    private Coroutine _animationRoutine;
    private Coroutine AnimationRoutineEditor
    {
        get { return _animationRoutine; }
        set
        {
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
            }
            _animationRoutine = value;
        }
    }

    public void Init()
    {
        StoreComponentsAndData();
        Debug.Log("panel booted up");
    }
    
    private void StoreComponentsAndData()
    {
        beastDisplayScales[0] = new Vector3(1f, 0.875f, 1f);
        beastDisplayScales[1] = new Vector3(1f, 0.6f, 1f);
        beastDisplayScales[2] = new Vector3(1f, 1f, 1f);

        HPBar = transform.Find("HPBar").gameObject.GetComponent<Slider>();
        XPBar = transform.Find("XPBar").gameObject.GetComponent<Slider>();
        Level = transform.Find("LevelHolder/Level").gameObject.GetComponent<Text>();
        BeastPortrait = transform.Find("BeastPicHolder/BeastPic").gameObject.GetComponent<Image>();
        AbilityIcons[0] = transform.Find("AbilityIcon1/LoadedAbility").gameObject.GetComponent<Image>();
        AbilityIcons[1] = transform.Find("AbilityIcon2/LoadedAbility").gameObject.GetComponent<Image>();
        AbilityIcons[2] = transform.Find("AbilityIcon3/LoadedAbility").gameObject.GetComponent<Image>();
    }

    public void LoadProfile(BeastState toLoad)
    {
        LoadedState = toLoad;
        //Debug.Log($"LoadProfile in BeastPanelManager CurrentHP: {toLoad.StatDict[Stat.CurrentHP]}");
        HPBar.value = (float)toLoad.StatDict[Stat.CurrentHP] / toLoad.StatDict[Stat.MaxHP];
        XPBar.value = (float)toLoad.StatDict[Stat.XP] / toLoad.XPToNextLevel + 0.02f; //will need to update this if there's an XP ladder later
        Level.text = toLoad.Level.ToString();

        //portrait manipulations
        BeastPortrait.gameObject.transform.localScale = beastDisplayScales[toLoad.EvolutionID[1]];
        //BeastPortrait.sprite = toLoad.AnimationDictionary["F"][0];
        AnimationRoutineEditor = StartCoroutine(AnimationRoutine());
        BeastPortrait.color = Color.white;

        for (int i = 0; i < toLoad.OtherLoadedAbilities.Count(); i++)
        {
            if (toLoad.OtherLoadedAbilities[i]==null||toLoad.OtherLoadedAbilities[i].Name == null || toLoad.OtherLoadedAbilities[i].Name == "NullAbility") return;
            //Debug.Log(toLoad.AbilityNames[i]);
            //Debug.Log($"toLoad.LoadedAbilities.Count: {toLoad.LoadedAbilities.Count()}");
            AbilityIcons[i].sprite = toLoad.OtherLoadedAbilities[i].GetSplashArt();
            AbilityIcons[i].color = Color.white;
        }
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        int spriteCount = LoadedState.AnimationDictionary["F"].Count;
        while (true)
        {
            BeastPortrait.sprite = LoadedState.AnimationDictionary["F"][currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.35f);
        }
    }
}
*/
