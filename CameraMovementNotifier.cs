using UnityEngine;

public class CameraMovementNotifier : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public delegate void CameraMoved();
    public static event CameraMoved OnCameraMove;

    void Start()
    {
        OnCameraMove?.Invoke();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
    /*
    void Update()
    {
        if (lastPosition != transform.position || lastRotation != transform.rotation)
        {
            OnCameraMove?.Invoke();
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }
    */


    public static void InvokeMovement() { OnCameraMove?.Invoke(); }
}
