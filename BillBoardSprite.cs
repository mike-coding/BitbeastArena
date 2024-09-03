using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

public class BillBoardSprite : MonoBehaviour
{
    public bool RandomizeObstacleSprite;
    private string _gameObjectName;
    private Transform _transform;
    private Transform camTransform;
    private SpriteRenderer sr;
    private GameObject _spriteObject;
    public bool PointOnce = false;

    void Awake()
    {
        _gameObjectName = gameObject.name;
        camTransform = GameManager.MainCamera.transform;
        _transform = GetComponent<Transform>();
        string trimmedName = _gameObjectName.Replace("(", "").Replace(")", "");
        trimmedName = Regex.Replace(trimmedName, @"\d", "").Trim();
        sr = GetComponent<SpriteRenderer>();

        if (RandomizeObstacleSprite)
        {
            int spriteNameAddendum = UnityEngine.Random.Range(1, 5);
            string prefix;
            if (_gameObjectName.Contains("Beach")) prefix = "Beach";
            else prefix = "Forest";
            sr.sprite = Resources.Load<Sprite>($"Sprites/Obstacles/{prefix}Grass_{spriteNameAddendum}");
        }
        if (PointOnce) UpdateOrientation();
    }

    void OnEnable()
    {
        CameraMovementNotifier.OnCameraMove += UpdateOrientation;
    }

    void OnDisable()
    {
        CameraMovementNotifier.OnCameraMove -= UpdateOrientation;
    }

    void UpdateOrientation()
    {
        _transform.LookAt(_transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
    }
}

