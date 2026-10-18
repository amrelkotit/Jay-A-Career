using UnityEngine;
public class JayLightingController : MonoBehaviour
{
    [Min(30)] public float DayCycleSeconds=120;
    public Light sun, flashlight;
    public Light[] workspaceLights;
    [Header("Automatic evening road lighting")]
    public Light[] roadLights;
    public Renderer[] roadLampBulbs;
    public float roadLightIntensity=5;
    MaterialPropertyBlock roadLampProperties;
    [Header("Daylight enters through the authored window openings")]
    public Light[] windowDaylights;
    public ReflectionProbe laboratoryReflection;
    public bool workspaceLightsStartOn=false;
    [Header("Bedroom switch")]
    public Light[] bedroomLights;
    public bool bedroomLightsStartOn=true;
    public Transform bedroomSwitchRocker;
    public bool BedroomLightsOn {get;private set;}
    public bool FlashlightOn {get;private set;}
    public bool WorkspaceLightsOn {get;private set;}
    public float CycleTime {get;private set;}
    void Awake(){WorkspaceLightsOn=workspaceLightsStartOn;BedroomLightsOn=bedroomLightsStartOn;ApplyWorkspaceLights();ApplyBedroomLights();UpdateDaylight();}
    void Update(){CycleTime+=Time.deltaTime;UpdateDaylight();}
    void UpdateDaylight()
    {
        if(!sun)return;
        float daylight=.5f+.5f*Mathf.Cos(CycleTime/Mathf.Max(30,DayCycleSeconds)*Mathf.PI*2);
        // Begin at dusk and reach full output before the darkest part of the cycle.
        float evening=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.4f,daylight));
        foreach(var light in roadLights??System.Array.Empty<Light>())if(light){light.enabled=evening>.001f;light.intensity=roadLightIntensity*evening;}
        if(roadLampProperties==null)roadLampProperties=new MaterialPropertyBlock();
        foreach(var bulb in roadLampBulbs??System.Array.Empty<Renderer>())if(bulb)
        {
            bulb.GetPropertyBlock(roadLampProperties);
            roadLampProperties.SetColor("_EmissionColor",new Color(1,.69f,.32f)*4*evening);
            bulb.SetPropertyBlock(roadLampProperties);
        }
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
    void ApplyBedroomLights()
    {
        foreach(var light in bedroomLights??System.Array.Empty<Light>())if(light)light.enabled=BedroomLightsOn;
        if(bedroomSwitchRocker)bedroomSwitchRocker.localRotation=Quaternion.Euler(BedroomLightsOn?-15:15,0,0);
    }
    public bool ToggleBedroomLights(){BedroomLightsOn=!BedroomLightsOn;ApplyBedroomLights();return BedroomLightsOn;}
    public void BeginNextDay(){CycleTime=0;UpdateDaylight();}
}
