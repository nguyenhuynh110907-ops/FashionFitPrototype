param([int]$Limit=140,[switch]$Resume)
$ErrorActionPreference='Stop'
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$root=Join-Path $PSScriptRoot 'ModelWear100'
$candidates=Join-Path $root 'Candidates'
New-Item -ItemType Directory -Force -Path $candidates | Out-Null
$headers=@{'User-Agent'='FashionFitPrototype/0.8.2 (local garment-fit reference study; Commons attribution retained)'}
function Plain([string]$html){return [Net.WebUtility]::HtmlDecode(($html -replace '<[^>]+>',' ' -replace '\s+',' ').Trim())}
function Query([hashtable]$parameters){
    $parameters.action='query';$parameters.format='json';$parameters.prop='imageinfo';$parameters.iiprop='url|size|extmetadata|sha1|mime';$parameters.iiurlwidth='960'
    $pairs=@();foreach($key in $parameters.Keys){$pairs+=([Uri]::EscapeDataString($key)+'='+[Uri]::EscapeDataString([string]$parameters[$key]))}
    $uri='https://commons.wikimedia.org/w/api.php?'+($pairs -join '&')
    for($attempt=0;$attempt -lt 3;$attempt++){
        try{return Invoke-RestMethod -Uri $uri -Headers $headers -TimeoutSec 30}
        catch{if($attempt -eq 2){throw};Start-Sleep -Seconds (2+$attempt*2)}
    }
}
$sources=@(
    @{group='CODe_2024';category='Category:College of DuPage CODe Fashion Show 2024';quota=55},
    @{group='CODe_2023';category='Category:College of DuPage CODe Fashion Show 2023';quota=25},
    @{group='Crystal_Couture';search='"Crystal Couture Fashion Show" filetype:bitmap';quota=35},
    @{group='Trousers';search='"fashion show" "trousers" -bikini -lingerie filetype:bitmap';quota=10},
    @{group='Skirts';search='"fashion show" "skirt" -bikini -lingerie filetype:bitmap';quota=10},
    @{group='Fashion_Week';search='"fashion week" "dress" -bikini -lingerie filetype:bitmap';quota=30},
    @{group='Runway_Extra';search='"runway model" -bikini -lingerie filetype:bitmap';quota=30}
)
$records=New-Object 'System.Collections.Generic.List[object]'
$seen=New-Object 'System.Collections.Generic.HashSet[string]'
$index=0
$throttled=$false
if($Resume){
    foreach($old in (Get-Content -Raw -Encoding UTF8 (Join-Path $root 'candidates.json') | ConvertFrom-Json)){
        $records.Add($old);[void]$seen.Add([string]$old.original_sha1);$index=[Math]::Max($index,[int]$old.id)
    }
    $sources=@(
        @{group='Runway_Skirts';search='"fashion show" "skirt" -bikini -lingerie filetype:bitmap';quota=12},
        @{group='Runway_Tailoring';search='"fashion show" "suit" -bikini -lingerie filetype:bitmap';quota=12},
        @{group='Runway_Dresses';search='"fashion show" "gown" -bikini -lingerie filetype:bitmap';quota=20}
    )
}
foreach($source in $sources){
    if($records.Count -ge $Limit -or $throttled){break}
    $added=0;$continuation=@{}
    for($batch=0;$batch -lt 5 -and $added -lt $source.quota -and $records.Count -lt $Limit -and -not $throttled;$batch++){
        $parameters=@{}
        if($source.category){$parameters.generator='categorymembers';$parameters.gcmtitle=$source.category;$parameters.gcmtype='file';$parameters.gcmlimit='50'}
        else{$parameters.generator='search';$parameters.gsrsearch=$source.search;$parameters.gsrnamespace='6';$parameters.gsrlimit='50'}
        foreach($key in $continuation.Keys){$parameters[$key]=$continuation[$key]}
        try{$response=Query $parameters}catch{Write-Output ('SOURCE_ERROR '+$source.group+' '+$_.Exception.Message);break}
        if(-not $response.query.pages){break}
        $pages=@($response.query.pages.PSObject.Properties.Value | Sort-Object index,title)
        foreach($page in $pages){
            if($records.Count -ge $Limit -or $added -ge $source.quota){break}
            if(-not $page.imageinfo){continue};$info=$page.imageinfo[0];$meta=$info.extmetadata
            if($info.mime -notin @('image/jpeg','image/png')){continue}
            if($info.height -lt $info.width*1.1 -or $info.height -lt 700 -or $info.width -lt 350){continue}
            $lic=Plain $meta.LicenseShortName.value
            if($lic -notmatch '^(CC BY|CC BY-SA|CC0|Public domain|Public Domain)'){continue}
            if($page.title -match 'bikini|lingerie|swimwear|swimsuit|nude|naked|underwear|portrait|headshot|cropped|children|kids|junior'){continue}
            if(-not $info.thumburl -or -not $seen.Add([string]$info.sha1)){continue}
            $index++;$extension=if($info.mime -eq 'image/png'){'.png'}else{'.jpg'}
            $file=('{0:D3}' -f $index)+$extension;$path=Join-Path $candidates $file;$downloadURL=([string]$info.thumburl).Split('?')[0]
            try{
                Invoke-WebRequest -UseBasicParsing -Uri $downloadURL -Headers $headers -OutFile $path -TimeoutSec 30
                if((Get-Item -LiteralPath $path).Length -lt 5000){throw 'Image response too small'}
            }catch{
                Write-Output ('DOWNLOAD_ERROR '+$file+' '+$_.Exception.Message)
                if($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 429){$throttled=$true;Write-Output 'RATE_LIMIT_STOP: stop downloading; retry only after server cooldown';break}
                continue
            }
            $record=[PSCustomObject]@{
                id=('{0:D3}' -f $index);file='Candidates/'+$file;title=$page.title;source_group=$source.group;
                source_page=$info.descriptionurl;image_url=$info.url;download_url=$downloadURL;
                original_width=$info.width;original_height=$info.height;
                creator=(Plain $meta.Artist.value);credit=(Plain $meta.Credit.value);license=$lic;
                license_url=$meta.LicenseUrl.value;description=(Plain $meta.ImageDescription.value);
                original_sha1=$info.sha1;download_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash;
                acquired_utc=[DateTime]::UtcNow.ToString('o');review_status='pending_visual_review'
            }
            $records.Add($record);$added++
            if($records.Count%10 -eq 0){Write-Output ('DOWNLOADED '+$records.Count+' source='+$source.group)}
            [IO.File]::WriteAllText((Join-Path $root 'candidates.json'),(ConvertTo-Json -InputObject @($records.ToArray()) -Depth 8),[Text.UTF8Encoding]::new($false))
            Start-Sleep -Seconds 3
        }
        if(-not $response.continue){break}
        $continuation=@{};foreach($property in $response.continue.PSObject.Properties){$continuation[$property.Name]=$property.Value}
    }
    Write-Output ('SOURCE_DONE '+$source.group+' added='+$added+' total='+$records.Count)
}
[IO.File]::WriteAllText((Join-Path $root 'candidates.json'),(ConvertTo-Json -InputObject @($records.ToArray()) -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output ('COLLECT_DONE count='+$records.Count)
if($records.Count -lt 100){exit 1}
