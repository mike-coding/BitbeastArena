using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    private BeastController _executingBeastController;
    private GameObject _executingBeastObject;
    private PolygonCollider2D polygonCollider;
    private DamageParticle _damage;
    private SpriteRenderer _sp;
    private int _spriteType;
    private string _abilityName;
    private bool _isEnemy;
    private Transform _beastTransform; // Transform of the BeastController
    private Vector3 offset; // Offset from the BeastController
    private bool _canHit = true;
    private bool _terminateOnHit = true;
    private float _duration;
    private Dictionary<BeastController, Coroutine> _activeDamageCoroutines = new Dictionary<BeastController, Coroutine>();

    public void Init(string abilityName, DamageParticle damage, BeastController beast) //take the ability as an argument instead
    {
        _executingBeastController = beast;
        _executingBeastObject = _executingBeastController.gameObject;
        HandleStyle(abilityName);
        _sp = GetComponent<SpriteRenderer>();
        _sp.sprite = Resources.Load<Sprite>($"Sprites/Misc/hitbox{_spriteType}");
        _isEnemy = beast.IsEnemy;
        _beastTransform = beast.transform;
        _damage= damage;
        
        offset = transform.position - beast.transform.position;

        // Add or get PolygonCollider2D
        polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.isTrigger = true;

        // Rotate hitbox to face attack direction
        float angle = Mathf.Atan2(damage.Direction.y, damage.Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Start coroutine to destroy hitbox after duration
        StartCoroutine(DestroyAfterDuration(_duration));
    }

    private void HandleStyle(string abilityName)
    {
        _abilityName = abilityName;
        switch (abilityName)
        {
            case "Headbutt":
                _spriteType = 1;
                _duration = 0.15f;
                break;

            case "Fireball": //test
                _spriteType = 2;
                _duration = Ability.Dex["Fireball"].CoolDownTime;
                _terminateOnHit = false;
                transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                break;
                 
            case "Stench": //same as fireball, could condense this to one function
                _spriteType = 2;
                _duration = Ability.Dex["Stench"].CoolDownTime;
                _terminateOnHit = false;
                transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                break;
        }
    }

    private void Update() { if (_beastTransform != null) transform.position = _beastTransform.position + offset; }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime<duration && _executingBeastObject!=null)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            
        }
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (_terminateOnHit) return;
        BeastController beast = collision.GetComponent<BeastController>();
        if (beast != null && beast.IsEnemy != _isEnemy && _canHit) // Check if it's an enemy and different from hitbox's enemy status
        {
            //beast.TakeDamage(_damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        BeastController beast = collision.GetComponent<BeastController>();
        if (beast != null && beast.IsEnemy != _isEnemy && _canHit) // Check if it's an enemy and different from hitbox's enemy status
        {
            if (_terminateOnHit)
            {
                _canHit = false;
                Destroy(polygonCollider);
                // Get the particle emitter and set its position to the midpoint of the collision
                GameObject particleEmitter = GameManager.GetParticleEmitter();
                Vector3 collisionPoint = (transform.position + beast.transform.position) / 2;
                particleEmitter.transform.position = collisionPoint;
                particleEmitter.GetComponent<ParticleEmitterBrain>().SetCollisionPuff(new Color(1, 1, 1, 0.5f));
                particleEmitter.SetActive(true);
                if (!_executingBeastController.IsBeingKnockedBack && _damage.KnockbackMagnitude>0)_executingBeastController.Body.velocity *= 0.35f;
                beast.TakeDamage(_damage);

                Destroy(gameObject);
            }
            else _activeDamageCoroutines[beast] = StartCoroutine(ContinuousDamage(beast));
        }
    }

    private IEnumerator ContinuousDamage(BeastController beast)
    {
        while (true)
        {
            if (_abilityName=="Stench")
            {
                Vector3 newCenterPoint = beast.gameObject.transform.position;
                newCenterPoint.y += beast.CurrentState.YoffsetToCenter;
                GameObject particles = GameManager.GetParticleEmitter();
                if (particles != null)
                {
                    particles.transform.position = newCenterPoint;
                    particles.GetComponent<ParticleEmitterBrain>().SetProjectileDisintegration(Stench.averageColor);
                    particles.SetActive(true);
                }

                // modify knockback
            }
            beast.TakeDamage(_damage); // Deal damage to the beast
            yield return new WaitForSeconds(0.45f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        BeastController beast = collision.GetComponent<BeastController>();
        if (beast != null && _activeDamageCoroutines.ContainsKey(beast))
        {
            StopCoroutine(_activeDamageCoroutines[beast]);
            _activeDamageCoroutines[beast] = null;
        }
    }
}
