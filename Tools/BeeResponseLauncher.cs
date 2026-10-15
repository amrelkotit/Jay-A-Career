using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

// Project-local diagnostic launcher. Only generated Library/Bee graphs are changed.
// Every metadata argument/input/output is retained; Unity still executes the generator.
public static class BeeResponseLauncher
{
 public static int Main(string[] args)
 {
  try
  {
   string raw=Environment.CommandLine;
   int end=raw.StartsWith("\"")?raw.IndexOf('"',1)+1:raw.IndexOf(' ');
   string tail=end<0?"":raw.Substring(end).TrimStart();
   string dag=args.FirstOrDefault(a=>a.StartsWith("--dagfile="));
   string jsonArg=args.FirstOrDefault(a=>a.StartsWith("--dagfilejson="));
   if(dag!=null)
   {
    string jsonPath=jsonArg==null?dag.Substring("--dagfile=".Length)+".json":jsonArg.Substring("--dagfilejson=".Length);
    string root=Path.GetFullPath("Library/Bee")+Path.DirectorySeparatorChar;
    string full=Path.GetFullPath(jsonPath);
    if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Graph must stay inside this project's Library/Bee.");
    // Only patch a graph Unity has just explicitly handed to Bee. Forcing an older
    // JSON graph on a normal incremental launch can retain an obsolete source list.
    if(jsonArg!=null&&File.Exists(full))
    {
     var serializer=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=512};
     var graph=(Dictionary<string,object>)serializer.DeserializeObject(File.ReadAllText(full));
     bool changed=false;
     foreach(Dictionary<string,object> node in (IEnumerable)graph["Nodes"])
     {
      object actionObject;
      if(!node.TryGetValue("Action",out actionObject))continue;
      string action=actionObject as string;
      if(action==null||!action.Contains("BuildPlayerDataGenerator.exe")||action.Contains(" @\""))continue;
      var match=Regex.Match(action,"^(\"[^\"]+\"\\s+\"[^\"]+BuildPlayerDataGenerator\\.exe\")\\s+(.+)$",RegexOptions.Singleline);
      if(!match.Success)throw new InvalidOperationException("Unexpected metadata command format.");
      string rspPath="Library/Bee/jay-metadata-"+Path.GetFileNameWithoutExtension(full).Replace('.','-')+".rsp";
      var tokens=Regex.Matches(match.Groups[2].Value,"(?:[^\"\\s]+|\"[^\"]*\")+").Cast<Match>().Select(m=>m.Value);
      string contents=String.Join("\n",tokens)+"\n";
      if(!File.Exists(rspPath)||File.ReadAllText(rspPath)!=contents)File.WriteAllText(rspPath,contents,new UTF8Encoding(false));
      node["Action"]=match.Groups[1].Value+" @\""+rspPath+"\"";
      var inputs=new ArrayList((ICollection)node["Inputs"]);inputs.Add(rspPath);node["Inputs"]=inputs.ToArray();
      var flags=new ArrayList((ICollection)node["InputFlags"]);flags.Add(0);node["InputFlags"]=flags.ToArray();
      changed=true;
      Console.WriteLine("JAY RESPONSE FILE: "+node["Annotation"]+"; command "+action.Length+" -> "+((string)node["Action"]).Length+" characters; inputs and outputs retained.");
     }
     if(changed)File.WriteAllText(full,serializer.Serialize(graph),new UTF8Encoding(false));
    }
   }
   var info=new ProcessStartInfo(@"E:\Setups\Unity installs\6000.3.24f1\Editor\Data\Tools\BuildPipeline\bee_backend.exe",tail);
   info.UseShellExecute=false;
   using(var process=Process.Start(info)){process.WaitForExit();return process.ExitCode;}
  }
  catch(Exception error){Console.Error.WriteLine(error);return 2;}
 }
}
