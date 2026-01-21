using UnityEngine;

public class LandingGearController : MonoBehaviour
{
    [Header("Gear Visuals")]
    [SerializeField] private GameObject gearOpenModel;
    [SerializeField] private GameObject gearClosedModel;

    [Header("Gear Colliders")]
    [SerializeField] private GameObject gearCollision;

    private bool gearDown = true;

    void Start()
    {
        SetGearState(gearDown);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            gearDown = !gearDown;
            SetGearState(gearDown);
        }
    }

    private void SetGearState(bool isDown)
    {
        if (gearOpenModel != null)
            gearOpenModel.SetActive(isDown);
            gearCollision.SetActive(isDown);

        if (gearClosedModel != null)
            gearClosedModel.SetActive(!isDown);
    }

    public bool IsGearDown => gearDown;
}
