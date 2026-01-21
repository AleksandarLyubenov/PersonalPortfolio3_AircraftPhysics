using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FlapSurface : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AircraftPhysics aircraft;
    [SerializeField] private Transform visualFlap; // flap mesh to rotate
    [SerializeField] private Vector3 deflectionAxis = Vector3.right;

    [Header("Aerodynamics")]
    [SerializeField] private float surfaceArea = 3f;
    [SerializeField] private float liftCoefficient = 1.5f;
    [SerializeField] private float dragCoefficient = 0.02f;

    [Header("Deflection")]
    [SerializeField] private float[] flapAngles = new float[] { 0f, 15f, 40f }; // stages
    [SerializeField] private float deflectionSpeed = 30f;

    [Header("Visuals")]
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.cyan;

    private int targetFlapStage = 0;
    private float currentDeflection = 0f;
    private float targetAngle => flapAngles[targetFlapStage];
    private Renderer rend;
    private Vector3 liftForce;
    private Vector3 dragForce;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (aircraft == null) Debug.LogError("AircraftPhysics not assigned to flap.");
    }

    void Update()
    {
        // Cycle flap stage
        if (Input.GetKeyDown(KeyCode.Z))
        {
            targetFlapStage = Mathf.Clamp(targetFlapStage + 1, 0, flapAngles.Length - 1);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            targetFlapStage = Mathf.Clamp(targetFlapStage - 1, 0, flapAngles.Length - 1);
        }
    }

    void FixedUpdate()
    {
        if (aircraft == null) return;

        // Animate deflection smoothly
        currentDeflection = Mathf.MoveTowards(currentDeflection, targetAngle, deflectionSpeed * Time.fixedDeltaTime);

        // Rotate visual flap
        if (visualFlap != null)
        {
            visualFlap.localRotation = Quaternion.Euler(deflectionAxis * currentDeflection);
        }

        // Color update
        if (rend != null)
        {
            rend.material.color = (targetFlapStage > 0) ? activeColor : inactiveColor;
        }

        // Aero force
        Vector3 airflow = transform.InverseTransformDirection(aircraft.AirflowVelocity);
        float localSpeed = airflow.z;
        float aoa = Mathf.Atan2(airflow.y, airflow.z) + Mathf.Deg2Rad * currentDeflection;

        float lift = 0.5f * aircraft.AirDensity * localSpeed * localSpeed * surfaceArea * liftCoefficient;
        liftForce = transform.up * lift * Mathf.Sin(aoa * 2);

        Rigidbody rb = aircraft.GetComponent<Rigidbody>();
        rb.AddForceAtPosition(liftForce, transform.position);

        // Optional drag force
        float drag = 0.5f * aircraft.AirDensity * localSpeed * localSpeed * surfaceArea * dragCoefficient;
        dragForce = -transform.forward * drag * (targetFlapStage / 2f); // scale with flap stage
        rb.AddForceAtPosition(dragForce, transform.position);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Lift vector
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + liftForce.normalized * Mathf.Log10(liftForce.magnitude + 1f));

        // Drag vector
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + dragForce.normalized * Mathf.Log10(dragForce.magnitude + 1f));
    }
}
