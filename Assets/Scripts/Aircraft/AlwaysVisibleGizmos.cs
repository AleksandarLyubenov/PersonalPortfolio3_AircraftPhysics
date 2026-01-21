using UnityEngine;

public class AlwaysVisibleGizmos : MonoBehaviour
{
    [SerializeField] private AircraftPhysics aircraftPhysics;

    private void OnDrawGizmos()
    {
        if (aircraftPhysics == null)
            aircraftPhysics = GetComponent<AircraftPhysics>();

        // This empty component ensures gizmos are drawn even when the object isn't selected
    }
}