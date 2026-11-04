using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// Explicit authoring command. All lamps and references are saved in the scene.
public static class CustomerRoadUpgrade
{
    const string Root="Assets/JayACareer/";
    [MenuItem("Jay A Career/Build Customer and Evening Lights Update")]
    public static void ApplyAndBuild()
    {
        EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");
        var game=Object.FindFirstObjectByType<JayGameManager>();
        if(!game)throw new Exception("Missing game manager.");
        game.customerCounter=Object.FindObjectsByType<JayPackageDock>(FindObjectsSortMode.None)
            .Single(d=>d.destination==JayDailyLoop.PackageLocation.Counter);
        var lamps=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t=>t.name=="StreetLightNorth"||t.name=="StreetLightSouth").ToArray();
        if(lamps.Length!=2)throw new Exception("Expected the two existing roadside lamps.");
        string matPath=Root+"Materials/Realism/EveningLampBulb.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
        mat.SetColor("_BaseColor",new Color(.9f,.75f,.48f));mat.SetFloat("_Smoothness",.3f);
        mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(mat);
        var lights=new System.Collections.Generic.List<Light>();
        var bulbs=new System.Collections.Generic.List<Renderer>();
        foreach(var lamp in lamps)
        {
            var renderers=lamp.GetComponentsInChildren<Renderer>().Where(r=>r.name!="EveningBulb").ToArray();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            // The imported lantern glass was opaque and black. Illuminate its glass
            // material slots as well as the inner bulb; leave the metal frame intact.
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;bool glass=false;
                for(int i=0;i<materials.Length;i++)if(materials[i] &&
                    (materials[i].name.IndexOf("glass",StringComparison.OrdinalIgnoreCase)>=0 || materials[i].name.IndexOf("bulb",StringComparison.OrdinalIgnoreCase)>=0))
                {materials[i]=mat;glass=true;}
                if(glass){renderer.sharedMaterials=materials;bulbs.Add(renderer);}
            }
            Vector3 head=new Vector3(bounds.center.x,bounds.max.y-.9f,bounds.center.z);
            var bulb=lamp.Find("EveningBulb");
            if(!bulb)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="EveningBulb";
                Object.DestroyImmediate(go.GetComponent<Collider>());bulb=go.transform;bulb.SetParent(lamp,false);
            }
            bulb.position=head;bulb.localScale=new Vector3(.16f,.28f,.16f);
            var glow=bulb.GetComponent<Renderer>();glow.sharedMaterial=mat;glow.shadowCastingMode=ShadowCastingMode.Off;bulbs.Add(glow);
            var beam=lamp.Find("EveningRoadLight");
            if(!beam){beam=new GameObject("EveningRoadLight").transform;beam.SetParent(lamp,false);}
            // Place below the opaque lantern base so its own mesh cannot block the beam.
            beam.position=head+Vector3.down*.55f;beam.rotation=Quaternion.Euler(90,0,0);
            var light=beam.GetComponent<Light>();if(!light)light=beam.gameObject.AddComponent<Light>();
            light.type=LightType.Spot;light.spotAngle=125;light.innerSpotAngle=70;light.range=16;
            light.color=new Color(1,.73f,.4f);light.intensity=0;light.enabled=false;light.shadows=LightShadows.Soft;
            light.shadowBias=.03f;light.shadowNormalBias=.12f;light.shadowNearPlane=.1f;lights.Add(light);
        }
        game.lighting.roadLights=lights.ToArray();game.lighting.roadLampBulbs=bulbs.ToArray();game.lighting.roadLightIntensity=12;
        game.lighting.BeginNextDay();
        EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.lighting);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Deliverables/Evidence");
        File.WriteAllText("Deliverables/Evidence/customer-road-authoring-20261017.json",
            JsonUtility.ToJson(new Authoring{roadLamps=lights.Count,customerDock=game.customerCounter.name,savedScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path},true));
        ResponseFileFullBuild.BuildTo("Windows Customer Road 20261017","customer-road-build-20261017.json");
    }
    [Serializable] class Authoring {public int roadLamps;public string customerDock,savedScene;}
}
