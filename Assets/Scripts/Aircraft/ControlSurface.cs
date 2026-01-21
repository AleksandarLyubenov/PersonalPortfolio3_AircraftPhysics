using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ControlSurface : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private AircraftPhysics aircraft;
    [SerializeField] private float surfaceArea = 2f;
    [SerializeField] private float liftCoefficient = 1f;

    public enum InputAxis { Pitch, Roll, Yaw }
    [SerializeField] private InputAxis axis;
    [SerializeField] private float inputMultiplier = 1f;

    [Header("Deflection Settings")]
    [SerializeField] private float maxDeflectionAngle = 25f;
    [SerializeField] private float deflectionSpeed = 90f; // degrees per second
    [SerializeField] private Transform visualDeflectionTarget;
    [SerializeField] private Vector3 deflectionAxis = Vector3.right;

    [Header("Visual Feedback")]
    [SerializeField] private Color defaultColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;

    private Renderer rend;
    private float targetDeflection = 0f;
    private float currentDeflection = 0f;
    private Vector3 liftForce;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (aircraft == null) Debug.LogError("AircraftPhysics not assigned!");
    }

    void FixedUpdate()
    {
        if (!aircraft) return;

        // --- Input ---
        float rawInput = GetInputForAxis() * inputMultiplier;
        targetDeflection = AircraftPhysics.SmoothedInput(targetDeflection, rawInput * maxDeflectionAngle, 3f);

        // --- Smooth transition ---
        currentDeflection = Mathf.MoveTowards(currentDeflection, targetDeflection, deflectionSpeed * Time.fixedDeltaTime);

        // --- Visual deflection ---
        if (visualDeflectionTarget != null)
        {
            Quaternion localRot = Quaternion.Euler(deflectionAxis * currentDeflection);
            visualDeflectionTarget.localRotation = localRot;
        }

        // --- Color Feedback ---
        if (rend != null)
        {
            rend.material.color = Mathf.Abs(rawInput) > 0.01f ? activeColor : defaultColor;
        }

        // --- Aerodynamics ---
        Vector3 airflow = transform.InverseTransformDirection(aircraft.AirflowVelocity);
        float localSpeed = airflow.z;
        float effectiveDeflection = Mathf.Lerp(0f, currentDeflection, 0.7f); // smooth force effect (70% real-time)
        float aoa = Mathf.Atan2(airflow.y, airflow.z) + Mathf.Deg2Rad * effectiveDeflection;


        float liftMagnitude = 0.5f * aircraft.AirDensity * localSpeed * localSpeed * surfaceArea * liftCoefficient;
        liftForce = transform.up * liftMagnitude * Mathf.Sin(aoa * 2f);

        Rigidbody rb = aircraft.GetComponent<Rigidbody>();
        rb.AddForceAtPosition(liftForce, transform.position);
    }

    private float GetInputForAxis()
    {
        switch (axis)
        {
            case InputAxis.Pitch:
                return (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            case InputAxis.Roll:
                return (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            case InputAxis.Yaw:
                return (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            default:
                return 0f;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || liftForce == Vector3.zero) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + liftForce.normalized * Mathf.Log10(liftForce.magnitude + 1f));
        Gizmos.DrawSphere(transform.position + liftForce.normalized * 0.5f, 0.05f);
    }
}
