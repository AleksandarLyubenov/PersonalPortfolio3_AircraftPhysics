using UnityEngine;

public class IntakeRamp : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] private AircraftPhysics aircraft;
    [SerializeField] private float maxDownAngle = 10f; // degrees
    [SerializeField] private float deflectionSpeed = 30f; // deg/sec
    [SerializeField] private AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 25, 1); // AoA -> 0..1

    private float currentDeflection = 0f;

    private void FixedUpdate()
    {
        if (aircraft == null) return;

        float aoa = CalculateAngleOfAttack();
        float t = responseCurve.Evaluate(Mathf.Abs(aoa));
        float targetDeflection = -maxDownAngle * t;

        currentDeflection = Mathf.MoveTowards(currentDeflection, targetDeflection, deflectionSpeed * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(currentDeflection, 0f, 0f);
    }

    private float CalculateAngleOfAttack()
    {
        Vector3 airflow = aircraft.AirflowVelocity.normalized;
        Vector3 forward = -transform.forward; // because nose is -Z
        Vector3 up = transform.up;

        Vector3 projectedAirflow = Vector3.ProjectOnPlane(airflow, forward).normalized;
        float aoa = Vector3.SignedAngle(up, projectedAirflow, forward);
        return aoa;
    }
}
