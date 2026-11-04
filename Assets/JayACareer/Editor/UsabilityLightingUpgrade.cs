using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Targeted authoring pass: never runs during normal opening or gameplay.
public static class UsabilityLightingUpgrade
{
 const string Root="Assets/JayACareer/";
 static Transform Find(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
 static Material NoteMaterial(string name)
 {
  string path=Root+"Textures/ObjectNotes/"+name+".png";
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  string target=Root+"Materials/ObjectNotes/"+name+".mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(target);
  if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,target);}
  mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.04f);mat.SetFloat("_Metallic",0);mat.SetFloat("_Cull",0);mat.SetFloat("_SpecularHighlights",0);mat.DisableKeyword("_EMISSION");EditorUtility.SetDirty(mat);return mat;
 }
 static void Paper(Transform parent,string name,Vector3 world,Quaternion rotation,Vector2 size,Material mat)
 {
  var old=parent.Find(name);var go=old?old.gameObject:new GameObject(name);if(!old)go.transform.SetParent(parent,false);
  string meshPath=Root+"Materials/ObjectNotes/StickyPaper.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
  if(!mesh){mesh=new Mesh();mesh.name="StickyPaper";mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,meshPath);}
  var filter=go.GetComponent<MeshFilter>();if(!filter)filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
  go.transform.SetPositionAndRotation(world,rotation);Vector3 scale=parent.lossyScale;go.transform.localScale=new Vector3(size.x/Mathf.Abs(scale.x),size.y/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
  var renderer=go.GetComponent<MeshRenderer>();if(!renderer)renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
 }
 static void Label(Transform root,bool liftable)
 {
  var mat=NoteMaterial(root.name);var box=root.GetComponent<BoxCollider>();if(!box)throw new Exception("Missing label surface: "+root.name);
  Vector3 center=box.center,half=box.size*.5f;
  float width=liftable?Mathf.Min(.27f,box.bounds.size.x*.72f):.52f;
  // Inactive package bounds are empty; its centred collider is exactly 0.5 x 0.4 x 0.36 m.
  if(liftable&&width<.1f)width=.27f;
  float height=width*576/1024;
  foreach(int face in new[]{-1,1})
  {
   float noteY=liftable?center.y:center.y+half.y-height*.48f/Mathf.Abs(root.lossyScale.y);
   if(root.name=="BedroomBed")noteY=center.y-half.y+.27f/Mathf.Abs(root.lossyScale.y);
   Vector3 p=new Vector3(center.x,noteY,center.z+face*(half.z+.003f/Mathf.Max(.001f,Mathf.Abs(root.lossyScale.z))));
   Paper(root,"YellowNote_"+face,root.TransformPoint(p),root.rotation*Quaternion.Euler(0,face<0?0:180,0),new Vector2(width,height),mat);
  }
  if(liftable)
  {
   // A second side is visible when the carton sits on the east storage shelf.
   Vector3 p=center+new Vector3(-half.x-.004f/Mathf.Max(.001f,Mathf.Abs(root.lossyScale.x)),0,0);
   Paper(root,"YellowNote_Side",root.TransformPoint(p),root.rotation*Quaternion.Euler(0,90,0),new Vector2(width,height),mat);
  }
 }
 static GameObject Piece(Transform parent,string name,Vector3 position,Vector3 size,Material material)
 {
  var existing=parent.Find(name);if(existing)return existing.gameObject;
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.TwoSided;return go;
 }
 static Light Window(Transform wall,Material architecture,Material glass)
 {
  var box=wall.GetComponent<BoxCollider>();float w=box.size.x,h=box.size.y,d=box.size.z;Vector3 c=box.center;
  var old=wall.Find("Visual");if(old)old.gameObject.SetActive(false); // Retain the original imported module for recovery.
  var visual=wall.Find("DaylightWindowVisual");if(!visual){visual=new GameObject("DaylightWindowVisual").transform;visual.SetParent(wall,false);}
  float sill=1.15f,top=2.35f,opening=w*.66f,jamb=(w-opening)*.5f;
  Piece(visual,"SillWall",new Vector3(c.x,sill*.5f,c.z),new Vector3(w,sill,d),architecture);
  Piece(visual,"HeaderWall",new Vector3(c.x,(top+h)*.5f,c.z),new Vector3(w,h-top,d),architecture);
  Piece(visual,"LeftJamb",new Vector3(c.x-(w-jamb)*.5f,(sill+top)*.5f,c.z),new Vector3(jamb,top-sill,d),architecture);
  Piece(visual,"RightJamb",new Vector3(c.x+(w-jamb)*.5f,(sill+top)*.5f,c.z),new Vector3(jamb,top-sill,d),architecture);
  var trim=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/DarkTrim.mat");
  foreach(int side in new[]{-1,1})Piece(visual,"WindowFrameSide"+side,new Vector3(c.x+side*opening*.5f,(sill+top)*.5f,c.z),new Vector3(.045f,top-sill,.25f),trim);
  Piece(visual,"WindowFrameTop",new Vector3(c.x,top,c.z),new Vector3(opening,.045f,.25f),trim);Piece(visual,"WindowFrameBottom",new Vector3(c.x,sill,c.z),new Vector3(opening,.045f,.25f),trim);
  var pane=Piece(visual,"ClearGlass",new Vector3(c.x,(sill+top)*.5f,c.z),new Vector3(opening,top-sill,.018f),glass);pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
  bool west=wall.name.StartsWith("West");Vector3 inward=west?Vector3.right:Vector3.forward;
  var transform=wall.Find("WindowDaylight");if(!transform){transform=new GameObject("WindowDaylight").transform;transform.SetParent(wall,false);}
  transform.position=wall.TransformPoint(new Vector3(c.x,(sill+top)*.5f,c.z))-inward*.19f;transform.rotation=Quaternion.LookRotation(inward,Vector3.up);
  var light=transform.GetComponent<Light>();if(!light)light=transform.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.range=6;light.spotAngle=110;light.innerSpotAngle=65;light.color=new Color(.68f,.8f,1);light.intensity=.45f;light.shadowNearPlane=.04f;light.shadows=LightShadows.Soft;light.shadowBias=.035f;light.shadowNormalBias=.08f;
  var data=light.GetUniversalAdditionalLightData();var serialized=new SerializedObject(data);serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;serialized.ApplyModifiedPropertiesWithoutUndo();return light;
 }
 public static void ApplyAndBuild(){Apply();ResponseFileFullBuild.BuildTo("Windows Labeled 20261015","labeled-windows-build-20261015.json");}
 public static void ReviewOnly(){EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");CaptureReview(Object.FindFirstObjectByType<JayGameManager>());}
 public static void Apply()
 {
  EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");
  Directory.CreateDirectory(Root+"Materials/ObjectNotes");AssetDatabase.Refresh();
  var game=Object.FindFirstObjectByType<JayGameManager>();if(!game)throw new Exception("Game manager missing");
  var roots=new[]{"PreparationBench","InstrumentBench","GlasswareBench","CleaningSink","ReturnShelf","CustomerCounter","BedroomDesk","BedroomBed","BedroomChair"};
  foreach(string name in roots){var root=Find(name);if(!root)throw new Exception("Missing station: "+name);Label(root,false);}
  var liftables=Object.FindObjectsByType<LiftableObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);foreach(var item in liftables)Label(item.transform,true);
  var switchRoot=Find("LabLightSwitch");Paper(switchRoot,"YellowNote_Switch",switchRoot.position+new Vector3(0,.28f,.045f),Quaternion.Euler(0,180,0),new Vector2(.4f,.225f),NoteMaterial("LabLightSwitch"));
  var architecture=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/Architecture.mat");
  string glassPath=Root+"Materials/ObjectNotes/ClearWindowGlass.mat";var glass=AssetDatabase.LoadAssetAtPath<Material>(glassPath);
  if(!glass){glass=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(glass,glassPath);}
  glass.SetColor("_BaseColor",new Color(.72f,.86f,.95f,.065f));glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.SetFloat("_Smoothness",.4f);glass.SetFloat("_SpecularHighlights",0);glass.SetFloat("_EnvironmentReflections",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;EditorUtility.SetDirty(glass);
  var windows=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("WestWindowModule")||t.name.StartsWith("FrontWindow")).ToArray();
  game.lighting.windowDaylights=windows.Select(t=>Window(t,architecture,glass)).ToArray();game.lighting.workspaceLightsStartOn=false;
  foreach(var light in game.lighting.workspaceLights)light.enabled=false;
  foreach(var renderer in Find("Laboratory").GetComponentsInChildren<Renderer>(true))if(renderer.sharedMaterial&&renderer.sharedMaterial.renderQueue<3000&& !renderer.name.StartsWith("YellowNote"))renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
  game.lighting.laboratoryReflection=Object.FindFirstObjectByType<ReflectionProbe>();
  string skyPath=Root+"Materials/ObjectNotes/DayNightSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
  if(!sky){sky=new Material(RenderSettings.skybox?RenderSettings.skybox:new Material(Shader.Find("Skybox/Procedural")));AssetDatabase.CreateAsset(sky,skyPath);}RenderSettings.skybox=sky;
  RenderSettings.ambientMode=AmbientMode.Trilight;game.lighting.BeginNextDay();EditorUtility.SetDirty(sky);
  // Save the labeled interaction prefabs using stable roots; gameplay references stay intact.
  foreach(string name in roots.Concat(liftables.Select(i=>i.name)))
  {var root=Find(name);string path=Root+"Prefabs/Realism/"+name+".prefab";PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,path,InteractionMode.AutomatedAction);}
  EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Deliverables/Evidence");File.WriteAllText("Deliverables/Evidence/labels-lighting-authoring.json",JsonUtility.ToJson(new Result{labeledLiftables=liftables.Length,labeledStations=roots.Length+1,daylightWindows=windows.Length,labLightsStartOff=true},true));
  CaptureReview(game);Debug.Log("LABELS AND LIGHTING SAVED: "+liftables.Length+" liftables; "+windows.Length+" transparent daylight windows.");
 }
 [Serializable] class Result{public int labeledLiftables,labeledStations,daylightWindows;public bool labLightsStartOff;}
 static void CaptureReview(JayGameManager g)
 {
  if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){Debug.Log("Visual review requires a graphics-enabled player; headless authoring screenshots skipped.");return;}
  string dir="Deliverables/Evidence/LabelsLighting";Directory.CreateDirectory(dir);var cam=g.playerCamera;var oldPosition=cam.transform.position;var oldRotation=cam.transform.rotation;var oldTarget=cam.targetTexture;bool active=g.deliveryPackage.gameObject.activeSelf;
  var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  void Shot(string name,Vector3 position,Vector3 target){cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(dir+"/"+name+".png",image.EncodeToPNG());RenderTexture.active=null;}
  try
  {
   g.deliveryPackage.gameObject.SetActive(true);g.lighting.BeginNextDay();
   Shot("labels-preparation-package",new Vector3(-10.5f,1.5f,13.1f),new Vector3(-10.5f,1.0f,14.8f));
   Shot("daylight-lights-off",new Vector3(-7,1.7f,7.3f),new Vector3(-7,1.3f,14));
   typeof(JayLightingController).GetField("<CycleTime>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g.lighting,60f);g.lighting.SendMessage("UpdateDaylight");
   Shot("night-lights-off",new Vector3(-7,1.7f,7.3f),new Vector3(-7,1.3f,14));
   g.lighting.ToggleFlashlight();Shot("night-flashlight",new Vector3(-7,1.7f,7.3f),new Vector3(-7,1.3f,14));g.lighting.ToggleFlashlight();
   g.lighting.ToggleWorkspaceLights();Shot("night-lab-switch-on",new Vector3(-7,1.7f,7.3f),new Vector3(-7,1.3f,14));g.lighting.ToggleWorkspaceLights();
  }
  finally{g.deliveryPackage.gameObject.SetActive(active);g.lighting.BeginNextDay();cam.targetTexture=oldTarget;cam.transform.SetPositionAndRotation(oldPosition,oldRotation);Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
 }
}
