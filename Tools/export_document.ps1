param([string]$DocumentPath)
$taskWord = New-Object -ComObject Word.Application
$taskWord.Visible = $false
$taskWord.DisplayAlerts = 0
try {
 $taskDocument = $taskWord.Documents.Open($DocumentPath, $false, $true)
 $taskDocument.Repaginate()
 $taskPdf = [IO.Path]::ChangeExtension($DocumentPath, '.pdf')
 $taskDocument.ExportAsFixedFormat($taskPdf, 17)
 Write-Output ('Exported ' + $taskDocument.ComputeStatistics(2) + ' pages to ' + $taskPdf)
 $taskDocument.Close(0)
} finally { $taskWord.Quit() }
