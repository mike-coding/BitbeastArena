using UnityEngine;

[RequireComponent(typeof(Camera))]

public class MaintainAspectRatio : MonoBehaviour
{
    [SerializeField] private float _targetAspectRatio = 16f / 9f; // Set the target aspect ratio (e.g., 4:3, 16:9, etc.)
    private Transform _transform;
    private Camera _camera;
    private float _startingZPosition;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _transform = GetComponent<Transform>();
        _startingZPosition = _transform.position.z;

    }

    private void Update()
    {
        EnforceAspectRatio();
    }

    private void EnforceAspectRatio()
    {
        float currentAspectRatio = (float)Screen.width / (float)Screen.height;
        float scaleHeight = currentAspectRatio / _targetAspectRatio;

        if (scaleHeight < 1.0f)
        {
            _camera.rect = new Rect(0, (1.0f - scaleHeight) / 2.0f, 1, scaleHeight);
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            _camera.rect = new Rect((1.0f - scaleWidth) / 2.0f, 0, scaleWidth, 1);
        }
    }
}