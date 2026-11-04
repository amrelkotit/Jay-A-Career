using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using C = StoryCondition;
using E = StoryEffect;
using UI = Assignment3UI;

/// <summary>
/// Builds the complete Assignment 3 game from the Assignment 2 realism scene:
///   MainMenu → Loading → L1_FirstDelivery → L2_Middleman → L3_BigBuyer → Ending
/// Each level is a copy of the A2 world with its own story layer: UI, objectives, characters'
/// conversations, lore objects, triggers, time of day, street lamps and cutscenes.
/// Menu: Jay A Career ▸ Assignment 3 ▸ Build Story Game.  Batch: -executeMethod Assignment3Builder.BuildAllBatch
/// Running it again rebuilds all scenes from the A2 source, so it is safe to repeat.
/// </summary>
public static class Assignment3Builder
{
    const string Scenes = "Assets/JayACareer/Scenes/";
    const string Source = Scenes + "JayACareer_Realism.unity";
    const string StoryRoot = "Assets/JayACareer/Story/";
    const string Timelines = StoryRoot + "Timelines/";
    const string Materials = StoryRoot + "Materials/";
    static string ScenePath(string name) => Scenes + name + ".unity";

    // ================================================================== entry points
    [MenuItem("Jay A Career/Assignment 3/Build Story Game (all scenes)")]
    public static void BuildAll()
    {
        if (!EnsureTextMeshPro())
        {
            EditorUtility.DisplayDialog("Jay A Career", "TextMesh Pro essential resources were just imported.\nRun 'Build Story Game' once more to build the scenes.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
        EditorUtility.DisplayDialog("Jay A Career", "Assignment 3 built:\nMainMenu, Loading, L1, L2, L3, Ending.\nOpen MainMenu and press Play.", "OK");
    }

    /// <summary>Command-line entry: Unity.exe -batchmode -projectPath ... -executeMethod Assignment3Builder.BuildAllBatch -quit</summary>
    public static void BuildAllBatch()
    {
        try
        {
            EnsureTextMeshPro();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Build();
            Debug.Log("ASSIGNMENT3_BUILD_OK");
        }
        catch (Exception e) { Debug.LogError("ASSIGNMENT3_BUILD_FAILED " + e); EditorApplication.Exit(1); }
    }

    static bool EnsureTextMeshPro()
    {
        if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return true;
        string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        if (!File.Exists(package))
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (info != null) package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        }
        AssetDatabase.ImportPackage(package, false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset") && Application.isBatchMode;
    }

    static void Build()
    {
        foreach (var f in new[] { StoryRoot, Timelines, Materials }) Directory.CreateDirectory(f);
        AssetDatabase.Refresh();
        UI.Init();
        Assignment3Content.Build();
        BuildLevel1();
        BuildLevel2();
        BuildLevel3();
        BuildMainMenu();
        BuildLoading();
        BuildEnding();
        EditorBuildSettings.scenes = new[] { "MainMenu", "Loading", LevelTransitionManager.Level1, LevelTransitionManager.Level2, LevelTransitionManager.Level3, "Ending" }
            .Select(n => new EditorBuildSettingsScene(ScenePath(n), true)).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath("MainMenu"));
    }

    // ================================================================== shared level setup
    /// <summary>Scene references collected once per level.</summary>
    class Level
    {
        public Scene scene;
        public JayGameManager game;
        public GameObject systems;
        public DialogueManager dialogue;
        public StoryHUD hud;
        public ObjectiveManager objectives;
        public LevelDirector director;
        public CanvasGroup skipHint;
        public Image skipFill;
        public GameObject legacyHud;
        public Transform sink, bed, prepBench, labDoor;
    }

    static Level OpenLevelCopy(string name)
    {
        string path = ScenePath(name);
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        if (!AssetDatabase.CopyAsset(Source, path)) throw new Exception("Could not copy " + Source + " to " + path);
        var lv = new Level { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single) };
        lv.game = Object.FindFirstObjectByType<JayGameManager>(FindObjectsInactive.Include);
        if (!lv.game) throw new Exception("JayGameManager missing in " + path);
        Physics.SyncTransforms();

        // Remove A2 automated demo / validation components if they were ever saved into the scene.
        foreach (var t in new[] { typeof(JayDemonstration), typeof(JayInputValidation), typeof(JayActionsReview), typeof(JayLightingReview) })
            foreach (var c in Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(c);

        var sinkStation = Object.FindFirstObjectByType<CleanableStation>(FindObjectsInactive.Include);
        lv.sink = sinkStation ? sinkStation.transform : null;
        foreach (var s in Object.FindObjectsByType<JayStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (s.action == JayStation.Action.Sleep) lv.bed = s.transform;
            if (s.action == JayStation.Action.Prepare) lv.prepBench = s.transform;
        }
        lv.labDoor = Find("LabEntranceHinge");

        lv.systems = new GameObject("Assignment3 Story Systems");
        lv.objectives = lv.systems.AddComponent<ObjectiveManager>();
        lv.director = lv.systems.AddComponent<LevelDirector>();
        lv.director.game = lv.game; lv.director.lighting = lv.game.lighting;

        HideLegacyHud(lv);
        BuildStoryUI(lv);
        AddStreetLamps(lv);
        return lv;
    }

    static void Save(Level lv)
    {
        EditorSceneManager.MarkSceneDirty(lv.scene);
        EditorSceneManager.SaveScene(lv.scene);
    }

    static Transform Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    static void HideLegacyHud(Level lv)
    {
        var hud = Find("HUD_Canvas");
        lv.legacyHud = hud ? hud.gameObject : null;
        if (hud) foreach (Transform child in hud) if (child.name != "Reticle") child.gameObject.SetActive(false);
        if (lv.game.pausePanel) lv.game.pausePanel.SetActive(false);
    }

    static TalkableCharacter Hale(Level lv) => lv.game.hale;
    static TalkableCharacter Buyer(Level lv) => lv.game.customer;
    static Transform Head(TalkableCharacter c) => c.headTarget ? c.headTarget : c.transform;

    static void SetConversations(TalkableCharacter who, CharacterProfile profile, params ConversationOption[] options)
    {
        who.profile = profile; who.CharacterName = profile.displayName;
        who.conversations = new List<ConversationOption>(options);
        EditorUtility.SetDirty(who);
    }

    static ConversationOption Talk(string dialogueId, string note, params C[] conditions)
        => new ConversationOption { note = note, dialogue = Assignment3Content.Get(dialogueId), conditions = new List<C>(conditions) };

    static ObjectiveStep Step(string text, Transform marker, C[] show, params C[] complete)
        => new ObjectiveStep { text = text, marker = marker, showWhen = new List<C>(show ?? new C[0]), completeWhen = new List<C>(complete) };

    static C Stage(JayDailyLoop.Stage s) => C.Text(StoryKeys.LoopStage, s.ToString());

    static LevelDirector.LoopEventEffects OnLoop(string ev, params E[] effects)
        => new LevelDirector.LoopEventEffects { loopEvent = ev, effects = new List<E>(effects) };

    static LevelDirector.TimeOfDayKey TOD(string note, float fraction, float blend, params C[] conditions)
        => new LevelDirector.TimeOfDayKey { note = note, cycleFraction = fraction, blendSeconds = blend, conditions = new List<C>(conditions) };

    static LevelDirector.RecapVariant Recap(string text, params C[] conditions)
        => new LevelDirector.RecapVariant { recap = text, conditions = new List<C>(conditions) };

    /// <summary>Standard loop objectives shared by all levels (wording customised per level).</summary>
    static IEnumerable<ObjectiveStep> LoopSteps(Level lv, string seal, string carry, string deal, string buyerMarkerName = null)
    {
        yield return Step(seal, lv.prepBench ? lv.prepBench : lv.game.preparationSlot, new[] { Stage(JayDailyLoop.Stage.Preparation) });
        yield return Step(carry, lv.game.counterSlot, new[] { Stage(JayDailyLoop.Stage.Delivery) });
        yield return Step(deal, Buyer(lv).transform, new[] { Stage(JayDailyLoop.Stage.Negotiation) });
    }

    static IEnumerable<ObjectiveStep> WrapUpSteps(Level lv)
    {
        yield return Step("Put the unsold carton back on the lab's return shelf", lv.game.returnSlot, new[] { Stage(JayDailyLoop.Stage.Return) });
        yield return Step("Clean the preparation bench at the sink", lv.sink, new[] { Stage(JayDailyLoop.Stage.Cleaning) });
        yield return Step("Go home and sleep. The day is over", lv.bed, new[] { Stage(JayDailyLoop.Stage.Sleep) });
    }

    // ================================================================== LEVEL 1
    static void BuildLevel1()
    {
        var lv = OpenLevelCopy(LevelTransitionManager.Level1);
        var d = lv.director;
        d.dayNumber = 1; d.chapterTitle = "DAY 1 — FIRST DELIVERY"; d.chapterSubtitle = "Calder Flats. Rent due in three days.";
        d.timeOfDay = new List<LevelDirector.TimeOfDayKey>
        {
            TOD("Dawn: lamps still on", .70f, 0),
            TOD("Sunrise after the intro: lamps switch off", .88f, 45, C.Flag(StoryKeys.IntroSeen)),
        };
        d.loopEventEffects = new List<LevelDirector.LoopEventEffects>
        {
            OnLoop("prepared", E.Set(StoryKeys.CartonSealed)),
            OnLoop("slept", E.Set(StoryKeys.Act1Complete)),
        };
        d.completionFlag = StoryKeys.Act1Complete; d.nextScene = LevelTransitionManager.Level2; d.nextTitle = "DAY 2 — THE MIDDLEMAN";
        d.recaps = new List<LevelDirector.RecapVariant>
        {
            Recap("Leo paid $100 for a sealed carton nobody would describe. In Hale's desk, Jay found an old Corvane badge: \"E. Hale, Senior Chemist\".\n\nRent: ${cash} of ${rent}. Two days left.\n\nTomorrow's buyer is Luka. Hale says he is \"not a customer\".", C.Flag(StoryKeys.BadgeFound)),
            Recap("Leo paid $100 for a sealed carton nobody would describe. Hale keeps his desk closed and his past closer.\n\nRent: ${cash} of ${rent}. Two days left.\n\nTomorrow's buyer is Luka. Hale says he is \"not a customer\"."),
        };

        SetConversations(Hale(lv), Assignment3Content.Hale,
            Talk("L1_HaleBriefing", "Day 1 briefing", Stage(JayDailyLoop.Stage.Briefing)),
            Talk("L1_HaleDesk", "Asked about the badge", C.Flag(StoryKeys.BadgeFound)),
            Talk("L1_HaleAfterSale", "After Leo paid", C.Flag(StoryKeys.LeoPaid)),
            Talk("L1_HaleWork", "Idle"));
        SetConversations(Buyer(lv), Assignment3Content.Leo,
            Talk("L1_LeoDeal", "Negotiation", C.Flag(StoryKeys.CanNegotiate)),
            Talk("L1_LeoWaiting", "After the deal", C.Flag(StoryKeys.LeoPaid)),
            Talk("L1_LeoBefore", "Before the carton arrives"));
        if (lv.game.customerClothing && lv.game.customerVariantA) lv.game.customerClothing.sharedMaterial = lv.game.customerVariantA;

        var steps = new List<ObjectiveStep>
        {
            Step("Cross the road and talk to Dr. Hale in his laboratory", Head(Hale(lv)), new[] { C.Flag(StoryKeys.IntroSeen) }, C.Flag(StoryKeys.BriefingDone)),
        };
        steps.AddRange(LoopSteps(lv, "Seal Leo's order at the preparation bench", "Carry the carton across the road to the counter", "Talk to Leo and close the deal"));
        steps.AddRange(WrapUpSteps(lv));
        lv.objectives.steps = steps;

        // Lore objects (environmental storytelling): Hale's past and Jay's money problems.
        Lore(lv, "badge", "Corvane staff badge", "Hale's desk", new Vector3(-5.0f, 0, 14.45f), new Vector3(.09f, .006f, .13f), Mat("Badge", new Color(.85f, .86f, .9f)),
            "A laminated staff badge from Corvane Industrial Chemicals: \"E. HALE — SENIOR CHEMIST\". The photo has been scratched out with a key.",
            "Corvane", "Hale used to be a senior chemist at Corvane Industrial Chemicals. Somebody, probably Hale himself, scratched his own face off the badge.",
            "L1_BadgeThought", null, E.Set(StoryKeys.BadgeFound));
        Lore(lv, "certificate", "Face-down certificate", "Glassware bench", new Vector3(-9.45f, 0, 10.15f), new Vector3(.32f, .02f, .24f), Mat("Frame", new Color(.25f, .17f, .1f)),
            "A framed certificate, turned face-down, the glass cracked: \"Corvane Safety Excellence Award — Dr. Elias Hale\".",
            "", "", "L1_CertificateThought", null);
        Lore(lv, "bills", "Unpaid bills", "Jay's desk", new Vector3(9.25f, 0, 10.55f), new Vector3(.24f, .01f, .3f), Mat("Paper", new Color(.92f, .9f, .82f)),
            "Electricity: overdue. Water: overdue. A calendar with three days circled in red. Jay is out of time and out of money.",
            "", "", "L1_BillsThought", null);

        // End-of-day reflection once the work is done (beat: emotional close of Act 1).
        Trigger(lv, "L1_EndOfDay", StoryTrigger.Activation.WhenConditionsMet, "L1_EndOfDay", 1.5f,
            new[] { Stage(JayDailyLoop.Stage.Sleep), C.Flag(StoryKeys.LeoPaid) });

        // Opening narrative: Timeline + Cinemachine intro (skippable), then the eviction notice.
        var intro = Cutscene(lv, "L1_Intro", StoryKeys.IntroSeen, new[]
        {
            Shot(new Vector3(1, 6.5f, -5), new Vector3(1, 5.2f, -1.5f), new Vector3(1, 1, 12), 0, 5.5f),
            Shot(new Vector3(8.15f, 1.55f, 6.2f), new Vector3(8.55f, 1.45f, 7.1f), new Vector3(9.4f, .85f, 10.6f), 5f, 5.5f),
            Shot(new Vector3(-2.5f, 1.9f, .2f), new Vector3(-3.6f, 1.75f, 1.3f), new Vector3(-7, 1.5f, 5), 10f, 5.5f),
            Shot(new Vector3(-5f, 1.7f, 2.2f), new Vector3(-5.6f, 1.68f, 2.9f), new Vector3(-7, 1.45f, 5.2f), 15f, 4f),
        }, new[]
        {
            Sub(.5f, 4.2f, "Jay", "Calder Flats. Population: shrinking."),
            Sub(5.3f, 4.6f, "Jay", "Eviction notice on my door this morning. Four hundred dollars. Three days."),
            Sub(10.3f, 4.6f, "Jay", "Only one man in town is hiring. Dr. Elias Hale. Nobody visits his lab."),
            Sub(15.2f, 3.4f, "Jay", "Nobody but me, apparently."),
        });
        intro.playOnStart = true;
        intro.effectsOnFinish.Add(E.Item("eviction", "Eviction notice", "\"Unpaid rent: $400. Vacate within 3 days.\" Taped to Jay's door at 6 a.m. The landlord underlined \"3 days\" twice."));
        intro.effectsOnFinish.Add(E.Message("Objective markers point the way. [E] interacts, [Tab] inventory, [J] journal."));
        Save(lv);
    }

    // ================================================================== LEVEL 2
    static void BuildLevel2()
    {
        var lv = OpenLevelCopy(LevelTransitionManager.Level2);
        var d = lv.director;
        d.dayNumber = 2; d.chapterTitle = "DAY 2 — THE MIDDLEMAN"; d.chapterSubtitle = "Late afternoon. Somebody new is buying.";
        d.directPlayDefaults = new List<E> { E.Set(StoryKeys.IntroSeen), E.Set(StoryKeys.BriefingDone), E.Set(StoryKeys.LeoPaid), E.Set(StoryKeys.Act1Complete), E.SetNumber(StoryKeys.Cash, 150) };
        d.timeOfDay = new List<LevelDirector.TimeOfDayKey>
        {
            TOD("Late afternoon", .25f, 0),
            TOD("Dusk falls during Luka's deal: street lamps come on", .42f, 35, C.Flag("lukaArrived")),
        };
        d.loopEventEffects = new List<LevelDirector.LoopEventEffects>
        {
            OnLoop("prepared", E.Set(StoryKeys.CartonSealed + "2")),
            OnLoop("delivered", E.Set("lukaArrived")),
            OnLoop("slept", E.Set(StoryKeys.Act2Complete)),
        };
        d.completionFlag = StoryKeys.Act2Complete; d.nextScene = LevelTransitionManager.Level3; d.nextTitle = "DAY 3 — THE BIG BUYER";
        d.recaps = new List<LevelDirector.RecapVariant>
        {
            Recap("You took Luka's $150 and kept things quiet. Hale called it \"forgettable\", which from him is praise.\n\nSomeone who signs as \"P.\" is asking about you.\n\nRent: ${cash} of ${rent}. One day left.", C.Text(StoryKeys.LukaDeal, "accepted")),
            Recap("You squeezed Luka for $250. Hale says you made the two of you visible, and Luka promised that nerve gets noticed.\n\nSomeone who signs as \"P.\" is asking about you.\n\nRent: ${cash} of ${rent}. One day left.", C.Text(StoryKeys.LukaDeal, "pushed")),
            Recap("You sent Luka away with nothing. Hale didn't say he was proud, but he was. The rent didn't move.\n\nSomeone who signs as \"P.\" is asking about you.\n\nRent: ${cash} of ${rent}. ${short} short, one day left.", C.Text(StoryKeys.LukaDeal, "refused")),
        };

        SetConversations(Hale(lv), Assignment3Content.Hale,
            Talk("L2_HaleWarning", "Warning about Luka", Stage(JayDailyLoop.Stage.Briefing)),
            Talk("L2_HaleReaction", "Reaction to the Luka choice", C.Flag(StoryKeys.LukaDealt)),
            Talk("L2_HaleAfter", "After the reaction", C.Flag(StoryKeys.HaleReacted)),
            Talk("L2_HaleWork", "Idle"));
        SetConversations(Buyer(lv), Assignment3Content.Luka,
            Talk("L2_LukaDeal", "The meaningful choice", C.Flag(StoryKeys.CanNegotiate)),
            Talk("L2_LukaWaiting", "After the deal", C.Flag(StoryKeys.LukaDealt)),
            Talk("L2_LukaBefore", "Before the carton arrives"));
        if (lv.game.customerClothing && lv.game.customerVariantB) lv.game.customerClothing.sharedMaterial = lv.game.customerVariantB;

        var phone = Lore(lv, "phone", "Luka's phone", "Customer counter", new Vector3(9.55f, 0, 13.75f), new Vector3(.075f, .01f, .15f), Mat("PhoneBody", new Color(.04f, .04f, .05f)),
            "Luka's phone lit up on the counter. One message, signed only \"P.\":\n\"Is the old man's new boy reliable? Find out tonight.\"",
            "\"P.\"", "Someone who signs as \"P.\" is asking Luka about me. P... Paul? Leo said Paul's people leave Hale's orders alone.",
            "L2_PhoneThought", new[] { C.Flag(StoryKeys.LukaDealt) }, E.Set(StoryKeys.PhoneNoteFound));
        phone.removeFromWorld = false; phone.lockedMessage = "";
        AddScreen(phone.transform, Mat("PhoneScreen", new Color(.3f, .55f, 1f), 2.5f));

        var steps = new List<ObjectiveStep>
        {
            Step("Talk to Dr. Hale before today's order", Head(Hale(lv)), new[] { Stage(JayDailyLoop.Stage.Briefing) }),
        };
        steps.AddRange(LoopSteps(lv, "Seal Luka's order at the preparation bench", "Carry the carton to the counter. Luka is waiting", "Deal with Luka. It's your call"));
        steps.Add(Step("Check the phone Luka left on the counter", phone.transform, new[] { C.Flag(StoryKeys.LukaDealt) }, C.Flag(StoryKeys.PhoneNoteFound)));
        steps.Add(Step("Go back to the lab and tell Dr. Hale how it went", Head(Hale(lv)), new[] { C.Flag(StoryKeys.LukaDealt) }, C.Flag(StoryKeys.HaleReacted)));
        steps.AddRange(WrapUpSteps(lv));
        lv.objectives.steps = steps;

        // Environmental storytelling: someone has been waiting at the counter.
        var props = new GameObject("L2 Environmental Story").transform;
        for (int i = 0; i < 5; i++)
        {
            var butt = Prop(props, "CigaretteButt", PrimitiveType.Cylinder, new Vector3(8.2f + i * .23f, .06f, 13.1f + (i % 2) * .21f), new Vector3(.012f, .02f, .012f), Mat("Cigarette", new Color(.95f, .92f, .85f)));
            butt.transform.rotation = Quaternion.Euler(90, i * 47, 0);
        }
        for (int s = -1; s <= 1; s += 2)
            Prop(props, "TyreTrack", PrimitiveType.Cube, new Vector3(5.9f, .057f, 14.2f + s * .78f), new Vector3(4.2f, .004f, .22f), Mat("TyreTrack", new Color(.12f, .11f, .1f)));

        Trigger(lv, "L2_EndOfDay", StoryTrigger.Activation.WhenConditionsMet, "L2_EndOfDay", 1.5f, new[] { Stage(JayDailyLoop.Stage.Sleep), C.Flag(StoryKeys.LukaDealt) });
        Save(lv);
    }

    // ================================================================== LEVEL 3
    static void BuildLevel3()
    {
        var lv = OpenLevelCopy(LevelTransitionManager.Level3);
        var d = lv.director;
        d.dayNumber = 3; d.chapterTitle = "DAY 3 — THE BIG BUYER"; d.chapterSubtitle = "Midnight. The lights are off at Hale's.";
        d.directPlayDefaults = new List<E> { E.Set(StoryKeys.IntroSeen), E.Set(StoryKeys.LeoPaid), E.Set(StoryKeys.Act1Complete), E.Set(StoryKeys.LukaDealt),
            E.Text(StoryKeys.LukaDeal, "accepted"), E.Set(StoryKeys.PhoneNoteFound), E.Set(StoryKeys.Act2Complete), E.SetNumber(StoryKeys.Cash, 300), E.SetNumber(StoryKeys.Heat, 1) };
        d.timeOfDay = new List<LevelDirector.TimeOfDayKey> { TOD("Midnight", .5f, 0) };
        d.completionFlag = ""; d.nextScene = "";   // the ending cutscene loads the Ending scene

        var hale = Hale(lv); var paul = Buyer(lv);
        SetConversations(hale, Assignment3Content.Hale,
            Talk("L3_HaleConfession", "Confession (varies with lukaDeal)", C.Flag(StoryKeys.NotesFound), Stage(JayDailyLoop.Stage.Briefing)),
            Talk("L3_HaleWork", "Idle"));
        SetConversations(paul, Assignment3Content.Paul,
            Talk("L3_PaulDeal", "Final deal (varies with lukaDeal)", C.Flag(StoryKeys.CanNegotiate)),
            Talk("L3_PaulBefore", "Waiting"));
        if (lv.game.customerClothing && lv.game.customerVariantA) lv.game.customerClothing.sharedMaterial = lv.game.customerVariantA;

        // Night start: Jay wakes in his room; Hale and Paul are not here yet.
        var player = lv.game.player.transform;
        player.SetPositionAndRotation(new Vector3(10.6f, .2f, 8.2f), Quaternion.Euler(0, -140, 0));
        hale.gameObject.SetActive(false);
        paul.gameObject.SetActive(false);
        var haleEntrance = Marker("HaleEntranceMarker", new Vector3(-7f, .08f, 6.1f), 0);
        var haleDoorway = Marker("HaleDoorwayMarker", new Vector3(-7f, .08f, 5.45f), 62);

        // Search clues: three lore items, each adds one to "clues".
        var clue = E.Add("clues", 1);
        Lore(lv, "ledger", "Hale's ledger", "Instrument bench", new Vector3(-3.35f, 0, 14.45f), new Vector3(.22f, .035f, .3f), Mat("Ledger", new Color(.32f, .16f, .1f)),
            "Eight years of orders in Hale's neat handwriting. Every month one name: \"P.\" Every month a payment out, never in.\nTonight's page: \"P. — in person. Bulk. Keep Jay out of it.\"",
            "The ledger", "Hale has paid someone called P. every month for eight years. Tonight P. comes in person.", "L3_LedgerThought", null, clue);
        Lore(lv, "letter", "Unsent letter", "Glassware bench", new Vector3(-9.45f, 0, 11.05f), new Vector3(.21f, .004f, .29f), Mat("Paper", new Color(.92f, .9f, .82f)),
            "\"Clara — I took the blame because I thought it would end there. It didn't. Corvane's settlement owns me, and Paul owns the settlement. Don't come looking for me.\"\nIt was never sent.",
            "Clara", "Hale wrote to someone called Clara, probably his daughter, and never sent it. Paul owns his debt.", "L3_LetterThought", null, clue);
        var lockbox = Lore(lv, "lockbox", "Forced lockbox", "Storage shelves", new Vector3(-2.05f, 0, 11.15f), new Vector3(.32f, .14f, .22f), Mat("Lockbox", new Color(.36f, .38f, .4f)),
            "A metal lockbox, forced open. Empty except for a court document:\n\"Corvane Industrial Chemicals v. E. Hale — settlement: $480,000.\"",
            "The settlement", "Corvane settled with Hale for $480,000. He could never pay it. Paul bought the debt.", "L3_LockboxThought", null, clue);
        lockbox.removeFromWorld = false; lockbox.lockedMessage = "";

        // Hale's coat still on the hook by the door (environmental storytelling).
        var coat = Prop(null, "HalesCoatOnHook", PrimitiveType.Cube, new Vector3(-8.45f, 1.25f, 5.22f), new Vector3(.42f, .9f, .08f), Mat("Coat", new Color(.86f, .84f, .78f)));

        // Beats.
        Trigger(lv, "L3_JayWakes", StoryTrigger.Activation.OnStart, "L3_JayWakes", 1.2f, new C[0]);
        var labZone = Trigger(lv, "L3_LabEmpty", StoryTrigger.Activation.EnterZone, "L3_LabEmpty", .2f, new C[0], E.Set(StoryKeys.HaleMissingSeen));
        var box = labZone.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true;
        labZone.transform.position = new Vector3(-7f, 1.5f, 10.5f); box.size = new Vector3(11.5f, 3f, 10.5f);
        var returns = Trigger(lv, "L3_HaleReturns", StoryTrigger.Activation.WhenConditionsMet, "L3_Footsteps", .8f,
            new[] { C.AtLeast("clues", 3) }, E.Set(StoryKeys.NotesFound));
        returns.moves.Add(new StoryTrigger.Move { target = hale.transform, destination = haleEntrance });
        returns.activateObjects = new[] { hale.gameObject };
        returns.dialogueFocus = null;

        // Paul's arrival: headlights at the end of the road.
        var headlights = new GameObject("PaulHeadlights").transform;
        for (int s = -1; s <= 1; s += 2)
        {
            var l = new GameObject(s < 0 ? "HeadlightL" : "HeadlightR").AddComponent<Light>();
            l.transform.SetParent(headlights); l.type = LightType.Spot; l.range = 28; l.spotAngle = 48; l.intensity = 9; l.color = new Color(1, .95f, .85f);
            l.transform.SetPositionAndRotation(new Vector3(2.0f + s * .75f, .75f, 23.2f), Quaternion.LookRotation(new Vector3(8.5f, .4f, 14.5f) - new Vector3(2f, .75f, 23.2f)));
            l.shadows = LightShadows.None;
        }
        headlights.gameObject.SetActive(false);
        var arrival = Trigger(lv, "L3_PaulArrives", StoryTrigger.Activation.WhenConditionsMet, null, 1f, new[] { C.Flag(StoryKeys.ConfessionHeard) },
            E.Message("Headlights at the end of the road. A car stops by the counter."));
        arrival.activateObjects = new[] { paul.gameObject, headlights.gameObject };

        var steps = new List<ObjectiveStep>
        {
            Step("Find out why Hale's lab is dark", lv.labDoor, null, C.Flag(StoryKeys.HaleMissingSeen)),
            Step("Search the lab with your flashlight [F]: ledger, letter, lockbox", null, new[] { C.Flag(StoryKeys.HaleMissingSeen) }, C.Flag(StoryKeys.NotesFound)),
            Step("Dr. Hale is back. Talk to him", Head(hale), new[] { C.Flag(StoryKeys.NotesFound) }, C.Flag(StoryKeys.ConfessionHeard)),
        };
        steps.AddRange(LoopSteps(lv, "Seal Paul's order at the preparation bench", "Carry the carton to the counter. Paul is waiting", "Meet Paul at the counter"));
        lv.objectives.steps = steps;
        lv.objectives.allCompleteText = "";

        // Closing narrative: Paul's card, Hale in the doorway, cliffhanger.
        var card = Prop(null, "PaulsCard", PrimitiveType.Cube, new Vector3(9.12f, 0, 13.82f), new Vector3(.09f, .002f, .055f), Mat("Card", new Color(.97f, .96f, .93f)));
        PlaceOnSurface(card.transform, .001f);
        card.SetActive(false);
        var doorLight = new GameObject("LabDoorwaySpill").AddComponent<Light>();
        doorLight.type = LightType.Point; doorLight.range = 5; doorLight.intensity = 2.2f; doorLight.color = new Color(1, .8f, .55f);
        doorLight.transform.position = new Vector3(-7f, 2.2f, 6.2f); doorLight.gameObject.SetActive(false);
        var ending = Trigger(lv, "L3_EndingSetup", StoryTrigger.Activation.WhenConditionsMet, null, 0, new[] { C.Flag(StoryKeys.PaulMet) });
        ending.moves.Add(new StoryTrigger.Move { target = hale.transform, destination = haleDoorway });
        ending.activateObjects = new[] { card, doorLight.gameObject };

        var outro = Cutscene(lv, "L3_Ending", StoryKeys.EndingSeen, new[]
        {
            Shot(new Vector3(8.75f, 1.62f, 12.75f), new Vector3(8.9f, 1.6f, 13.15f), new Vector3(9f, 1.58f, 15.4f), 0, 4.6f),
            Shot(new Vector3(9.3f, 1.62f, 13.45f), new Vector3(9.22f, 1.48f, 13.6f), new Vector3(9.1f, 1.17f, 13.85f), 4.2f, 8f),
            Shot(new Vector3(-2.4f, 1.7f, 2.6f), new Vector3(-3.4f, 1.65f, 3.4f), new Vector3(-7f, 1.45f, 5.45f), 12f, 5f),
            Shot(new Vector3(1.2f, 6.2f, -3.5f), new Vector3(1.2f, 7.2f, -5f), new Vector3(3f, 1f, 12f), 16.8f, 5f),
        }, new[]
        {
            Sub(.3f, 4f, "Paul", "Two hundred. Count it if you like. I never miss."),
            Sub(4.4f, 3.8f, "Paul", "And this. Read it when I'm gone."),
            Sub(8.4f, 3.6f, "Jay", "\"Tomorrow. Without the old man.\""),
            Sub(12.2f, 4.6f, "", "Across the road, Dr. Hale stands in the lab doorway. He heard every word."),
            Sub(17f, 4.4f, "Jay", "Whose career is this?"),
        });
        outro.playOnStart = false; outro.playWhenConditionsMet = true;
        outro.startConditions = new List<C> { C.Flag(StoryKeys.PaulMet) };
        outro.loadSceneOnFinish = LevelTransitionManager.EndingScene;
        outro.loadTitle = ""; outro.loadRecap = "";
        Save(lv);
    }

    // ================================================================== story UI (every level)
    static void BuildStoryUI(Level lv)
    {
        var canvas = UI.Canvas("Assignment3 Story UI", 20).transform;
        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include)) NewEventSystem();

        // ---------------- HUD
        var hudRoot = UI.Stretch(canvas, "HUD");
        var hud = canvas.gameObject.AddComponent<StoryHUD>();
        hud.hudGroup = UI.Group(hudRoot.gameObject);
        hud.hudGroup.blocksRaycasts = false;

        var status = UI.Img(hudRoot, "StatusPanel", UI.Panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(360, 178)).transform;
        hud.dayText = UI.Txt(status, "Day", "DAY 1 / 3", 20, UI.Accent, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -14), new Vector2(320, 26), FontStyles.Bold);
        hud.cashText = UI.Txt(status, "Cash", "$50", 36, UI.Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -40), new Vector2(330, 46), FontStyles.Bold);
        hud.trustFill = UI.Bar(status, "Trust", "TRUST · DR. HALE", UI.TrustColor, new Vector2(22, -94), 316);
        hud.heatFill = UI.Bar(status, "Heat", "HEAT · ATTENTION", UI.HeatColor, new Vector2(22, -134), 316);

        var obj = UI.Img(hudRoot, "ObjectivePanel", UI.Panel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(560, 116)).transform;
        UI.Img(obj, "Accent", UI.Accent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(5, 0), false);
        hud.objectiveLabel = UI.Txt(obj, "Label", "OBJECTIVE", 18, UI.Muted, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -12), new Vector2(500, 24), FontStyles.Bold);
        hud.objectiveText = UI.Txt(obj, "Text", "", 25, UI.Text, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -40), new Vector2(516, 70));

        hud.promptText = UI.Pill(hudRoot, "Prompt", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 150), 26, 900, out _);
        hud.toastText = UI.Pill(hudRoot, "Toast", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -150), 24, 900, out var toastRoot);
        hud.toastGroup = UI.Group(toastRoot, 0);

        var marker = UI.Img(hudRoot, "ObjectiveMarker", UI.Accent, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26), false);
        marker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        hud.marker = marker.rectTransform;
        hud.markerDistance = UI.Txt(marker.transform, "Distance", "", 18, UI.Text, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, 1), new Vector2(0, -22), new Vector2(90, 24), FontStyles.Bold);
        hud.markerDistance.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);

        UI.Txt(hudRoot, "Controls", "<b>[E]</b> Interact   <b>[Tab]</b> Inventory   <b>[J]</b> Journal   <b>[F]</b> Flashlight   <b>[V]</b> Camera   <b>[Esc]</b> Pause",
            17, UI.Muted, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24, 18), new Vector2(1100, 26));

        // ---------------- chapter card + subtitles (outside the HUD group so cutscenes keep them)
        var chapter = UI.Img(canvas, "ChapterCard", new Color(0, 0, 0, .55f), new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f), new Vector2(0, 90), new Vector2(0, 170), false);
        hud.chapterGroup = UI.Group(chapter.gameObject, 0); hud.chapterGroup.blocksRaycasts = false;
        hud.chapterTitle = UI.Txt(chapter.transform, "Title", "", 58, UI.Text, TextAlignmentOptions.Center, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f), new Vector2(0, 22), new Vector2(0, 74), FontStyles.Bold);
        hud.chapterSubtitle = UI.Txt(chapter.transform, "Subtitle", "", 26, UI.Accent, TextAlignmentOptions.Center, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(.5f, .5f), new Vector2(0, -38), new Vector2(0, 36), FontStyles.Italic);
        hud.subtitleText = UI.Pill(canvas, "Subtitle", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 80), 30, 1300, out _);

        // ---------------- dialogue box
        var dlg = UI.Img(canvas, "DialogueBox", UI.Panel, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(1500, 250));
        var dm = canvas.gameObject.AddComponent<DialogueManager>();
        dm.panel = UI.Group(dlg.gameObject, 0);
        var frame = UI.Img(dlg.transform, "PortraitFrame", UI.Accent, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(24, 0), new Vector2(204, 204));
        dm.portraitFrame = frame.gameObject;
        dm.portraitImage = UI.Img(frame.transform, "Portrait", Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(196, 196), false);
        dm.portraitImage.preserveAspect = true;
        dm.speakerNameText = UI.Txt(dlg.transform, "Speaker", "", 30, UI.Accent, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(256, -18), new Vector2(-280, 38), FontStyles.Bold);
        dm.bodyText = UI.Txt(dlg.transform, "Body", "", 29, UI.Text, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(256, -62), new Vector2(-290, 140));
        dm.continueHint = UI.Txt(dlg.transform, "ContinueHint", "", 19, UI.Muted, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-26, 14), new Vector2(600, 26), FontStyles.Bold);
        var choices = UI.Column(canvas, "Choices", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 300), new Vector2(1100, 260), 10);
        choices.childAlignment = TextAnchor.LowerCenter;
        dm.choiceContainer = choices.GetComponent<RectTransform>();
        dm.choiceTemplate = UI.Btn(choices.transform, "ChoiceTemplate", "Choice", new Vector2(1100, 58), 25);
        dm.choiceTemplate.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
        dm.uiAudio = canvas.gameObject.AddComponent<AudioSource>(); dm.uiAudio.playOnAwake = false;
        dm.openSound = lv.game.confirmSound; dm.choiceSound = lv.game.placementSound;
        lv.dialogue = dm;

        // ---------------- notebook (inventory + journal)
        var nb = UI.Img(canvas, "Notebook", new Color(.07f, .07f, .08f, .96f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1240, 720));
        var notebook = canvas.gameObject.AddComponent<NotebookUI>();
        notebook.panel = UI.Group(nb.gameObject, 0);
        notebook.headerText = UI.Txt(nb.transform, "Header", "INVENTORY", 40, UI.Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -28), new Vector2(700, 56), FontStyles.Bold);
        var tabs = UI.Rect(nb.transform, "Tabs", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(560, 52));
        var hl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 12; hl.childControlWidth = false; hl.childForceExpandWidth = false; hl.childAlignment = TextAnchor.MiddleRight;
        notebook.inventoryTab = UI.Btn(tabs, "InventoryTab", "Inventory [Tab]", new Vector2(220, 50), 22);
        notebook.journalTab = UI.Btn(tabs, "JournalTab", "Journal [J]", new Vector2(190, 50), 22);
        notebook.closeButton = UI.Btn(tabs, "Close", "Close", new Vector2(110, 50), 22);
        UI.Img(nb.transform, "Divider", UI.Accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(-80, 3), false);
        var listBg = UI.Img(nb.transform, "ListBack", new Color(1, 1, 1, .04f), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(40, 40), new Vector2(400, -170));
        notebook.listText = UI.FillText(listBg.transform, "List", "", 25, UI.Text, TextAlignmentOptions.TopLeft, 20);
        var detailBg = UI.Img(nb.transform, "DetailBack", new Color(1, 1, 1, .04f), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(460, 40), new Vector2(-500, -170));
        notebook.detailText = UI.FillText(detailBg.transform, "Detail", "", 26, UI.Text, TextAlignmentOptions.TopLeft, 26);

        // ---------------- skip hint for cutscenes
        var skip = UI.Img(canvas, "SkipHint", UI.Panel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(300, 56));
        lv.skipHint = UI.Group(skip.gameObject, 0);
        UI.Txt(skip.transform, "Text", "Hold <b>[Space]</b> to skip", 21, UI.Text, TextAlignmentOptions.Left, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(70, 0), new Vector2(230, 40));
        var ringBack = UI.Img(skip.transform, "RingBack", new Color(1, 1, 1, .15f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(38, 38), false);
        ringBack.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        lv.skipFill = UI.Img(skip.transform, "Ring", UI.Accent, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(38, 38), false);
        lv.skipFill.sprite = ringBack.sprite; lv.skipFill.type = Image.Type.Filled; lv.skipFill.fillMethod = Image.FillMethod.Radial360; lv.skipFill.fillAmount = 0;

        // ---------------- pause menu + settings
        var pause = UI.Img(canvas, "PauseMenu", new Color(0, 0, 0, .72f), Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, false);
        var pauseCtl = pause.gameObject.AddComponent<PauseMenuController>(); pauseCtl.game = lv.game;
        UI.Txt(pause.transform, "Title", "PAUSED", 64, UI.Text, TextAlignmentOptions.Center, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 260), new Vector2(800, 80), FontStyles.Bold);
        var col = UI.Column(pause.transform, "Buttons", new Vector2(.5f, .5f), new Vector2(.5f, 1), new Vector2(0, 190), new Vector2(420, 420), 14);
        pauseCtl.resumeButton = UI.Btn(col.transform, "Resume", "Resume", new Vector2(420, 64));
        pauseCtl.restartButton = UI.Btn(col.transform, "Restart", "Restart Day", new Vector2(420, 64));
        pauseCtl.settingsButton = UI.Btn(col.transform, "Settings", "Settings", new Vector2(420, 64));
        pauseCtl.mainMenuButton = UI.Btn(col.transform, "MainMenu", "Main Menu", new Vector2(420, 64));
        pauseCtl.quitButton = UI.Btn(col.transform, "Quit", "Quit Game", new Vector2(420, 64));
        pauseCtl.settings = BuildSettingsPanel(pause.transform, col.gameObject);
        pause.gameObject.SetActive(false);
        lv.game.pausePanel = pause.gameObject;
        EditorUtility.SetDirty(lv.game);
        lv.hud = hud;
    }

    static SettingsManager BuildSettingsPanel(Transform parent, GameObject hideWhileOpen)
    {
        var panel = UI.Img(parent, "SettingsPanel", new Color(.07f, .07f, .08f, .97f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(780, 560));
        var s = panel.gameObject.AddComponent<SettingsManager>();
        s.panel = UI.Group(panel.gameObject, 0);
        UI.Txt(panel.transform, "Title", "SETTINGS", 40, UI.Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 56), FontStyles.Bold);
        s.volumeSlider = UI.SliderRow(panel.transform, "Master volume", -120);
        s.sensitivitySlider = UI.SliderRow(panel.transform, "Mouse sensitivity", -190);
        s.subtitleSlider = UI.SliderRow(panel.transform, "Subtitle size", -260);
        s.invertToggle = UI.ToggleRow(panel.transform, "Invert mouse Y", -330);
        s.qualityDropdown = UI.DropdownRow(panel.transform, "Graphics quality", -400);
        s.closeButton = UI.Btn(panel.transform, "Close", "Back", new Vector2(220, 56));
        var rt = s.closeButton.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); rt.anchoredPosition = new Vector2(-40, 30);
        s.hideWhileOpen = hideWhileOpen ? new[] { hideWhileOpen } : new GameObject[0];
        return s;
    }

    static void NewEventSystem()
    {
        var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    // ================================================================== street lamps
    static void AddStreetLamps(Level lv)
    {
        var lighting = lv.game.lighting;
        var north = Find("StreetLightNorth"); var south = Find("StreetLightSouth");
        // Two extra lamps so the road reads as a lit street, not two isolated posts.
        if (north) Clone(north, "StreetLightNorthMid", new Vector3(-2.8f, 0, 9.5f));
        if (south) Clone(south, "StreetLightSouthMid", new Vector3(4.1f, 0, 11.6f));
        var lamps = new List<Light>(); var bulbs = new List<Renderer>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("StreetLight")) continue;
                foreach (var l in t.GetComponentsInChildren<Light>(true)) if (!lamps.Contains(l)) lamps.Add(l);
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Bulb") && !bulbs.Contains(r)) bulbs.Add(r);
            }
        var ctl = lighting.gameObject.AddComponent<StreetLampController>();
        ctl.lighting = lighting; ctl.lamps = lamps.ToArray(); ctl.bulbs = bulbs.ToArray(); ctl.intensity = lighting.roadLightIntensity;
        lighting.roadLights = ctl.lamps; lighting.roadLampBulbs = ctl.bulbs;
        EditorUtility.SetDirty(lighting);
    }

    static void Clone(Transform source, string name, Vector3 position)
    {
        var copy = Object.Instantiate(source.gameObject, source.parent);
        copy.name = name; copy.transform.position = position; copy.transform.rotation = source.rotation;
    }

    // ================================================================== lore props, triggers, markers
    static LoreItem Lore(Level lv, string id, string title, string where, Vector3 xz, Vector3 size, Material mat, string description,
        string journalTitle, string journalBody, string thought, C[] conditions, params E[] effects)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Lore_" + id;
        go.transform.localScale = size;
        go.transform.position = xz;
        PlaceOnSurface(go.transform, size.y * .5f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        // Slightly larger collider so small objects are easy to aim at.
        var bc = go.GetComponent<BoxCollider>(); bc.size = new Vector3(1.6f, Mathf.Max(1, .05f / size.y), 1.6f);
        var item = go.AddComponent<LoreItem>();
        item.itemId = id; item.title = title; item.description = description;
        item.journalTitle = journalTitle; item.journalBody = journalBody;
        item.inspectDialogue = string.IsNullOrEmpty(thought) ? null : Assignment3Content.Get(thought);
        item.conditions = new List<C>(conditions ?? new C[0]);
        item.effects = new List<E>(effects);
        go.transform.SetParent(lv.systems.transform, true);
        return item;
    }

    /// <summary>Drops an object onto the first surface below it (bench, counter, desk...).</summary>
    static void PlaceOnSurface(Transform t, float halfHeight)
    {
        Physics.SyncTransforms();
        var origin = new Vector3(t.position.x, 2.05f, t.position.z);
        var hits = Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.collider.transform != t && !h.collider.transform.IsChildOf(t)).OrderBy(h => h.distance).ToArray();
        float y = hits.Length > 0 ? hits[0].point.y : 1f;
        t.position = new Vector3(t.position.x, y + halfHeight + .002f, t.position.z);
    }

    static void AddScreen(Transform phone, Material glow)
    {
        var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(screen.GetComponent<Collider>());
        screen.name = "Screen"; screen.transform.SetParent(phone, false);
        screen.transform.localPosition = new Vector3(0, .55f, 0); screen.transform.localScale = new Vector3(.86f, .1f, .9f);
        screen.GetComponent<Renderer>().sharedMaterial = glow;
    }

    static GameObject Prop(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.position = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        return go;
    }

    static Transform Marker(string name, Vector3 position, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        return t;
    }

    static StoryTrigger Trigger(Level lv, string id, StoryTrigger.Activation mode, string dialogueId, float delay, C[] conditions, params E[] effects)
    {
        var go = new GameObject("Trigger_" + id);
        go.layer = 2;   // Ignore Raycast: trigger volumes never block the interaction ray
        go.transform.SetParent(lv.systems.transform, false);
        var t = go.AddComponent<StoryTrigger>();
        t.triggerId = id; t.activation = mode; t.delay = delay;
        t.conditions = new List<C>(conditions ?? new C[0]);
        t.effects = new List<E>(effects);
        t.dialogue = string.IsNullOrEmpty(dialogueId) ? null : Assignment3Content.Get(dialogueId);
        t.activateObjects = new GameObject[0]; t.deactivateObjects = new GameObject[0];
        return t;
    }

    static Material Mat(string name, Color color, float emission = 0)
    {
        string path = Materials + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .35f);
        if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * emission); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ================================================================== cutscenes (Timeline + Cinemachine)
    struct ShotSpec { public Vector3 from, to, look; public float start, duration; }
    static ShotSpec Shot(Vector3 from, Vector3 to, Vector3 look, float start, float duration)
        => new ShotSpec { from = from, to = to, look = look, start = start, duration = duration };
    static CutsceneController.Subtitle Sub(float time, float duration, string speaker, string text)
        => new CutsceneController.Subtitle { time = time, duration = duration, speaker = speaker, text = text };

    /// <summary>
    /// Creates a Timeline whose Cinemachine track cuts between shot cameras. Each shot uses two
    /// cameras (start and end framing) whose clips overlap for the whole shot, so Cinemachine
    /// blends between them: a slow dolly move without any animation curves.
    /// </summary>
    static CutsceneController Cutscene(Level lv, string id, string flag, ShotSpec[] shots, CutsceneController.Subtitle[] subtitles)
    {
        var root = new GameObject("Cutscene_" + id);
        root.transform.SetParent(lv.systems.transform, false);
        var director = root.AddComponent<PlayableDirector>();
        director.playOnAwake = false; director.extrapolationMode = DirectorWrapMode.None;

        string path = Timelines + id + ".playable";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        var track = timeline.CreateTrack<CinemachineTrack>(null, "Shots");

        var brain = lv.game.playerCamera ? lv.game.playerCamera.GetComponent<CinemachineBrain>() : null;
        if (!brain && lv.game.playerCamera) brain = lv.game.playerCamera.gameObject.AddComponent<CinemachineBrain>();
        director.SetGenericBinding(track, brain);

        for (int i = 0; i < shots.Length; i++)
        {
            var s = shots[i];
            var a = ShotCamera(root.transform, id + "_Shot" + (i + 1) + "A", s.from, s.look);
            var b = ShotCamera(root.transform, id + "_Shot" + (i + 1) + "B", s.to, s.look);
            AddShot(track, director, a, s.start, s.duration * .55f);
            AddShot(track, director, b, s.start + .05f, s.duration - .05f);
        }
        EditorUtility.SetDirty(timeline);
        director.playableAsset = timeline;

        var cut = root.AddComponent<CutsceneController>();
        cut.cutsceneId = id; cut.director = director; cut.flagOnFinish = flag;
        cut.subtitles = new List<CutsceneController.Subtitle>(subtitles);
        cut.skipHint = lv.skipHint; cut.skipFill = lv.skipFill;
        cut.hideWhilePlaying = lv.legacyHud ? new[] { lv.legacyHud } : new GameObject[0];
        return cut;
    }

    static CinemachineCamera ShotCamera(Transform parent, string name, Vector3 position, Vector3 look)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position));
        var cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = -10;   // never live unless the Timeline asks for it
        var lens = cam.Lens; lens.FieldOfView = 48; lens.NearClipPlane = .05f; lens.FarClipPlane = 150; cam.Lens = lens;
        return cam;
    }

    static void AddShot(CinemachineTrack track, PlayableDirector director, CinemachineCamera cam, double start, double duration)
    {
        var clip = track.CreateClip<CinemachineShot>();
        clip.start = start; clip.duration = duration; clip.displayName = cam.name;
        var shot = (CinemachineShot)clip.asset;
        shot.VirtualCamera.exposedName = GUID.Generate().ToString();
        director.SetReferenceValue(shot.VirtualCamera.exposedName, cam);
    }

    // ================================================================== menu / loading / ending scenes
    static void BuildMainMenu()
    {
        string path = ScenePath("MainMenu");
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(Source, path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var game = Object.FindFirstObjectByType<JayGameManager>(FindObjectsInactive.Include);
        // The real world as a living backdrop at dusk: gameplay objects off, a drifting camera on.
        var lighting = game ? game.lighting : Object.FindFirstObjectByType<JayLightingController>();
        if (game)
        {
            if (game.player) game.player.gameObject.SetActive(false);
            if (game.playerCamera) game.playerCamera.gameObject.SetActive(false);
            game.gameObject.SetActive(false);
        }
        foreach (var cm in Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) cm.gameObject.SetActive(false);
        var hud = Find("HUD_Canvas"); if (hud) hud.gameObject.SetActive(false);
        AddStreetLampsToScene(lighting);

        var camGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>(); cam.fieldOfView = 52; cam.nearClipPlane = .1f; cam.farClipPlane = 200;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var backdrop = camGo.AddComponent<MenuBackdrop>();
        backdrop.cameraTransform = camGo.transform; backdrop.lighting = lighting;
        backdrop.fromPosition = new Vector3(-0.5f, 2.8f, -1.8f); backdrop.toPosition = new Vector3(3.2f, 2.3f, 1.2f);
        backdrop.lookAt = new Vector3(-4.5f, 1.3f, 9.5f); backdrop.cycleFraction = .43f;

        var canvas = UI.Canvas("MainMenu UI", 10).transform;
        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include)) NewEventSystem();
        UI.Img(canvas, "LeftShade", new Color(0, 0, 0, .62f), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(760, 0), false);
        UI.Txt(canvas, "Title", "JAY A CAREER", 92, UI.Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -150), new Vector2(900, 110), FontStyles.Bold);
        UI.Txt(canvas, "Tagline", "Three days. Three buyers. One career.", 32, UI.Accent, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(94, -262), new Vector2(900, 44), FontStyles.Italic);
        var col = UI.Column(canvas, "Buttons", new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -380), new Vector2(440, 360), 16);
        col.childAlignment = TextAnchor.UpperLeft;
        var menu = canvas.gameObject.AddComponent<MainMenuController>();
        menu.newGameButton = UI.Btn(col.transform, "NewGame", "New Game", new Vector2(440, 68), 30);
        menu.settingsButton = UI.Btn(col.transform, "Settings", "Settings", new Vector2(440, 68), 30);
        menu.creditsButton = UI.Btn(col.transform, "Credits", "Credits", new Vector2(440, 68), 30);
        menu.quitButton = UI.Btn(col.transform, "Quit", "Quit", new Vector2(440, 68), 30);
        UI.Txt(canvas, "Footer", "DMET 909 Video Game Design · Assignment 3 · Amr El Kotit", 18, UI.Muted, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(94, 34), new Vector2(900, 28));
        UI.Txt(canvas, "Controls", "WASD move · Mouse look · E interact · Tab inventory · J journal · F flashlight · V camera · Esc pause", 18, UI.Muted, TextAlignmentOptions.BottomLeft, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(94, 64), new Vector2(1200, 28));

        var credits = UI.Img(canvas, "CreditsPanel", new Color(.07f, .07f, .08f, .97f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000, 660));
        menu.creditsPanel = UI.Group(credits.gameObject, 0);
        UI.Txt(credits.transform, "Title", "CREDITS", 40, UI.Text, TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 56), FontStyles.Bold);
        UI.Txt(credits.transform, "Body",
            "<b>Design, story, code:</b> Amr El Kotit (with AI assistance)\n\n" +
            "<b>Engine:</b> Unity 6 · URP · Cinemachine 3 · Timeline · TextMeshPro · Input System\n\n" +
            "<b>Environment & props:</b> Poly Haven (CC0) · ModKit modular pack (CC0) · original Blender models\n\n" +
            "<b>Characters:</b> MakeHuman / MPFB (CC0 output) · Mixamo animations\n\n" +
            "<b>Audio:</b> see ASSET_CREDITS.md in the repository\n\n" +
            "All characters, places and products are fictional.",
            24, UI.Text, TextAlignmentOptions.TopLeft, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(40, -110), new Vector2(-80, 460));
        menu.creditsCloseButton = UI.Btn(credits.transform, "Close", "Back", new Vector2(220, 56));
        var crt = menu.creditsCloseButton.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(1, 0); crt.pivot = new Vector2(1, 0); crt.anchoredPosition = new Vector2(-40, 30);
        menu.settings = BuildSettingsPanel(canvas, col.gameObject);
        EditorSceneManager.SaveScene(scene);
    }

    static void AddStreetLampsToScene(JayLightingController lighting)
    {
        if (!lighting) return;
        var lamps = new List<Light>(); var bulbs = new List<Renderer>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("StreetLight"))
                {
                    lamps.AddRange(t.GetComponentsInChildren<Light>(true));
                    bulbs.AddRange(t.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains("Bulb")));
                }
        var ctl = lighting.gameObject.AddComponent<StreetLampController>();
        ctl.lighting = lighting; ctl.lamps = lamps.Distinct().ToArray(); ctl.bulbs = bulbs.Distinct().ToArray(); ctl.intensity = lighting.roadLightIntensity;
    }

    static Scene NewUIScene(string name, out Transform canvas)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.035f, .04f, .045f);
        camGo.tag = "MainCamera";
        NewEventSystem();
        canvas = UI.Canvas(name + " UI", 10).transform;
        UI.Img(canvas, "Accent", UI.Accent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 6), false);
        return scene;
    }

    static void BuildLoading()
    {
        var scene = NewUIScene("Loading", out var canvas);
        var ctl = canvas.gameObject.AddComponent<LoadingScreenController>();
        ctl.titleText = UI.Txt(canvas, "Title", "", 56, UI.Text, TextAlignmentOptions.Left, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, 0), new Vector2(180, 170), new Vector2(1500, 80), FontStyles.Bold);
        ctl.recapText = UI.Txt(canvas, "Recap", "", 30, UI.Text, TextAlignmentOptions.TopLeft, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, 1), new Vector2(180, 150), new Vector2(1300, 380));
        ctl.tipText = UI.Txt(canvas, "Tip", "", 22, UI.Muted, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(180, 150), new Vector2(1300, 34), FontStyles.Italic);
        var barBack = UI.Img(canvas, "ProgressBack", new Color(1, 1, 1, .1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(180, 110), new Vector2(1560, 10));
        ctl.progressFill = UI.Img(barBack.transform, "Fill", UI.Accent, Vector2.zero, Vector2.one, new Vector2(0, .5f), Vector2.zero, Vector2.zero, false);
        ctl.progressFill.type = Image.Type.Filled; ctl.progressFill.fillMethod = Image.FillMethod.Horizontal;
        ctl.progressFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        ctl.statusText = UI.Txt(canvas, "Status", "Loading...", 24, UI.Accent, TextAlignmentOptions.Right, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-180, 60), new Vector2(900, 34), FontStyles.Bold);
        EditorSceneManager.SaveScene(scene, ScenePath("Loading"));
    }

    static void BuildEnding()
    {
        var scene = NewUIScene("Ending", out var canvas);
        var ctl = canvas.gameObject.AddComponent<EndScreenController>();
        ctl.titleText = UI.Txt(canvas, "Title", "TO BE CONTINUED", 76, UI.Text, TextAlignmentOptions.Center, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(1400, 100), FontStyles.Bold);
        ctl.summaryText = UI.Txt(canvas, "Summary", "", 30, UI.Text, TextAlignmentOptions.Top, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -290), new Vector2(1300, 480));
        var row = UI.Rect(canvas, "Buttons", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 110), new Vector2(720, 70));
        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 30; hl.childAlignment = TextAnchor.MiddleCenter; hl.childControlWidth = false; hl.childForceExpandWidth = false;
        ctl.mainMenuButton = UI.Btn(row, "MainMenu", "Main Menu", new Vector2(320, 66), 28);
        ctl.quitButton = UI.Btn(row, "Quit", "Quit", new Vector2(320, 66), 28);
        EditorSceneManager.SaveScene(scene, ScenePath("Ending"));
    }
}
