param([string]$ImagePath)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$python=Join-Path $projectRoot '.ml-runtime\python\python.exe'
if(-not (Test-Path -LiteralPath $python)){throw 'Training environment missing. Read AI/FitVision/README.md.'}
if(-not $ImagePath){
    Add-Type -AssemblyName System.Windows.Forms
    $dialog=New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title='FitVision - choose a garment photo (processed locally)'
    $dialog.Filter='Images|*.jpg;*.jpeg;*.png;*.webp'
    try{if($dialog.ShowDialog() -ne 'OK'){exit 0};$ImagePath=$dialog.FileName}finally{$dialog.Dispose()}
}
$env:PYTHONUTF8='1'
$env:HF_HUB_OFFLINE='1'
$env:HF_HUB_DISABLE_TELEMETRY='1'
& $python (Join-Path $PSScriptRoot 'fitvision.py') predict --image $ImagePath
if($LASTEXITCODE -ne 0){throw 'Image analysis failed'}
Write-Host 'Result: AI/FitVision/Artifacts/latest_prediction.json'
