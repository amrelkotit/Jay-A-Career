using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using C = StoryCondition;
using E = StoryEffect;

/// <summary>
/// Authoring script for the Assignment 3 story CONTENT (character profiles and every
/// conversation). It writes ScriptableObject assets into Assets/JayACareer/Story so the
/// dialogue lives as data, editable in the Inspector afterwards. Running it again updates
/// the same assets in place (GUIDs are kept, so scene references never break).
/// The text follows Docs/Story Bible.md (beat table §9, choice consequences §8).
/// </summary>
public static class Assignment3Content
{
    public const string Root = "Assets/JayACareer/Story/";
    public const string DialogueFolder = Root + "Dialogue/";
    public const string CharacterFolder = Root + "Characters/";

    public static CharacterProfile Jay, Hale, Leo, Luka, Paul;
    static readonly Dictionary<string, DialogueData> all = new Dictionary<string, DialogueData>();

    public static DialogueData Get(string id) => all.TryGetValue(id, out var d) ? d : null;

    // ------------------------------------------------------------------ helpers
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static CharacterProfile Profile(string file, string name, string hex, string bio)
    {
        var p = LoadOrCreate<CharacterProfile>(CharacterFolder + file + ".asset");
        p.displayName = name;
        ColorUtility.TryParseHtmlString(hex, out p.nameColor);
        p.shortBio = bio;
        EditorUtility.SetDirty(p);
        return p;
    }

    /// <summary>Creates/updates a conversation asset and clears its old content.</summary>
    static DialogueData D(string id, bool playOnce = false)
    {
        var d = LoadOrCreate<DialogueData>(DialogueFolder + id + ".asset");
        d.id = id; d.playOnce = playOnce;
        d.lines.Clear(); d.choices.Clear(); d.onComplete.Clear();
        d.loopActionOnComplete = DialogueLoopAction.None;
        EditorUtility.SetDirty(d);
        all[id] = d;
        return d;
    }

    static DialogueData L(this DialogueData d, CharacterProfile who, string text, params C[] conditions)
    {
        d.lines.Add(new DialogueLine { speaker = who, text = text, conditions = new List<C>(conditions) });
        return d;
    }

    static DialogueChoice Choice(this DialogueData d, string text, DialogueLoopAction action = DialogueLoopAction.None, int payment = 0, DialogueData next = null, params E[] effects)
    {
        var c = new DialogueChoice { text = text, loopAction = action, payment = payment, next = next, effects = new List<E>(effects) };
        d.choices.Add(c);
        return c;
    }

    // ------------------------------------------------------------------ build
    public static void Build()
    {
        foreach (var f in new[] { Root, DialogueFolder, CharacterFolder })
            if (!AssetDatabase.IsValidFolder(f.TrimEnd('/'))) Directory.CreateDirectory(f);
        AssetDatabase.Refresh();
        all.Clear();

        Jay = Profile("Jay", "Jay", "#8FC7FF", "23, broke, three days from eviction. The player.");
        Hale = Profile("DrHale", "Dr. Hale", "#E9C46A", "58, former senior chemist at Corvane. Precise, protective, secretive.");
        Leo = Profile("Leo", "Leo", "#9BE39B", "Nervous small-time buyer. Pays in small bills, talks too much.");
        Luka = Profile("Luka", "Luka", "#F28C8C", "Sharp, impatient middleman who collects for someone bigger.");
        Paul = Profile("Paul", "Paul", "#C9B6FF", "Calm, polite, frightening. The buyer at the top.");

        BuildLevel1();
        BuildLevel2();
        BuildLevel3();
        AssetDatabase.SaveAssets();
    }

    // ================================================================== LEVEL 1 — First Delivery
    static void BuildLevel1()
    {
        D("L1_HaleBriefing", true)
            .L(Hale, "You're Jay. You're early. Good. Nobody in this town is early.")
            .L(Jay, "You said there was work. My rent's due in three days.")
            .L(Hale, "The work is simple. You seal the order at the preparation bench, carry it across the road to the counter, and take the money.")
            .L(Hale, "Then you come back, clean the bench, and go home. You do not open the cartons. You do not talk about the cartons.")
            .L(Jay, "What's in them?")
            .L(Hale, "An order. That is all it ever needs to be.")
            .L(Hale, "Today's buyer is Leo. He is nervous, he talks too much, and he pays. Don't keep him waiting.")
            .L(Hale, "And Jay... my desk is not part of the job.");
        var briefing = Get("L1_HaleBriefing");
        briefing.loopActionOnComplete = DialogueLoopAction.CompleteBriefing;
        briefing.onComplete.Add(E.Set(StoryKeys.BriefingDone));

        D("L1_HaleWork").L(Hale, "The bench, Jay. Seal it, carry it, get paid. In that order.");
        D("L1_HaleAfterSale").L(Hale, "Leo paid? Good. Clean the bench before you go. I don't like evidence of a good day.");
        D("L1_HaleDesk", true)
            .L(Jay, "Corvane Industrial Chemicals... Senior chemist. That's you?")
            .L(Hale, "That was somebody with my name. Put it back. Or keep it. It doesn't open any doors any more.");

        D("L1_BadgeThought").L(Jay, "\"E. Hale, Senior Chemist.\" So what is a senior chemist doing in a shed in Calder Flats?");
        D("L1_CertificateThought").L(Jay, "A safety award. Turned face-down, glass cracked. Somebody didn't want to look at this.");
        D("L1_BillsThought").L(Jay, "Electricity, overdue. Water, overdue. And three red circles on the calendar. Great.");

        var after = D("L1_LeoAfter").L(Leo, "Pleasure. Tell Hale I said... nothing. Tell him nothing. Bye.");
        var hint = D("L1_LeoHint")
            .L(Leo, "Who else? Man, I don't... look. Everybody knows Hale's stuff is the only stuff Paul's people don't touch.")
            .L(Jay, "Who's Paul?")
            .L(Leo, "Nobody. Forget I said it. Can we, uh, can we just do this?");
        hint.onComplete.Add(E.Journal("paul", "A name: Paul", "Leo let a name slip: Paul. Whoever he is, \"Paul's people\" leave Hale's orders alone. Leo looked scared just saying it."));
        hint.Choice("Count the money and hand over the carton.", DialogueLoopAction.SellPackage, 100, after, E.Set(StoryKeys.LeoPaid));

        var deal = D("L1_LeoDeal")
            .L(Leo, "You're the new guy? Hale never hires anyone. Huh. Okay.")
            .L(Leo, "Is that mine? Sealed and everything. Hale's stuff is always sealed. That's why people trust it.")
            .L(Jay, "A hundred, right? That's what he said.")
            .L(Leo, "Yeah, yeah, a hundred, it's all here. Small bills, sorry. It's how they pay me.");
        deal.Choice("Count the money and hand over the carton.", DialogueLoopAction.SellPackage, 100, after, E.Set(StoryKeys.LeoPaid));
        deal.Choice("\"Who else buys from Hale?\"", DialogueLoopAction.None, 0, hint);

        D("L1_LeoWaiting").L(Leo, "My ride's late. Paul's people are never late. ...Forget I said that.");
        D("L1_LeoBefore").L(Leo, "Put it on the counter first. Nothing goes hand to hand. Hale's rule, not mine.");

        D("L1_EndOfDay", true)
            .L(Jay, "A hundred bucks for carrying a box across a road.")
            .L(Jay, "Nobody makes money that easy. Nobody honest, anyway.");
    }

    // ================================================================== LEVEL 2 — The Middleman
    static void BuildLevel2()
    {
        var warn = D("L2_HaleWarning", true)
            .L(Hale, "Before you seal anything: today is different.")
            .L(Hale, "Today's buyer is Luka. Luka is not a customer, Jay. Luka collects for someone else.")
            .L(Jay, "Collects for who?")
            .L(Hale, "For people who notice things. He'll offer you a price. He'll expect you to argue, and he'll expect you to lose.")
            .L(Hale, "Take his price, push for more, or send him away. It's your counter today. I want to see what you do with it.")
            .L(Jay, "And if I get it wrong?")
            .L(Hale, "Then we both find out what kind of man I hired.");
        warn.loopActionOnComplete = DialogueLoopAction.CompleteBriefing;
        warn.onComplete.Add(E.Set(StoryKeys.LukaWarned));

        D("L2_HaleWork").L(Hale, "Seal it. Carry it. Luka doesn't like waiting, and I don't like Luka.");
        D("L2_HaleAfter").L(Hale, "Clean the bench. Go home. Sleep, if you can.");

        var accepted = D("L2_LukaAccepted")
            .L(Luka, "Smart. Smart and quiet. People like quiet.")
            .L(Luka, "Hold on. That's my phone. Don't touch anything.");
        var pushed = D("L2_LukaPushed")
            .L(Luka, "...Two-fifty. You've got nerve, new boy.")
            .L(Luka, "Fine. But nerve gets noticed. Remember I told you that.")
            .L(Luka, "Hold on. That's my phone.");
        var refused = D("L2_LukaRefused")
            .L(Luka, "You're refusing me? Me?")
            .L(Luka, "Okay. Okay. Somebody is going to hear about this, and it isn't going to be Hale.")
            .L(Luka, "Hold on. That's my phone.");

        var deal = D("L2_LukaDeal", true)
            .L(Luka, "So you're the old man's new boy. Look at you. Clean shoes. That won't last.")
            .L(Luka, "Here's how this goes. I give you a hundred and fifty for that carton. You say thank you.")
            .L(Jay, "Hale said you'd say that.")
            .L(Luka, "Hale says a lot of things to people who work for him. So? We doing this?");
        deal.Choice("Accept $150.", DialogueLoopAction.SellPackage, 150, accepted,
            E.Text(StoryKeys.LukaDeal, "accepted"), E.Add(StoryKeys.Heat, 1), E.Set(StoryKeys.LukaDealt));
        deal.Choice("Push: \"Two-fifty, or it goes back on the shelf.\"", DialogueLoopAction.SellPackage, 250, pushed,
            E.Text(StoryKeys.LukaDeal, "pushed"), E.Add(StoryKeys.Trust, -1), E.Add(StoryKeys.Heat, 2), E.Set(StoryKeys.LukaDealt));
        deal.Choice("Refuse: \"Not today, Luka.\"", DialogueLoopAction.RefusePackage, 0, refused,
            E.Text(StoryKeys.LukaDeal, "refused"), E.Add(StoryKeys.Trust, 2), E.Add(StoryKeys.Heat, 1), E.Set(StoryKeys.LukaDealt));

        D("L2_LukaBefore").L(Luka, "The carton goes on the counter. Then we talk. That's how grown-ups do business.");
        D("L2_LukaWaiting").L(Luka, "Don't look at my phone.");
        D("L2_PhoneThought").L(Jay, "\"P.\" ...Paul? Leo said Paul's people don't touch Hale's orders. So why is Paul asking about me?");

        var accept = C.Text(StoryKeys.LukaDeal, "accepted");
        var push = C.Text(StoryKeys.LukaDeal, "pushed");
        var refuse = C.Text(StoryKeys.LukaDeal, "refused");
        var reaction = D("L2_HaleReaction", true)
            .L(Hale, "He paid what he offered? Fine. Clean. Forgettable. Good.", accept)
            .L(Hale, "Forgettable is how men like me stay alive out here.", accept)
            .L(Hale, "Two hundred and fifty.", push)
            .L(Hale, "You made us visible, Jay. Do you understand what that means out here? It means somebody writes our names down.", push)
            .L(Hale, "You sent Luka away.", refuse)
            .L(Hale, "...That took more nerve than you know. More than I had at your age.", refuse)
            .L(Jay, "Luka got a message. From someone who signs as \"P.\"", C.Flag(StoryKeys.PhoneNoteFound))
            .L(Hale, "Don't say that letter inside my lab again. Clean the bench. Go home.", C.Flag(StoryKeys.PhoneNoteFound))
            .L(Hale, "Clean up. Go home. Tomorrow will be... different.", C.NotFlag(StoryKeys.PhoneNoteFound));
        reaction.onComplete.Add(E.Set(StoryKeys.HaleReacted));

        D("L2_EndOfDay", true)
            .L(Jay, "\"P.\" Leo went pale saying that name. Hale went pale hearing it.", C.Flag(StoryKeys.PhoneNoteFound))
            .L(Jay, "Whoever P. is, my name is on his phone now.", C.Flag(StoryKeys.PhoneNoteFound))
            .L(Jay, "Luka is going to tell somebody about me tonight. I just don't know who.", C.NotFlag(StoryKeys.PhoneNoteFound));
    }

    // ================================================================== LEVEL 3 — The Big Buyer
    static void BuildLevel3()
    {
        D("L3_JayWakes", true)
            .L(Jay, "Midnight, and Hale's lab is dark. Three days, and I've never seen his lights off.")
            .L(Jay, "And that car has been parked out there for an hour.");
        D("L3_LabEmpty", true)
            .L(Jay, "Dr. Hale? ...His coat is still on the hook. He never goes anywhere without that coat.")
            .L(Jay, "Something's wrong. I should look around. Flashlight first. [F]");
        D("L3_LedgerThought").L(Jay, "Eight years. Every month, a payment to \"P.\" Never the other way round.");
        D("L3_LetterThought").L(Jay, "Clara. His daughter? He wrote this and never sent it.");
        D("L3_LockboxThought").L(Jay, "Four hundred and eighty thousand dollars. That's not a debt. That's a cage.");
        D("L3_Footsteps", true).L(Jay, "Footsteps. Someone's at the door.");

        var accept = C.Text(StoryKeys.LukaDeal, "accepted");
        var push = C.Text(StoryKeys.LukaDeal, "pushed");
        var refuse = C.Text(StoryKeys.LukaDeal, "refused");
        var confession = D("L3_HaleConfession", true)
            .L(Hale, "You read them. Of course you read them. I would have.")
            .L(Hale, "Corvane. Eleven years ago a reactor line failed. A young engineer made the mistake. I signed the report that said it was mine.")
            .L(Hale, "They took my licence and gave me a settlement I could never pay. Paul bought my debt. I have been paying him in cartons ever since.")
            .L(Hale, "You kept things quiet with Luka. I thought that meant you understood. It means you are good at this. That frightens me.", accept)
            .L(Hale, "And yesterday you squeezed his collector for an extra hundred. Paul doesn't forget a hundred dollars, Jay. He doesn't forget anything.", push)
            .L(Hale, "And yesterday you said no to Luka. I hired you so there would be someone between Paul and me. I never meant it to be someone braver than me.", refuse)
            .L(Jay, "So why did you hire me?")
            .L(Hale, "Because tonight's order goes out in person. Paul is coming himself.")
            .L(Hale, "Seal it. Carry it. Be polite. Then go home and forget his face.");
        confession.loopActionOnComplete = DialogueLoopAction.CompleteBriefing;
        confession.onComplete.Add(E.Set(StoryKeys.ConfessionHeard));

        D("L3_HaleWork").L(Hale, "Seal it, Jay. Before he gets impatient. Men like Paul are very patient right up until they aren't.");
        D("L3_PaulBefore").L(Paul, "Take your time. Put it on the counter when you are ready. I'm a patient man.");

        var take = D("L3_PaulTakes").L(Paul, "Good. Count it if you like. I never miss.");
        var aboutHale = D("L3_PaulAboutHale")
            .L(Paul, "Hale owes me. Hale will always owe me. That isn't about you.")
            .L(Paul, "Unless you want it to be.");
        aboutHale.Choice("Take the $200.", DialogueLoopAction.SellPackage, 200, take, E.Set(StoryKeys.PaulMet));

        var deal = D("L3_PaulDeal", true)
            .L(Paul, "Luka says you're easy to work with. Easy is useful.", accept)
            .L(Paul, "You squeezed my man for an extra hundred. Bold. Expensive.", push)
            .L(Paul, "The kid who said no to Luka. I respect a man who knows his price.", refuse)
            .L(Paul, "I'm Paul. You've heard the name, I think. Most people hear it before they meet me.")
            .L(Paul, "Two hundred, for the carton. And something else. For you.")
            .L(Jay, "Something else?");
        deal.Choice("Take the $200.", DialogueLoopAction.SellPackage, 200, take, E.Set(StoryKeys.PaulMet));
        deal.Choice("\"What do you want with Hale?\"", DialogueLoopAction.None, 0, aboutHale);
    }
}
