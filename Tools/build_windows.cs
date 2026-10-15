using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json;
public static class WindowsBuild
{
 public static string Main()
 {
  Directory.CreateDirectory("Deliverables/Windows");
  var o=new BuildPlayerOptions{scenes=new[]{"Assets/JayACareer/Scenes/JayACareer_Realism.unity"},locationPathName="Deliverables/Windows/Jay A Career.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.DetailedBuildReport};
  {
   try {var r=BuildPipeline.BuildPlayer(o);var s=r.summary;File.WriteAllText("Deliverables/Evidence/windows-build-summary.json",JsonConvert.SerializeObject(new {result=s.result.ToString(),size=s.totalSize,seconds=s.totalTime.TotalSeconds,errors=s.totalErrors,warnings=s.totalWarnings,path=s.outputPath},Formatting.Indented));}
   catch(Exception e){File.WriteAllText("Deliverables/Evidence/windows-build-summary.json",JsonConvert.SerializeObject(new {error=e.ToString()},Formatting.Indented));}
  }
  return File.ReadAllText("Deliverables/Evidence/windows-build-summary.json");
 }
}
