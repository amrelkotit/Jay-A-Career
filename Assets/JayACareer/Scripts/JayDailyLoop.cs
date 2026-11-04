using System;

/// <summary>Session-only rules. No scene, input or payment side effects live here.</summary>
[Serializable]
public sealed class JayDailyLoop
{
    public enum Stage { Briefing, Preparation, Delivery, Negotiation, Return, Cleaning, Sleep }
    public enum PackageLocation { None, Bench, Carried, World, Counter, Returned, Sold }
    public enum Deal { Accept, Counter, Reject }
    public Stage Current { get; private set; } = Stage.Briefing;
    public PackageLocation Package { get; private set; }
    public int Day { get; private set; } = 1;
    public int Cash { get; private set; } = 100;
    public int PackageId { get; private set; }
    public bool Dirty { get; private set; }
    public bool NegotiationResolved { get; private set; }
    public string Feedback { get; private set; } = "Talk to Dr. Hale to begin.";
    public string Objective => Current switch
    {
        Stage.Briefing => "Talk to Dr. Hale [E].",
        Stage.Preparation => "Prepare one sealed package at the preparation bench [E].",
        Stage.Delivery => "Carry the package [E] to the customer counter; aim at the counter and place it [E].",
        Stage.Negotiation => "Talk to the customer [E]; choose one offer.",
        Stage.Return => "Return the unsold package to the laboratory return shelf [E].",
        Stage.Cleaning => "Clean the marked workspace at the sink [E].",
        _ => "Sleep at the bedroom bed [E] to begin another day."
    };
    bool Deny(string text) { Feedback = text; return false; }
    public bool Brief()
    {
        if (Current != Stage.Briefing) return Deny(Objective);
        Current = Stage.Preparation; Feedback = "Prepare, deliver, settle the deal, clean, then rest."; return true;
    }
    public bool Prepare()
    {
        if (Current != Stage.Preparation) return Deny(Objective);
        PackageId++; Package = PackageLocation.Bench; Dirty = true;
        Current = Stage.Delivery; Feedback = "One sealed delivery package is ready."; return true;
    }
    public bool PickUp()
    {
        if (Package == PackageLocation.None || Package == PackageLocation.Sold || Package == PackageLocation.Returned || Package == PackageLocation.Carried)
            return Deny("No available delivery package to pick up.");
        Package = PackageLocation.Carried;
        if (Current == Stage.Negotiation) Current = Stage.Delivery;
        Feedback = "Package picked up."; return true;
    }
    public bool Drop(PackageLocation destination)
    {
        if (Package != PackageLocation.Carried) return Deny("Pick up the package first.");
        if (destination != PackageLocation.Bench && destination != PackageLocation.World && destination != PackageLocation.Counter && destination != PackageLocation.Returned)
            return Deny("That is not a valid placement surface.");
        if (destination == PackageLocation.Returned && Current != Stage.Return) return Deny("The return shelf is only needed after an unsuccessful deal.");
        Package = destination;
        if (destination == PackageLocation.Counter && Current == Stage.Delivery) Current = Stage.Negotiation;
        if (destination == PackageLocation.Returned) Current = Stage.Cleaning;
        Feedback = destination == PackageLocation.Counter ? "Package placed at the customer handoff position." : "Package placed safely.";
        return true;
    }
    public bool CanNegotiate => Current == Stage.Negotiation && Package == PackageLocation.Counter && !NegotiationResolved;
    public bool Resolve(Deal deal, double randomValue)
    {
        if (!CanNegotiate) return Deny(Objective);
        NegotiationResolved = true;
        bool sold = deal == Deal.Accept || (deal == Deal.Counter && randomValue < .5);
        if (sold)
        {
            int payment = deal == Deal.Accept ? 50 : 75;
            Cash += payment; Package = PackageLocation.Sold; Current = Stage.Cleaning;
            Feedback = "Sale complete. Payment received: $" + payment + ".";
        }
        else { Current = Stage.Return; Feedback = "No sale and no payment. Return this package to the lab shelf."; }
        return true;
    }
    // ---------------------------------------------------------------- Assignment 3 story hooks
    /// <summary>Story levels set the day and the cash carried over from earlier levels.</summary>
    public void SetDay(int day) { Day = Math.Max(1, day); }
    public void SetCash(int cash) { Cash = Math.Max(0, cash); }

    /// <summary>
    /// Deterministic deal used by the story levels: the buyer's dialogue choice decides the
    /// outcome and payment (no 50% coin flip), so a choice always has the consequence the
    /// player read on screen.
    /// </summary>
    public bool ResolveStory(bool sold, int payment)
    {
        if (!CanNegotiate) return Deny(Objective);
        NegotiationResolved = true;
        if (sold)
        {
            Cash += Math.Max(0, payment); Package = PackageLocation.Sold; Current = Stage.Cleaning;
            Feedback = "Sale complete. Payment received: $" + payment + ".";
        }
        else { Current = Stage.Return; Feedback = "No sale. Return the package to the lab shelf."; }
        return true;
    }

    public bool Clean()
    {
        if (Current != Stage.Cleaning) return Deny(Objective);
        Dirty = false; Current = Stage.Sleep; Feedback = "Workspace cleaned. You can now sleep."; return true;
    }
    public bool Sleep()
    {
        if (Current != Stage.Sleep) return Deny(Objective);
        Day++; Current = Stage.Briefing; Package = PackageLocation.None; NegotiationResolved = false;
        Feedback = "Day " + Day + " begins. Talk to Dr. Hale."; return true;
    }
    public bool Recover()
    {
        if (Package == PackageLocation.None || Package == PackageLocation.Returned || Package == PackageLocation.Sold || Package == PackageLocation.Carried)
            return Deny("There is no lost package to recover.");
        Package = PackageLocation.Bench;
        if (Current == Stage.Negotiation) Current = Stage.Delivery;
        Feedback = "The same delivery package was recovered at the preparation bench."; return true;
    }
}
