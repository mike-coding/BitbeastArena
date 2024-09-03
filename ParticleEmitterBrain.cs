using UnityEngine;
using UnityEngine.UIElements;

public class ParticleEmitterBrain : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private ParticleSystem.MainModule _mainModule;
    private ParticleSystem.VelocityOverLifetimeModule _velModule;
    private ParticleSystem.EmissionModule _emissionModule;
    private ParticleSystem.ShapeModule _shapeModule;
    private ParticleSystemRenderer _renderModule;


    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _renderModule = _particleSystem.GetComponent<ParticleSystemRenderer>();
        _mainModule = _particleSystem.main;
        _velModule = _particleSystem.velocityOverLifetime;
        _emissionModule = _particleSystem.emission;
        _shapeModule = _particleSystem.shape;
    }

    private void OnEnable()
    {
        _particleSystem.Play();
    }

    public void ToggleSystem()
    {
        if (_particleSystem.isPlaying) _particleSystem.Stop();
        else _particleSystem.Play();
    }

    private void Update()
    {
        if (!_particleSystem.IsAlive())
        {
            if (gameObject.transform.parent != null) gameObject.transform.SetParent(null);
            gameObject.transform.up = Vector2.up;
            _renderModule.sortingLayerName = "Particle";
            GameManager.RequeueParticleEmitterToPool(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void SetUI()
    {
        _renderModule.sortingLayerName = "UI";
        gameObject.layer = LayerMask.NameToLayer("UI");
    }

    public void SetColor(Color newColor)
    {
        _mainModule.startColor = newColor;
    }

    public void SetProjectileDisintegration(Color projectileColor)
    {

        //main
        _mainModule.startLifetime = 0.15f; 
        _mainModule.startSpeed = 4.51f; 
        _mainModule.startSize = 0.08f;
        _mainModule.maxParticles = 6;
        _mainModule.duration = 0.15f;

        //emission
        _emissionModule.rateOverTime = 146f;
        

        //velocity
        _velModule.x = 0;
        _velModule.y = 0;
        _velModule.z = 0;

        SetColor(projectileColor);
        
    }

    public void SetBubbleSpawnEffect(GameObject parent,float Yoffset=0)
    {
        if (parent != null)
        {
            gameObject.transform.parent = parent.transform;
            Vector3 offsetPosition = Vector3.zero;
            offsetPosition.y += Yoffset;
            gameObject.transform.localPosition = offsetPosition;
        }

        SetProjectileDisintegration(Bubble.averageColor);

        _mainModule.startLifetime =0.25f;
        _mainModule.startSpeed = -1f;
        _mainModule.maxParticles = 15;
        _shapeModule.radius = 0.325f;

        //gameObject.transform.localScale *= 1.7f;
    }

    public void SetOnFireEffect(GameObject parent, float Yoffset = 0)
    {
        gameObject.transform.parent = parent.transform;
        Vector3 offsetPosition = Vector3.zero;
        offsetPosition.y += Yoffset;
        gameObject.transform.localPosition = offsetPosition;
        gameObject.transform.up = Vector3.up;
        _renderModule.sortingLayerName = "Default";


        SetProjectileDisintegration(Fireball.averageColor);

        _mainModule.duration= Ability.Dex["On Fire"].CoolDownTime+0.05f;
        _mainModule.startSize = 0.1f;
        _mainModule.startLifetime = 0.2f;
        _mainModule.startSpeed = 0.75f;
        _mainModule.maxParticles = 40;
        _shapeModule.radius = 0.325f;
        _velModule.y = 0f;
        _velModule.x = 0f;
        _velModule.z = 0f;

        //gameObject.transform.localScale *= 1.7f;
    }

    public void SetCollisionPuff(Color projectileColor)
    {
        //main
        _mainModule.startLifetime = 0.12f;
        _mainModule.startSpeed = 5.5f;
        _mainModule.startSize = 0.095f;
        _mainModule.maxParticles = 6;
        _mainModule.duration = 0.01f;

        //emission
        _emissionModule.rateOverTime = 999f;

        //velocity
        _velModule.x = 0;
        _velModule.y = 0;
        _velModule.z = -9.4f;

        SetColor(projectileColor);
    }

    public void SetParticleSize(float size)
    {
        _mainModule.startSize = size;
    }

    public void SetSmokePuff(Transform followTransform=null)
    {
        //main
        _mainModule.startLifetime = 0.25f; 
        _mainModule.startSpeed = 3f;
        _mainModule.startSize = 0.2f; 
        _mainModule.maxParticles = 10;
        _mainModule.duration = 0.15f;

        //emission
        _emissionModule.rateOverTime = 150f;

        //velocity
        _velModule.x = 0;
        _velModule.y = 0;
        _velModule.z = 0;

        SetColor(new Color(1f, 1f, 1f, 0.3f));
    }

    public void SetEvolutionBuildUp()
    {
        SetProjectileDisintegration(Color.white);
        _mainModule.startSpeed = 1f;
        _mainModule.startLifetime = 0.25f;
        _mainModule.maxParticles = 10;
    }

    public void SetEvolutionClimax()
    {
        SetSmokePuff();
        _mainModule.startLifetime = 0.35f;
        _mainModule.startSize = 0.3f;
        _mainModule.maxParticles = 16;
        _mainModule.startSpeed = .75f;

    }
}
