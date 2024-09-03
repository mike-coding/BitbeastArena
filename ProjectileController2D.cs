using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ProjectileController2D : MonoBehaviour
{
    public ProjectileProfile Profile;
    private SpriteRenderer _sp;
    private Rigidbody2D _body;
    private PolygonCollider2D _collider;
    private GameObject _3DeffectChild;
    private SpriteRenderer _effectChildsp;
    private DamageParticle _damage;
    private Vector3 _startPoint;
    public bool IsPartyProjectile;
    public int ProfileID;
    public bool ObstacleCollisionAvailable = true;
    private bool _initComplete = false;
    private bool _collisionInProgress = false;

    private List<BeastController> _damagedControllers = new List<BeastController>();

    private void Awake()
    {
        _sp = GetComponent<SpriteRenderer>();
        _body = GetComponent<Rigidbody2D>();
    }

    public void Init(int id, DamageParticle damage, bool isparty, Rigidbody2D parentBody = null)
    {
        if (_initComplete) return;

        //set variables
        ProfileID = id;
        IsPartyProjectile=isparty;
        Profile = ProjectileProfile.Dex[ProfileID];
        _damage = damage;
        //_parentBody = parentBody;
        _sp.sprite = Profile.Sprite;
        _sp.color = new Color(_sp.color.r, _sp.color.g, _sp.color.b, Profile.spriteAlpha);
        if (Profile.Sprites.Count > 0) StartCoroutine(AnimationRoutine());
        _collider = gameObject.AddComponent<PolygonCollider2D>();
        _collider.isTrigger = true;
        _startPoint = transform.position;

        //get that projectile pushin baby
        if (Profile.velocityMagnitude>0) LaunchDirection(damage.Direction);
        if (Profile.lifetimeDuration>0) StartCoroutine(DecayRoutine());
        if (Profile.inheritsParentVelocity) StartCoroutine(InheritParentVelocityRoutine(parentBody, damage.Direction));
        if (Profile.rotationMagnitude > 0f)
        {
            float angle = Mathf.Atan2(damage.Direction.y, damage.Direction.x) * Mathf.Rad2Deg - 45f; // Calculate angle between velocity and default facing direction
            transform.rotation = Quaternion.Euler(0f, 0f, angle); // Set the rotation of the Transform to the calculated angle
            StartCoroutine(RotationRoutine(Profile.rotationMagnitude*20));
        }
        if (Profile.isOrbiter)
        {
            _sp.sortingOrder = 3;
            _sp.material = Resources.Load<Material>("Shaders/ShadowShader");
            StartCoroutine(NoObstacleCollisionBufferRoutine(0.25f));
        }
        if (Profile.usesFaux3DEffect) Create3DEffectChild();
        StartCoroutine(NoObstacleCollisionBufferRoutine(0.25f));
        _initComplete = true;
    }

    private void Create3DEffectChild()
    {
        _3DeffectChild = GameObject.Instantiate(new GameObject(), transform);
        _3DeffectChild.transform.localEulerAngles = new Vector3(45f,90f, 45f);
        _effectChildsp = _3DeffectChild.AddComponent<SpriteRenderer>();
        _effectChildsp.sprite = _sp.sprite;
        _effectChildsp.sortingOrder = _sp.sortingOrder;
        _effectChildsp.color = _sp.color;
    }

    private IEnumerator AnimationRoutine()
    {
        int currentFrame = 0;
        while (true)
        {
            _sp.sprite = Profile.Sprites[currentFrame];
            if (_3DeffectChild != null && _effectChildsp != null) _effectChildsp.sprite = _sp.sprite;
            if (currentFrame < Profile.Sprites.Count - 1) currentFrame++;
            else currentFrame = 0;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator InheritParentVelocityRoutine(Rigidbody2D parentBody, Vector2 launchDirection)
    {
        while (true)  // Infinite loop to constantly update velocity
        {
            Vector2 additionalVelocity = launchDirection.normalized * Profile.velocityMagnitude;
            _body.velocity = parentBody.velocity + additionalVelocity;

            yield return new WaitForFixedUpdate();  // Wait until the next physics update to re-apply
        }
    }

    private IEnumerator RotationRoutine(float rotationMagnitude)
    {
        while (true)
        {
            // Access the current rotation angles
            Vector3 currentAngles = transform.eulerAngles;

            // Increment only the z-axis
            currentAngles.z += rotationMagnitude * Time.deltaTime;

            // Apply the updated angles back to the transform
            transform.eulerAngles = currentAngles;

            yield return null; // Continue the coroutine in the next frame
        }
    }

    private IEnumerator NoObstacleCollisionBufferRoutine(float duration)
    {
        ObstacleCollisionAvailable = false;
        yield return new WaitForSeconds(duration);
        ObstacleCollisionAvailable = true;
    }

    public void LaunchDirection(Vector2 direction, bool isUpdating=false)
    {
        if (Profile.pointsToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f; // Calculate angle between velocity and default facing direction
            transform.rotation = Quaternion.Euler(0f, 0f, angle); // Set the rotation of the Transform to the calculated angle
        }

        if (!isUpdating) _body.velocity += direction.normalized * Profile.velocityMagnitude;
        else
        {
            _body.velocity = direction;
            _damage.UpdateDirection(direction.normalized);
        }
    }

    public void UpdateDamageDirection(Vector2 direction)
    {
        _damage.UpdateDirection(direction.normalized);
    }

    private IEnumerator DecayRoutine()
    {
        yield return new WaitForSeconds(Profile.lifetimeDuration);
        Deactivate(false);
    }

    public void Deactivate(bool isColliding = true)
    {
        ResetParameters();
        gameObject.SetActive(false);
        GameManager.RequeueProjectileToPool(gameObject);
        Debug.Log($"Projectile ID {Profile.ID} traveled: {Vector3.Distance(transform.position,_startPoint)}");

        if (isColliding) ProduceDisintegrationParticle();
    }

    public void ProduceDisintegrationParticle()
    {
        GameObject particles = GameManager.GetParticleEmitter();
        if (particles != null)
        {
            particles.transform.position = transform.position;
            particles.transform.rotation = transform.rotation;
            particles.GetComponent<ParticleEmitterBrain>().SetProjectileDisintegration(Profile.averageColor);
            particles.SetActive(true);
        }
    }

    private void ResetParameters()
    {
        _initComplete = false;
        transform.localScale = Vector3.one;
        _body.velocity = new Vector2(0, 0);
        Destroy(GetComponent<PolygonCollider2D>());
        var billboardSprite = GetComponent<BillBoardSprite>();
        if (billboardSprite != null) Destroy(billboardSprite);
        if (_3DeffectChild !=null) { Destroy(_3DeffectChild); }
        if (gameObject.transform.parent != null) gameObject.transform.SetParent(null);
        ObstacleCollisionAvailable = true;
        _collisionInProgress = false;
        StopAllCoroutines();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_collisionInProgress) { Debug.Log("collision in progress"); return; }
        _collisionInProgress = true;
        BeastController collidingController = collision.gameObject.GetComponent<BeastController>();
        ProjectileController2D collidingProjectile = collision.gameObject.GetComponent<ProjectileController2D>();
        if (collidingController)
        {
            if (!collidingController.IsEnemy != IsPartyProjectile)
            {
                if (Profile.piercing && _damagedControllers.Contains(collidingController)) { _collisionInProgress = false; return; }
                collidingController.TakeDamage(_damage);
                _damagedControllers.Add(collidingController);
                ProduceDisintegrationParticle();
                if (!Profile.piercing)Deactivate();
            }
        }
        else if (collidingProjectile)
        {
            if ((Profile.projectileCollisions || collidingProjectile.Profile.projectileCollisions) && IsPartyProjectile!=collidingProjectile.IsPartyProjectile)
            {
                ProduceDisintegrationParticle();
                Deactivate();
            }
        }
        else if (collision.gameObject.tag == "Obstacle" && !Profile.isOrbiter && ObstacleCollisionAvailable)
        {
            Deactivate();
        }
        _collisionInProgress = false;
        Debug.Log("collision terminated");
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        OnTriggerEnter2D(collision);
    }
}
