using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaypointFollow : MonoBehaviour
{
    public Transform[] waypoints;
    public float rotationSpeed = 2f;

    public float minSpeed = 170f;
    public float cruiseSpeed = 200f;
    public float maxSpeed = 300f;
    public float speedSmoothTime = 2f;

    private int currentWaypoint = 0;
    private float currentSpeed;
    private float speedVelocity;
    private float lastY;

    private Rigidbody rb;

    void Start()
    {
        if (waypoints.Length == 0)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        lastY = transform.position.y;
        currentSpeed = cruiseSpeed;
    }

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];
        Vector3 direction = (target.position - transform.position).normalized;

        float verticalDelta = transform.position.y - lastY;
        lastY = transform.position.y;

        float desiredSpeed = cruiseSpeed;
        if (verticalDelta > 0.1f) desiredSpeed = minSpeed;
        else if (verticalDelta < -0.1f) desiredSpeed = maxSpeed;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, desiredSpeed, ref speedVelocity, speedSmoothTime);

        rb.velocity = transform.forward * currentSpeed;

        // Smooth rotation toward target
        Quaternion targetRotation = Quaternion.LookRotation(direction, transform.up);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

        // Switch to next waypoint
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance < 100f)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        }
    }
}
