using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class RealismBuildEntry
{
 [MenuItem("Jay A Career/Build Final Windows Game")]
 public static void BuildWindows()
 {
  var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
  var output=Path.Combine(project,"Deliverables","Windows");
  var evidence=Path.Combine(project,"Deliverables","Evidence");
  Directory.CreateDirectory(output);Directory.CreateDirectory(evidence);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/JayACareer/Scenes/JayACareer_Realism.unity"},locationPathName=Path.Combine(output,"Jay A Career.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.DetailedBuildReport});
  var s=report.summary;
  File.WriteAllText(Path.Combine(evidence,"windows-build-summary.json"),JsonUtility.ToJson(new Summary{result=s.result.ToString(),size=(long)s.totalSize,seconds=(float)s.totalTime.TotalSeconds,errors=(int)s.totalErrors,warnings=(int)s.totalWarnings},true));
  if(s.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+s.result);
 }
 [Serializable]public class Summary{public string result;public long size;public float seconds;public int errors,warnings;}
}
