using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeastDummyController : MonoBehaviour
{
    //gameObject components 
    private BillBoardSprite _bbs;
    private SpriteRenderer _sp;
    private Transform _transform;
    private BeastState _currentState;
    private System.Random random = new System.Random();

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

    public BeastState CurrentState
    {
        get { return _currentState; }
    }

    //animation stuff
    private string _facing = "X";
    private int _spriteCount;
    private int _currentFrame = 0;
    public string Facing
    {
        get { return _facing; }
        set
        {
            if (_facing != value)
            {
                _facing = value;
                ResetAnimatorSettings();
                RunNewAnimationRoutine();
            }
        }
    }
    private bool _initComplete = false;

    public void SetJitterTransformPosition()
    {
        _bbs.enabled = false;
        transform.rotation = Quaternion.identity;
        float jitterAmount = 0.5f; // Set the jitter range

        // Calculate jitter in world space for X and Y
        Vector3 worldJitter = new Vector3(
            (float)(random.NextDouble() * 2 - 1) * jitterAmount,
            (float)(random.NextDouble() * 2 - 1) * jitterAmount,
            0);

        _transform.localPosition = Vector3.zero+worldJitter*0.01f;
        _bbs.enabled= true;
    }

    public void LoadNewBeastStateFromDexDefaults(int[] evolID)
    {
        if (!_initComplete) GetComponents();
        SetJitterTransformPosition();
        _currentState = new BeastState();
        _currentState.LoadBlueprintData(evolID);
        Facing = "F";
        RunNewAnimationRoutine();

    }

    public void LoadBeastState(BeastState state)
    {
        if (!_initComplete) GetComponents();
        SetJitterTransformPosition();
        state.LoadBlueprintData(state.EvolutionID);
        _currentState = state;
        Facing = "F";
        RunNewAnimationRoutine();
    }

    private void GetComponents()
    {
        _bbs = GetComponent<BillBoardSprite>();
        _transform = GetComponent<Transform>();
        _sp = GetComponent<SpriteRenderer>();
        _initComplete = true;
    }

    private void ResetAnimatorSettings()
    {
        _spriteCount = _currentState.AnimationDictionary[_facing].Count;
        _currentFrame = 0;
    }

    private IEnumerator AnimationRoutine()
    {
        while (true)
        {
            _sp.sprite = _currentState.AnimationDictionary[_facing][_currentFrame];
            if (_currentFrame < _spriteCount - 1) _currentFrame++;
            else _currentFrame = 0;
            yield return new WaitForSeconds(0.35f);
        }
    }
    private void RunNewAnimationRoutine()
    {
        AnimationRoutineEditor = StartCoroutine(AnimationRoutine());
    }
}
