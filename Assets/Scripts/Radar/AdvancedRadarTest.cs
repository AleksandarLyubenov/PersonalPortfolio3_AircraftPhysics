using UnityEngine;
using System.Collections.Generic;

public class AdvancedRadar : MonoBehaviour
{
    enum RadarMode { SRC, ACM }
    RadarMode currentMode = RadarMode.SRC;

    [Header("Radar Set General Settings")]
    [SerializeField] private float detectionRange = 1000f;
    [SerializeField] private float HorizontalLimit = 70f;
    [SerializeField] private float VerticalLimit = 10f;

    [Header("Radar Set ACM Settings")]
    [SerializeField] private float acmSweepSize = 3f;
    // [SerializeField] private int acmVerticalSlices = 1;

    [Header("Radar Set SRC Settings")]
    [SerializeField] private float horizontalSweepAngle = 70f;
    [SerializeField] private float verticalSweepAngle = 9f;
    [SerializeField] private int verticalSlices = 3;
    [SerializeField] private float sweepSpeed = 60f;
    [SerializeField] private float sweepResolution = 3f;

    [Header("Radar Set Notching and Data Settings")]
    [SerializeField] private float contactPersistenceTime = 3f;
    [SerializeField] private float notchingThreshold = 50f;
    
    [Header("Scene Setup")]
    [SerializeField] private LayerMask radarLayer;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private Camera mainCam;

    [SerializeField] public Transform GetLockedTarget() => lockedTarget;
    [SerializeField] public bool HasLock(Transform t) => t == lockedTarget;


    private Dictionary<Transform, RadarContact> contactHistory = new Dictionary<Transform, RadarContact>();

    private float currentHorizontalAngle = -35f;
    private int currentVerticalSlice = 0;
    private float horizontalCenterOffset;
    private float verticalSliceHeight;
    private Transform lockedTarget = null;
    private Transform selectedTarget = null;

    void Start()
    {
        horizontalCenterOffset = horizontalSweepAngle / 2f;
        verticalSliceHeight = verticalSweepAngle / verticalSlices;
    }

    void Update()
    {
        foreach (var contact in contactHistory.Values)
            contact.seenThisSweep = false;

        if (Input.GetKeyDown(KeyCode.T))
            horizontalSweepAngle = (horizontalSweepAngle == 70f) ? 30f : 70f;

        if (Input.GetKeyDown(KeyCode.R))
        {
            List<Transform> contacts = new List<Transform>(contactHistory.Keys);
            if (contacts.Count > 0)
            {
                int index = contacts.IndexOf(selectedTarget);
                index = (index + 1) % contacts.Count;
                selectedTarget = contacts[index];
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (lockedTarget != null)
            {
                Debug.Log("Unlocked target.");
                lockedTarget = null;
            }
            else if (selectedTarget != null)
            {
                lockedTarget = selectedTarget;
                Debug.Log("Locked target: " + lockedTarget.name);
            }
        }

        if (lockedTarget != null)
        {
            if (!contactHistory.ContainsKey(lockedTarget))
            {
                Debug.Log("Lost lock on target: " + lockedTarget.name);
                lockedTarget = null;
                currentMode = RadarMode.SRC;
            }
            else
            {
                currentMode = RadarMode.ACM;
            }
        }
        else
        {
            currentMode = RadarMode.SRC;
        }

        SweepRadar();
        DetectTargets();

        List<Transform> expired = new List<Transform>();
        foreach (var kvp in contactHistory)
        {
            if (kvp.Key == null || Time.time - kvp.Value.lastSeenTime > contactPersistenceTime)
                expired.Add(kvp.Key);
        }
        foreach (var key in expired)
            contactHistory.Remove(key);
    }

    void SweepRadar()
    {
        if (currentMode == RadarMode.SRC)
        {
            currentHorizontalAngle += sweepSpeed * Time.deltaTime;
            if (currentHorizontalAngle > horizontalSweepAngle / 2f)
            {
                currentHorizontalAngle = -horizontalSweepAngle / 2f;
                currentVerticalSlice++;
                if (currentVerticalSlice >= verticalSlices)
                    currentVerticalSlice = 0;
            }
        }
        else if (currentMode == RadarMode.ACM && lockedTarget != null)
        {
            Vector3 dirToTarget = (lockedTarget.position - transform.position).normalized;
            float hAngle = Vector3.SignedAngle(transform.forward, dirToTarget, transform.up);
            float vAngle = Vector3.SignedAngle(transform.forward, dirToTarget, transform.right);

            // Constrain ACM sweep to radar gimbal limits
            if (Mathf.Abs(hAngle) > HorizontalLimit || Mathf.Abs(vAngle) > VerticalLimit)
            {
                Debug.Log("Target out of ACM gimbal limits. Dropping lock.");
                lockedTarget = null;
                currentMode = RadarMode.SRC;
                return;
            }

            currentHorizontalAngle = hAngle;
            currentVerticalSlice = 0;
        }
    }

    void DetectTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, radarLayer);

        foreach (Collider hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            float horizAngle = Vector3.SignedAngle(transform.forward, dirToTarget, transform.up);
            float elevationAngle = Vector3.SignedAngle(transform.forward, dirToTarget, transform.right);

            bool inHorizontal = Mathf.Abs(horizAngle - currentHorizontalAngle) < sweepResolution;
            float verticalSliceStart = -verticalSweepAngle / 2 + currentVerticalSlice * verticalSliceHeight;
            bool inVertical = elevationAngle > verticalSliceStart && elevationAngle < verticalSliceStart + verticalSliceHeight;

            if (inHorizontal && inVertical)
            {
                Ray ray = new Ray(transform.position, (hit.transform.position - transform.position).normalized);
                if (Physics.Raycast(ray, out RaycastHit rayHit, detectionRange, obstructionMask))
                {
                    if (rayHit.transform != hit.transform) continue;
                }

                Rigidbody radarRb = GetComponent<Rigidbody>();
                Vector3 radarVel = radarRb != null ? radarRb.velocity : Vector3.zero;

                Rigidbody targetRb = hit.GetComponent<Rigidbody>();
                Vector3 targetVel = targetRb != null ? targetRb.velocity : Vector3.zero;

                Vector3 relVel = targetVel - radarVel;
                float closingSpeed = Vector3.Dot(dirToTarget, relVel);

                if (Mathf.Abs(closingSpeed) < notchingThreshold)
                {
                    Debug.DrawRay(transform.position, dirToTarget * 10f, Color.cyan, 0.1f);
                    continue;
                }

                RegisterRadarContact(hit.transform);
            }
        }
    }

    void RegisterRadarContact(Transform target)
    {
        if (contactHistory.ContainsKey(target))
        {
            var contact = contactHistory[target];
            if (!contact.seenThisSweep)
            {
                contact.lastSeenTime = Time.time;
                contact.seenThisSweep = true;
            }
        }
        else
        {
            contactHistory[target] = new RadarContact(target, Time.time);
            Debug.Log("Registered radar contact: " + target.name);
        }
    }

    void OnGUI()
    {
        DrawRadarScope();
        DrawTargetBoxes();
    }

    void DrawTargetBoxes()
    {
        foreach (var contact in contactHistory.Values)
        {
            if (contact.target == null) continue;
            Vector3 screenPos = mainCam.WorldToScreenPoint(contact.target.position);
            if (screenPos.z > 0)
            {
                float boxSize = 50f;
                Rect rect = new Rect(screenPos.x - boxSize / 2, Screen.height - screenPos.y - boxSize / 2, boxSize, boxSize);

                if (contact.target == lockedTarget)
                {
                    DrawThickRectOutline(rect);

                    Rigidbody radarRb = GetComponent<Rigidbody>();
                    Vector3 radarVel = radarRb != null ? radarRb.velocity : Vector3.zero;
                    Rigidbody targetRb = lockedTarget.GetComponent<Rigidbody>();
                    Vector3 targetVel = targetRb != null ? targetRb.velocity : Vector3.zero;

                    float distance = Vector3.Distance(transform.position, lockedTarget.position);
                    float closingSpeed = Vector3.Dot((lockedTarget.position - transform.position).normalized,
                        targetVel - radarVel);

                    string lockInfo = $"{distance:F1}m\n{(closingSpeed >= 0 ? "+" : "")}{closingSpeed:F1} m/s";
                    GUI.color = Color.green;
                    GUI.Label(new Rect(rect.xMax + 5, rect.yMin, 100, 40), lockInfo);
                }
                else
                {
                    DrawDashedRect(rect);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 origin = transform.position;

        if (lockedTarget != null)
        {
            Gizmos.color = Color.red;
            if (lockedTarget != null)
                Gizmos.DrawLine(origin, lockedTarget.position);
        }

        Gizmos.color = Color.yellow;
        foreach (var contact in contactHistory.Values)
        {
            if (contact.target == null || contact.target == lockedTarget) continue;
            Gizmos.DrawLine(origin, contact.target.position);
        }

        int raysPerSide = 4;
        float angleStepHor = sweepResolution / raysPerSide;
        float angleStepVert = verticalSliceHeight / raysPerSide;

        float baseHoriz = currentHorizontalAngle;
        float baseVert = -verticalSweepAngle / 2 + currentVerticalSlice * verticalSliceHeight;

        Gizmos.color = Color.green;

        for (int i = -raysPerSide; i <= raysPerSide; i++)
        {
            for (int j = 0; j <= raysPerSide; j++)
            {
                float horizOffset = baseHoriz + i * angleStepHor;
                float vertOffset = baseVert + j * angleStepVert;

                Quaternion yaw = Quaternion.AngleAxis(horizOffset, transform.up);
                Quaternion pitch = Quaternion.AngleAxis(-vertOffset, transform.right);
                Vector3 dir = yaw * pitch * transform.forward;

                if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionRange, obstructionMask))
                {
                    Gizmos.DrawRay(origin, dir.normalized * hit.distance);
                }
                else
                {
                    Gizmos.DrawRay(origin, dir.normalized * detectionRange);
                }
            }
        }

        Gizmos.color = new Color(0f, 1f, 0f, 0.05f);
        Gizmos.DrawWireSphere(origin, detectionRange);
    }

    void DrawDashedRect(Rect rect, int dashLength = 4, int gapLength = 4)
    {
        GUI.color = Color.green;
        for (float x = rect.xMin; x < rect.xMax; x += dashLength + gapLength)
        {
            GUI.DrawTexture(new Rect(x, rect.yMin, dashLength, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, rect.yMax - 2, dashLength, 2), Texture2D.whiteTexture);
        }
        for (float y = rect.yMin; y < rect.yMax; y += dashLength + gapLength)
        {
            GUI.DrawTexture(new Rect(rect.xMin, y, 2, dashLength), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2, y, 2, dashLength), Texture2D.whiteTexture);
        }
    }

    void DrawRadarScope()
    {
        float scopeWidth = 300f;
        float scopeHeight = 200f;
        float margin = 20f;
        float right = Screen.width - margin;
        float bottom = Screen.height - margin;

        Rect scopeRect = new Rect(right - scopeWidth, bottom - scopeHeight, scopeWidth, scopeHeight);
        GUI.color = new Color(0f, 1f, 0f, 0.2f);
        GUI.DrawTexture(scopeRect, Texture2D.whiteTexture);

        Vector2 playerPos = new Vector2(scopeRect.x + scopeWidth / 2, scopeRect.y + scopeHeight);

        int azimuthSegments = 60;
        for (int i = -azimuthSegments; i <= azimuthSegments; i++)
        {
            float az = (i / (float)azimuthSegments) * (horizontalSweepAngle / 2f);
            Quaternion yaw = Quaternion.AngleAxis(az, Vector3.up);
            Vector3 dir = yaw * transform.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange, obstructionMask))
            {
                float distance = hit.distance;
                float screenX = playerPos.x + (az / (horizontalSweepAngle / 2f)) * (scopeWidth / 2);
                float screenY = playerPos.y - (distance / detectionRange) * scopeHeight;

                GUI.color = new Color(1f, 0f, 0f, 0.3f);
                //GUI.DrawTexture(new Rect(screenX - 1, screenY, 2, scopeHeight - (screenY - scopeRect.y)), Texture2D.whiteTexture);
                //Different way of visualising:
                GUI.DrawTexture(new Rect(screenX - 1, scopeRect.y, 2, screenY - scopeRect.y), Texture2D.whiteTexture);
            }
        }

        foreach (var contact in contactHistory.Values)
        {
            if (contact.target == null) continue;
            Vector3 localPos = transform.InverseTransformPoint(contact.target.position);
            float azimuth = Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;
            float distance = localPos.magnitude;

            if (Mathf.Abs(azimuth) > horizontalSweepAngle / 2 || distance > detectionRange)
                continue;

            float x = playerPos.x + (azimuth / (horizontalSweepAngle / 2)) * (scopeWidth / 2);
            float y = playerPos.y - (distance / detectionRange) * scopeHeight;

            Rect blip = new Rect(x - 10, y, 20, 2);
            GUI.color = contact.target == selectedTarget ? Color.white : Color.green;
            GUI.DrawTexture(blip, Texture2D.whiteTexture);

            if (contact.target == selectedTarget)
            {
                GUI.DrawTexture(new Rect(blip.xMin, blip.yMin - 5, 2, 10), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(blip.xMax - 2, blip.yMin - 5, 2, 10), Texture2D.whiteTexture);
            }
        }

        // Draw sweep line (azimuth)
        float normalizedAz = (currentHorizontalAngle + horizontalSweepAngle / 2f) / horizontalSweepAngle;
        float sweepX = scopeRect.xMin + normalizedAz * scopeWidth;
        GUI.color = new Color(0f, 1f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(sweepX - 1, scopeRect.y, 2, scopeHeight), Texture2D.whiteTexture);

        // Labels
        string modeText = $"MODE: {currentMode}";
        string scanArea = currentMode == RadarMode.SRC ? $"{horizontalSweepAngle}x{verticalSweepAngle}" : $"{acmSweepSize}x{acmSweepSize}";
        string left = $"-{horizontalSweepAngle / 2}";
        string rightLabel = $"+{horizontalSweepAngle / 2}";
        string rangeText = $"{detectionRange / 1000f:F1}km";

        GUI.color = Color.green;
        GUI.Label(new Rect(scopeRect.xMin, scopeRect.yMin - 60, 200, 20), modeText);
        GUI.Label(new Rect(scopeRect.xMin, scopeRect.yMin - 40, 200, 20), scanArea);
        GUI.Label(new Rect(scopeRect.xMin, scopeRect.yMin - 20, 200, 20), rangeText);
        GUI.Label(new Rect(scopeRect.xMin, scopeRect.yMax, 50, 20), left);
        GUI.Label(new Rect(scopeRect.xMax - 50, scopeRect.yMax, 50, 20), rightLabel);
        GUI.Label(new Rect(scopeRect.xMin + scopeWidth / 2 - 10, scopeRect.yMax, 50, 20), "0");
    }


    void DrawThickRectOutline(Rect rect, int thickness = 3)
    {
        GUI.color = Color.green;
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
    }

    class RadarContact
    {
        public Transform target;
        public float lastSeenTime;
        public bool seenThisSweep;

        public RadarContact(Transform t, float time)
        {
            target = t;
            lastSeenTime = time;
            seenThisSweep = true;
        }
    }
}
