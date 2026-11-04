using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NARRATIVE SCRIPT #1 — the single source of truth for story progress.
///
/// Stores three kinds of values that survive scene changes (DontDestroyOnLoad):
///   • flags   (bool)   — "has this beat happened?"  e.g. briefingDone, act1Complete
///   • ints             — status values shown on the HUD: cash, trust, heat, day
///   • texts            — choice results, e.g. lukaDeal = "pushed"
/// plus the player's lore inventory and journal.
///
/// Every other narrative system (dialogue, objectives, triggers, level transitions, HUD)
/// reads from here and listens to <see cref="Changed"/> / <see cref="FlagSet"/>, so the
/// story always has one consistent state. A snapshot is taken when each level starts so
/// "Restart Level" can rewind exactly to that point.
/// </summary>
public class StoryStateManager : MonoBehaviour
{
    // ---------------------------------------------------------------- singleton
    static StoryStateManager instance;
    static bool quitting;

    /// <summary>Global access. Creates the manager on demand so any level can be played directly from the Editor.</summary>
    public static StoryStateManager Instance
    {
        get
        {
            if (instance == null && !quitting)
            {
                instance = FindFirstObjectByType<StoryStateManager>();
                if (instance == null) instance = new GameObject("StoryStateManager").AddComponent<StoryStateManager>();
            }
            return instance;
        }
    }
    public static bool Exists => instance != null;

    // ---------------------------------------------------------------- events
    /// <summary>Raised after ANY value changes. UI and objectives refresh on this.</summary>
    public event Action Changed;
    /// <summary>Raised when a flag becomes true (the brief's "OnFlagSet" event).</summary>
    public event Action<string> FlagSet;

    // ---------------------------------------------------------------- data
    [Serializable] public class InventoryRecord { public string id, title, description; }
    [Serializable] public class JournalRecord { public string id, title, body; }

    /// <summary>Plain serializable container so the whole state can be snapshotted / saved as JSON.</summary>
    [Serializable]
    public class StoryData
    {
        public List<string> flags = new List<string>();
        public List<string> intKeys = new List<string>();
        public List<int> intValues = new List<int>();
        public List<string> textKeys = new List<string>();
        public List<string> textValues = new List<string>();
        public List<InventoryRecord> inventory = new List<InventoryRecord>();
        public List<JournalRecord> journal = new List<JournalRecord>();
    }

    readonly HashSet<string> flags = new HashSet<string>();
    readonly Dictionary<string, int> ints = new Dictionary<string, int>();
    readonly Dictionary<string, string> texts = new Dictionary<string, string>();
    readonly List<InventoryRecord> inventory = new List<InventoryRecord>();
    readonly List<JournalRecord> journal = new List<JournalRecord>();
    string levelSnapshot;

    public IReadOnlyList<InventoryRecord> Inventory => inventory;
    public IReadOnlyList<JournalRecord> Journal => journal;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (flags.Count == 0 && ints.Count == 0) ApplyNewGameDefaults();
    }
    void OnApplicationQuit() { quitting = true; }

    // ---------------------------------------------------------------- new game
    /// <summary>Starting values from the story bible: $50, trust 2/5, heat 0/5, day 1.</summary>
    public void ApplyNewGameDefaults()
    {
        flags.Clear(); ints.Clear(); texts.Clear(); inventory.Clear(); journal.Clear();
        ints[StoryKeys.Cash] = 50; ints[StoryKeys.Trust] = 2; ints[StoryKeys.Heat] = 0; ints[StoryKeys.Day] = 1;
        texts[StoryKeys.LukaDeal] = "";
        Notify();
    }

    public void StartNewGame()
    {
        ApplyNewGameDefaults();
        SetFlag(StoryKeys.GameStarted, true);
    }

    // ---------------------------------------------------------------- flags
    public bool GetFlag(string key) => !string.IsNullOrEmpty(key) && flags.Contains(key);

    public void SetFlag(string key, bool value = true)
    {
        if (string.IsNullOrEmpty(key)) return;
        bool changed = value ? flags.Add(key) : flags.Remove(key);
        if (!changed) return;              // setting an already-set flag never re-fires events
        if (value) FlagSet?.Invoke(key);
        Notify();
    }

    // ---------------------------------------------------------------- integers
    public int GetInt(string key) => key != null && ints.TryGetValue(key, out var v) ? v : 0;

    public void SetInt(string key, int value)
    {
        if (string.IsNullOrEmpty(key)) return;
        value = Clamp(key, value);
        if (ints.TryGetValue(key, out var old) && old == value) return;
        ints[key] = value; Notify();
    }

    public void AddInt(string key, int amount) => SetInt(key, GetInt(key) + amount);

    /// <summary>Trust and Heat are 0–5 meters; cash can never go negative.</summary>
    static int Clamp(string key, int value)
    {
        if (key == StoryKeys.Trust || key == StoryKeys.Heat) return Mathf.Clamp(value, 0, 5);
        if (key == StoryKeys.Cash) return Mathf.Max(0, value);
        return value;
    }

    // ---------------------------------------------------------------- texts
    public string GetText(string key) => key != null && texts.TryGetValue(key, out var v) ? v : "";

    public void SetText(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        value ??= "";
        if (texts.TryGetValue(key, out var old) && old == value) return;
        texts[key] = value; Notify();
    }

    // ---------------------------------------------------------------- inventory & journal
    public bool HasItem(string id) => inventory.Exists(i => i.id == id);

    public bool AddItem(string id, string title, string description)
    {
        if (string.IsNullOrEmpty(id) || HasItem(id)) return false;
        inventory.Add(new InventoryRecord { id = id, title = title, description = description });
        StoryHUD.Toast("Added to inventory: " + title + "   [Tab]");
        Notify(); return true;
    }

    public bool AddJournalEntry(string id, string title, string body)
    {
        if (string.IsNullOrEmpty(id) || journal.Exists(j => j.id == id)) return false;
        journal.Add(new JournalRecord { id = id, title = title, body = body });
        StoryHUD.Toast("Journal updated: " + title + "   [J]");
        Notify(); return true;
    }

    // ---------------------------------------------------------------- snapshot / save
    public StoryData ToData()
    {
        var d = new StoryData();
        d.flags.AddRange(flags);
        foreach (var kv in ints) { d.intKeys.Add(kv.Key); d.intValues.Add(kv.Value); }
        foreach (var kv in texts) { d.textKeys.Add(kv.Key); d.textValues.Add(kv.Value); }
        foreach (var i in inventory) d.inventory.Add(new InventoryRecord { id = i.id, title = i.title, description = i.description });
        foreach (var j in journal) d.journal.Add(new JournalRecord { id = j.id, title = j.title, body = j.body });
        return d;
    }

    public void FromData(StoryData d)
    {
        flags.Clear(); ints.Clear(); texts.Clear(); inventory.Clear(); journal.Clear();
        if (d != null)
        {
            foreach (var f in d.flags) flags.Add(f);
            for (int i = 0; i < d.intKeys.Count && i < d.intValues.Count; i++) ints[d.intKeys[i]] = d.intValues[i];
            for (int i = 0; i < d.textKeys.Count && i < d.textValues.Count; i++) texts[d.textKeys[i]] = d.textValues[i];
            inventory.AddRange(d.inventory); journal.AddRange(d.journal);
        }
        Notify();
    }

    /// <summary>Called by LevelDirector at the start of every level.</summary>
    public void TakeLevelSnapshot() => levelSnapshot = JsonUtility.ToJson(ToData());

    /// <summary>Rewinds to the moment the current level began (used by "Restart Level").</summary>
    public bool RestoreLevelSnapshot()
    {
        if (string.IsNullOrEmpty(levelSnapshot)) return false;
        FromData(JsonUtility.FromJson<StoryData>(levelSnapshot));
        return true;
    }

    /// <summary>Readable dump for debugging and for the GDD's state-flag screenshots.</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Flags: ").Append(string.Join(", ", flags)).Append('\n');
        foreach (var kv in ints) sb.Append(kv.Key).Append('=').Append(kv.Value).Append("  ");
        foreach (var kv in texts) sb.Append(kv.Key).Append("=\"").Append(kv.Value).Append("\"  ");
        return sb.ToString();
    }

    void Notify() => Changed?.Invoke();
}
