$taskProject='E:\El Gam3a\JayCareerFinal20261013'
$taskLog=Join-Path $taskProject 'finish-fast-build.log'
$taskStart=Get-Date
$taskProcess=Start-Process -FilePath 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' -ArgumentList ('-batchmode -quit -projectPath "'+$taskProject+'" -executeMethod RealismBuildEntry.BuildWindows -logFile "'+$taskLog+'"') -WorkingDirectory $taskProject -WindowStyle Hidden -PassThru
$taskStopped=$false
while(-not $taskProcess.WaitForExit(1000)) {
 $taskElapsed=(Get-Date)-$taskStart
 $taskStalled=$false
 if(Test-Path -LiteralPath $taskLog){$taskInfo=Get-Item -LiteralPath $taskLog;$taskTail=(Get-Content -LiteralPath $taskLog -Tail 8)-join ' ';$taskStalled=($taskElapsed.TotalSeconds -gt 100 -and ((Get-Date)-$taskInfo.LastWriteTime).TotalSeconds -gt 90 -and $taskTail -match 'ScriptAssembliesAndTypeDB|BuildPlayerDataGenerator')}
 if($taskElapsed.TotalSeconds -gt 300 -or $taskStalled){
  $taskAll=Get-CimInstance Win32_Process;$taskIds=@([int]$taskProcess.Id)
  do{$taskChildren=@($taskAll|Where-Object {$_.ParentProcessId -in $taskIds -and $_.ProcessId -notin $taskIds});$taskIds+=@($taskChildren|ForEach-Object {[int]$_.ProcessId})}while($taskChildren.Count -gt 0)
  foreach($taskId in $taskIds){Stop-Process -Id $taskId -ErrorAction SilentlyContinue}
  $taskStopped=$true;break
 }
}
$taskResult=@{project=$taskProject;log=$taskLog;seconds=((Get-Date)-$taskStart).TotalSeconds;stoppedAfterStallOrTimeLimit=$taskStopped;success=$false}
$taskSummary=Join-Path $taskProject 'Deliverables\Evidence\windows-build-summary.json'
if(-not $taskStopped -and (Test-Path -LiteralPath $taskSummary) -and (Get-Item -LiteralPath $taskSummary).LastWriteTime -ge $taskStart){$taskResult.success=((Get-Content -Raw -LiteralPath $taskSummary|ConvertFrom-Json).result -eq 'Succeeded')}
$taskResult|ConvertTo-Json|Set-Content -Encoding UTF8 -LiteralPath 'E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career\Deliverables\Evidence\bounded-final-build-attempt.json'
$taskResult|ConvertTo-Json
