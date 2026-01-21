using UnityEngine;

public class ChaseCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target; // The plane

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 25f;
    private float currentZoom;

    [Header("Free Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Vector2 pitchLimits = new Vector2(-60f, 80f);
    [SerializeField] private float returnSpeed = 5f;

    private bool isFreeLooking = false;

    private Vector3 initialLocalOffset;  // Local offset from plane at start
    private float pitch;
    private float yaw;
    private float currentPitch;
    private float currentYaw;

    void Start()
    {
        if (!target)
        {
            Debug.LogError("No target assigned to ChaseCameraController.");
            enabled = false;
            return;
        }

        // Store initial offset (where you placed the camera in editor)
        initialLocalOffset = target.InverseTransformPoint(transform.position);
        currentZoom = initialLocalOffset.magnitude;

        // Calculate yaw/pitch from initial offset
        Vector3 dir = initialLocalOffset.normalized;
        yaw = Mathf.Atan2(dir.x, -dir.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;

        currentYaw = yaw;
        currentPitch = pitch;
    }

    void Update()
    {
        // --- Zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed, minZoom, maxZoom);

        // --- Toggle freelook ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            isFreeLooking = true;
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            isFreeLooking = false;
        }

        // --- Free Look ---
        if (isFreeLooking)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        }
        else
        {
            // Smoothly return to original orbit behind the plane
            yaw = Mathf.Lerp(yaw, currentYaw, Time.deltaTime * returnSpeed);
            pitch = Mathf.Lerp(pitch, currentPitch, Time.deltaTime * returnSpeed);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Orbit offset ---
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = orbitRotation * new Vector3(0, 0, -currentZoom);
        Vector3 worldOffset = target.TransformDirection(offset);

        transform.position = target.position + worldOffset;
        transform.LookAt(target.position);

        if (!isFreeLooking)
        {
            // Apply plane's roll to camera (so horizon tilts properly)
            float planeRoll = -target.eulerAngles.z;
            transform.rotation = Quaternion.AngleAxis(planeRoll, transform.forward) * transform.rotation;
        }
    }

}
