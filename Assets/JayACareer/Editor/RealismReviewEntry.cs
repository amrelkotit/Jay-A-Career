using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Object=UnityEngine.Object;
public static class RealismReviewEntry
{
 const string Root="Assets/JayACareer/";
 static void FinishExterior(JayGameManager g)
 {
  var wire=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/modular_chainlink_fence_wire.mat");
  wire.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/Realism/modular_chainlink_fence_wire_diff_alpha_1k.png"));wire.SetTextureScale("_BaseMap",new Vector2(4,4));wire.SetFloat("_AlphaClip",1);wire.SetFloat("_Cutoff",.5f);wire.SetFloat("_Cull",0);wire.EnableKeyword("_ALPHATEST_ON");wire.renderQueue=2450;EditorUtility.SetDirty(wire);
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/Realism/modular_chainlink_fence.fbx").GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="modular_chainlink_fence");
  foreach(var fence in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="NorthSouthFence").ToArray())
  {
   var old=fence.Find("Visual");if(old)Object.DestroyImmediate(old.gameObject);var wrapper=new GameObject("Visual").transform;wrapper.SetParent(fence,false);
   for(int i=0;i<3;i++)
   {
    var panel=new GameObject("Panel_"+i);panel.transform.SetParent(wrapper,false);
    var part=Object.Instantiate(source.gameObject,panel.transform);part.name="Visual";part.transform.rotation=source.transform.rotation;part.transform.localScale=source.transform.lossyScale;
    var prefab=PrefabUtility.GetOutermostPrefabInstanceRoot(part);if(prefab)PrefabUtility.UnpackPrefabInstance(prefab,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    foreach(var r in part.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/"+m.name+".mat")??m).ToArray();
    Normalize(panel,new Vector3(2.32f,2.2f,.12f));panel.transform.localPosition=new Vector3((i-1)*2.34f,0,0);
   }
   Normalize(fence.gameObject,new Vector3(7,2.2f,.12f));
  }
  var pole=GameObject.Find("UtilityPole");
  if(!pole.transform.Find("CrossArm"))
  {
   var arm=GameObject.CreatePrimitive(PrimitiveType.Cube);arm.name="CrossArm";arm.transform.SetParent(pole.transform,false);arm.transform.localPosition=new Vector3(0,5.1f,0);arm.transform.localScale=new Vector3(1.8f,.1f,.13f);arm.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/DarkTrim.mat");Object.DestroyImmediate(arm.GetComponent<Collider>());
   for(int i=-1;i<=1;i++){var cap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cap.name="Insulator";cap.transform.SetParent(pole.transform,false);cap.transform.localPosition=new Vector3(i*.65f,5.27f,0);cap.transform.localScale=new Vector3(.1f,.12f,.1f);cap.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/LabelPaper.mat");Object.DestroyImmediate(cap.GetComponent<Collider>());}
  }
  if(!GameObject.Find("DesertBackdrop"))
  {
   var desert=GameObject.CreatePrimitive(PrimitiveType.Cube);desert.name="DesertBackdrop";desert.transform.position=new Vector3(0,-.29f,10.5f);desert.transform.localScale=new Vector3(240,.1f,240);Object.DestroyImmediate(desert.GetComponent<Collider>());
   string path=Root+"Materials/Realism/BackdropSand.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/sand_03.mat"));mat.SetTextureScale("_BaseMap",new Vector2(80,80));AssetDatabase.CreateAsset(mat,path);}desert.GetComponent<Renderer>().sharedMaterial=mat;desert.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
  // Conform the tamper strip to the scanned carton lid instead of floating over its crease.
  var package=g.deliveryPackage;bool wasActive=package.gameObject.activeSelf;package.gameObject.SetActive(true);var seal=package.transform.Find("TamperSeal");var filter=package.transform.Find("Visual").GetComponentInChildren<MeshFilter>(true);
  var probe=filter.gameObject.AddComponent<MeshCollider>();probe.sharedMesh=filter.sharedMesh;Physics.SyncTransforms();
  var vertices=new Vector3[18];var triangles=new int[48];
  for(int i=0;i<9;i++)for(int j=0;j<2;j++)
  {
   Vector3 local=new Vector3(j==0?-.03f:.03f,1,-.14f+i*.035f);var ray=new Ray(package.transform.TransformPoint(local),Vector3.down);
   vertices[i*2+j]=probe.Raycast(ray,out var hit,2)?package.transform.InverseTransformPoint(hit.point+Vector3.up*.0015f):new Vector3(local.x,.202f,local.z);
  }
  for(int i=0;i<8;i++){int a=i*2;int t=i*6;triangles[t]=a;triangles[t+1]=a+2;triangles[t+2]=a+1;triangles[t+3]=a+1;triangles[t+4]=a+2;triangles[t+5]=a+3;}
  Object.DestroyImmediate(probe);var strip=new Mesh();strip.vertices=vertices;strip.triangles=triangles;strip.RecalculateNormals();
  string meshPath=Root+"Models/Realism/DeliveryTamperSeal.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing){EditorUtility.CopySerialized(strip,existing);Object.DestroyImmediate(strip);strip=existing;EditorUtility.SetDirty(strip);}else AssetDatabase.CreateAsset(strip,meshPath);
  seal.localPosition=Vector3.zero;seal.localRotation=Quaternion.identity;seal.localScale=Vector3.one;seal.GetComponent<MeshFilter>().sharedMesh=strip;package.gameObject.SetActive(wasActive);
 }
 static void Normalize(GameObject root,Vector3 size,bool centered=false)
 {
  if(!root)return;var rotation=root.transform.rotation;root.transform.rotation=Quaternion.identity;
  var visual=root.transform.Find("Visual");if(!visual)return;
  Transform wrapper=visual;
  if(!visual.Find("MeshSource")){wrapper=new GameObject("VisualScale").transform;wrapper.SetParent(root.transform,false);visual.SetParent(wrapper,true);visual.name="MeshSource";wrapper.name="Visual";}
  wrapper.localScale=Vector3.one;var renderers=wrapper.GetComponentsInChildren<Renderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
  wrapper.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
  b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
  wrapper.position-=new Vector3(b.center.x-root.transform.position.x,b.min.y-root.transform.position.y,b.center.z-root.transform.position.z);
  if(centered)wrapper.localPosition-=Vector3.up*size.y*.5f;
  b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
  var collider=root.GetComponent<BoxCollider>();if(collider){collider.center=root.transform.InverseTransformPoint(b.center);collider.size=b.size;}
  root.transform.rotation=rotation;
 }
 static void Repack(Material mat,string source,bool packed)
 {
  var input=new Texture2D(2,2,TextureFormat.RGB24,false,true);input.LoadImage(File.ReadAllBytes(source));var pixels=input.GetPixels();
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(packed?pixels[i].b:mat.GetFloat("_Metallic"),0,0,1-(packed?pixels[i].g:pixels[i].r));
  var rgba=new Texture2D(input.width,input.height,TextureFormat.RGBA32,false,true);rgba.SetPixels(pixels);rgba.Apply();
  string path=Root+"Textures/Realism/"+mat.name+"_MetalSmooth.png";File.WriteAllBytes(path,rgba.EncodeToPNG());Object.DestroyImmediate(input);Object.DestroyImmediate(rgba);AssetDatabase.ImportAsset(path);
  var importer=AssetImporter.GetAtPath(path) as TextureImporter;importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.SaveAndReimport();
  mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(mat);
 }
 public static void Prepare()
 {
  EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");
  var prefabRoots=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(o=>o).Distinct().ToArray();
  foreach(var prefabRoot in prefabRoots)PrefabUtility.UnpackPrefabInstance(prefabRoot,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  var g=Object.FindFirstObjectByType<JayGameManager>();
  if(!g||!g.player||!g.hale||!g.customer||!g.deliveryPackage||!g.returnSlot||!g.counterSlot||!g.preparationSlot)throw new Exception("Missing saved scene references");
  foreach(var file in Directory.GetFiles(Root+"Models/Realism","*.materials.json"))foreach(var row in Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(file)))
  {
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/"+(string)row["name"]+".mat");if(!mat)continue;
   string diffuse=(string)row["textures"]["Base Color"];if(diffuse!=null){string path=Root+"Textures/Realism/"+diffuse;if(!File.Exists(path))path=Root+"Textures/Characters/"+diffuse;mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));}
   string arm=(string)row["textures"]["Roughness"];if(arm!=null&&arm.Contains("arm"))Repack(mat,Root+"Textures/Realism/"+arm,true);EditorUtility.SetDirty(mat);
  }
  foreach(var asset in new[]{"asphalt_03","concrete_floor","sand_03","rusty_painted_metal"})
  {var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/"+asset+".mat");Repack(mat,Directory.GetFiles(Root+"Textures/PolyHaven/"+asset,"*_rough_1k.*").First(),false);}
  foreach(var root in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>c.gameObject))
  {
   string n=root.name;Vector3? size=null;
   if(n.StartsWith("NorthWall")||n.StartsWith("WestWindow")||n.StartsWith("EastWall"))size=new Vector3(4,3.2f,.22f);
   else if(n.StartsWith("FrontWindow"))size=new Vector3(4.4f,3.2f,.22f);
   else if(n=="DoorPanel")size=new Vector3(2,2.1f,.09f);
   else if(n=="RoofPanel")size=new Vector3(4,.16f,4);
   else if(n=="StorageShelves")size=new Vector3(1.6f,2.1f,.65f);
   else if(n=="SealedDeliveryPackage")size=new Vector3(.5f,.4f,.36f);
   else if(n=="IndustrialDrum")size=new Vector3(.57f,.88f,.57f);
   else if(n=="CarryableEmptyDrum")size=new Vector3(.4f,.6f,.4f);
   else if(n=="CarryableCrate")size=new Vector3(.55f,.5f,.55f);
   else if(n=="SupplyCarton")size=new Vector3(.4f,.3f,.35f);
   else if(n=="WoodenPallet")size=new Vector3(1.2f,.15f,1);
   else if(n=="WarehouseCrate")size=new Vector3(.9f,.8f,.8f);
   else if(n=="ElectricalServiceBox")size=new Vector3(.8f,1.2f,.3f);
   else if(n=="LabAirConditioner")size=new Vector3(.85f,.6f,.4f);
   else if(n=="UtilityPole")size=new Vector3(.24f,6,.24f);
   else if(n.StartsWith("StreetLight"))size=new Vector3(.7f,4,.7f);
   else if(n=="NorthSouthFence")size=new Vector3(7,2.2f,.12f);
   if(size.HasValue)Normalize(root,size.Value,n=="SealedDeliveryPackage");
  }
  g.deliveryPackage.transform.Find("TamperSeal").localPosition=new Vector3(0,.203f,0);
  FinishExterior(g);
  if(g.customerVariantB){g.customerVariantB.CopyPropertiesFromMaterial(g.customerVariantA);g.customerVariantB.color=new Color(.55f,.65f,.85f);EditorUtility.SetDirty(g.customerVariantB);}
  var stain=GameObject.Find("VisibleWorkspaceStain");
  if(stain)
  {
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"Models/Realism/WorkspaceResidue.asset");
   if(!mesh){mesh=new Mesh();var vertices=new Vector3[19];var triangles=new int[54];for(int i=0;i<18;i++){float angle=i*Mathf.PI*2/18;float radius=.8f+.15f*Mathf.Sin(i*2.7f);vertices[i+1]=new Vector3(Mathf.Cos(angle)*.38f*radius,0,Mathf.Sin(angle)*.22f*radius);triangles[i*3]=0;triangles[i*3+1]=(i+1)%18+1;triangles[i*3+2]=i+1;}mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Root+"Models/Realism/WorkspaceResidue.asset");}
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/WorkspaceResidue.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.12f,.075f,.04f);mat.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(mat,Root+"Materials/Realism/WorkspaceResidue.mat");}
   foreach(Transform child in stain.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var v=new GameObject("Visual");v.transform.SetParent(stain.transform,false);v.AddComponent<MeshFilter>().sharedMesh=mesh;v.AddComponent<MeshRenderer>().sharedMaterial=mat;
  }
  var hale=g.hale.gameObject;var old=hale.transform.Find("Visual");
  var controller=old.GetComponentInChildren<Animator>().runtimeAnimatorController;
  Object.DestroyImmediate(old.gameObject);
  var visual=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/Realism/DrHale.fbx"),hale.transform);visual.name="Visual";visual.transform.localPosition=Vector3.zero;
  var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  visual.transform.position-=new Vector3(bounds.center.x-hale.transform.position.x,bounds.min.y-hale.transform.position.y,bounds.center.z-hale.transform.position.z);
  foreach(var r in renderers)r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/"+m.name+".mat")??m).ToArray();
  var animator=visual.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
  hale.GetComponent<JayHumanAnimation>().animator=animator;
  PrefabUtility.SaveAsPrefabAssetAndConnect(hale,Root+"Prefabs/Realism/DrHale.prefab",InteractionMode.AutomatedAction);
  if(!GameObject.Find("ReturnShelfSupport"))
  {
   var support=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Realism/InstrumentBench.prefab"));support.name="ReturnShelfSupport";
   var box=support.GetComponent<BoxCollider>();support.transform.localScale=new Vector3(1.2f/box.size.x,.77f/box.size.y,.6f/box.size.z);
   support.transform.SetPositionAndRotation(new Vector3(-2.85f,.08f,11.5f),Quaternion.Euler(0,90,0));support.transform.SetParent(hale.transform.parent,true);
  }
  GameObject.Find("CM Interaction Close Up").GetComponent<CinemachineFollow>().FollowOffset=new Vector3(.45f,-.1f,1.6f);
  var concrete=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/concrete_floor.mat");
  foreach(var row in new[]{new{name="DedicatedLabFloor",material="LabFloor_PBR",scale=new Vector2(4,4)},new{name="LabAccessPath",material="AccessConcrete",scale=new Vector2(10f/3,2f/3)},new{name="BedroomFloor",material="BedroomConcrete",scale=new Vector2(7f/3,2)},new{name="DeliveryApron",material="ApronConcrete",scale=new Vector2(4,4f/3)}})
  {
   string path=Root+"Materials/Realism/"+row.material+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(concrete);AssetDatabase.CreateAsset(mat,path);}mat.SetTextureScale("_BaseMap",row.scale);GameObject.Find(row.name).GetComponent<Renderer>().sharedMaterial=mat;EditorUtility.SetDirty(mat);
  }
  var roof=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/RoofMetal.mat");var paint=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Realism/rusty_painted_metal.mat");
  roof.CopyPropertiesFromMaterial(paint);roof.color=new Color(.42f,.48f,.47f);roof.SetTextureScale("_BaseMap",new Vector2(2,2));EditorUtility.SetDirty(roof);
  foreach(var light in g.lighting.workspaceLights.Concat(new[]{g.lighting.flashlight}))
  {
   if(light!=g.lighting.flashlight){light.transform.position=light.transform.parent.position+Vector3.down*.16f;light.transform.parent.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
   var data=light.GetUniversalAdditionalLightData();var so=new SerializedObject(data);so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
  }
  foreach(var camera in Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)){var lens=camera.Lens;lens.FieldOfView=65;lens.NearClipPlane=.08f;lens.FarClipPlane=110;camera.Lens=lens;var first=camera.GetComponent<CinemachineFollow>();if(first&&camera.name=="CM First Person"){var tracker=first.TrackerSettings;tracker.PositionDamping=Vector3.zero;first.TrackerSettings=tracker;}var follow=camera.GetComponent<CinemachineThirdPersonFollow>();if(follow){follow.AvoidObstacles.CollisionFilter=~0;follow.AvoidObstacles.CameraRadius=.2f;}}
  foreach(var file in Directory.GetFiles(Root+"Textures/Characters","*.png"))
  {var imp=AssetImporter.GetAtPath(file) as TextureImporter;if(imp&&imp.maxTextureSize!=2048){imp.maxTextureSize=2048;imp.SaveAndReimport();}}
  foreach(var driver in Object.FindObjectsByType<JayHumanAnimation>(FindObjectsSortMode.None))
  {driver.game=g;if(!driver.animator||!driver.animator.avatar||!driver.animator.avatar.isValid||!driver.animator.avatar.isHuman)throw new Exception("Invalid avatar: "+driver.name);}
  if(!g.confirmSound||!g.placementSound)throw new Exception("Missing feedback audio");
  var animation=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Root+"Prefabs/Realism/Humanoid.controller");var layers=animation.layers;layers[0].iKPass=true;animation.layers=layers;EditorUtility.SetDirty(animation);
  foreach(var name in new[]{"PreparationBench","CustomerCounter","CleaningSink","ReturnShelf","ReturnShelfSupport","BedroomBed","LabEntranceHinge","SealedDeliveryPackage","InstrumentBench","GlasswareBench","StorageShelves","DrHale","Customer","JayBody"}){var obj=GameObject.Find(name);if(obj)PrefabUtility.SaveAsPrefabAssetAndConnect(obj,Root+"Prefabs/Realism/"+name+".prefab",InteractionMode.AutomatedAction);}
  Directory.CreateDirectory("Deliverables/Evidence");
  File.WriteAllText("Deliverables/Evidence/saved-scene-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,humans=Object.FindObjectsByType<JayHumanAnimation>(FindObjectsSortMode.None).Length,cameras=Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).Length,materials=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count(),labFloorMaterial=GameObject.Find("DedicatedLabFloor").GetComponent<Renderer>().sharedMaterial.name,confirm=g.confirmSound.name,placement=g.placementSound.name},Newtonsoft.Json.Formatting.Indented));
  var inventory=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText("Deliverables/Evidence/scene-inventory.json"));var transforms=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  foreach(var row in inventory){var expected=new Vector3((float)row["x"],(float)row["y"],(float)row["z"]);var root=transforms.Where(t=>t.name==(string)row["name"]).OrderBy(t=>Vector3.Distance(t.position,expected)).FirstOrDefault();if(root&&root.TryGetComponent<BoxCollider>(out var box)){row["width"]=box.size.x;row["height"]=box.size.y;row["depth"]=box.size.z;}}
  File.WriteAllText("Deliverables/Evidence/scene-inventory.json",inventory.ToString());
  EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
 }
 public static void StartDemo(){EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");EditorApplication.isPlaying=true;}
 public static void StartValidation(){EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");Debug.Log("START INPUT VALIDATION "+String.Join(" ",Environment.GetCommandLineArgs()));EditorApplication.isPlaying=true;}
 public static void ValidateSaved(){EditorSceneManager.OpenScene(Root+"Scenes/JayACareer_Realism.unity");var g=Object.FindFirstObjectByType<JayGameManager>();if(!g||!g.hale||!g.customer||!g.player||!g.deliveryPackage)throw new Exception("Saved references missing");Debug.Log("Saved realism scene reopened successfully");}
}

