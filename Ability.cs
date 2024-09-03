using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum AbilityType
{
    Movement,
    Skill,
    None
}

public class Ability
{
    public static Dictionary<string, Ability> Dex = new Dictionary<string, Ability>();

    //accessibility
    public string Name;
    public string Description;

    //main ability stats
    public AbilityType ThisAbilityType;
    public int LearnCost;
    public int BaseDamage;
    public float CoolDownTime;
    public float UseRange;
    public float MaxRange=0;
    public float KnockbackStrength;
    public List<Stat> ScalingStats;
    public List<float> ScaleRates;
    public List<string> PrerequisiteAbilities;
    public bool Interruptable=true;
    public int[] EvolutionProc = new int[2] { 0, 0 };

    //prefab references
    public static GameObject HitboxPrefab;
    public static GameObject ProjectilePrefab; //pooled, unnecessary
    public static GameObject StenchEffectPrefab;

    public static void LoadDex()
    {
        if (Dex.Count > 0) return;

        TextAsset dexAsText = Resources.Load<TextAsset>("GameData/AbilityDex");
        var abilityDataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(dexAsText.ToString());

        foreach (var abilityData in abilityDataList)
        {
            Ability ability;
            string abilityName = abilityData["Name"].ToString();

            switch (abilityName)
            {
                case "Headbutt":
                    Debug.Log("attempting to load Headbutt Ability in DEX");
                    ability = new Headbutt();
                    break;
                case "None":
                    ability = new None();
                    break;
                case "Screech":
                    ability = new Screech();
                    break;
                case "Bubble":
                    ability = new Bubble();
                    break;
                case "Hop":
                    ability = new Hop();
                    break;
                case "Scramble":
                    ability = new Scramble();
                    break;
                case "Flee":
                    ability = new Flee();
                    break;
                case "Sidestep":
                    ability = new Sidestep();
                    break;
                case "Water Gun":
                    ability = new WaterGun();
                    break;
                case "Fireball":
                    ability = new Fireball();
                    break;
                case "Vine Toss":
                    ability = new VineToss();
                    break;
                case "Stench":
                    ability = new Stench();
                    break;
                case "Rock Armor":
                    ability = new RockArmor();
                    break;
                case "Sludge":
                    ability = new Sludge();
                    break;
                case "Wander":
                    ability = new Wander();
                    break;
                // Add cases for other specific abilities
                default:
                    continue; // Skip if no matching ability found
            }
            ability.LoadAbilityStats(abilityData);
            Dex.Add(ability.Name, ability);
        }

        LoadPrefabReferences();
    }

    public static void LoadPrefabReferences()
    {
        if (StenchEffectPrefab != null) return;
        HitboxPrefab = Resources.Load<GameObject>("gameObjects/HitBox");
        ProjectilePrefab = Resources.Load<GameObject>("gameObjects/BattleEffects/2D_Projectile");
        StenchEffectPrefab = Resources.Load<GameObject>("gameObjects/BattleEffects/StenchEffect");
    }

    public static Ability GetInstance(string name)
    {
        if (Dex.TryGetValue(name, out Ability ability))
        {
            return ability.Clone();
        }
        else
        {
            Debug.LogError($"Ability with name {name} not found in Dex.");
            return null;
        }
    }

    public static Ability GetAceAbilityByEvolID(int[] evolID)
    {
        foreach (Ability ability in Dex.Values)
        {
            if (ability.EvolutionProc.SequenceEqual(evolID)) return ability.Clone();
        }
        return null;
    }

    public Sprite GetSplashArt() { return Resources.Load<Sprite>($"Sprites/AbilityArt/{Name}"); }

    public bool PrerequisitesMet(BeastState state)
    {
        foreach (string abilityName in PrerequisiteAbilities) if (!state.LearnedAbilityNames.Contains(abilityName)) return false;
        return true;
    }

    public void LoadAbilityStats(Dictionary<string, object> abilityData)
    {
        // Check and assign required properties
        if (!abilityData.TryGetValue("Name", out object name) ||
            !abilityData.TryGetValue("Description", out object description) ||
            !abilityData.TryGetValue("ThisAbilityType", out object abilityType) ||
            !abilityData.TryGetValue("LearnCost", out object learnCost) ||
            !abilityData.TryGetValue("CoolDownTime", out object cooldownTime))
        {
            Debug.LogError("Missing required ability stats");
            return; // Early return if any required property is missing
        }

        Name = name.ToString();
        Description = description.ToString();
        ThisAbilityType = (AbilityType)Enum.Parse(typeof(AbilityType), abilityType.ToString());
        LearnCost = Convert.ToInt32(learnCost);
        CoolDownTime = Convert.ToSingle(cooldownTime);

        // Optional properties with default values
        if (abilityData.TryGetValue("BaseDamage", out object baseDamage)) BaseDamage = Convert.ToInt32(baseDamage);
        if (abilityData.TryGetValue("UseRange", out object useRange)) UseRange = Convert.ToSingle(useRange);
        if (abilityData.TryGetValue("MaxRange", out object maxRange)) MaxRange = Convert.ToSingle(maxRange);
        if (abilityData.TryGetValue("KnockbackStrength", out object knockbackStrength)) KnockbackStrength = Convert.ToSingle(knockbackStrength);
        if (abilityData.TryGetValue("Interruptable", out object interruptable)) Interruptable = Convert.ToBoolean(interruptable);
        if (abilityData.TryGetValue("EvolutionProc", out object evolutionProc)) EvolutionProc = JsonConvert.DeserializeObject<int[]>(evolutionProc.ToString());

        // Deserialize lists if they exist, otherwise initialize empty lists
        ScalingStats = abilityData.TryGetValue("ScalingStats", out object scalingStats) ?
            JsonConvert.DeserializeObject<List<Stat>>(scalingStats.ToString()) : new List<Stat>();
        ScaleRates = abilityData.TryGetValue("ScaleRates", out object scaleRates) ?
            JsonConvert.DeserializeObject<List<float>>(scaleRates.ToString()) : new List<float>();
        PrerequisiteAbilities = abilityData.TryGetValue("PrerequisiteAbilities", out object prereqAbilities) ?
            JsonConvert.DeserializeObject<List<string>>(prereqAbilities.ToString()) : new List<string>();
    }

    public virtual void Execute(BeastController beast) {  }

    public virtual Ability Clone()
    {
        // Perform a shallow copy
        var cloned = (Ability)this.MemberwiseClone();
        cloned.EvolutionProc = this.EvolutionProc;
        cloned.ScalingStats = new List<Stat>(this.ScalingStats);
        cloned.ScaleRates = new List<float>(this.ScaleRates);
        cloned.PrerequisiteAbilities = this.PrerequisiteAbilities==null ? new List<String>() : new List<string>(this.PrerequisiteAbilities);
        return cloned;
    }

    protected Vector2 CalculateInaccurateDirection(BeastController beast, Vector2 originalDirection, float scaler=1f)
    {
        // Maximum offset at DEX level 1 is 60 degrees
        const float maxOffsetAtDex1 = 45f;
        // Maximum offset at DEX level 40 is 5 degrees
        const float maxOffsetAtDex20 = 2.5f;
        // Calculate decrease rate based on DEX level (linear interpolation)
        float decreaseRatePerDex = (maxOffsetAtDex1 - maxOffsetAtDex20) / 19f; // 39 because DEX level 40 - DEX level 1 = 39 levels difference

        int dex = beast.CurrentState.StatDict[Stat.DEX];
        // Calculate current max offset based on DEX level
        float currentMaxOffset = maxOffsetAtDex1 - ((dex - 1) * decreaseRatePerDex);
        // Ensure currentMaxOffset does not go below maxOffsetAtDex40
        currentMaxOffset = Mathf.Max(currentMaxOffset, maxOffsetAtDex20);

        // Randomly choose an offset degree within the range from 0 to currentMaxOffset
        float offsetDegrees = (float)(beast.RandomSystem.NextDouble() * currentMaxOffset);

        // Randomly choose the direction of the offset (+/-)
        offsetDegrees *= beast.RandomSystem.Next(0, 2) * 2 - 1; // Will result in either -1 or 1 to decide the direction
        offsetDegrees *= scaler;
        if (beast.CurrentState.HeldItem != null && beast.CurrentState.HeldItem.EnhancementType == Enhancement.Accuracy) offsetDegrees *= (1 / beast.CurrentState.HeldItem.EnhancementMagnitude);
        // Apply the offset to the original direction
        Quaternion rotation = Quaternion.Euler(0, 0, offsetDegrees);
        Vector2 inaccurateDirection = rotation * originalDirection;

        return inaccurateDirection;
    }

    protected DamageParticle GenerateDamageParticle(BeastController beast, Vector2 direction)
    {
        //calculate stat boosted damage and pass it to particle
        int damageRoll = BaseDamage;
        for (int i = 0; i < ScalingStats.Count; i++)
        {
            damageRoll += Mathf.RoundToInt((beast.CurrentState.StatDict[ScalingStats[i]] - 1) * ScaleRates[i]);
        }
        DamageParticle damage = new DamageParticle();
        damage.Init(damageRoll, KnockbackStrength, direction, beast);

        return damage;
    }

    protected Vector2 CheckForCollisionsAndFindFreeSpace(BeastController beast, Vector2 direction, float distance)
    {
        float newDistance = distance * 0.25f;
        float angleStep = 15f; // Degrees to step each check
        int maxChecks = 360 / (int)angleStep; // Ensures full circle coverage
        float radius = beast.GetComponent<CircleCollider2D>().radius; // Use beast's own collider size for accuracy

        Vector2 closestDirection = direction;
        float smallestAngleDifference = float.MaxValue;

        for (int i = 0; i < maxChecks; i++)
        {
            // Check clockwise direction
            float angleClockwise = angleStep * i;
            Vector2 checkDirectionClockwise = Quaternion.Euler(0, 0, angleClockwise) * direction;
            RaycastHit2D[] hitsClockwise = Physics2D.CircleCastAll(beast.transform.position, radius, checkDirectionClockwise, newDistance);

            bool collisionFoundClockwise = false;
            foreach (RaycastHit2D hit in hitsClockwise)
            {
                if (hit.collider != null && hit.collider.gameObject != beast.gameObject && !hit.collider.gameObject.name.Contains("Projectile"))
                {
                    collisionFoundClockwise = true;
                    break;
                }
            }

            if (!collisionFoundClockwise)
            {
                float angleDifference = Mathf.Abs(angleClockwise);
                if (angleDifference < smallestAngleDifference)
                {
                    smallestAngleDifference = angleDifference;
                    closestDirection = checkDirectionClockwise;
                }
            }

            // Check counterclockwise direction
            float angleCounterclockwise = -angleStep * i;
            Vector2 checkDirectionCounterclockwise = Quaternion.Euler(0, 0, angleCounterclockwise) * direction;
            RaycastHit2D[] hitsCounterclockwise = Physics2D.CircleCastAll(beast.transform.position, radius, checkDirectionCounterclockwise, newDistance);

            bool collisionFoundCounterclockwise = false;
            foreach (RaycastHit2D hit in hitsCounterclockwise)
            {
                if (hit.collider != null && hit.collider.gameObject != beast.gameObject && !hit.collider.gameObject.name.Contains("Projectile"))
                {
                    collisionFoundCounterclockwise = true;
                    break;
                }
            }

            if (!collisionFoundCounterclockwise)
            {
                float angleDifference = Mathf.Abs(angleCounterclockwise);
                if (angleDifference < smallestAngleDifference)
                {
                    smallestAngleDifference = angleDifference;
                    closestDirection = checkDirectionCounterclockwise;
                }
            }
        }

        if (smallestAngleDifference == float.MaxValue) return direction;
        else return closestDirection;
    }
}

public class Headbutt : Ability
{
    public override void Execute(BeastController beast)
    {
        // Check if TargetPoint is within UseRange
        if (Vector3.Distance(beast.transform.position, beast.TargetPoint) <= UseRange)
        {
            // Calculate direction towards target
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            float speed = 4; //fixed headbutt speed for now

            // Calculate direction with inaccuracy
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, direction2D, 0.5f);

            beast.Body.velocity = direction2D * speed;
            beast.LastVelocity = beast.Body.velocity;
            beast.Facing = beast.GetDirection(direction, true);

            string playerStatus = beast.IsEnemy ? "Enemy" : "Player";

            // Instantiate and initialize the HitBox
            if (HitboxPrefab != null)
            {
                Vector3 hitboxPosition = beast.transform.position + direction * 0.25f; // 1 unit in front
                GameObject hitboxInstance = GameObject.Instantiate(HitboxPrefab, hitboxPosition, Quaternion.identity);
                HitBox hitBoxScript = hitboxInstance.GetComponent<HitBox>();
                if (hitBoxScript != null)
                {
                    DamageParticle damage = GenerateDamageParticle(beast, direction2D);
                    damage.SetEffectInformation(EffectStyle.Stun, 0.7f, 1f);
                    //DamageParticle damage = new DamageParticle();
                    //damage.Init(10, 1, direction2D, beast);
                    hitBoxScript.Init(Name, damage, beast);
                }
            }
            else Debug.LogError("No hitbox prefab available?");
        }
    }
}

public class None : Ability
{
    public override void Execute(BeastController beast)
    {

    }
}

public class Sludge : Ability
{
    public override void Execute(BeastController beast)
    {
        Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
        Vector2 direction2D = new Vector2(direction.x, direction.y);
        direction2D = CalculateInaccurateDirection(beast, direction2D, 0.5f);
        Vector2 direction2 = GameManager.RotateVector2(direction2D, -15f);
        Vector2 direction3 = GameManager.RotateVector2(direction2D, 15f);
        List<Vector2> directions = new List<Vector2>() { direction2D, direction2, direction3 };
        for (int i = 0; i < directions.Count; i++)
        {
            GameObject proj = GameManager.GetProjectile();
            if (proj != null)
            {
                proj.transform.position = beast.transform.position;
                proj.transform.rotation = beast.transform.rotation;
                float newYPosition = beast.CurrentState.YoffsetToTop * 0.35f;
                proj.transform.position = new Vector3(proj.transform.position.x, proj.transform.position.y, proj.transform.position.z - newYPosition);
                ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
                proj.SetActive(true);
                DamageParticle damage = GenerateDamageParticle(beast, directions[i]);
                damage.SetEffectInformation(EffectStyle.Poison, 1f, 1f);
                currentProjectile.Init(14, damage, !beast.IsEnemy, beast.Body); //ID=1 hardcoded in for water bolt
            }
            else throw new System.Exception("No projectiles available in pool");
        }
        beast.Facing = beast.GetDirection(direction2D, true);
    }
}

public class RockArmor : Ability
{
    public override void Execute(BeastController beast)
    {
        OrbitManager orbitManager;
        bool initializing = false;
        if (!beast.OrbitingIsSetup) initializing = true;
        orbitManager = beast.InitOrbitManager(OrbiterType.RockArmor);
        if (!initializing) orbitManager.AddOrbiter(2);
    }
}

public class Screech: Ability
{
    public override void Execute(BeastController beast)
    {
        GameObject proj = GameManager.GetProjectile();
        if (proj != null)
        {
            proj.transform.position = beast.transform.position;
            proj.transform.rotation = beast.transform.rotation;
            float newYPosition = beast.CurrentState.YoffsetToTop * 0.35f;
            proj.transform.position = new Vector3(proj.transform.position.x, proj.transform.position.y, proj.transform.position.z - newYPosition);
            ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
            proj.SetActive(true);
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, direction2D, 0.5f);
            Debug.Log($"currentProjectile null?!: {currentProjectile==null}");


            //DamageParticle damage = new DamageParticle();
            //damage.Init(10, 1, direction2D, beast);
            DamageParticle damage = GenerateDamageParticle(beast,direction2D);
            damage.SetEffectInformation(EffectStyle.Stun, 0.35f, 1f);
            currentProjectile.Init(1, damage, !beast.IsEnemy, beast.Body); //ID=1 hardcoded in for water bolt


            beast.Facing = beast.GetDirection(direction2D, true);
        }
        else throw new System.Exception("No projectiles available in pool");
    }
}

public class WaterGun : Ability
{
    public async override void Execute(BeastController beast)
    {
        Vector3 newCenterPoint = beast.gameObject.transform.position;
        newCenterPoint.y += beast.CurrentState.YoffsetToCenter;
        for (int i = 0; i < 3; i++)
        {
            GameObject proj = GameManager.GetProjectile();
            if (proj != null)
            {
                proj.transform.position = newCenterPoint;
                proj.transform.rotation = beast.transform.rotation;
                proj.transform.localScale *= 2f;
                float newYPosition = beast.CurrentState.YoffsetToTop * 0.35f;
                proj.transform.position = new Vector3(proj.transform.position.x, proj.transform.position.y, proj.transform.position.z - newYPosition);
                ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
                proj.SetActive(true);
                Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
                Vector2 direction2D = new Vector2(direction.x, direction.y);
                direction2D = CalculateInaccurateDirection(beast, direction2D,0.5f);
                Debug.Log($"currentProjectile null?!: {currentProjectile == null}");

                DamageParticle damage = GenerateDamageParticle(beast, direction2D);
                currentProjectile.Init(5, damage, !beast.IsEnemy, beast.Body);

                beast.Facing = beast.GetDirection(direction2D, true);

                await Task.Delay(200); // 250 milliseconds delay
            }
            else throw new System.Exception("No projectiles available in pool");
        }
    }
}

public class Fireball : Ability
{
    public static Color averageColor = GameManager.CalculateAverageColor(Resources.Load<Sprite>("Sprites/Projectiles/12_anim"), 4, 4);
    public override void Execute(BeastController beast)
    {
        GameObject proj = GameManager.GetProjectile();
        if (proj != null)
        {
            proj.transform.position = beast.transform.position;
            proj.transform.rotation = beast.transform.rotation;
            float newYPosition = beast.CurrentState.YoffsetToTop * 0.35f;
            proj.transform.position = new Vector3(proj.transform.position.x, proj.transform.position.y , proj.transform.position.z - newYPosition);
            ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
            proj.SetActive(true);
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, direction2D, 0.65f);
            DamageParticle damage = GenerateDamageParticle(beast, direction2D);
            damage.SetEffectInformation(EffectStyle.Burn,1f,2.5f);
            currentProjectile.Init(12, damage, !beast.IsEnemy, beast.Body); //ID=1 hardcoded in for water bolt


            beast.Facing = beast.GetDirection(direction2D, true);
        }
        else throw new System.Exception("No projectiles available in pool");
    }
}

public class VineToss : Ability
{
    public override void Execute(BeastController beast)
    {
        Vector3 newCenterPoint = beast.gameObject.transform.position;
        newCenterPoint.y += beast.CurrentState.YoffsetToCenter;
        GameObject proj = GameManager.GetProjectile();
        if (proj != null)
        {
            proj.transform.position = newCenterPoint;
            float newYPosition = beast.CurrentState.YoffsetToTop * 0.35f;
            proj.transform.position = new Vector3(proj.transform.position.x, proj.transform.position.y, proj.transform.position.z - newYPosition);
            proj.transform.rotation = beast.transform.rotation;
            ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
            proj.SetActive(true);
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, direction2D, 0.5f);
            Debug.Log($"currentProjectile null?!: {currentProjectile == null}");


            //DamageParticle damage = new DamageParticle();
            //damage.Init(10, 1, direction2D, beast);
            DamageParticle damage = GenerateDamageParticle(beast, direction2D);
            damage.SetEffectInformation(EffectStyle.Ensnare,2, 3);
            currentProjectile.Init(11, damage, !beast.IsEnemy, beast.Body); //ID=1 hardcoded in for water bolt


            beast.Facing = beast.GetDirection(direction2D, true);
        }
        else throw new System.Exception("No projectiles available in pool");
    }
}

public class Stench : Ability
{
    public static Color averageColor = GameManager.CalculateAverageColor(Resources.Load<Sprite>("Sprites/Projectiles/45_anim"), 4, 4);
    public override void Execute(BeastController beast)
    {
        Vector3 newCenterPoint = beast.gameObject.transform.position;
        //newCenterPoint.y += beast.CurrentState.YoffsetToCenter;

        if (StenchEffectPrefab != null)
        {
            GameObject stenchInstance = GameObject.Instantiate(StenchEffectPrefab, beast.transform);
            float newYPosition = beast.CurrentState.YoffsetToTop * 0.7f;
            stenchInstance.transform.localPosition = new Vector3(0, newYPosition, 0);
            BeastBillBoard stenchController = stenchInstance.transform.Find("StinkLines").gameObject.GetComponent<BeastBillBoard>();
            stenchController.Init(beast);
            beast.ModifyBillBoards(stenchController, Mod.Addition);
        }
        else Debug.LogError("No stench effect prefab loaded?");

        // Instantiate and initialize the HitBox
        if (HitboxPrefab != null)
        {
            Vector3 hitboxPosition = newCenterPoint; // 1 unit in front
            GameObject hitboxInstance = GameObject.Instantiate(HitboxPrefab, hitboxPosition, Quaternion.identity);
            HitBox hitBoxScript = hitboxInstance.GetComponent<HitBox>();
            if (hitBoxScript != null)
            {
                DamageParticle damage = GenerateDamageParticle(beast, Vector2.zero);
                hitBoxScript.Init(Name, damage, beast);
            }
        }
        else Debug.LogError("No hitbox prefab available?");
    }
}

public class OLDStench : Ability
{
    private Vector2[] _allAroundProjectileDirections = new Vector2[]{
    new Vector2(1,1),
    new Vector2(-1, -1),
    new Vector2(0, 1),
    new Vector2(1, 0),
    new Vector2(0, -1),
    new Vector2(-1, 0),
    new Vector2(1, -1),
    new Vector2(-1, 1), };

    public override void Execute(BeastController beast)
    {
        Vector3 newCenterPoint = beast.gameObject.transform.position;
        newCenterPoint.y += beast.CurrentState.YoffsetToCenter;
        foreach (Vector2 direction in _allAroundProjectileDirections)
        {
            GameObject proj = GameManager.GetProjectile();
            if (proj != null)
            {
                proj.transform.position = newCenterPoint;
                proj.transform.rotation = beast.transform.rotation;
                proj.transform.localScale *= 0.75f;
                ProjectileController2D currentProjectile = proj.GetComponent<ProjectileController2D>();
                proj.SetActive(true);

                //DamageParticle damage = new DamageParticle();
                //damage.Init(10, 1, direction2D, beast);
                DamageParticle damage = GenerateDamageParticle(beast, direction);
                //damage.Direction += beast.gameObject.GetComponent<Rigidbody2D>().velocity;
                currentProjectile.Init(10, damage, !beast.IsEnemy, beast.Body); //ID=1 hardcoded in for water bolt
            }
            else throw new System.Exception("No projectiles available in pool");
        }
    }
}

public class Bubble : Ability
{
    public static Color averageColor = GameManager.CalculateAverageColor(Resources.Load<Sprite>("Sprites/AbilityEffects/Bubble"), 4, 4);
    public override void Execute(BeastController beast)
    {
        Vector3 newCenterPoint = beast.gameObject.transform.position;
        newCenterPoint.y += beast.CurrentState.YoffsetToCenter;
        beast.GainShield(ShieldSource.Bubble,Mathf.RoundToInt(beast.CurrentState.StatDict[Stat.MaxHP] * 0.15f));
        ParticleController.CreateParticle(newCenterPoint, 3, Vector2.zero, 1, null, false, beast.gameObject);

        GameObject particles = GameManager.GetParticleEmitter();
        if (particles != null)
        {
            particles.transform.position = newCenterPoint;
            particles.transform.rotation = beast.transform.rotation;
            particles.GetComponent<ParticleEmitterBrain>().SetBubbleSpawnEffect(beast.gameObject, beast.CurrentState.YoffsetToCenter);
            particles.SetActive(true);
        }
    }
}

public class Hop : Ability
{
    public override void Execute(BeastController beast)
    {
        if (Vector3.Distance(beast.TargetPoint, beast.transform.position) <= beast.CurrentState.GetMinimumMaxRange(0.75f)) return;
        // Calculate direction towards target
        Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
        float magnitude = 3.25f; // Speed at level 1 SPD stat

        // Calculate direction with inaccuracy
        Vector2 direction2D = new Vector2(direction.x, direction.y);
        direction2D = CalculateInaccurateDirection(beast, direction2D, 1.25f);

        // Check for collisions and find free space
        Vector2 finalDirection = CheckForCollisionsAndFindFreeSpace(beast, direction2D, magnitude);

        beast.Body.velocity = finalDirection * magnitude;
        beast.LastVelocity = beast.Body.velocity;
        beast.Facing = beast.GetDirection(finalDirection, true);
    }
}

public class Flee : Ability
{
    public override void Execute(BeastController beast)
    {
        bool outsideAbilityRange = Vector3.Distance(beast.TargetPoint, beast.transform.position) > beast.CurrentState.GetMaximumMaxRange();
        if (outsideAbilityRange) Dex["Scramble"].Clone().Execute(beast);
        else
        {
            // Calculate direction towards target
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            float magnitude = 3.25f; // Speed at level 1 SPD stat

            // Calculate direction with inaccuracy
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, -direction2D);

            // Check for collisions and find free space
            Vector2 finalDirection = CheckForCollisionsAndFindFreeSpace(beast, direction2D, magnitude * 1.25f);

            beast.Body.velocity = finalDirection * magnitude;
            beast.LastVelocity = beast.Body.velocity;
            beast.Facing = beast.GetDirection(finalDirection, true);
        }
    }
}

public class Sidestep : Ability
{
    public override void Execute(BeastController beast)
    {
        bool outsideAbilityRange = Vector3.Distance(beast.TargetPoint, beast.transform.position) > beast.CurrentState.GetMaximumMaxRange();
        if (outsideAbilityRange) Dex["Scramble"].Clone().Execute(beast);
        else
        {
            // Calculate direction towards target
            Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
            float magnitude = 3; // Speed at level 1 SPD stat

            // Calculate direction with inaccuracy
            Vector2 direction2D = new Vector2(direction.x, direction.y);
            direction2D = CalculateInaccurateDirection(beast, direction2D, 0.5f);

            // Determine rotation direction based on the target's X relative to the beast's X
            if (beast.TargetPoint.x < beast.transform.position.x) direction2D = new Vector2(direction2D.y, -direction2D.x);
            else direction2D = new Vector2(-direction2D.y, direction2D.x);

            // Check for collisions and find free space
            Vector2 finalDirection = CheckForCollisionsAndFindFreeSpace(beast, direction2D, magnitude * 1.25f);

            // Set the velocity based on the modified direction
            beast.Body.velocity = finalDirection * magnitude;
            beast.LastVelocity = beast.Body.velocity;

            // Update facing direction based on the new velocity
            beast.Facing = beast.GetDirection(new Vector3(finalDirection.x, finalDirection.y, 0), true);
        }
    }
}

public class Scramble : Ability
{
    public override void Execute(BeastController beast)
    {
        if (Vector3.Distance(beast.TargetPoint, beast.transform.position) <= beast.CurrentState.GetMinimumMaxRange(0.9f)) return;
        // Calculate direction towards target
        Vector3 direction = (beast.TargetPoint - beast.transform.position).normalized;
        float magnitude = 3f; // Speed at level 1 SPD stat

        // Calculate direction with inaccuracy
        Vector2 direction2D = new Vector2(direction.x, direction.y);
        direction2D = CalculateInaccurateDirection(beast, direction2D, 1.8f);

        // Check for collisions and find free space
        Vector2 finalDirection = CheckForCollisionsAndFindFreeSpace(beast, direction2D, magnitude);

        beast.Body.velocity = finalDirection * magnitude;
        beast.LastVelocity = beast.Body.velocity;
        beast.Facing = beast.GetDirection(finalDirection, true);
    }
}

public class Wander : Ability
{
    private Vector2 lastDirection = Vector2.zero;

    public override void Execute(BeastController beast)
    {
        // Set the initial direction if not already set
        if (lastDirection == Vector2.zero) lastDirection = GetRandomDirection(beast);

        float magnitude = 3f; // Speed at level 1 SPD stat

        // Calculate the new direction with a bias towards the last direction
        Vector2 newDirection = GetBiasedRandomDirection(lastDirection, beast);
        newDirection = CalculateInaccurateDirection(beast, newDirection, 2f);

        // Check for collisions and find free space
        Vector2 finalDirection = CheckForCollisionsAndFindFreeSpace(beast, newDirection, magnitude*4f);

        beast.Body.velocity = finalDirection * magnitude;
        beast.LastVelocity = beast.Body.velocity;
        beast.Facing = beast.GetDirection(finalDirection, true);

        // Update the last direction for the next execution
        lastDirection = finalDirection;
    }

    private Vector2 GetRandomDirection(BeastController beast)
    {
        double angle = beast.RandomSystem.NextDouble() * 2 * Mathf.PI;
        return new Vector2(Mathf.Cos((float)angle), Mathf.Sin((float)angle)).normalized;
    }

    private Vector2 GetBiasedRandomDirection(Vector2 lastDirection, BeastController beast)
    {
        double biasAngle = 30f * Mathf.Deg2Rad;
        double randomAngle = (beast.RandomSystem.NextDouble() * 2 - 1) * biasAngle;
        Quaternion rotation = Quaternion.Euler(0, 0, (float)(randomAngle * Mathf.Rad2Deg));
        return rotation * lastDirection;
    }
}
