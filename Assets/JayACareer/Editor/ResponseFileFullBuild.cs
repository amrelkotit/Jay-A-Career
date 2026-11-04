using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Opt-in machine-specific build diagnosis. Does not run when opening or playing the project.
public static class ResponseFileFullBuild
{
 public static void Execute()
 {BuildTo("Windows Fresh 20261015","fresh-windows-build-20261015.json");}
 public static void BuildTo(string folder,string reportName)
 {
  string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
  string launcher=Path.Combine(project,"Tools","BeeResponseLauncher.exe");
  if(!File.Exists(launcher))throw new FileNotFoundException("Compile Tools/BeeResponseLauncher.cs first.",launcher);
  var driver=typeof(EditorApplication).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.UnityBeeDriver",true);
  var field=driver.GetField("BeeBackendExecutable",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
  var original=field.GetValue(null);
  string output=Path.Combine(project,"Deliverables",folder,"Jay A Career.exe");
  Directory.CreateDirectory(Path.GetDirectoryName(output));
  try
  {
   field.SetValue(null,launcher);
   Debug.Log("JAY FULL BUILD: original Unity generator using response-file arguments; full scene build to a separate destination.");
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/JayACareer/Scenes/JayACareer_Realism.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.DetailedBuildReport});
   var s=report.summary;
   string json=JsonUtility.ToJson(new RealismBuildEntry.Summary{result=s.result.ToString(),size=(long)s.totalSize,seconds=(float)s.totalTime.TotalSeconds,errors=(int)s.totalErrors,warnings=(int)s.totalWarnings},true);
   File.WriteAllText(Path.Combine(project,"Deliverables","Evidence",reportName),json);
   if(s.result!=BuildResult.Succeeded)throw new Exception("Full Windows build: "+s.result);
  }
  finally{field.SetValue(null,original);}
 }
}
