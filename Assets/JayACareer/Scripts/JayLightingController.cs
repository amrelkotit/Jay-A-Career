using UnityEngine;
public class JayLightingController : MonoBehaviour
{
    [Min(30)] public float DayCycleSeconds=120;
    public Light sun, flashlight;
    public Light[] workspaceLights;
    [Header("Daylight enters through the authored window openings")]
    public Light[] windowDaylights;
    public ReflectionProbe laboratoryReflection;
    public bool workspaceLightsStartOn=false;
    public bool FlashlightOn {get;private set;}
    public bool WorkspaceLightsOn {get;private set;}
    public float CycleTime {get;private set;}
    void Awake(){WorkspaceLightsOn=workspaceLightsStartOn;ApplyWorkspaceLights();UpdateDaylight();}
    void Update(){CycleTime+=Time.deltaTime;UpdateDaylight();}
    void UpdateDaylight()
    {
        if(!sun)return;
        float daylight=.5f+.5f*Mathf.Cos(CycleTime/Mathf.Max(30,DayCycleSeconds)*Mathf.PI*2);
        sun.intensity=Mathf.Lerp(.008f,1.5f,daylight);
        sun.color=Color.Lerp(new Color(.25f,.36f,.6f),new Color(1,.84f,.65f),daylight);
        sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(8,52,daylight),-30,0);
        // Trilight uses these colours directly; ambientIntensity alone did not darken the room.
        float sky=Mathf.Lerp(.003f,.075f,daylight);
        RenderSettings.ambientSkyColor=new Color(.38f,.48f,.58f)*sky;
        RenderSettings.ambientEquatorColor=new Color(.3f,.28f,.23f)*sky*.55f;
        RenderSettings.ambientGroundColor=new Color(.18f,.16f,.12f)*sky*.3f;
        RenderSettings.ambientIntensity=1;
        // URP can retain the scene's ambient SH probe even after Trilight colours change.
        // Refresh it explicitly so night does not retain the morning's indirect light.
        var ambient=new UnityEngine.Rendering.SphericalHarmonicsL2();
        ambient.AddAmbientLight((RenderSettings.ambientSkyColor+RenderSettings.ambientEquatorColor+RenderSettings.ambientGroundColor)/3);
        ambient.AddDirectionalLight(Vector3.up,RenderSettings.ambientSkyColor,.12f);
        RenderSettings.ambientProbe=ambient;
        if(RenderSettings.skybox&&RenderSettings.skybox.HasProperty("_Exposure"))RenderSettings.skybox.SetFloat("_Exposure",Mathf.Lerp(.035f,1f,daylight));
        foreach(var light in windowDaylights??System.Array.Empty<Light>())if(light){light.intensity=.45f*daylight*daylight;light.enabled=daylight>.015f;}
        if(laboratoryReflection)laboratoryReflection.intensity=WorkspaceLightsOn?.55f:Mathf.Lerp(.01f,.16f,daylight);
    }
    public void ToggleFlashlight(){FlashlightOn=!FlashlightOn;if(flashlight)flashlight.enabled=FlashlightOn;}
    void ApplyWorkspaceLights(){foreach(var light in workspaceLights??System.Array.Empty<Light>())if(light)light.enabled=WorkspaceLightsOn;}
    public bool ToggleWorkspaceLights(){WorkspaceLightsOn=!WorkspaceLightsOn;ApplyWorkspaceLights();UpdateDaylight();if(laboratoryReflection&&Application.isPlaying)laboratoryReflection.RenderProbe();return WorkspaceLightsOn;}
    public void BeginNextDay(){CycleTime=0;UpdateDaylight();}
}
