using UnityEngine;

// Editor-only head-look simulation. Cardboard's real head tracking comes from the phone's
// gyroscope via native code (CardboardUnity_*), which doesn't exist in the Editor - so without
// this, pressing Play just shows a static frame. Hold the right mouse button and drag to look
// around, like orbiting in the Scene view. Wrapped in UNITY_EDITOR so it never ships to device.
#if UNITY_EDITOR
public class EditorLookAroundSimulator : MonoBehaviour
{
    public float MouseSensitivity = 2f;

    private float _yaw;
    private float _pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = e.x;
    }

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * MouseSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }
}
#endif
