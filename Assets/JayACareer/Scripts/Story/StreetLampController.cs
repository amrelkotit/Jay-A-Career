using UnityEngine;

/// <summary>
/// ENVIRONMENT SCRIPT — automatic street lighting.
///
/// Reads how dark it is from <see cref="JayLightingController.Evening"/> (0 = daylight,
/// 1 = full night). Each lamp has its own switching threshold so the lamps along the road come
/// on one after another at dusk, each with a short electrical flicker, then fade to full
/// brightness. In the morning they switch off again as daylight returns. This is an
/// environmental-storytelling cue: in Level 2 the lamps come on in the middle of Luka's deal.
/// </summary>
public class StreetLampController : MonoBehaviour
{
    public JayLightingController lighting;
    public Light[] lamps;
    public Renderer[] bulbs;
    public float intensity = 5;
    public Color bulbColor = new Color(1, .69f, .32f);
    public float bulbEmission = 4;
    [Tooltip("Lamp i switches on when Evening rises above this threshold (+ a per-lamp offset).")]
    public float baseThreshold = .25f;
    public float thresholdSpread = .35f;
    public float flickerSeconds = .6f;

    float[] thresholds, level, flickerUntil;
    bool[] on;
    MaterialPropertyBlock block;

    void Start()
    {
        if (!lighting) lighting = FindFirstObjectByType<JayLightingController>();
        int n = Mathf.Max(lamps?.Length ?? 0, bulbs?.Length ?? 0);
        thresholds = new float[n]; level = new float[n]; flickerUntil = new float[n]; on = new bool[n];
        var rng = new System.Random(1234);   // deterministic order so the video always matches
        for (int i = 0; i < n; i++) thresholds[i] = baseThreshold + (float)rng.NextDouble() * thresholdSpread;
        if (lighting) lighting.externalRoadLamps = true;
        block = new MaterialPropertyBlock();
        // Start in the correct state without flickering when the level loads.
        float e = lighting ? lighting.Evening : 0;
        for (int i = 0; i < n; i++) { on[i] = e > thresholds[i]; level[i] = on[i] ? 1 : 0; }
        Apply();
    }

    void Update()
    {
        if (!lighting || thresholds == null) return;
        float e = lighting.Evening;
        for (int i = 0; i < thresholds.Length; i++)
        {
            // Small hysteresis so a lamp never chatters around its threshold.
            if (!on[i] && e > thresholds[i] + .02f) { on[i] = true; flickerUntil[i] = Time.time + flickerSeconds; }
            else if (on[i] && e < thresholds[i] - .02f) on[i] = false;
            float target = on[i] ? 1 : 0;
            level[i] = Mathf.MoveTowards(level[i], target, Time.deltaTime * (on[i] ? .8f : 1.5f));
        }
        Apply();
    }

    void Apply()
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            float v = level[i];
            if (on[i] && Time.time < flickerUntil[i]) v *= Mathf.PerlinNoise(Time.time * 25f, i * 3.1f) > .45f ? 1 : .05f;
            if (lamps != null && i < lamps.Length && lamps[i]) { lamps[i].enabled = v > .01f; lamps[i].intensity = intensity * v; }
            if (bulbs != null && i < bulbs.Length && bulbs[i])
            {
                bulbs[i].GetPropertyBlock(block);
                block.SetColor("_EmissionColor", bulbColor * bulbEmission * v);
                bulbs[i].SetPropertyBlock(block);
            }
        }
    }
}
