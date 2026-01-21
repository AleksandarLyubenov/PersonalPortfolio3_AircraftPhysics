using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingMissile : MonoBehaviour
{
    public enum GuidanceType { SARH, ARH }

    [Header("Flight Settings")]
    public float maxSpeed = 300f;
    public float acceleration = 100f;
    public float burnTime = 3f;
    public float maxGForce = 50f;
    public float guidanceDelay = 1f;
    public float timeout = 10f;

    [Header("Guidance")]
    public GuidanceType guidanceType = GuidanceType.SARH;
    public GameObject contrailPrefab;

    [Header("Fuse & Effects")]
    public float proximityRange = 5f;
    public GameObject explosionPrefab;

    private Transform target;
    private AdvancedRadar externalRadar;
    private Rigidbody rb;

    private float currentSpeed = 0f;
    private float timeSinceLaunch = 0f;
    private bool isAutonomous = false;

    private Vector3 inheritedVelocity;
    private bool launched = false;

    public void Launch(Transform lockedTarget, AdvancedRadar radarSource, Vector3 inheritedVel)
    {
        target = lockedTarget;
        externalRadar = radarSource;
        inheritedVelocity = inheritedVel;
        launched = true;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        if (contrailPrefab)
            Instantiate(contrailPrefab, transform.position, transform.rotation, transform);
    }

    void FixedUpdate()
    {
        if (!launched) return;

        timeSinceLaunch += Time.fixedDeltaTime;

        if (timeSinceLaunch > timeout || target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (timeSinceLaunch <= burnTime)
        {
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
        }

        Vector3 forward = -transform.right;
        rb.velocity = forward * currentSpeed + inheritedVelocity;

        if (timeSinceLaunch >= guidanceDelay)
        {
            bool hasLock = externalRadar && externalRadar.GetLockedTarget() == target;

            if (guidanceType == GuidanceType.SARH && hasLock)
                GuideToTarget();
            else if (guidanceType == GuidanceType.ARH)
            {
                if (!isAutonomous && Vector3.Distance(transform.position, target.position) < 5000f)
                    isAutonomous = true;

                if (isAutonomous || hasLock)
                    GuideToTarget();
            }
        }

        if (target != null && Vector3.Distance(transform.position, target.position) <= proximityRange)
        {
            ExplodeAt(target);
        }
    }
    void ExplodeAt(Transform victim)
    {
        if (explosionPrefab != null)
        {
            // Instantiate explosion at the plane's position
            GameObject fx = Instantiate(explosionPrefab, victim.position, Quaternion.identity);
            fx.transform.parent = null;

            // Inherit velocity if both have rigidbodies
            Rigidbody victimRb = victim.GetComponent<Rigidbody>();
            Rigidbody fxRb = fx.GetComponent<Rigidbody>();
            if (victimRb != null && fxRb != null)
            {
                fxRb.velocity = victimRb.velocity;
            }
        }

        Destroy(victim.gameObject);
        Destroy(gameObject);
    }

    void GuideToTargetWithLofting()
    {
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

        // Only apply lofting if outside pitbull range (i.e. 5km)
        if (distance > 5000f)
        {
            float loftAmount = Mathf.Clamp01((distance - 5000f) / 5000f);
            Vector3 upOffset = transform.up * loftAmount;
            direction += upOffset;
            direction.Normalize();
        }

        Vector3 missileNose = -transform.right;
        Quaternion desiredRotation = Quaternion.FromToRotation(missileNose, direction) * transform.rotation;

        float maxTurnRate = maxGForce * Physics.gravity.magnitude / Mathf.Max(currentSpeed, 1f);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, desiredRotation, maxTurnRate * Mathf.Rad2Deg * Time.fixedDeltaTime));
    }

    void GuideToTarget()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        Vector3 direction = toTarget.normalized;

        Vector3 missileNose = -transform.right;
        Quaternion desiredRotation = Quaternion.FromToRotation(missileNose, direction) * transform.rotation;

        float maxTurnRate = maxGForce * Physics.gravity.magnitude / Mathf.Max(currentSpeed, 1f);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, desiredRotation, maxTurnRate * Mathf.Rad2Deg * Time.fixedDeltaTime));
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Plane"))
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}
