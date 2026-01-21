using UnityEngine;
using System.Collections.Generic;

public class ArmamentManager : MonoBehaviour
{
    [SerializeField] private Transform[] missileHardpoints;
    [SerializeField] private GameObject[] missileVisuals; // visual models at each hardpoint
    [SerializeField] private HomingMissile missilePrefab;
    [SerializeField] private AdvancedRadar radarReference;
    [SerializeField] private Rigidbody aircraftRb;

    private List<HomingMissile> activeMissiles = new List<HomingMissile>();
    private int currentHardpoint = 0;
    private int missilesRemaining;

    private bool seekerActive = false;
    private float seekerTimer = 0f;
    private float seekerWarmupTime = 1f;
    private bool readyToFire = false;
    private float seekerTimeout = 10f;

    [SerializeField] private Texture2D seekerCircleTexture;

    void Start()
    {
        missilesRemaining = missileHardpoints.Length;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!seekerActive)
            {
                TryArmSeeker();
            }
            else if (readyToFire)
            {
                FireMissile();
            }
        }

        if (seekerActive)
        {
            seekerTimer += Time.deltaTime;

            if (seekerTimer >= seekerWarmupTime)
                readyToFire = true;

            if (seekerTimer >= seekerTimeout)
            {
                seekerActive = false;
                readyToFire = false;
                seekerTimer = 0f;
            }
        }
    }

    void TryArmSeeker()
    {
        if (radarReference.GetLockedTarget() != null && missilesRemaining > 0)
        {
            seekerActive = true;
            seekerTimer = 0f;
        }
    }

    void FireMissile()
    {
        if (radarReference.GetLockedTarget() == null || missilesRemaining <= 0) return;

        Transform hardpoint = missileHardpoints[currentHardpoint];
        GameObject visualModel = missileVisuals[currentHardpoint];

        HomingMissile missile = Instantiate(missilePrefab, hardpoint.position, hardpoint.rotation);
        missile.Launch(radarReference.GetLockedTarget(), radarReference, aircraftRb.velocity);

        if (visualModel != null)
            visualModel.SetActive(false);

        activeMissiles.Add(missile);
        missilesRemaining--;

        seekerActive = false;
        readyToFire = false;
        seekerTimer = 0f;

        currentHardpoint = (currentHardpoint + 1) % missileHardpoints.Length;
    }

    public int GetMissilesRemaining() => missilesRemaining;

    void DrawHollowCircle(Rect rect, Color color, float thickness)
    {
        GUI.color = color;

        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
    }


    void OnGUI()
    {
        if (seekerActive)
        {
            Transform target = radarReference.GetLockedTarget();
            if (target != null && seekerCircleTexture != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
                if (screenPos.z > 0)
                {
                    float size = 50f;
                    Rect rect = new Rect(screenPos.x - size / 2, Screen.height - screenPos.y - size / 2, size, size);
                    GUI.color = readyToFire ? Color.red : Color.white;
                    GUI.DrawTexture(rect, seekerCircleTexture);
                }
            }
        }

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 60), GUI.skin.box);
        GUILayout.Label("Missiles Remaining:");
        GUILayout.Label($"AAM-4B [{missilesRemaining}/8]");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 200, 200, 190, 100), GUI.skin.box);
        GUILayout.Label("R     - Cycle Target");
        GUILayout.Label("T     - Cycle Scope");
        GUILayout.Label("RMB - Lock/Unlock Target");
        GUILayout.Label("Space - Turn on Seeker / Fire");
        GUILayout.EndArea();
    }
}
