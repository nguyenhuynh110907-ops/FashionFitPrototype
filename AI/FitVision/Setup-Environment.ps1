$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$runtime=Join-Path $projectRoot '.ml-runtime'
$pythonDir=Join-Path $runtime 'python'
$env:PYTHONUTF8='1'
$env:PIP_CACHE_DIR=Join-Path $runtime 'pip-cache'
$env:HF_HOME=Join-Path $runtime 'huggingface'
$env:HF_HUB_DISABLE_TELEMETRY='1'
$env:HF_HUB_DISABLE_XET='1'
New-Item -ItemType Directory -Force -Path $pythonDir | Out-Null
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$python=Join-Path $pythonDir 'python.exe'
if(-not (Test-Path -LiteralPath $python)){
    $zip=Join-Path $runtime 'python-3.12.10-embed-amd64.zip'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://www.python.org/ftp/python/3.12.10/python-3.12.10-embed-amd64.zip' -OutFile $zip -TimeoutSec 120
    Expand-Archive -LiteralPath $zip -DestinationPath $pythonDir -Force
    [IO.File]::WriteAllText((Join-Path $pythonDir 'python312._pth'),"python312.zip`n.`nLib/site-packages`nimport site`n",[Text.Encoding]::ASCII)
}
& $python --version
if(-not (Test-Path -LiteralPath (Join-Path $pythonDir 'Lib\site-packages\pip'))){
    $bootstrap=Join-Path $runtime 'get-pip.py'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://bootstrap.pypa.io/get-pip.py' -OutFile $bootstrap -TimeoutSec 120
    & $python $bootstrap --no-warn-script-location
    if($LASTEXITCODE -ne 0){throw 'pip bootstrap failed'}
}
$lock=Join-Path $PSScriptRoot 'requirements.lock.txt'
if(Test-Path -LiteralPath $lock){
    & $python -m pip install --only-binary=:all: --no-deps --no-warn-script-location -r $lock --extra-index-url https://download.pytorch.org/whl/cpu
    if($LASTEXITCODE -ne 0){throw 'Locked training dependencies install failed'}
}else{
    & $python -m pip install --only-binary=:all: --no-warn-script-location torch --index-url https://download.pytorch.org/whl/cpu
    if($LASTEXITCODE -ne 0){throw 'PyTorch CPU install failed'}
    & $python -m pip install --only-binary=:all: --no-warn-script-location transformers safetensors pillow numpy scikit-learn matplotlib
    if($LASTEXITCODE -ne 0){throw 'Training dependencies install failed'}
    & $python -m pip freeze | Set-Content -LiteralPath $lock -Encoding UTF8
}
& $python -m pip check
if($LASTEXITCODE -ne 0){throw 'Training dependencies are inconsistent'}
& $python -c "import torch; from transformers import AutoImageProcessor, AutoModel; print('CPU training ready:',torch.__version__)"
if($LASTEXITCODE -ne 0){throw 'Training environment verification failed'}
