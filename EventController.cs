using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventController : MonoBehaviour
{
    private SpriteRenderer _sp;
    private EventStyle EventType;
    private int _progLevel;
    private int randomSeed;
    //store enemy type here
    public List<BeastState> enemyStates;
    private GameObject _dummyDisplayObject;

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

    private void SetRandomSeed()
    {
        randomSeed = (int)(OverworldManager.ProgressionLevel + transform.position.x + transform.position.y);
    }

    public void SetEvent(EventStyle setTo, int progressionLevel = 1)
    {
        _sp = GetComponent<SpriteRenderer>();
        SetRandomSeed();
        _progLevel = progressionLevel;

        EventType = setTo;
        switch (EventType)
        {
            case EventStyle.DisguisedEnemy:
                _dummyDisplayObject = Resources.Load<GameObject>("gameObjects/BeastDisplayDummy");
                enemyStates = RollEnemies();
                SpawnEnemyDisplayDummies();
                break;
            case EventStyle.Enemy:
                _dummyDisplayObject = Resources.Load<GameObject>("gameObjects/BeastDisplayDummy");
                enemyStates = RollEnemies();
                SpawnEnemyDisplayDummies();
                break;
            case EventStyle.WeakEnemy:
                _dummyDisplayObject = Resources.Load<GameObject>("gameObjects/BeastDisplayDummy");
                enemyStates = RollEnemies();
                WeakenEnemies();
                SpawnEnemyDisplayDummies();
                break;
            case EventStyle.Loot:
                LoadSprite(setTo);
                break;
            case EventStyle.VendingMachine:
                LoadSprite(setTo);
                break;
            case EventStyle.Picnic:
                BillBoardSprite bbs = GetComponent<BillBoardSprite>();
                bbs.enabled = false;
                transform.rotation = Quaternion.identity;
                _sp.sprite = Resources.Load<Sprite>("Sprites/Events/PicnicBlanket");
                _sp.sortingOrder = 3;
                _sp.spriteSortPoint = SpriteSortPoint.Pivot;
                GameObject basket = new GameObject();
                basket.transform.localScale *= 0.75f;
                basket.transform.localPosition = new Vector3(0,-0.5f,0);
                SpriteRenderer basketSP = basket.AddComponent<SpriteRenderer>();
                basketSP.sprite = Resources.Load<Sprite>("Sprites/Events/PicnicBasket");
                basketSP.sortingOrder = 3;
                basketSP.material = Resources.Load<Material>("Shaders/ShadowShader");
                basketSP.spriteSortPoint = SpriteSortPoint.Pivot;
                basket.AddComponent<BillBoardSprite>();
                GameObject.Instantiate(basket, transform);
                break;
            case EventStyle.WishingWell:
                LoadSprite(setTo);
                break;
            case EventStyle.GumballMachine:
                LoadSprite(setTo);
                break;
            case EventStyle.Shrine:
                LoadSprite(setTo);
                break;
        }
    }

    public void LoadSprite(EventStyle eventType)
    {
        string eventName = eventType.ToString();
        _sp.sprite = Resources.Load<Sprite>($"Sprites/Events/{eventName}");
    }

    public void DestroyAttachedGameObject()
    {
        GameObject.Destroy(gameObject);
    }

    public List<BeastState> RollEnemies()
    {
        System.Random random = new System.Random(randomSeed);
        //determineNumEnemies()
        int numberOfEnemies;
        if (_progLevel == 1) numberOfEnemies = 1; //baby level
        else if (_progLevel < 4) numberOfEnemies = random.Next(1, Mathf.Min(GameManager.PartyBeastStates.Count + 1,4));
        else numberOfEnemies = random.Next(1, Mathf.Min(GameManager.PartyBeastStates.Count+2,4));

        float baseLevels = (_progLevel * 1.5f * numberOfEnemies);
        int partyAdvantage = _progLevel!=1 ? GameManager.PartyBeastStates.Count - numberOfEnemies : 0;
        int totalLevels = partyAdvantage >0 ? Mathf.RoundToInt(baseLevels + partyAdvantage * 10) : Mathf.RoundToInt(baseLevels);
        int maxLevelAllotment = Mathf.CeilToInt(totalLevels * 0.8f);

        List<BeastState> rolledEnemies = new List<BeastState>();
        for (int i = 0; i < numberOfEnemies; i++)
        {
            int levelPortion;
            if (i != numberOfEnemies - 1)
            {
                int maxPossibleLevel = Mathf.Min(maxLevelAllotment, totalLevels - (numberOfEnemies - i - 1)); // Ensure at least 1 level for each remaining enemy
                levelPortion = random.Next(1, maxPossibleLevel + 1);
                totalLevels -= levelPortion;
            }
            else levelPortion = totalLevels;
            if (_progLevel == 1 && GameManager.PartyBeastStates.Count == 1) levelPortion =0;
            rolledEnemies.Add(BeastState.RollRandomState(randomSeed,levelPortion,true));
        }
        return rolledEnemies;
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        int spriteCount = enemyStates[0].AnimationDictionary["F"].Count;
        while (true)
        {
            _sp.sprite = enemyStates[0].AnimationDictionary["F"][currentFrame];
            if (currentFrame < spriteCount - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.35f);
        }
    }

    public void DestroySelf()
    {
        GameObject particleEmitter = GameManager.GetParticleEmitter();
        particleEmitter.transform.position = transform.position;
        particleEmitter.GetComponent<ParticleEmitterBrain>().SetSmokePuff();
        particleEmitter.SetActive(true);
        GameObject.Destroy(gameObject);
    }

    private void WeakenEnemies()
    {
        foreach (BeastState enemy in enemyStates) enemy.StatDict[Stat.CurrentHP] -= Mathf.RoundToInt(enemy.StatDict[Stat.MaxHP] / 2f);
    }

    private void SpawnEnemyDisplayDummies()
    {
        BillBoardSprite bbs = GetComponent<BillBoardSprite>();
        bbs.enabled = false;
        transform.rotation = Quaternion.identity;
        _sp.enabled = false;
        foreach (BeastState beast in enemyStates)
        {

            // Instantiate the dummy at the calculated world position without specifying the parent
            GameObject monObject = Instantiate(_dummyDisplayObject, new Vector3(transform.position.x, transform.position.y,0), Quaternion.identity);

            // After instantiation, set this GameObject as the parent, maintaining world position
            monObject.transform.SetParent(transform, false);

            monObject.GetComponent<BeastDummyController>().LoadBeastState(beast);
        }
    }
}
