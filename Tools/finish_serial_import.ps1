$taskProject='E:\El Gam3a\JayCareerPackagedReview20261013'
$taskLog=Join-Path $taskProject 'serial-import-build.log'
$taskStarted=Get-Date
$taskProcess=Start-Process -FilePath 'E:\Setups\Unity installs\6000.3.24f1\Editor\Unity.exe' -ArgumentList ('-batchmode -quit -refreshImportMode InProcess -job-worker-count 2 -projectPath "'+$taskProject+'" -executeMethod FinishVerifiedBuild.Execute -logFile "'+$taskLog+'"') -WorkingDirectory $taskProject -WindowStyle Hidden -PassThru
$taskStopped=$false
while(-not $taskProcess.WaitForExit(1000)){
 $taskElapsed=((Get-Date)-$taskStarted).TotalSeconds;$taskStall=$false
 if(Test-Path -LiteralPath $taskLog){$taskInfo=Get-Item -LiteralPath $taskLog;$taskTail=(Get-Content -LiteralPath $taskLog -Tail 8)-join ' ';$taskStall=($taskElapsed -gt 100 -and ((Get-Date)-$taskInfo.LastWriteTime).TotalSeconds -gt 90 -and $taskTail -match 'ScriptAssembliesAndTypeDB|BuildPlayerDataGenerator')}
 if($taskElapsed -gt 300 -or $taskStall){
  $taskAll=Get-CimInstance Win32_Process;$taskIds=@([int]$taskProcess.Id)
  do{$taskChildren=@($taskAll|Where-Object {$_.ParentProcessId -in $taskIds -and $_.ProcessId -notin $taskIds});$taskIds+=@($taskChildren|ForEach-Object {[int]$_.ProcessId})}while($taskChildren.Count -gt 0)
  foreach($taskId in $taskIds){Stop-Process -Id $taskId -ErrorAction SilentlyContinue}
  $taskStopped=$true;break
 }
}
$taskOpened=(Select-String -LiteralPath $taskLog -Pattern 'Saved realism scene reopened successfully' -Quiet)
$taskBuilt=$false;$taskSummary=Join-Path $taskProject 'Deliverables\Evidence\windows-build-summary.json'
if(-not $taskStopped -and (Test-Path -LiteralPath $taskSummary) -and (Get-Item -LiteralPath $taskSummary).LastWriteTime -ge $taskStarted){$taskBuilt=((Get-Content -Raw -LiteralPath $taskSummary|ConvertFrom-Json).result -eq 'Succeeded')}
$taskResult=@{project=$taskProject;log=$taskLog;seconds=((Get-Date)-$taskStarted).TotalSeconds;stoppedAfterStallOrLimit=$taskStopped;sceneReopened=$taskOpened;buildSucceeded=$taskBuilt;importMode='InProcess';jobWorkerCount=2}
$taskResult|ConvertTo-Json|Set-Content -Encoding UTF8 -LiteralPath 'E:\El Gam3a\Semester 9\Video Game Design\Assignment\Jay A Career\Deliverables\Evidence\serial-import-build-result.json'
$taskResult|ConvertTo-Json
