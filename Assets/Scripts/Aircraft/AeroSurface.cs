using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AeroSurface : MonoBehaviour
{
    [SerializeField] private AircraftPhysics aircraft;
    [SerializeField] private float liftCoefficient = 1.0f;
    [SerializeField] private float surfaceArea = 5.0f;
    [SerializeField] private Transform forcePoint; // Where to apply the lift

    //private void FixedUpdate()
    //{
    //    if (aircraft == null) return;

    //    Rigidbody rb = aircraft.GetComponent<Rigidbody>();
    //    Vector3 airflowWorld = aircraft.AirflowVelocity;
    //    Vector3 chordLine = -transform.forward;        // Nose is -Z
    //    Vector3 liftDirection = transform.up;          // Local lift direction

    //    // Project airflow onto the plane defined by the chord line
    //    Vector3 projectedAirflow = Vector3.ProjectOnPlane(airflowWorld.normalized, chordLine).normalized;

    //    // Angle of attack = angle between lift direction and projected airflow
    //    float angleOfAttack = Vector3.SignedAngle(liftDirection, projectedAirflow, chordLine) * Mathf.Deg2Rad;

    //    float speed = airflowWorld.magnitude;

    //    // Standard lift formula
    //    float liftMagnitude = 0.5f * aircraft.AirDensity * speed * speed * surfaceArea * liftCoefficient;
    //    Vector3 liftForce = liftDirection * liftMagnitude * Mathf.Sin(angleOfAttack * 2f); // AoA boosted

    //    Vector3 point = forcePoint ? forcePoint.position : transform.position;
    //    rb.AddForceAtPosition(liftForce, point);
    //}

    //private void OnDrawGizmos()
    //{
    //    if (!Application.isPlaying) return;

    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
    //    Gizmos.color = Color.magenta;
    //    Gizmos.DrawLine(transform.position, transform.position + -transform.forward * 2f);
    //}


    private void FixedUpdate()
    {
        if (aircraft == null) return;

        Vector3 localAirflow = transform.InverseTransformDirection(aircraft.AirflowVelocity);
        float localSpeed = localAirflow.z; // Forward flow over the surface
        float angleOfAttack = Mathf.Atan2(localAirflow.y, localAirflow.z);

        float lift = 0.5f * aircraft.AirDensity * localSpeed * localSpeed * surfaceArea * liftCoefficient;
        Vector3 liftDirection = transform.up;
        Vector3 liftForce = liftDirection * lift * Mathf.Sin(angleOfAttack * 2); // Boost responsiveness

        Vector3 point = forcePoint ? forcePoint.position : transform.position;
        aircraft.GetComponent<Rigidbody>().AddForceAtPosition(liftForce, point);
    }
}
