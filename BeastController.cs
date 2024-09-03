using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class BeastController : MonoBehaviour
{
    private BeastState _currentState;
    public int BehaviorSeed;
    public System.Random RandomSystem;

    //gameObject components 
    public Rigidbody2D Body;
    private SpriteRenderer _sp;
    private Transform _transform;
    private StatBar _statBar;
    private CircleCollider2D _collider;
    private List<BeastBillBoard> _billBoards = new List<BeastBillBoard>();
    private OrbitManager _orbitManager;

    //Coroutines
    private Coroutine _movementRoutine;
    private Coroutine OW_MovementRoutine
    {
        get { return _movementRoutine; }
        set { if(_movementRoutine!=null)
            {
                StopCoroutine(_movementRoutine);
            }
            _movementRoutine= value;
            }
    }
    private Coroutine _animationRoutine;
    private Coroutine AnimationRoutineEditor
    {
        get { return _animationRoutine; }
        set { if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine); 
            }
            _animationRoutine = value;
            }
    }
    private List<IEnumerator> AbilityRoutines = new List<IEnumerator>();
    public BeastState CurrentState
    {
        get { return _currentState; }
    }

    //Stats
    public Vector2 LastVelocity;
    public Vector3 TargetPoint;
    public Dictionary<int, float> attackCoolDowns = new Dictionary<int, float> {
        {0, -1 },
        {1, -1 },
        {2, -1 }
    };
    public ShieldManager Shield = new ShieldManager();

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

    //bools
    public bool Alive { get { return _currentState.StatDict[Stat.CurrentHP] > 0; } }
    public bool OrbitingIsSetup { get { return _orbitManager!=null; } }
    private bool _isInOverworld;
    public bool IsEnemy;
    public bool IsOWLead=false;

    //effect states
    public bool IsBeingKnockedBack = false;
    public bool PostDamageInvincibility = false;
    public bool IsEnsnared = false;
    public bool IsBurned = false;
    public bool IsPoisoned = false;
    public bool IsStunned = false;

    //editor settings TEMPLATES
    //[SerializeField] private var exampleVar;

    #region Setup
    public void Init(bool isInOverworld=true, bool isEnemy=false)
    {
        _isInOverworld = isInOverworld;
        IsEnemy = isEnemy;
        ResetRandomSeed();
        GetComponents();
        SetupController();
        //Debug.Log(PlayerProfile.EvolutionID);
        //Debug.Log($"Player profile null: {PlayerProfile == null}");
        if (GameManager.CurrentScene.name == "WildsMap")
        {
            GetComponent<CircleCollider2D>().radius *= 2.5f;
            Body.drag = 0;
            OW_MovementRoutine = StartCoroutine(MoveTowardsPointerRoutine());
        }
    }

    public void ResetRandomSeed()
    {
        BehaviorSeed= Guid.NewGuid().GetHashCode();
        RandomSystem = new System.Random(BehaviorSeed);
    }

    private void SetupController()
    {
        
        if (GameManager.CurrentScene.name.Contains("Battle")) InitializeStatBar();
        else _statBar.gameObject.SetActive(false);
    }

    private void GetComponents()
    {
        Body = GetComponent<Rigidbody2D>();
        _transform = GetComponent<Transform>();
        _sp = GetComponent<SpriteRenderer>();
        _statBar = transform.Find("Canvas/StatBar").gameObject.GetComponent<StatBar>();
        _collider = GetComponent<CircleCollider2D>();
    }

    private void InitializeStatBar() // just handle spacing here
    {
        _statBar.Init(IsEnemy);
        _statBar.UpdateToController(this);
    }

    public void LoadNewBeastStateFromDexDefaults(int[] evolID, bool isInOverworld = true, bool isEnemy = false)
    {
        //Debug.Log("attempting to load new beast blueprints into state");
        _currentState = new BeastState();
        _currentState.LoadBlueprintData(evolID);
        Init(isInOverworld, isEnemy);
        Facing = "F";
        RunNewAnimationRoutine();
        
    }

    public void LoadBeastState(BeastState state, bool isInOverworld = true, bool isEnemy = false)
    {
        state.LoadBlueprintData(state.EvolutionID);
        _currentState = state;
        Init(isInOverworld, isEnemy);
        Facing = "F";
        RunNewAnimationRoutine();
    }
    #endregion

    #region Animation
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
    #endregion

    #region Overworld Movement
    public void PointTowards(Vector3 targetPoint) {  OW_MovementRoutine = StartCoroutine(MoveTowardsPointerRoutine()); }

    private IEnumerator MoveTowardsPointerRoutine(float stopDistance = 1.2f)
    {
        float baseSpeed = 12f;  // Base speed for the movement
        float maxVelocity = 5f;  // Maximum speed cap in any direction
        float deltaTimeScale = 285f;  // Scaling factor for deltaTime adjustment
        Vector3? lastValidTargetPoint = null;

        while (true)
        {
            if (OverworldManager.PartyTargetPoint != null) lastValidTargetPoint = OverworldManager.PartyTargetPoint;
            if (lastValidTargetPoint != null)
            {
                Vector3 targetPoint = (Vector3)lastValidTargetPoint;
                float currentDistance = Vector3.Distance(_transform.position, targetPoint);

                Facing = GetDirection(targetPoint);  // Ensure the beast is facing towards the target point

                Vector2 moveDirection = (targetPoint - _transform.position).normalized;
                float speedFactor = Mathf.Pow((currentDistance / stopDistance)*0.9f , 5); // Sharper curve for acceleration and deceleration
                float speed = Mathf.Lerp(0f, baseSpeed * deltaTimeScale * Time.deltaTime, speedFactor); // Scale speed with deltaTimeScale factor to match desired speed
                Vector2 desiredVelocity = moveDirection * speed;
                Vector2 forceToAdd = (desiredVelocity - Body.velocity) * Body.mass;

                if (currentDistance <= stopDistance)
                {
                    float decelerationFactor = Mathf.Lerp(15f * deltaTimeScale * Time.deltaTime, 0f, currentDistance / stopDistance); // Scaled deceleration factor
                    Body.AddForce(-Body.velocity * Body.mass * decelerationFactor, ForceMode2D.Force);
                    OverworldManager.RecordMonsArrivalAtTarget(targetPoint);
                }
                else
                {
                    // Calculate the perpendicular component of the current velocity relative to the applied force direction
                    Vector2 currentVelocityPerpendicular = Vector2.Perpendicular(forceToAdd.normalized) * Vector2.Dot(Body.velocity, Vector2.Perpendicular(forceToAdd.normalized));
                    // Apply calculated force to move towards the target
                    Body.AddForce(forceToAdd, ForceMode2D.Force);
                    // Scale down the perpendicular component of the velocity
                    Body.velocity -= currentVelocityPerpendicular * 0.075f; // Reduce by 10% to adjust for smoother turning
                    CameraMovementNotifier.InvokeMovement();
                }

                // Ensure the velocity does not exceed the maximum velocity
                if (Body.velocity.magnitude > maxVelocity) Body.velocity = Body.velocity.normalized * maxVelocity;
            }
            yield return null;  // Wait for the next frame
        }
    }

    public void CancelMovement()
    {
        OW_MovementRoutine = null;
    }
    #endregion

    public string GetDirection(Vector3 targetPoint, bool isDirection = false)
    {
        Vector3 direction = targetPoint;
        if (!isDirection) { direction = (targetPoint - _transform.position).normalized; }

        float angle = Vector3.SignedAngle(Vector3.right, direction, Vector3.forward);  // Get signed angle in degrees between the right vector and direction vector
        if (angle >= -52.5 && angle <= 52.5) return "R";
        else if (angle >= 112.5 || angle <= -112.5) return "L";
        else if (angle >= 52.5 && angle <= 112.5) return "U";
        else if (angle <= -52.5 && angle >= -112.5) return "F";

        Debug.Log("no angle match found");
        return "F";
    }

    #region Battle
    public void ActivateBattleRoutine()
    {
        ResetRandomSeed();
        StartCoroutine(BattleRoutine());
        Shield.Initialize(CurrentState.StatDict[Stat.MaxHP]);
        HandleItemShielding();
    }

    private void HandleItemShielding()
    {
        if (CurrentState.HeldItem!=null && CurrentState.HeldItem.EnhancementType == Enhancement.ShieldStart) // ADDING MORE SHIELDS -> fix following line:
        { GainShield(ShieldSource.ShoddyShield, Mathf.RoundToInt(CurrentState.StatDict[Stat.MaxHP] * CurrentState.HeldItem.EnhancementMagnitude)); };
    }

    private IEnumerator BattleRoutine()
    {
        for (int i=0; i < _currentState.SkillAbilities.Count(); i++) // initialize routines for all abilities
        {
            Ability skillAbility = Ability.GetInstance(_currentState.SkillAbilities[i]);
            if (skillAbility != null && skillAbility.Name != "None")
            {
                float randomDelay = RandomSystem.Next(30) / 100f;
                Debug.Log(randomDelay);
                yield return new WaitForSeconds(randomDelay);
                IEnumerator abilityRoutine = AbilityRoutine(i);
                AbilityRoutines.Add(abilityRoutine);
                StartCoroutine(abilityRoutine);
            }
        }

        Ability movementAbility = Ability.GetInstance(_currentState.MovementAbility);
        if (movementAbility != null && movementAbility.Name != "None")
        {
            float randomDelay = RandomSystem.Next(30) / 100f;
            Debug.Log(randomDelay);
            yield return new WaitForSeconds(randomDelay);
            IEnumerator abilityRoutine = AbilityRoutine(-1);
            AbilityRoutines.Add(abilityRoutine);
            StartCoroutine(abilityRoutine);
        }

        if (CurrentState.HeldItem != null && CurrentState.HeldItem.EnhancementType == Enhancement.HealthRegen) StartCoroutine(HealthRegenRoutine(CurrentState.HeldItem.EnhancementMagnitude,1));

        while (Alive & BattleManager.BattleActive) //update targetpoint every quarter second as long as beast is alive
        {
            UpdateTargetPoint();
            yield return new WaitForSeconds(0.2f);
        }
        //stop ability routines here
        if (_statBar.gameObject.activeInHierarchy) _statBar.gameObject.SetActive(false);
        if (!Alive) StartCoroutine(DeathRoutine(0.3f));
    }

    private IEnumerator AbilityRoutine(int abilitySlot)
    {
        //hardcoded base cooldown?????
        Ability executingAbility;
        if (abilitySlot > -1) executingAbility = Ability.GetInstance(_currentState.SkillAbilities[abilitySlot]);
        else executingAbility = Ability.GetInstance(_currentState.MovementAbility);

        //Cool down reduction calculation
        float decreaseRate = 0.03f; // Rate of cooldown reduction per STAT level
        //SPD or DEX if mvmt or skill
        int coolDownScalingStat = abilitySlot == -1 ? _currentState.StatDict[Stat.SPD] : _currentState.StatDict[Stat.DEX];
        float coolDownReduction = decreaseRate * (coolDownScalingStat - 1); //CDR calculation
        float cooldownTime = executingAbility.CoolDownTime * (1.0f - coolDownReduction); //apply as proportion of base cooldown
        if (CurrentState.HeldItem!=null)
        {
            if (CurrentState.HeldItem.EnhancementType == Enhancement.Speed && abilitySlot == -1) cooldownTime *= (1 / CurrentState.HeldItem.EnhancementMagnitude);
            if (CurrentState.HeldItem.EnhancementType == Enhancement.Sturdy && abilitySlot == -1) cooldownTime *= (1.5f);
            if (CurrentState.HeldItem.EnhancementType == Enhancement.CooldownReduction && abilitySlot > -1) cooldownTime *= CurrentState.HeldItem.EnhancementMagnitude;
        }
        while (Alive && BattleManager.BattleActive)
        {
            if (IsBeingKnockedBack && executingAbility.Interruptable) yield return new WaitWhile(() => IsBeingKnockedBack);
            if (IsEnsnared && (executingAbility.ThisAbilityType==AbilityType.Movement||executingAbility.Name=="Headbutt")) yield return new WaitWhile(() => IsEnsnared);
            if (IsStunned) yield return new WaitWhile(() => IsStunned);
            executingAbility.Execute(this);
            yield return new WaitForSeconds(cooldownTime);
            Debug.Log($"ability routine iteration completed for: {executingAbility.Name}");
        }
    }

    private IEnumerator DeathRoutine(float seconds)
    {
        Vector3 startScale = _transform.localScale; // Starting scale
        Vector3 endScale = Vector3.zero; // Target scale

        float elapsedTime = 0;
        GameObject particleEmitter = GameManager.GetParticleEmitter();
        particleEmitter.transform.position = transform.position;
        particleEmitter.GetComponent<ParticleEmitterBrain>().SetSmokePuff();
        particleEmitter.SetActive(true);

        while (elapsedTime < seconds)
        {
            _transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / seconds);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _transform.localScale = endScale; // Ensure it's exactly zero at the end
        Team beastTeam = IsEnemy ? Team.Team2 : Team.Team1;
        BattleManager.Instance.RemoveBeast(this,beastTeam);
    }

    private void UpdateTargetPoint()
    {
        // Find nearest opponent based on IsEnemy status
        Dictionary<int, BeastBattleProfile> teamDictionary = IsEnemy ? BattleManager.Team1 : BattleManager.Team2;

        // Extract the GameObjects from the team's BeastBattleProfiles
        List<GameObject> potentialTargets = teamDictionary.Values
                                                            .Where(profile => profile.IsLoaded)
                                                            .Select(profile => profile.BeastObject)
                                                            .ToList();
        GameObject nearestOpponent = FindNearestGameObject(potentialTargets);
        if (nearestOpponent != null) TargetPoint = nearestOpponent.transform.position;
    }

    private GameObject FindNearestGameObject(List<GameObject> gameObjects)
    {
        GameObject nearestObj = null;
        float minDistance = float.MaxValue;
        foreach (var obj in gameObjects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestObj = obj;
            }
        }
        return nearestObj;
    }

    public void TakeDamage(DamageParticle damage)
    {
        float effectDurationMultiplier = 1;
        if (CurrentState.HeldItem !=null && CurrentState.HeldItem.EnhancementType == Enhancement.DampenStatusEffects) effectDurationMultiplier = 1 - CurrentState.HeldItem.EnhancementMagnitude;
        // Status effect rolls
        if (UnityEngine.Random.Range(0.0f, 1.0f) < damage.Effects[EffectStyle.Stun][0]) StartCoroutine(StunRoutine(damage.Effects[EffectStyle.Stun][1]* effectDurationMultiplier));
        if (UnityEngine.Random.Range(0.0f, 1.0f) < damage.Effects[EffectStyle.Poison][0]) StartCoroutine(PoisonRoutine(damage.Effects[EffectStyle.Poison][1]* effectDurationMultiplier));
        if (UnityEngine.Random.Range(0.0f, 1.0f) < damage.Effects[EffectStyle.Ensnare][0]) StartCoroutine(EnsnaredRoutine(damage.Effects[EffectStyle.Ensnare][1]* effectDurationMultiplier));
        if (UnityEngine.Random.Range(0.0f, 1.0f) < damage.Effects[EffectStyle.Burn][0]) StartCoroutine(BurnRoutine(damage.Effects[EffectStyle.Burn][1]* effectDurationMultiplier));
        if (PostDamageInvincibility) return;

        // Given values
        float baseEvasionChance = 1.0f / 30.0f; // Evasion chance at level 1 SPD stat
        float increaseRate = 0.01453f; // Rate of evasion chance increase per SPD level

        // Calculate evasion chance based on current SPD stat
        float evasionChance = baseEvasionChance + increaseRate * (_currentState.StatDict[Stat.SPD] - 1);
        if (damage.Damage < 0) evasionChance = 0;

        // Perform beast speed check and evasion roll
        if (UnityEngine.Random.Range(0.0f, 1.0f) > evasionChance)
        {
            ParticleController.CreateDamageParticle(transform.position, damage, this);

            int remainingDamage = Shield.ReceiveDamage(damage.Damage);
            _currentState.StatDict[Stat.CurrentHP] -= remainingDamage;

            // Ensure HP does not drop below zero or exceed max
            _currentState.StatDict[Stat.CurrentHP] = Mathf.Clamp(_currentState.StatDict[Stat.CurrentHP], 0, _currentState.StatDict[Stat.MaxHP]);
            _statBar.UpdateToController(this);
            GameManager.UImanager.RefreshProfilePanels();

            if (damage.KnockbackMagnitude > 0) TakeKnockback(damage.Direction, damage.KnockbackMagnitude);
            // StartCoroutine(InvincibilityRoutine(0.25f));
        }
        else
        {
            DamageParticle whiffDamage = new DamageParticle();
            whiffDamage.ConvertToWhiff();
            ParticleController.CreateDamageParticle(transform.position, whiffDamage, this);
        }
    }

    public void GainShield(ShieldSource source, int amount, bool set = false)
    {
        if (set)
        {
            Shield.ClearAllShields();
            Shield.AddShield(source, amount);
        }
        else Shield.AddShield(source, amount);
        Shield.ClampShield(ShieldSource.Bubble, Mathf.RoundToInt(CurrentState.StatDict[Stat.MaxHP] * 0.5f));
        _statBar.UpdateToController(this);
        GameManager.UImanager.RefreshProfilePanels();
    }

    public void TakeKnockback(Vector2 direction, float magnitude)
    {
        if (magnitude <= 0) return;
        float sturdyModifier = 1;
        if (CurrentState.HeldItem != null && CurrentState.HeldItem.EnhancementType == Enhancement.Sturdy) sturdyModifier = 0.25f;

        float newMagnitude = magnitude * sturdyModifier;
        Body.velocity *= (0.35f / sturdyModifier);
        Body.AddForce(direction.normalized * newMagnitude, ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine());
    }

    public void ProduceDisintegration(Color color)
    {
        Vector3 newCenterPoint = transform.position;
        newCenterPoint.y += CurrentState.YoffsetToCenter;
        GameObject particles = GameManager.GetParticleEmitter();
        if (particles != null)
        {
            particles.transform.position = newCenterPoint;
            particles.transform.rotation = transform.rotation;
            particles.GetComponent<ParticleEmitterBrain>().SetProjectileDisintegration(color);
            particles.SetActive(true);
        }
    }

    public void ModifyBillBoards(BeastBillBoard billBoard, Mod mod)
    {
        //check if billboard exists?
        var oldBillboard = _billBoards.FirstOrDefault(a => a.Type == billBoard.Type);
        if (oldBillboard != null)
        {
            _billBoards.Remove(oldBillboard);
            oldBillboard.DestroySelf();
        }

        if (mod == Mod.Addition) _billBoards.Add(billBoard);
    }

    public OrbitManager InitOrbitManager(OrbiterType type)
    {
        if ( _orbitManager == null )
        {
            GameObject orbitManagerPrefab = Resources.Load<GameObject>("gameObjects/BattleEffects/OrbitManager");
            _orbitManager = GameObject.Instantiate(orbitManagerPrefab, transform).GetComponent<OrbitManager>();
            _orbitManager.transform.localPosition = new Vector3(0, CurrentState.YoffsetToCenter, 0);
            _orbitManager.Init(this, type);
        }
        return _orbitManager;
    }

    #region Effect Routines
    private IEnumerator InvincibilityRoutine(float seconds)
    {
        PostDamageInvincibility = true;
        yield return new WaitForSeconds(seconds);
        PostDamageInvincibility = false;
    }

    public IEnumerator BurnRoutine(float duration)
    {
        if (IsBurned) yield break;
        ProduceDisintegration(Fireball.averageColor);
        BeastBillBoard burnBillBoard = GameObject.Instantiate(BattleManager.BurnEffect, transform).GetComponent<BeastBillBoard>();
        burnBillBoard.Init(this);
        ModifyBillBoards(burnBillBoard, Mod.Addition);
        IsBurned = true;
        float timer = 0;
        while (timer < duration && BattleManager.BattleActive)
        {
            yield return new WaitForSeconds(0.45f);
            DamageParticle damage = new DamageParticle();
            damage.DirtyInit(Mathf.RoundToInt(CurrentState.StatDict[Stat.MaxHP] * 0.03f));
            TakeDamage(damage);
            ProduceDisintegration(Fireball.averageColor);
            timer += 0.45f;
        }

        IsBurned = false;
        ModifyBillBoards(burnBillBoard, Mod.Subtraction);
        ProduceDisintegration(BattleManager.SnareColor);
    }

    public IEnumerator KnockbackRoutine()
    {
        IsBeingKnockedBack = true;
        float elapsedTime = 0f;
        while (elapsedTime < 1f && Body.velocity.sqrMagnitude > 0f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        IsBeingKnockedBack = false;
    }

    public IEnumerator EnsnaredRoutine(float duration)
    {
        ProduceDisintegration(BattleManager.SnareColor);
        BeastBillBoard ensnareBillBoard = GameObject.Instantiate(BattleManager.SnareEffect, transform).GetComponent<BeastBillBoard>();
        ensnareBillBoard.Init(this);
        ModifyBillBoards(ensnareBillBoard, Mod.Addition);
        IsEnsnared = true;
        yield return new WaitForSeconds(duration);
        IsEnsnared = false;
        ModifyBillBoards(ensnareBillBoard, Mod.Subtraction);
        ProduceDisintegration(BattleManager.SnareColor);
    }

    public IEnumerator StunRoutine(float duration)
    {
        BeastBillBoard stunBillBoard = GameObject.Instantiate(BattleManager.StunEffect, transform).GetComponent<BeastBillBoard>();
        stunBillBoard.Init(this);
        ModifyBillBoards(stunBillBoard, Mod.Addition);
        IsStunned = true;
        yield return new WaitForSeconds(duration);
        IsStunned = false;
        ModifyBillBoards(stunBillBoard, Mod.Subtraction);
        ProduceDisintegration(Color.white);
    }

    public IEnumerator PoisonRoutine(float duration)
    {
        if (IsPoisoned) yield break;
        ProduceDisintegration(BattleManager.PoisonColor);
        BeastBillBoard poisonBillBoard = GameObject.Instantiate(BattleManager.PoisonEffect, transform).GetComponent<BeastBillBoard>();
        poisonBillBoard.Init(this);
        ModifyBillBoards(poisonBillBoard, Mod.Addition);
        IsPoisoned = true;
        float timer = 0;
        while (timer < duration && BattleManager.BattleActive)
        {
            yield return new WaitForSeconds(0.45f);
            DamageParticle damage = new DamageParticle();
            damage.DirtyInit(Mathf.RoundToInt(CurrentState.StatDict[Stat.MaxHP] * 0.05f));
            TakeDamage(damage);
            ProduceDisintegration(BattleManager.PoisonColor);
            timer += 0.45f;
        }

        IsPoisoned = false;
        ModifyBillBoards(poisonBillBoard, Mod.Subtraction);
        ProduceDisintegration(BattleManager.PoisonColor);
    }

    public IEnumerator HealthRegenRoutine(float regenMagnitude=0.05f, float tickWait=1)
    {
        while (Alive)
        {
            yield return new WaitForSeconds(tickWait);
            DamageParticle healing = DamageParticle.GetHealingParticle(Mathf.RoundToInt(CurrentState.StatDict[Stat.MaxHP]*regenMagnitude));
            if (CurrentState.StatDict[Stat.CurrentHP] < CurrentState.StatDict[Stat.MaxHP]) TakeDamage(healing);
        }
    }
    #endregion

    #endregion
}
