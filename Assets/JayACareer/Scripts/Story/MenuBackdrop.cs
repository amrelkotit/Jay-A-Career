using UnityEngine;

/// <summary>
/// PRESENTATION SCRIPT — the living backdrop behind the main menu: the real game world at
/// dusk (street lamps on) seen through a camera that drifts slowly between two viewpoints.
/// It sets the tone (aesthetics: "the world before the story starts") without any gameplay.
/// </summary>
public class MenuBackdrop : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 fromPosition = new Vector3(-1, 3.2f, -2), toPosition = new Vector3(3, 2.6f, 1);
    public Vector3 lookAt = new Vector3(-4, 1.4f, 10);
    public float secondsPerPass = 40;
    public JayLightingController lighting;
    [Range(0, 1)] public float cycleFraction = .42f;

    void Start()
    {
        if (lighting) { lighting.autoAdvance = false; lighting.SetCycleFraction(cycleFraction); }
    }

    void Update()
    {
        if (!cameraTransform) return;
        float t = Mathf.SmoothStep(0, 1, Mathf.PingPong(Time.time / secondsPerPass, 1));
        cameraTransform.position = Vector3.Lerp(fromPosition, toPosition, t);
        cameraTransform.rotation = Quaternion.LookRotation(lookAt - cameraTransform.position);
    }
}
