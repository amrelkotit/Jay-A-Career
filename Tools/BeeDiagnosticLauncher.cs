using System;
using System.Diagnostics;
public static class BeeDiagnosticLauncher
{
 public static int Main(string[] args)
 {
  // Forward Unity's original command line and inherited IPC handles unchanged.
  string raw=Environment.CommandLine;
  int end=raw.StartsWith("\"")?raw.IndexOf('"',1)+1:raw.IndexOf(' ');
  string tail=end<0?"":raw.Substring(end).TrimStart();
  var info=new ProcessStartInfo(@"E:\Setups\Unity installs\6000.3.24f1\Editor\Data\Tools\BuildPipeline\bee_backend.exe", "--unprotected "+tail);
  info.UseShellExecute=false;
  using(var process=Process.Start(info)){process.WaitForExit();return process.ExitCode;}
 }
}
