using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEST / CAPTURE SCRIPT — plays the whole game from the main menu to the end screen using the
/// same functions the player's keys call (talk, prepare, carry, place, choose, clean, sleep).
///
///   Validate mode : fast run that CHECKS every story beat, flag, cash/trust/heat value and
///                   transition for one Luka branch, then writes a PASS/FAIL report.
///   Cinematic mode: the same route at walking speed in third person, with reading pauses,
///                   screenshots of each beat and a timestamp log, used to record the video.
///
/// Started from the Editor menu (Jay A Career ▸ Assignment 3 ▸ ...). It never runs in a normal
/// game: it only boots when the Editor leaves a request in PlayerPrefs.
/// </summary>
public class Assignment3AutoPilot : MonoBehaviour
{
    public const string RequestKey = "jay.autopilot", DoneKey = "jay.autopilot.done";
    enum Mode { Validate, Cinematic }

    Mode mode;
    string branch;          // accepted / pushed / refused
    readonly StringBuilder report = new StringBuilder();
    readonly StringBuilder timestamps = new StringBuilder();
    int failures, checks;
    float startTime;
    string outputDir;
    bool Cine => mode == Mode.Cinematic;

    // ------------------------------------------------------------------ boot
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string request = PlayerPrefs.GetString(RequestKey, "");
        if (string.IsNullOrEmpty(request)) return;
        PlayerPrefs.DeleteKey(RequestKey); PlayerPrefs.SetInt(DoneKey, 0); PlayerPrefs.Save();
        var parts = request.Split('|');
        var go = new GameObject("Assignment3AutoPilot");
        DontDestroyOnLoad(go);
        var pilot = go.AddComponent<Assignment3AutoPilot>();
        pilot.mode = parts[0] == "cinematic" ? Mode.Cinematic : Mode.Validate;
        pilot.branch = parts.Length > 1 ? parts[1] : "accepted";
        pilot.outputDir = parts.Length > 2 ? parts[2] : Path.Combine(Application.dataPath, "..", "AutoPilot");
        pilot.StartCoroutine(pilot.Run());
    }

    // ------------------------------------------------------------------ main route
    IEnumerator Run()
    {
        startTime = Time.realtimeSinceStartup;
        Directory.CreateDirectory(outputDir);
        Log("AutoPilot " + mode + " branch=" + branch + " " + DateTime.Now);
        LoadingScreenController.AutoContinueAfter = Cine ? 6f : 0f;

        if (SceneManager.GetActiveScene().name == LevelTransitionManager.MainMenuScene)
        {
            Beat("Main menu");
            yield return Pause(5f, .3f);
            Shot("00_main_menu");
            yield return Captured();
            var menu = FindFirstObjectByType<MainMenuController>();
            Check(menu.chaptersButton && menu.chapters.Length == 3, "Main menu offers New Game and the three chapters in story order");
            Check(Playing("MenuMusic"), "Main menu theme is playing");
            if (Cine && menu.chaptersButton)
            {
                menu.chaptersButton.onClick.Invoke();
                yield return Pause(2.5f, .1f);
                Shot("00b_chapters");
                yield return Captured();
                if (menu.chaptersCloseButton) menu.chaptersCloseButton.onClick.Invoke();
                yield return Pause(.8f, .1f);
            }
            menu.NewGame();
        }
        else StoryStateManager.Instance.StartNewGame();

        yield return WaitScene(LevelTransitionManager.LoadingScene, 20, true);
        if (SceneManager.GetActiveScene().name == LevelTransitionManager.LoadingScene) { Beat("Loading screen: Day 1 recap"); yield return Pause(1.2f, .1f); Shot("01_loading_day1"); }
        yield return WaitScene(LevelTransitionManager.Level1, 60);
        yield return Level1();
        yield return WaitScene(LevelTransitionManager.LoadingScene, 30, true);
        if (SceneManager.GetActiveScene().name == LevelTransitionManager.LoadingScene) { Beat("Loading screen: Day 2 recap"); yield return Pause(1.2f, .1f); Shot("12_loading_day2"); }
        yield return WaitScene(LevelTransitionManager.Level2, 60);
        yield return Level2();
        yield return WaitScene(LevelTransitionManager.LoadingScene, 30, true);
        if (SceneManager.GetActiveScene().name == LevelTransitionManager.LoadingScene) { Beat("Loading screen: Day 3 recap"); yield return Pause(1.2f, .1f); Shot("24_loading_day3"); }
        yield return WaitScene(LevelTransitionManager.Level3, 60);
        yield return Level3();
        yield return WaitScene(LevelTransitionManager.EndingScene, 90);
        yield return Pause(2.5f, .5f);
        var end = FindFirstObjectByType<EndScreenController>();
        Check(end && end.summaryText && end.summaryText.text.Contains("Tomorrow"), "End screen shows the summary and the cliffhanger line");
        Beat("End screen: consequences summary");
        Shot("40_end_screen");
        yield return Pause(6f, .2f);
        Finish();
    }

    // ================================================================== LEVEL 1
    IEnumerator Level1()
    {
        yield return LevelStart("Day 1 chapter");
        var s = StoryStateManager.Instance;
        Beat("Opening cutscene (Timeline + Cinemachine)");
        yield return CutsceneShots("02_intro", 2);
        yield return WaitFor(() => s.GetFlag(StoryKeys.IntroSeen), 40, "Intro cutscene finishes and sets introSeen");
        Check(s.HasItem("eviction"), "Eviction notice added to the inventory by the intro");
        yield return SettleAfterCutscene();
        Shot("03_hud_objective");
        Check(!string.IsNullOrEmpty(ObjectiveText()), "Objective shown after the intro: " + ObjectiveText());

        yield return Go("HALE");
        Beat("Dr. Hale's briefing");
        yield return Talk(G.hale, "04_hale_briefing");
        Check(s.GetFlag(StoryKeys.BriefingDone) && G.Loop.Current == JayDailyLoop.Stage.Preparation, "Briefing sets briefingDone and starts preparation");

        yield return Go("INSTR");
        Beat("Lore: Hale's Corvane badge");
        yield return Collect("badge", "05_badge");
        Check(s.GetFlag(StoryKeys.BadgeFound) && s.Journal.Count >= 1, "Badge sets badgeFound and adds a journal entry");
        if (Cine) { yield return OpenNotebook(false, "06_inventory"); }

        yield return Go("GLASS");
        yield return Collect("certificate", null);

        yield return Prepare();
        Check(s.GetFlag(StoryKeys.CartonSealed), "Sealing the carton sets cartonSealed");
        yield return Deliver();
        Beat("Leo's deal (asks about Paul first)");
        yield return Talk(G.customer, "07_leo_deal");
        Check(s.GetFlag(StoryKeys.LeoPaid) && s.GetInt(StoryKeys.Cash) == 150, "Leo pays $100 (cash $150)");
        Check(s.Journal.Count >= 2, "Leo's slip about Paul is added to the journal");
        if (Cine) yield return OpenNotebook(true, "08_journal");

        yield return Go("BEDDESK");
        yield return Collect("bills", null);
        yield return Clean();
        yield return Sleep("L1");
        yield return WaitFor(() => s.GetFlag(StoryKeys.Act1Complete), 15, "Sleeping sets act1Complete");
    }

    // ================================================================== LEVEL 2
    IEnumerator Level2()
    {
        yield return LevelStart("Day 2 chapter");
        var s = StoryStateManager.Instance;
        Check(s.GetInt(StoryKeys.Cash) == 150 && s.GetFlag(StoryKeys.Act1Complete), "Level 1 progress carried into Level 2");
        Shot("13_day2_afternoon");
        yield return Go("HALE");
        Beat("Hale warns about Luka");
        yield return Talk(G.hale, "14_hale_warning");
        Check(s.GetFlag(StoryKeys.LukaWarned), "Warning sets lukaWarned");
        yield return Prepare();
        yield return Deliver();
        Beat("Dusk falls: street lamps switch on");
        yield return Pause(8f, 1f);
        Shot("15_dusk_lamps");
        Beat("Luka's deal — meaningful choice: " + branch);
        yield return Talk(G.customer, "16_luka_choice");
        Check(s.GetText(StoryKeys.LukaDeal) == branch, "lukaDeal recorded as " + branch);
        int cash = s.GetInt(StoryKeys.Cash), trust = s.GetInt(StoryKeys.Trust), heat = s.GetInt(StoryKeys.Heat);
        if (branch == "accepted") Check(cash == 300 && trust == 2 && heat == 1, "Accept: $300, trust 2, heat 1 (got $" + cash + ", " + trust + ", " + heat + ")");
        if (branch == "pushed") Check(cash == 400 && trust == 1 && heat == 2, "Push: $400, trust 1, heat 2 (got $" + cash + ", " + trust + ", " + heat + ")");
        if (branch == "refused") Check(cash == 150 && trust == 4 && heat == 1 && G.Loop.Current == JayDailyLoop.Stage.Return, "Refuse: $150, trust 4, heat 1, carton must go back (got $" + cash + ", " + trust + ", " + heat + ")");
        Shot("17_status_after_choice");

        Beat("Clue: the \"P.\" message on Luka's phone");
        yield return WaitFor(() => Playing("PhoneBuzz"), 3, "Luka's phone buzzes on the counter (sound cue)");
        yield return Collect("phone", "18_phone");
        Check(s.GetFlag(StoryKeys.PhoneNoteFound), "Phone sets phoneNoteFound");
        yield return WaitFor(() => !Playing("PhoneBuzz"), 3, "The phone stops buzzing once the message is read");
        if (branch == "refused") yield return ReturnCarton();
        yield return Go("HALE");
        Beat("Hale reacts to the choice");
        yield return Talk(G.hale, "19_hale_reaction");
        Check(s.GetFlag(StoryKeys.HaleReacted), "Hale's reaction sets haleReacted");
        yield return Clean();
        yield return Sleep("L2");
        yield return WaitFor(() => s.GetFlag(StoryKeys.Act2Complete), 15, "Sleeping sets act2Complete");
    }

    // ================================================================== LEVEL 3
    IEnumerator Level3()
    {
        yield return LevelStart("Day 3 chapter");
        var s = StoryStateManager.Instance;
        Check(s.GetText(StoryKeys.LukaDeal) == branch, "Choice carried into Level 3");
        Check(!G.hale.gameObject.activeInHierarchy && !G.customer.gameObject.activeInHierarchy, "Hale and Paul are absent at the start of the night");
        Check(!G.lighting.WorkspaceLightsOn && !Playing("LabHum"), "Hale's lab is dark and silent");
        Check(PlayerPrefs.GetInt(MainMenuController.ChapterKey, 1) >= 3, "Day 3 is unlocked in the chapter list");
        yield return FlushDialogue("25_jay_wakes");
        Beat("Jay walks to the dark lab");
        yield return Go("LAB_A");
        yield return WaitFor(() => s.GetFlag(StoryKeys.HaleMissingSeen), 12, "Entering the lab sets haleMissingSeen");
        yield return FlushDialogue("26_lab_empty");
        if (!G.lighting.FlashlightOn) G.lighting.ToggleFlashlight();
        Beat("Flashlight search: ledger, letter, lockbox");
        yield return Go("LEDGER"); yield return Collect("ledger", "27_ledger");
        yield return Go("GLASS"); yield return Collect("letter", null);
        yield return Go("LOCKBOX"); yield return Collect("lockbox", "28_lockbox");
        yield return WaitFor(() => s.GetFlag(StoryKeys.NotesFound), 10, "Three clues set notesFound");
        yield return FlushDialogue(null);
        yield return WaitFor(() => G.hale.gameObject.activeInHierarchy, 5, "Hale returns after the clues are found");
        yield return WaitFor(() => G.lighting.WorkspaceLightsOn && Playing("LabHum"), 4, "Hale's return switches the lab lights (and their hum) back on");
        yield return Go("HALE3");
        Beat("Hale's confession (varies with the Luka choice)");
        yield return Talk(G.hale, "29_confession");
        Check(s.GetFlag(StoryKeys.ConfessionHeard), "Confession sets confessionHeard");
        yield return WaitFor(() => G.customer.gameObject.activeInHierarchy, 6, "Paul arrives after the confession");
        if (G.lighting.FlashlightOn) G.lighting.ToggleFlashlight();
        yield return Prepare();
        yield return Deliver();
        Shot("30_paul_headlights");
        Beat("Paul's deal and offer");
        int before = s.GetInt(StoryKeys.Cash);
        yield return Talk(G.customer, "31_paul");
        Check(s.GetFlag(StoryKeys.PaulMet) && s.GetInt(StoryKeys.Cash) == before + 200, "Paul pays $200 and sets paulMet");
        Beat("Closing cutscene: the card and Hale in the doorway");
        yield return WaitFor(() => CutsceneController.IsPlaying, 8, "Ending cutscene starts after Paul's conversation");
        yield return CutsceneShots("32_ending", 3);
    }

    // ================================================================== building blocks
    JayGameManager game;
    JayGameManager G => game ? game : game = FindFirstObjectByType<JayGameManager>();
    JayPlayerController Player => G.player;

    IEnumerator LevelStart(string what)
    {
        game = null;
        yield return null; yield return null;
        Check(G != null, SceneManager.GetActiveScene().name + " loaded with its game manager");
        Player.enabled = false;                   // the autopilot drives the character
        G.SetPrompt("");                          // no stale prompt from the disabled controller
        foreach (var d in FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None)) d.Open(G);
        var rig = Player.GetComponent<JayCameraRig>();
        if (Cine && rig && !rig.IsThirdPerson) rig.ToggleView(G);
        if (Player.bodyVisual) Player.bodyVisual.gameObject.SetActive(rig && rig.IsThirdPerson);
        Beat(SceneManager.GetActiveScene().name + " starts");
    }

    IEnumerator WaitScene(string scene, float timeout, bool optional = false)
    {
        float t = 0;
        while (SceneManager.GetActiveScene().name != scene && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        if (SceneManager.GetActiveScene().name != scene && !optional) Fail("Scene " + scene + " did not load within " + timeout + "s");
        yield return null;
    }

    IEnumerator WaitFor(Func<bool> condition, float timeout, string description)
    {
        float t = 0;
        while (!condition() && t < timeout)
        {
            if (DialogueManager.IsOpen) yield return Converse(null);
            t += Time.deltaTime; yield return null;
        }
        Check(condition(), description);
    }

    IEnumerator Pause(float cinematicSeconds, float validateSeconds) { yield return new WaitForSeconds(Cine ? cinematicSeconds : validateSeconds); }

    IEnumerator CutsceneShots(string prefix, int count)
    {
        yield return new WaitForSeconds(.5f);
        if (!CutsceneController.IsPlaying) yield break;
        Check(AnyMusicPlaying(), "Cutscene " + prefix + " plays its score");
        if (!Cine) { yield return new WaitForSeconds(.5f); Check(true, "Cutscene " + prefix + " playing"); CutsceneController.SkipActive(); yield return null; yield break; }
        for (int i = 0; i < count && CutsceneController.IsPlaying; i++) { yield return new WaitForSeconds(4.5f); Shot(prefix + "_" + (i + 1)); }
        while (CutsceneController.IsPlaying) yield return null;
    }

    IEnumerator SettleAfterCutscene() { yield return Pause(4.5f, .3f); }

    // ---------------------------------------------------------------- dialogue
    int ChoiceFor(string dialogueId)
    {
        switch (dialogueId)
        {
            case "L1_LeoDeal": return 1;                 // ask who else buys from Hale first
            case "L2_LukaDeal": return branch == "pushed" ? 1 : branch == "refused" ? 2 : 0;
            case "L3_PaulDeal": return 1;                // ask what he wants with Hale
            default: return 0;
        }
    }

    IEnumerator Talk(TalkableCharacter who, string shot)
    {
        yield return FlushDialogue(null);
        if (who) yield return Face(Head(who), .6f);
        G.TalkTo(who);
        yield return null;
        Check(DialogueManager.IsOpen, "Conversation with " + (who ? who.Name : "?") + " opens");
        yield return Converse(shot);
    }

    IEnumerator Converse(string shot)
    {
        var dm = DialogueManager.Instance;
        bool shotTaken = shot == null, choiceShot = shot == null;
        int lines = 0;
        while (DialogueManager.IsOpen)
        {
            if (dm.AwaitingChoice)
            {
                int index = Mathf.Min(ChoiceFor(dm.CurrentDialogueId), dm.VisibleChoiceCount - 1);
                yield return Pause(2.6f, .05f);
                if (!choiceShot && Cine) { Shot(shot + "_choices"); shotTaken = choiceShot = true; yield return Captured(); }
                Log("   choice in " + dm.CurrentDialogueId + ": option " + (index + 1));
                dm.ChooseOption(index);
                yield return null; continue;
            }
            if (dm.IsTyping) { if (!Cine) dm.Advance(); yield return null; continue; }
            float read = Cine ? 1.3f + dm.CurrentText.Length * .045f : .03f;
            yield return new WaitForSeconds(read);
            lines++;
            if (!shotTaken && lines == 2 && Cine) { Shot(shot); shotTaken = true; yield return Captured(); }
            dm.Advance();
            yield return null;
        }
        if (!shotTaken && Cine) Shot(shot);
        yield return Pause(.6f, .05f);
    }

    IEnumerator FlushDialogue(string shot)
    {
        float t = 0;
        while (!DialogueManager.IsOpen && t < (Cine ? 3.5f : 2.5f)) { t += Time.deltaTime; yield return null; }
        if (DialogueManager.IsOpen) yield return Converse(shot);
    }

    // ---------------------------------------------------------------- loop actions
    IEnumerator Prepare()
    {
        yield return Go("BENCH");
        yield return Face(G.preparationSlot, .4f);
        Beat("Seal the order at the preparation bench");
        G.Prepare();
        yield return WaitFor(() => !G.IsPerformingAction, 15, "Preparation action finishes");
        Check(G.Loop.Current == JayDailyLoop.Stage.Delivery, "Carton sealed; stage = Delivery");
    }

    IEnumerator Deliver()
    {
        G.BeginCarry(G.deliveryPackage);
        Check(G.CarriedObject == G.deliveryPackage, "Jay picks up the sealed carton");
        Beat("Carry the carton across the road");
        yield return Go("COUNTER");
        yield return Face(G.counterSlot, .4f);
        G.PlacePackageAt(JayDailyLoop.PackageLocation.Counter);
        Check(G.Loop.CanNegotiate, "Carton placed on the counter; negotiation available");
        yield return Pause(1f, .1f);
    }

    IEnumerator ReturnCarton()
    {
        G.BeginCarry(G.deliveryPackage);
        Check(G.CarriedObject == G.deliveryPackage, "Refused carton picked up again");
        yield return Go("SHELF");
        yield return Face(G.returnSlot, .4f);
        G.PlacePackageAt(JayDailyLoop.PackageLocation.Returned);
        Check(G.Loop.Current == JayDailyLoop.Stage.Cleaning, "Refused carton returned to the shelf");
    }

    IEnumerator Clean()
    {
        yield return FlushDialogue(null);
        yield return Go("SINK");
        Beat("Clean the bench");
        G.CleanWorkspace();
        yield return WaitFor(() => !G.IsPerformingAction, 15, "Cleaning action finishes");
        Check(G.Loop.Current == JayDailyLoop.Stage.Sleep, "Workspace clean; stage = Sleep");
    }

    IEnumerator Sleep(string level)
    {
        yield return FlushDialogue(level == "L1" ? "09_end_of_day1" : "20_end_of_day2");
        yield return Go("BED");
        Beat("Sleep: the day ends");
        G.Sleep();
        yield return WaitFor(() => !G.IsPerformingAction, 15, "Sleep action finishes");
    }

    IEnumerator Collect(string itemId, string shot)
    {
        LoreItem item = null;
        foreach (var l in FindObjectsByType<LoreItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (l.itemId == itemId) item = l;
        if (!item) { Fail("Lore item " + itemId + " not found"); yield break; }
        yield return Face(item.transform, .5f);
        if (shot != null && Cine) { yield return new WaitForSeconds(.4f); Shot(shot + "_object"); }
        item.Interact(G);
        Check(StoryStateManager.Instance.HasItem(itemId), "Lore item collected: " + item.title);
        yield return FlushDialogue(shot);
        yield return Pause(1.2f, .05f);
    }

    IEnumerator OpenNotebook(bool journal, string shot)
    {
        var nb = NotebookUI.Instance;
        if (!nb) { Fail("Notebook UI missing"); yield break; }
        nb.Show(journal);
        yield return new WaitForSeconds(2.2f);
        Shot(shot);
        yield return new WaitForSeconds(1.5f);
        nb.Close();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ---------------------------------------------------------------- navigation (simple waypoint graph of the site)
    static readonly Dictionary<string, Vector3> Nodes = new Dictionary<string, Vector3>
    {
        { "OUT_LAB", new Vector3(-7f, 0, 3.6f) }, { "LAB_A", new Vector3(-7f, 0, 6.6f) }, { "LAB_B", new Vector3(-7.6f, 0, 9.0f) },
        { "LAB_C", new Vector3(-7.6f, 0, 12.6f) }, { "LAB_D", new Vector3(-7.2f, 0, 13.7f) }, { "LAB_E", new Vector3(-6.2f, 0, 11.6f) },
        { "ROAD", new Vector3(-1f, 0, 4.2f) }, { "ROAD_N", new Vector3(5.5f, 0, 11.0f) }, { "COUNTER", new Vector3(8.6f, 0, 12.75f) },
        { "BED_OUT", new Vector3(7.7f, 0, 4.2f) }, { "BED_IN", new Vector3(7.7f, 0, 6.7f) }, { "BED", new Vector3(11.4f, 0, 8.0f) },
        // destinations (leaf spots)
        { "HALE", new Vector3(-6.85f, 0, 11.15f) }, { "HALE3", new Vector3(-6.75f, 0, 8.35f) }, { "BENCH", new Vector3(-10.4f, 0, 13.45f) },
        { "SINK", new Vector3(-4.0f, 0, 8.45f) }, { "SHELF", new Vector3(-4.0f, 0, 11.4f) }, { "INSTR", new Vector3(-5.0f, 0, 13.7f) },
        { "LEDGER", new Vector3(-3.4f, 0, 13.7f) }, { "GLASS", new Vector3(-8.35f, 0, 10.4f) }, { "LOCKBOX", new Vector3(-3.25f, 0, 11.15f) },
        { "BEDDESK", new Vector3(9.4f, 0, 8.9f) },
    };
    static readonly string[][] Edges =
    {
        new[] { "OUT_LAB", "LAB_A" }, new[] { "LAB_A", "LAB_B" }, new[] { "LAB_B", "LAB_C" }, new[] { "LAB_C", "LAB_D" },
        new[] { "LAB_B", "LAB_E" }, new[] { "LAB_C", "LAB_E" }, new[] { "OUT_LAB", "ROAD" }, new[] { "ROAD", "ROAD_N" },
        new[] { "ROAD_N", "COUNTER" }, new[] { "ROAD", "BED_OUT" }, new[] { "BED_OUT", "BED_IN" }, new[] { "BED_IN", "BED" },
        new[] { "LAB_B", "HALE" }, new[] { "LAB_B", "HALE3" }, new[] { "LAB_C", "BENCH" }, new[] { "LAB_A", "SINK" },
        new[] { "LAB_E", "SHELF" }, new[] { "LAB_D", "INSTR" }, new[] { "LAB_D", "LEDGER" }, new[] { "LAB_B", "GLASS" },
        new[] { "LAB_E", "LOCKBOX" }, new[] { "BED_IN", "BEDDESK" }, new[] { "BEDDESK", "BED" },
    };

    IEnumerator Go(string destination)
    {
        var path = FindPath(Nearest(Player.transform.position), destination);
        foreach (var node in path) yield return WalkTo(Nodes[node]);
    }

    string Nearest(Vector3 p)
    {
        string best = "OUT_LAB"; float bestD = float.MaxValue;
        foreach (var kv in Nodes)
        {
            float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(kv.Value.x, kv.Value.z));
            if (d < bestD) { bestD = d; best = kv.Key; }
        }
        return best;
    }

    static List<string> FindPath(string from, string to)
    {
        var previous = new Dictionary<string, string> { { from, null } };
        var queue = new Queue<string>(); queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue(); if (n == to) break;
            foreach (var e in Edges)
            {
                string other = e[0] == n ? e[1] : e[1] == n ? e[0] : null;
                if (other != null && !previous.ContainsKey(other)) { previous[other] = n; queue.Enqueue(other); }
            }
        }
        var path = new List<string>();
        if (!previous.ContainsKey(to)) { path.Add(to); return path; }
        for (var n = to; n != null; n = previous[n]) path.Insert(0, n);
        return path;
    }

    IEnumerator WalkTo(Vector3 target)
    {
        var cc = Player.GetComponent<CharacterController>();
        float speed = Cine ? 2.7f : 9f, stuck = 0;
        Vector3 last = Player.transform.position;
        while (true)
        {
            Vector3 p = Player.transform.position;
            Vector3 flat = new Vector3(target.x - p.x, 0, target.z - p.z);
            if (flat.magnitude < .2f) break;
            Quaternion want = Quaternion.LookRotation(flat);
            Player.transform.rotation = Quaternion.RotateTowards(Player.transform.rotation, want, (Cine ? 240 : 900) * Time.deltaTime);
            SetPitch(Mathf.MoveTowards(CurrentPitch(), 4, 60 * Time.deltaTime));
            Vector3 move = flat.normalized * Mathf.Min(speed * Time.deltaTime, flat.magnitude) + Vector3.down * 4 * Time.deltaTime;
            if (cc && cc.enabled) cc.Move(move); else Player.transform.position += move;
            Player.SetPresentationMovement(1);
            stuck = (Player.transform.position - last).sqrMagnitude < .00002f ? stuck + Time.deltaTime : 0;
            last = Player.transform.position;
            if (stuck > 1.2f)
            {
                Log("   (stuck near " + p + " -> teleport to " + target + ")");
                if (cc) cc.enabled = false; Player.transform.position = new Vector3(target.x, p.y, target.z); if (cc) cc.enabled = true;
                break;
            }
            yield return null;
        }
        Player.SetPresentationMovement(0);
    }

    IEnumerator Face(Transform target, float seconds)
    {
        if (!target) yield break;
        Vector3 to = target.position - Player.transform.position;
        Quaternion yaw = Quaternion.LookRotation(new Vector3(to.x, 0, to.z).sqrMagnitude > .0001f ? new Vector3(to.x, 0, to.z) : Player.transform.forward);
        var pivot = Player.GetComponent<JayCameraRig>().LookPivot;
        Vector3 fromEye = target.position - pivot.position;
        float pitch = -Mathf.Atan2(fromEye.y, new Vector2(fromEye.x, fromEye.z).magnitude) * Mathf.Rad2Deg;
        float t = 0, dur = Cine ? seconds : .02f;
        Quaternion start = Player.transform.rotation; float p0 = CurrentPitch();
        while (t < dur)
        {
            t += Time.deltaTime; float k = Mathf.SmoothStep(0, 1, t / dur);
            Player.transform.rotation = Quaternion.Slerp(start, yaw, k);
            SetPitch(Mathf.Lerp(p0, Mathf.Clamp(pitch, -70, 70), k));
            yield return null;
        }
        Player.transform.rotation = yaw; SetPitch(Mathf.Clamp(pitch, -70, 70));
    }

    float CurrentPitch() { var p = Player.GetComponent<JayCameraRig>().LookPivot.localEulerAngles.x; return p > 180 ? p - 360 : p; }
    void SetPitch(float pitch) { Player.GetComponent<JayCameraRig>().LookPivot.localEulerAngles = new Vector3(pitch, 0, 0); }
    static Transform Head(TalkableCharacter c) => c.headTarget ? c.headTarget : c.transform;
    static string ObjectiveText() => StoryHUD.Instance && StoryHUD.Instance.objectiveText ? StoryHUD.Instance.objectiveText.text : "";

    // ---------------------------------------------------------------- reporting
    void Check(bool ok, string what)
    {
        checks++;
        if (!ok) failures++;
        Log((ok ? "PASS  " : "FAIL  ") + what);
    }
    void Fail(string what) => Check(false, what);

    static bool Playing(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (!go) return false;
        var a = go.GetComponent<AudioSource>();
        return a && a.isPlaying;
    }

    static bool AnyMusicPlaying()
    {
        foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (a.isPlaying && a.name.StartsWith("Music_")) return true;
        return false;
    }

    void Beat(string what)
    {
        float t = Time.realtimeSinceStartup - startTime;
        string stamp = ((int)(t / 60)).ToString("00") + ":" + ((int)(t % 60)).ToString("00");
        timestamps.Append(stamp).Append("  ").Append(what).Append('\n');
        Log("[" + stamp + "] " + what);
    }

    /// <summary>ScreenCapture writes at the end of the frame: wait for it before changing what is on screen.</summary>
    IEnumerator Captured() { yield return new WaitForEndOfFrame(); yield return null; }

    void Shot(string name)
    {
        if (!Cine || string.IsNullOrEmpty(name)) return;
        string dir = Path.Combine(outputDir, "Screenshots");
        Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
    }

    void Log(string line) { report.Append(line).Append('\n'); Debug.Log("AUTOPILOT " + line); }

    void Finish()
    {
        string summary = (failures == 0 ? "RESULT: PASS" : "RESULT: FAIL") + " — " + (checks - failures) + "/" + checks + " checks passed";
        Log(summary);
        File.WriteAllText(Path.Combine(outputDir, "autopilot-" + mode.ToString().ToLower() + "-" + branch + ".txt"), report.ToString());
        if (Cine) File.WriteAllText(Path.Combine(outputDir, "timestamps-" + branch + ".txt"), timestamps.ToString());
        LoadingScreenController.AutoContinueAfter = -1;
        PlayerPrefs.SetInt(DoneKey, 1); PlayerPrefs.Save();
        Destroy(gameObject);
    }
}
