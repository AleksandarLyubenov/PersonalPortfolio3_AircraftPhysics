using UnityEngine;

[CreateAssetMenu(fileName = "AircraftPhysicsDisplaySettings", menuName = "Aircraft/Physics Display Settings")]
public class AircraftPhysicsDisplaySettings : ScriptableObject
{
    private static AircraftPhysicsDisplaySettings _instance;
    public static AircraftPhysicsDisplaySettings Instance
    {
        get
        {
#if UNITY_EDITOR
            if (_instance == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AircraftPhysicsDisplaySettings");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AircraftPhysicsDisplaySettings>(path);
                }
                else
                {
                    Debug.LogWarning("No AircraftPhysicsDisplaySettings asset found. Using default in-memory settings.");
                    _instance = CreateInstance<AircraftPhysicsDisplaySettings>();
                }
            }
#endif
            return _instance;
        }
    }

    [Header("Global Scaling")]
    public bool scaleForcesByWeight = true;
    public float lengthScale = 1f;
    public float widthScale = 1f;

    [Header("Center of Mass")]
    public bool showCenterOfMass = true;
    public Color comColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("Aerodynamic Center")]
    public bool showAerodynamicCenter = true;
    public Color adcColor = new Color(0.3f, 0.7f, 1f, 1f);
    public float displayAngleOfAttack = 5f;
    public float displayAirspeed = 100f;
    public float displayAirDensity = 1.225f;

    [Header("Surface Panels")]
    public bool showSurfaces = true;
    public Color wingColor = new Color(0.2f, 0.2f, 1f, 0.25f);
    public Color flapColor = new Color(1f, 0.6f, 0.2f, 0.4f);
    public Color wingAtStallColor = new Color(0.8f, 0.1f, 0.1f, 0.4f);
    public Color flapAtStallColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("Surface Forces")]
    public bool showForces = true;
    public bool showTorque = true;
    public Color liftColor = new Color(0.3f, 0.7f, 1f, 0.95f);
    public Color dragColor = new Color(0.9f, 0.1f, 0.2f, 0.9f);
    public Color torqColor = new Color(0.2f, 0.8f, 0.1f, 0.7f);
}
