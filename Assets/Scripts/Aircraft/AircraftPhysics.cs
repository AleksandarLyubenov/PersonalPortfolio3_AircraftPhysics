using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AircraftPhysics : MonoBehaviour
{
    [Header("Rigidbody & Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float airDensity = 1.225f;

    [Header("Thrust Settings")]
    [SerializeField] private float maxThrust = 50000f;
    [SerializeField] private float thrustChangeRate = 0.5f;
    private float currentThrustPercent = 0f; // 0 to 1
    private Vector3 thrustForce;

    public float inputSmoothing = 5f;

    [Header("Brake Settings")]
    [SerializeField] private float brakeForce = 5000f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private float minPitch = 0.5f;
    [SerializeField] private float maxPitch = 2f;

    [Header("Airbrake Settings")]
    [SerializeField] private Transform airbrakeModel;
    [SerializeField] private Vector3 deployedRotation = new Vector3(60f, 0f, 0f);
    [SerializeField] private Vector3 stowedRotation = Vector3.zero;
    [SerializeField] private float airbrakeDeploySpeed = 10f;
    [SerializeField] private float airbrakeDrag = 15000f;

    private bool airbrakeEngaged = false;

    [Header("Aerodynamic Drag")]
    [SerializeField] private float dragCoefficient = 0.02f;
    [SerializeField] private float frontalArea = 2.5f;


    public Vector3 AirflowVelocity => -rb.velocity;
    public float AirDensity => airDensity;

    public static float SmoothedInput(float current, float target, float rate)
    {
        return Mathf.Lerp(current, target, Time.fixedDeltaTime * rate);
    }

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.angularDrag = 0.05f;
        rb.mass = 1000f;

        if (!engineAudio)
        {
            engineAudio = gameObject.AddComponent<AudioSource>();
            engineAudio.playOnAwake = true;
            engineAudio.loop = true;
            engineAudio.spatialBlend = 1f;
            engineAudio.minDistance = 5f;
            engineAudio.maxDistance = 500f;
        }

        if (!engineAudio.isPlaying)
            engineAudio.Play();
    }
    private void ApplyAerodynamicDrag()
    {
        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;
        if (speed < 0.1f) return; // negligible drag at low speed

        float dragMagnitude = 0.5f * airDensity * speed * speed * dragCoefficient * frontalArea;
        Vector3 dragForce = -velocity.normalized * dragMagnitude;
        rb.AddForce(dragForce);
    }


    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
            currentThrustPercent = Mathf.Clamp01(currentThrustPercent + thrustChangeRate * Time.deltaTime);
        if (Input.GetKey(KeyCode.LeftControl))
            currentThrustPercent = Mathf.Clamp01(currentThrustPercent - thrustChangeRate * Time.deltaTime);

        UpdateEngineSound();
    }

    void FixedUpdate()
    {
        ApplyThrust();
        ApplyAerodynamicDrag();
        ApplyBrakeLogic();
        AnimateAirbrake();
    }

    private void ApplyBrakeLogic()
    {
        if (Input.GetKey(KeyCode.B))
        {
            if (IsGrounded())
            {
                ApplyGroundBrakes();
                airbrakeEngaged = false;
            }
            else
            {
                ApplyAirbrake();
                airbrakeEngaged = true;
            }
        }
        else
        {
            airbrakeEngaged = false;
        }
    }


    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -transform.up, raycastDistance, groundLayer);
    }

    private void ApplyGroundBrakes()
    {
        Vector3 brakeDir = -rb.velocity.normalized;
        float brakeMag = Mathf.Clamp(rb.velocity.magnitude, 0f, 100f);
        rb.AddForce(brakeDir * brakeForce * brakeMag);
    }

    private void ApplyAirbrake()
    {
        Vector3 velocity = rb.velocity;
        float speed = velocity.magnitude;
        if (speed < 1f) return;

        float dragMagnitude = airbrakeDrag;
        Vector3 dragForce = -velocity.normalized * dragMagnitude;
        rb.AddForce(dragForce);
    }

    private void AnimateAirbrake()
    {
        if (!airbrakeModel) return;

        Quaternion targetRotation = Quaternion.Euler(airbrakeEngaged ? deployedRotation : stowedRotation);
        airbrakeModel.localRotation = Quaternion.Lerp(airbrakeModel.localRotation, targetRotation, Time.deltaTime * airbrakeDeploySpeed);
    }



    private void ApplyThrust()
    {
        float thrust = currentThrustPercent * maxThrust;
        thrustForce = -transform.forward * thrust;
        rb.AddForce(thrustForce);
    }

    private void UpdateEngineSound()
    {
        if (engineAudio)
        {
            engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, currentThrustPercent);
            engineAudio.volume = Mathf.Lerp(0.1f, 1f, currentThrustPercent);
        }
    }

    private float GetAltitude()
    {
        return transform.position.y;
    }

    private float GetRadarAltitude()
    {
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, 10000f, groundLayer))
        {
            return hit.distance;
        }
        return transform.position.y;
    }

    private float GetAOA()
    {
        Vector3 velocity = rb.velocity.normalized;
        float angle = Vector3.SignedAngle(transform.forward, velocity, transform.right);
        return angle;
    }

    private void OnGUI()
    {
        float altitude = GetAltitude();
        float radarAlt = GetRadarAltitude();
        float speed = rb.velocity.magnitude;
        float aoa = GetAOA();
        float thrustN = currentThrustPercent * maxThrust;

        // --- Main HUD (Top Left) ---
        GUILayout.BeginArea(new Rect(10, 10, 300, 160), GUI.skin.box);
        GUI.contentColor = Color.white;
        GUILayout.Label($"Alt:  {altitude:F1} m");
        GUILayout.Label($"RAlt: {radarAlt:F1} m");

        GUI.contentColor = speed < 30f ? Color.red : Color.white;
        GUILayout.Label($"Speed: {speed:F1} m/s");

        GUI.contentColor = Mathf.Abs(aoa) > 15f && speed < 40f ? Color.red : Color.white;
        GUILayout.Label($"AOA: {aoa:F1}°");

        GUI.contentColor = Color.white;
        GUILayout.Label($"Throttle: {(currentThrustPercent * 100f):F0}% ({thrustN:F0} N)");
        GUILayout.EndArea();

        // --- Controls Guide (Bottom Left) ---
        GUILayout.BeginArea(new Rect(10, Screen.height - 190, 300, 180), GUI.skin.box);
        GUILayout.Label("Controls:");
        GUILayout.Label("W/S - Pitch");
        GUILayout.Label("A/D - Roll");
        GUILayout.Label("Q/E - Yaw");
        GUILayout.Label("Z/X - Flaps");
        GUILayout.Label("B   - Brakes");
        GUILayout.Label("G   - Gear");
        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || thrustForce.magnitude < 1f) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + thrustForce.normalized * Mathf.Log10(thrustForce.magnitude + 1f));
        Gizmos.DrawSphere(transform.position + thrustForce.normalized * 0.5f, 0.05f);
    }
}
