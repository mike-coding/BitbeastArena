using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ParticleController : MonoBehaviour
{
    private Rigidbody2D _body;
    private SpriteRenderer _sp;
    private GameObject _canvas;
    private Text _text;
    private Text _text2;
    private int _type;
    private bool _isUIElement;
    private GameObject _parentObject;

    public void Init(int type, Color? particleColor = null, float scale = 1f, bool isUIElement = false)
    {
        _type = type;
        _isUIElement=isUIElement;
        Color colorToUse = particleColor ?? Color.white;
        LoadComponents();
        _sp.color = colorToUse;
        _text.color = colorToUse;
        // Set the scale of the particle's transform
        transform.localScale = new Vector3(scale, scale, scale);
        //spriteLoading
        switch (type)
        {
            case 0: // egg left half
                HandleEggHalfSetup();
                break;
            case 1: // egg right half
                HandleEggHalfSetup();
                break;
            case 2: // damage particle
                _sp.enabled = false;
                _canvas.SetActive(true);
                StartCoroutine(LifetimeRoutine(0.5f));
                break;
            case 3: // Brace ability Effect
                _sp.sprite = Resources.Load<Sprite>("Sprites/AbilityEffects/Bubble");
                StartCoroutine(LifetimeRoutine(1f));
                StartCoroutine(FadeOutSprite(1f));
                break;
        }
    }

    private void HandleEggHalfSetup()
    {
        if (_type == 0) _sp.sprite = Resources.Load<Sprite>("Sprites/Particles_Projectiles/eggHalf_0");
        if (_type == 1) _sp.sprite = Resources.Load<Sprite>("Sprites/Particles_Projectiles/eggHalf_1");

        if (_isUIElement)
        {
            _sp.sortingLayerName = "UI";
            _sp.sortingOrder = 2;
        }
        //lifetime Coroutine
        transform.LookAt(transform.position + GameManager.MainCamera.transform.rotation * Vector3.forward, GameManager.MainCamera.transform.rotation * Vector3.up);
        StartCoroutine(LifetimeRoutine(0.45f));
        StartCoroutine(FadeOutSprite(0.45f));
    }

    private void LoadComponents()
    {
        _sp = GetComponent<SpriteRenderer>();
        _body= GetComponent<Rigidbody2D>();
        _canvas = transform.Find("Canvas").gameObject;
        _canvas.GetComponent<Canvas>().sortingLayerName = "Particle";
        _text = transform.Find("Canvas/TEXT").gameObject.GetComponent<Text>();
        _text2 = transform.Find("Canvas/TEXT2").gameObject.GetComponent<Text>();
        if (_canvas == null || _text == null) Debug.Log("yea u not gettin em mate");
    }

    public void LaunchDirection(Vector2 direction, float scale=1f)
    {
        _body.velocity = direction*scale;
        //Debug.Log(_body.velocity);
        //Debug.Log(scale);
        if (_type == 0) _body.angularVelocity = 50f;
        if (_type == 1) _body.angularVelocity = -50f;
        if (_type == 2) _body.gravityScale = 0.5f;
    }

    public static ParticleController CreateParticle(Vector3 position, int type, Vector2 direction, float scale = 1f, Color? particleColor = null, bool isUIElement = false, GameObject parent = null)
    {
        Color colorToUse = particleColor ?? Color.white;
        GameObject particlePrefab = Resources.Load<GameObject>("gameObjects/Particle");

        // Instantiate the particle with or without a parent
        GameObject particleInstance;
        if (parent != null)
        {
            particleInstance = Instantiate(particlePrefab, position, Quaternion.identity, parent.transform);
            Vector3 newPosition = new Vector3(0, particleInstance.transform.localPosition.y, 0);
            particleInstance.transform.localPosition = newPosition;
        }
        else particleInstance = Instantiate(particlePrefab, position, Quaternion.identity);

        ParticleController particleController = particleInstance.GetComponent<ParticleController>();
        if (particleController)
        {
            if (parent != null) particleController._parentObject = parent;
            particleController.Init(type, colorToUse, scale, isUIElement);
            particleController.LaunchDirection(direction, scale);
        }
        return particleController;
    }

    public static ParticleController CreateDamageParticle(Vector3 position, DamageParticle damageInstance, BeastController beast)
    {
        // Define the two endpoint colors
        Color colorNegativeOne = new Color(25f / 255f, 224f / 255f, 107f / 255f); // RGB(25, 224, 107)
        Color colorPositiveOne = new Color(224f / 255f, 49f / 255f, 25f / 255f);  // RGB(224, 49, 25)

        // Interpolate the color
        Color colorToUse = GameManager.GetColor(damageInstance.VariationScale);

        // Rest of your particle creation logic
        GameObject particlePrefab = Resources.Load<GameObject>("gameObjects/Particle");
        GameObject particleInstance = Instantiate(particlePrefab, position, Quaternion.identity);

        ParticleController particleController = particleInstance.GetComponent<ParticleController>();
        if (particleController)
        {
            particleController.Init(2, colorToUse, 0.1f, false);
            particleController._text.text = Mathf.Abs(damageInstance.Damage).ToString();
            if (damageInstance.Damage < 0) particleController._text.text = "+" + particleController._text.text;
            if (damageInstance.VariationScale == 2) particleController._text.text += "!";
            if (damageInstance.Damage == 0) particleController._text.text = "Whiff!";
            particleController._text2.text = particleController._text.text;
            Vector2 directionOfLaunch = new Vector2(0, 0.35f) + beast.Body.velocity.normalized*0.3f;
            particleController.LaunchDirection(directionOfLaunch, 5f);
        }
        return particleController;
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        GameObject.Destroy(gameObject);
    }

    private IEnumerator FadeOutSprite(float duration)
    {
        float currentTime = 0;
        Color startColor = _sp.color;

        while (currentTime < duration)
        {
            float alpha = Mathf.Clamp(Mathf.Lerp(2f, 0f, currentTime / duration),0,1);
            //Debug.Log(_sp.color.a);
            _sp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            _text.color = new Color(_text.color.r,_text.color.g, _text.color.b,alpha);
            currentTime += Time.deltaTime;
            yield return null;
        }

        _sp.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        // After fade out, destroy the game object
        Destroy(gameObject);
    }
}
