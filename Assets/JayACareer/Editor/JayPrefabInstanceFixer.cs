using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class JayPrefabInstanceFixer
{
    const string ScenePath="Assets/JayACareer/Scenes/JayACareer_Main.unity";
    const string Marker="Assets/JayACareer/Prefabs/.instances_verified";
    static readonly string[] Names={"LabTable","Chair","StorageShelf","Barrel","WoodenCrate","StorageContainer","LaboratoryEquipment","Door","StreetLight","CleaningStation"};
    static JayPrefabInstanceFixer(){ EditorApplication.delayCall += AddInstancesOnce; }
    [MenuItem("Jay A Career/Place Prefab Instances")]
    public static void PlacePrefabInstances(){ AddInstancesOnce(); }
    static void AddInstancesOnce()
    {
        if (EditorApplication.isCompiling || !File.Exists(ScenePath) || File.Exists(Marker)) return;
        Scene scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        GameObject root=GameObject.Find("PrefabInstances_Used"); if(root==null) root=new GameObject("PrefabInstances_Used");
        for(int i=0;i<Names.Length;i++)
        {
            string assetPath="Assets/JayACareer/Prefabs/PF_"+Names[i]+".prefab";
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath); if(prefab==null) continue;
            GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene); instance.name="PrefabInstance_"+Names[i]; instance.transform.SetParent(root.transform); instance.transform.position=new Vector3(-18+i*4,.6f,27); instance.transform.localScale=Vector3.one*.65f;
        }
        EditorSceneManager.SaveScene(scene,ScenePath); File.WriteAllText(Marker,"Prefab instances verified and placed additively.\n"); AssetDatabase.Refresh();
    }
}
