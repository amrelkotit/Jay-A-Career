using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Characters for Assignment 3:
///  • imports three free Microsoft Rocketbox humans (MIT) as Humanoid rigs with URP materials
///  • swaps the single A2 customer for a different person in each level
///    (Leo = Male_Adult_18, Luka = Male_Adult_10, Paul = Business_Male_04), reusing the
///    existing Humanoid animator controller (idle / talk / hand-off) through Mecanim retargeting
///  • renders a head-and-shoulders portrait of every speaking character for the dialogue box
/// </summary>
public static class Assignment3Characters
{
    const string Root = "Assets/JayACareer/ThirdParty/Rocketbox/";
    const string Portraits = "Assets/JayACareer/Story/Portraits/";

    class Buyer { public string folder, code; public float height; public Buyer(string f, string c, float h) { folder = f; code = c; height = h; } }
    static readonly System.Collections.Generic.Dictionary<string, Buyer> Buyers = new System.Collections.Generic.Dictionary<string, Buyer>
    {
        { "Leo", new Buyer("Male_Adult_18", "m023", 1.74f) },
        { "Luka", new Buyer("Male_Adult_10", "m024", 1.80f) },
        { "Paul", new Buyer("Business_Male_04", "m015", 1.79f) },
    };

    static string Fbx(Buyer b) => Root + b.folder + "/" + b.folder + ".fbx";

    // ------------------------------------------------------------------ import
    public static void PrepareImports()
    {
        foreach (var b in Buyers.Values)
        {
            string tex = Root + b.folder + "/Textures/";
            foreach (var path in Directory.GetFiles(tex, "*.png"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                if (ti == null) continue;
                bool normal = path.Contains("_normal"), opacity = path.Contains("_opacity");
                var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (ti.textureType != type || ti.maxTextureSize != 1024 || ti.alphaIsTransparency != opacity)
                { ti.textureType = type; ti.maxTextureSize = 1024; ti.alphaIsTransparency = opacity; ti.SaveAndReimport(); }
            }
            string matDir = Root + b.folder + "/Materials/";
            Directory.CreateDirectory(matDir);
            AssetDatabase.Refresh();
            var body = Mat(matDir + b.code + "_body.mat", tex + b.code + "_body_color.png", tex + b.code + "_body_normal.png", false);
            var head = Mat(matDir + b.code + "_head.mat", tex + b.code + "_head_color.png", tex + b.code + "_head_normal.png", false);
            Material opacityMat = File.Exists(tex + b.code + "_opacity_color.png") ? Mat(matDir + b.code + "_opacity.mat", tex + b.code + "_opacity_color.png", null, true) : null;

            var mi = (ModelImporter)AssetImporter.GetAtPath(Fbx(b));
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importAnimation = false;
            mi.importBlendShapes = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), b.code + "_body"), body);
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), b.code + "_head"), head);
            if (opacityMat) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), b.code + "_opacity"), opacityMat);
            mi.SaveAndReimport();
        }
    }

    static Material Mat(string path, string color, string normal, bool cutout)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(color));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", .28f);
        m.SetFloat("_Metallic", 0);
        if (normal != null)
        {
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.SetFloat("_BumpScale", 1); m.EnableKeyword("_NORMALMAP");
        }
        if (cutout)
        {
            // Hair cards / eyelashes: alpha-tested and double-sided.
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .45f); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0); m.renderQueue = (int)RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ swap the level's buyer
    public static void SwapBuyer(JayGameManager game, string who)
    {
        var buyer = Buyers[who];
        var customer = game.customer;
        var root = customer.transform;
        var oldVisual = root.Find("Visual");
        var oldAnimator = oldVisual ? oldVisual.GetComponent<Animator>() : root.GetComponentInChildren<Animator>(true);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx(buyer));
        var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx(buyer)).OfType<Avatar>().FirstOrDefault();
        if (!model || !avatar) { Debug.LogError("Rocketbox model or avatar missing for " + who); return; }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
        inst.name = "Visual_" + who;
        inst.transform.localPosition = oldVisual ? oldVisual.localPosition : Vector3.zero;
        inst.transform.localRotation = oldVisual ? oldVisual.localRotation : Quaternion.identity;

        // Normalise height and put the feet on the ground.
        var renderers = inst.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y > .1f) inst.transform.localScale *= buyer.height / bounds.size.y;
        bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        inst.transform.position += Vector3.up * (root.position.y - bounds.min.y);
        foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.On; }

        var anim = inst.GetComponent<Animator>(); if (!anim) anim = inst.AddComponent<Animator>();
        anim.avatar = avatar;
        anim.runtimeAnimatorController = oldAnimator ? oldAnimator.runtimeAnimatorController : null;
        anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (oldVisual) oldVisual.gameObject.SetActive(false);

        foreach (var h in root.GetComponentsInChildren<JayHumanAnimation>(true)) if (!h.animator || h.animator == oldAnimator) { h.animator = anim; EditorUtility.SetDirty(h); }
        foreach (var h in Object.FindObjectsByType<JayHumanAnimation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (h.animator == oldAnimator) { h.animator = anim; EditorUtility.SetDirty(h); }
        game.customerClothing = null;

        // Talk camera target on the new face.
        var head = inst.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bip01 Head");
        if (head && customer.headTarget) customer.headTarget.position = head.position + Vector3.up * .08f;
        EditorUtility.SetDirty(customer); EditorUtility.SetDirty(game);
    }

    // ------------------------------------------------------------------ portraits
    /// <summary>Renders portraits in clean midday light (scenes are reopened afterwards, nothing is saved).</summary>
    public static void CapturePortraits(string level1, string level2, string level3)
    {
        Directory.CreateDirectory(Portraits);
        Capture(level1, g => new[] { ("DrHale", Face(g.hale)), ("Leo", Face(g.customer)), ("Jay", JayFace(g)) });
        Capture(level2, g => new[] { ("Luka", Face(g.customer)) });
        Capture(level3, g => { g.customer.gameObject.SetActive(true); return new[] { ("Paul", Face(g.customer)) }; });
        AssetDatabase.Refresh();
        Assign("Jay", Assignment3Content.Jay); Assign("DrHale", Assignment3Content.Hale); Assign("Leo", Assignment3Content.Leo);
        Assign("Luka", Assignment3Content.Luka); Assign("Paul", Assignment3Content.Paul);
        AssetDatabase.SaveAssets();
    }

    struct FaceShot { public Vector3 face, forward; }

    static FaceShot Face(TalkableCharacter c)
    {
        var head = c.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Bip01 Head" && t.gameObject.activeInHierarchy);
        var anim = c.GetComponentsInChildren<Animator>().FirstOrDefault(a => a.isHuman && a.gameObject.activeInHierarchy);
        if (!head && anim) head = anim.GetBoneTransform(HumanBodyBones.Head);
        Vector3 p = head ? head.position + Vector3.up * .07f : (c.headTarget ? c.headTarget.position : c.transform.position + Vector3.up * 1.65f);
        return new FaceShot { face = p, forward = c.transform.forward };
    }

    static FaceShot JayFace(JayGameManager g)
    {
        var body = g.player.bodyVisual;
        if (body) body.gameObject.SetActive(true);
        var anim = body ? body.GetComponentInChildren<Animator>(true) : null;
        Transform head = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 p = head ? head.position + Vector3.up * .07f : g.player.transform.position + Vector3.up * 1.62f;
        return new FaceShot { face = p, forward = g.player.transform.forward };
    }

    static void Capture(string scene, System.Func<JayGameManager, (string, FaceShot)[]> shots)
    {
        EditorSceneManager.OpenScene("Assets/JayACareer/Scenes/" + scene + ".unity", OpenSceneMode.Single);
        var g = Object.FindFirstObjectByType<JayGameManager>(FindObjectsInactive.Include);
        if (g.lighting) g.lighting.SetCycleFraction(.06f);   // late-morning light for every portrait
        foreach (var (name, shot) in shots(g)) Render(name, shot);
    }

    static void Render(string name, FaceShot shot)
    {
        var camGo = new GameObject("PortraitCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 20; cam.nearClipPlane = .05f; cam.farClipPlane = 60;
        Vector3 dir = Quaternion.AngleAxis(-18, Vector3.up) * shot.forward;     // three-quarter view
        camGo.transform.position = shot.face + dir * .95f + Vector3.up * .02f;
        camGo.transform.LookAt(shot.face - Vector3.up * .04f);
        var key = new GameObject("PortraitKey").AddComponent<Light>();
        key.type = LightType.Point; key.range = 4; key.intensity = 2.2f; key.color = new Color(1, .93f, .85f);
        key.transform.position = camGo.transform.position + camGo.transform.right * .5f + Vector3.up * .35f;
        var fill = new GameObject("PortraitFill").AddComponent<Light>();
        fill.type = LightType.Point; fill.range = 4; fill.intensity = .7f; fill.color = new Color(.75f, .85f, 1f);
        fill.transform.position = camGo.transform.position - camGo.transform.right * .6f;

        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var request = new RenderPipeline.StandardRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request); else cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Portraits + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null;
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo); Object.DestroyImmediate(key.gameObject); Object.DestroyImmediate(fill.gameObject);
    }

    static void Assign(string file, CharacterProfile profile)
    {
        string path = Portraits + file + ".png";
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti && ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.SaveAndReimport(); }
        profile.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        EditorUtility.SetDirty(profile);
    }
}
