using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickBattleMenuController : MonoBehaviour
{
    private static QuickBattleMenuController _instance;
    public static QuickBattleMenuController Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerObject = GameObject.Find("QUICKBATTLEINPUT");
                if (managerObject != null) _instance = managerObject.GetComponent<QuickBattleMenuController>();
            }
            return _instance;
        }
    }

    private InputField _input1;
    private InputField _input2;

    private Coroutine _keyboardListener;

    public bool IsOn { get {  return _isOn; } }
    private bool _isOn = false;

    public void StartBattle()
    {
        //generate random teams
        string seed1 = _input1.text;
        string seed2 = _input2.text;
        if (seed1.Length < 1 || seed2.Length < 1) return;
        List<BeastState> team1 = RollParty(seed1, 35);
        List<BeastState> team2 = RollParty(seed2, 35);
        BattleManager.PrepareBattle(team1, team2,seed1,seed2);
    }

    public void TurnOn()
    {
        SetUpInputFields();
        _input1.Select();
        _input1.ActivateInputField();
        _keyboardListener = StartCoroutine(KeyboardInputListener());
        _isOn = true;
    }

    public void ClearAndDeselectInputs()
    {
        _input1.text = string.Empty;
        _input2.text = string.Empty;
        EventSystem.current.SetSelectedGameObject(null); // Deselect any selected UI element
    }

    public void ShutDown()
    {
        ClearAndDeselectInputs();
        if (_keyboardListener!= null)
        {
            StopCoroutine(_keyboardListener);
            _keyboardListener = null;
        }
        _isOn = false;
    }

    private IEnumerator KeyboardInputListener()
    {
        while (true)
        {
            // Listen for Tab or Arrow Keys to toggle between input fields
            if (Input.GetKeyDown(KeyCode.Tab) ||
                Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (EventSystem.current.currentSelectedGameObject == _input1.gameObject)
                {
                    _input2.Select();
                    _input2.ActivateInputField();
                }
                else
                {
                    _input1.Select();
                    _input1.ActivateInputField();
                }
            }
            // Listen for the Enter key to start the battle
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartBattle();
            }
            yield return null; // Wait for the next frame
        }
    }

    private void SetUpInputFields()
    {
        _input1 = transform.Find("INPUT1").gameObject.GetComponent<InputField>();
        _input2 = transform.Find("INPUT2").gameObject.GetComponent<InputField>();
    }

    #region PartyRolling
    public static List<BeastState> RollParty(string seed, int baseLevelTotal)
    {
        int generatedSeed = GenerateSeedFromString(seed);
        System.Random random = new System.Random(generatedSeed);

        int numberOfBeasts = random.Next(3) + 1;
        int levelTotal = baseLevelTotal + (3 - numberOfBeasts) * 8;

        // Distribute levels among the beasts
        int[] levels = DistributeLevels(random, levelTotal, numberOfBeasts);

        List<BeastState> party = new List<BeastState>();
        for (int i = 0; i < numberOfBeasts; i++)
        {
            BeastState beast = BeastState.RollRandomState(random.Next(), levels[i], true);
            party.Add(beast);
        }

        return party;
    }

    private static int DetermineNumberOfBeasts(System.Random random)
    {
        int roll = random.Next(100);
        if (roll < 50) return 3; // 50% chance for 3 beasts
        if (roll < 80) return 2; // 30% chance for 2 beasts
        return 1; // 20% chance for 1 beast
    }

    private static int GenerateSeedFromString(string input)
    {
        unchecked
        {
            const int hash = 5381;
            int seed = hash;
            foreach (char c in input)
            {
                seed = ((seed << 5) + seed) + c; // seed * 33 + c
            }
            return seed;
        }
    }

    private static int[] DistributeLevels(System.Random random, int total, int count)
    {
        int[] levels = new int[count];
        for (int i = 0; i < count - 1; i++)
        {
            levels[i] = random.Next(1, total - (count - i - 1));
            total -= levels[i];
        }
        levels[count - 1] = total; // Assign the remaining levels to the last beast
        return levels;
    }
    #endregion
}
