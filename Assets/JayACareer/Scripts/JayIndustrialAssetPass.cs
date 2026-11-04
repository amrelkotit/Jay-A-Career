using UnityEngine;

/// <summary>
/// Replaces the prototype shell and selected blockout props with the staged ModKit meshes.
/// Existing gameplay colliders and interaction components stay on the original objects.
/// </summary>
public static class JayIndustrialAssetPass
{
    const string ResourceRoot = "JayACareer/ModKit/";
    const string RuntimeRootName = "JayModKitVisuals";

    public static void Apply()
    {
        if (GameObject.Find(RuntimeRootName) != null) return;
        var straightWall = Load("SM_Wall_Straight_4m");
        if (straightWall == null)
        {
            Debug.LogWarning("Jay A Career: ModKit FBX assets are not imported yet; keeping the existing blockout visuals.");
            return;
        }

        var root = new GameObject(RuntimeRootName).transform;
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ReplaceLabShell(root, straightWall);
        ReplaceProp("LabDoor", "SM_Door_Panel", new Vector3(1f, 1f, 1f), new Color(.16f, .20f, .23f), .72f, .32f);
        ReplaceProp("PrefabInstance_Barrel", "SM_Barrel", Vector3.one, new Color(.32f, .25f, .19f), .68f, .28f);
        ReplaceProp("Barrel", "SM_Barrel_Dented", Vector3.one, new Color(.34f, .27f, .20f), .68f, .28f);
        ReplaceProp("PrefabInstance_WoodenCrate", "SM_Crate_Large", Vector3.one, new Color(.48f, .34f, .20f), .08f, .24f);
        ReplaceProp("WoodenCrate", "SM_Crate_Small", Vector3.one, new Color(.48f, .34f, .20f), .08f, .24f);
        AddPipeAndVent(root);
        ApplySurface("Road", "asphalt_03_diff_1k", new Color(.18f, .19f, .20f), .05f, .24f, new Vector2(2f, 12f));
        ApplySurface("LabAccessRoad", "asphalt_03_diff_1k", new Color(.18f, .19f, .20f), .05f, .24f, new Vector2(2f, 3f));
        ApplySurface("MainFloor", "sand_03_diff_1k", new Color(.76f, .66f, .49f), 0f, .18f, new Vector2(12f, 16f));
    }

    static GameObject Load(string name) => Resources.Load<GameObject>(ResourceRoot + name);

    static void ReplaceLabShell(Transform parent, GameObject straightWall)
    {
        var shell = GameObject.Find("ProfessionalLabShell");
        if (shell != null)
            foreach (var renderer in shell.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

        var windowWall = Load("SM_Wall_Window_4m");
        var roof = Load("SM_Roof_Flat_4m");
        var concrete = Resources.Load<Texture2D>("JayACareer/PolyHaven/concrete_floor_diff_1k");
        var metalTexture = Resources.Load<Texture2D>("JayACareer/PolyHaven/rusty_painted_metal_diff_1k");
        Vector3 wallScale = new Vector3(1f, 1.33f, 1.25f);

        for (int i = 0; i < 3; i++)
        {
            float x = -11f + i * 4f;
            Place(straightWall, parent, "ModKit_BackWall_" + i, new Vector3(x, 2f, 17.05f), Quaternion.identity, wallScale, new Color(.47f, .49f, .50f), .12f, .27f, concrete);
            Place(straightWall, parent, "ModKit_LeftWall_" + i, new Vector3(-13f, 2f, 7f + i * 4f), Quaternion.Euler(0, 90f, 0), wallScale, new Color(.47f, .49f, .50f), .12f, .27f, concrete);
            Place(straightWall, parent, "ModKit_RightWall_" + i, new Vector3(-1f, 2f, 7f + i * 4f), Quaternion.Euler(0, 90f, 0), wallScale, new Color(.47f, .49f, .50f), .12f, .27f, concrete);

            if (i != 1)
            {
                float frontX = i == 0 ? -10.8f : -3.2f;
                GameObject frontAsset = windowWall != null ? windowWall : straightWall;
                Place(frontAsset, parent, "ModKit_FrontWall_" + i, new Vector3(frontX, 2f, 5.08f), Quaternion.identity,
                    new Vector3(1.1f, wallScale.y, wallScale.z), new Color(.47f, .49f, .50f), .12f, .27f, concrete);
            }
            for (int j = 0; j < 3; j++)
                if (roof != null)
                    Place(roof, parent, "ModKit_Roof_" + i + "_" + j, new Vector3(x, 4.12f, 7f + j * 4f), Quaternion.identity, Vector3.one, new Color(.13f, .16f, .19f), .74f, .36f, metalTexture);
        }

        var glass = MakeGlassMaterial();
        if (windowWall != null && glass != null)
        {
            MakeWindow(parent, "Lab_Glass_Left", new Vector3(-10.8f, 2.55f, 5.0f), glass);
            MakeWindow(parent, "Lab_Glass_Right", new Vector3(-3.2f, 2.55f, 5.0f), glass);
        }
    }

    static void ReplaceProp(string objectName, string assetName, Vector3 scale, Color tint, float metallic, float smoothness)
    {
        var original = GameObject.Find(objectName);
        var asset = Load(assetName);
        if (original == null || asset == null || original.transform.Find("ModKitVisual") != null) return;
        var door = original.GetComponent<DoorInteractable>();
        if (door != null) door.HideVisual = true;
        foreach (var renderer in original.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        var visual = Object.Instantiate(asset, original.transform);
        visual.name = "ModKitVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = scale;
        Texture texture = assetName.Contains("Barrel")
            ? Resources.Load<Texture2D>("JayACareer/PolyHaven/rusty_painted_metal_diff_1k")
            : null;
        Tint(visual, tint, metallic, smoothness, texture, Vector2.one);
    }

    static void AddPipeAndVent(Transform parent)
    {
        var pipe = Load("SM_Pipe_2m");
        var vent = Load("SM_Vent_Wall");
        if (pipe != null)
        {
            for (int i = 0; i < 3; i++)
                Place(pipe, parent, "ModKit_ServicePipe_" + i, new Vector3(-12.72f, 3.25f, 9f + i * 2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, new Color(.35f, .31f, .27f), .68f, .31f,
                    Resources.Load<Texture2D>("JayACareer/PolyHaven/rusty_painted_metal_diff_1k"));
        }
        if (vent != null)
            Place(vent, parent, "ModKit_ExteriorVent", new Vector3(-1.12f, 2.65f, 14.3f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, new Color(.25f, .29f, .32f), .76f, .34f);
        var pallet = Load("SM_Pallet");
        var crate = Load("SM_Crate_Small");
        if (pallet != null)
            Place(pallet, parent, "ModKit_ServicePallet", new Vector3(-10.2f, .12f, 15.1f), Quaternion.identity, Vector3.one, new Color(.38f, .28f, .19f), .05f, .24f);
        if (crate != null)
            Place(crate, parent, "ModKit_ServiceCrate", new Vector3(-10.2f, .82f, 15.1f), Quaternion.identity, Vector3.one, new Color(.48f, .34f, .20f), .05f, .24f);
    }

    static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Color tint, float metallic, float smoothness, Texture texture = null)
    {
        if (prefab == null) return null;
        var instance = Object.Instantiate(prefab, parent);
        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = scale;
        Tint(instance, tint, metallic, smoothness, texture, Vector2.one);
        return instance;
    }

    static void ApplySurface(string objectName, string textureName, Color tint, float metallic, float smoothness, Vector2 tiling)
    {
        var target = GameObject.Find(objectName);
        if (target == null) return;
        var texture = Resources.Load<Texture2D>("JayACareer/PolyHaven/" + textureName);
        Tint(target, tint, metallic, smoothness, texture, tiling);
    }

    static void Tint(GameObject target, Color color, float metallic, float smoothness, Texture texture, Vector2 tiling)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;
        foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            var material = new Material(shader) { color = color, name = target.name + "_PBR" };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                material.mainTextureScale = tiling;
            }
            renderer.sharedMaterial = material;
        }
    }

    static Material MakeGlassMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        var material = new Material(shader) { name = "JayLab_WindowGlass_Runtime", color = new Color(.23f, .48f, .56f, .34f) };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Metallic", .18f);
        material.SetFloat("_Smoothness", .82f);
        material.SetFloat("_AlphaClip", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    static void MakeWindow(Transform parent, string name, Vector3 position, Material material)
    {
        var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = name;
        glass.transform.SetParent(parent, true);
        glass.transform.position = position;
        glass.transform.localScale = new Vector3(1.6f, .9f, .035f);
        var collider = glass.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        var renderer = glass.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }
}
