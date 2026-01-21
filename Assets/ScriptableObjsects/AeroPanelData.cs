using UnityEngine;

[CreateAssetMenu(menuName = "Aircraft/Aero Panel Data")]
public class AeroPanelData : ScriptableObject
{
    public float span = 1.0f;
    public float chord = 1.0f;
    public float liftCoefficient = 1.2f;
    public float dragCoefficient = 0.02f;

    public float stallAngleHigh = 15f;
    public float stallAngleLow = -15f;

    public ControlType controlType = ControlType.None;
    public float maxDeflection = 30f;

    public enum ControlType { None, Pitch, Roll, Yaw, Flap }

    public float Area => span * chord;
}
