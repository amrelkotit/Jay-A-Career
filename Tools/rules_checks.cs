using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
public static class DailyRulesChecks
{
    public static string Main()
    {
        var results=new List<string>();
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAILED: "+name);results.Add(name);};
        foreach(var outcome in new[]{"accept","counterSuccess","counterRefusal","reject"})
        {
            var s=new JayDailyLoop();
            check(!s.Sleep(),outcome+": early sleep denied");
            check(!s.Clean(),outcome+": early cleaning denied");
            check(!s.Prepare(),outcome+": briefing required");
            check(!s.Resolve(JayDailyLoop.Deal.Accept,0),outcome+": package required");
            check(s.Brief()&&s.Prepare(),outcome+": briefing and preparation");
            int id=s.PackageId;
            check(!s.Prepare()&&s.PackageId==id,outcome+": no duplicate preparation");
            check(s.Recover()&&s.PackageId==id,outcome+": recovery preserves package identity");
            check(s.PickUp()&&s.Drop(JayDailyLoop.PackageLocation.World),outcome+": safe world placement");
            check(!s.CanNegotiate,outcome+": loose package cannot negotiate");
            check(s.PickUp()&&s.Drop(JayDailyLoop.PackageLocation.Counter)&&s.CanNegotiate,outcome+": counter delivery");
            var deal=outcome=="accept"?JayDailyLoop.Deal.Accept:outcome=="reject"?JayDailyLoop.Deal.Reject:JayDailyLoop.Deal.Counter;
            check(s.Resolve(deal,outcome=="counterRefusal"?.5:0),outcome+": settlement");
            int expected=outcome=="accept"?150:outcome=="counterSuccess"?175:100;
            check(s.Cash==expected,outcome+": correct payment");
            check(!s.Resolve(JayDailyLoop.Deal.Accept,0)&&s.Cash==expected,outcome+": duplicate payment denied");
            if(expected==100)
            {
                check(!s.Clean(),outcome+": return required before cleaning");
                check(s.PickUp()&&s.Drop(JayDailyLoop.PackageLocation.Returned),outcome+": unsold package returned");
            }
            check(s.Clean()&&!s.Dirty&&s.Sleep(),outcome+": completed day");
            check(s.Day==2&&s.Cash==expected&&s.Package==JayDailyLoop.PackageLocation.None&&!s.NegotiationResolved,outcome+": next-day reset and retained cash");
            check(s.Brief()&&s.Prepare()&&s.PackageId==id+1,outcome+": second distinct daily package");
            check(s.PickUp()&&s.Drop(JayDailyLoop.PackageLocation.Counter)&&s.Resolve(JayDailyLoop.Deal.Accept,0)&&s.Clean()&&s.Sleep()&&s.Day==3,outcome+": two consecutive completed days");
        }
        var report=new {passed=results.Count,failed=0,checks=results};
        Directory.CreateDirectory("Deliverables/Evidence");File.WriteAllText("Deliverables/Evidence/daily-rules-tests.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return results.Count+" daily-rule assertions passed.";
    }
}
