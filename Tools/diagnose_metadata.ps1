$projectRoot='E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career'
$commandText=Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Tools\build-helper-command.txt')
$tokens=[regex]::Matches($commandText,'(?:[^"\s]+|"[^"]*")+') | ForEach-Object { $_.Value.Replace('"','') }
$info=[Diagnostics.ProcessStartInfo]::new()
$info.FileName=$tokens[0]
$info.WorkingDirectory=$projectRoot
$info.UseShellExecute=$false
$info.CreateNoWindow=$true
$info.RedirectStandardOutput=$true
$info.RedirectStandardError=$true
$info.Arguments=$commandText.Substring($commandText.IndexOf('"',1)+1).Trim()
$proc=[Diagnostics.Process]::Start($info)
$stdout=$proc.StandardOutput.ReadToEndAsync()
$stderr=$proc.StandardError.ReadToEndAsync()
$samples=@()
for($i=0;$i -lt 6;$i++){
 if($proc.WaitForExit(5000)){break}
 $proc.Refresh()
 $samples+=@{seconds=5*($i+1);cpuSeconds=$proc.TotalProcessorTime.TotalSeconds;memoryBytes=$proc.WorkingSet64;threads=@($proc.Threads | ForEach-Object { @{id=$_.Id;state=$_.ThreadState.ToString();wait=if($_.ThreadState -eq 'Wait'){$_.WaitReason.ToString()}else{''}} })}
}
$timedOut=-not $proc.HasExited
if($timedOut){$proc.Kill($true);$proc.WaitForExit()}
$report=@{timedOut=$timedOut;exitCode=$proc.ExitCode;samples=$samples;stdout=$stdout.Result;stderr=$stderr.Result}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $projectRoot 'Deliverables\Evidence\metadata-direct-diagnostic.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
