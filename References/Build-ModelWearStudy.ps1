$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot 'ModelWear100'
$records=Get-Content -Raw -Encoding UTF8 (Join-Path $root 'candidates.json') | ConvertFrom-Json
$existing=@()
if(Test-Path -LiteralPath (Join-Path $root 'manifest.json')){$existing=Get-Content -Raw -Encoding UTF8 (Join-Path $root 'manifest.json') | ConvertFrom-Json}
$notes=@(Get-Content -Encoding UTF8 (Join-Path $root 'annotations.txt') | Where-Object {$_ -and -not $_.StartsWith('#')} | ForEach-Object {
    $p=$_ -split '\|',6
    if($p.Count -ne 6){throw 'Invalid annotation row'}
    [PSCustomObject]@{candidate_id=$p[0];category=$p[1];silhouette=$p[2];view=$p[3];coverage=$p[4];observation_vi=$p[5]}
})
if($notes.Count -ne 100 -or @($notes.candidate_id | Select-Object -Unique).Count -ne 100){throw 'Expected exactly 100 unique reviewed annotations'}
$imageDir=Join-Path $root 'Images'
New-Item -ItemType Directory -Force -Path $imageDir | Out-Null
Add-Type -AssemblyName System.Drawing
$final=@();$number=0
foreach($note in $notes){
    $matches=@($records | Where-Object {$_.id -ceq $note.candidate_id})
    if($matches.Count -ne 1){throw ('Missing or duplicated source '+$note.candidate_id)}
    $record=$matches[0];$source=Join-Path $root $record.file
    if(-not (Test-Path -LiteralPath $source)){
        $previous=@($existing | Where-Object {$_.candidate_id -ceq $note.candidate_id})
        if($previous.Count -ne 1){throw ('No cached image '+$note.candidate_id)}
        $source=Join-Path $root $previous[0].file
    }
    if((Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash -ne $record.download_sha256){throw ('Source hash mismatch '+$source)}
    $number++;$id='MW'+('{0:D3}' -f $number);$file='Images/'+$id+[IO.Path]::GetExtension($source)
    $destination=Join-Path $root $file
    if([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($destination)){Copy-Item -LiteralPath $source -Destination $destination -Force}
    $bitmap=[Drawing.Image]::FromFile($destination)
    try{$width=$bitmap.Width;$height=$bitmap.Height}finally{$bitmap.Dispose()}
    $final+=[PSCustomObject]@{
        id=$id;candidate_id=$note.candidate_id;file=$file;category=$note.category;silhouette=$note.silhouette;
        view=$note.view;coverage=$note.coverage;observation_vi=$note.observation_vi;
        title=$record.title;creator=$(if($note.candidate_id -eq '049'){'Pavel '+[char]0x0160+'evela (Sevela.p)'}elseif($note.candidate_id -eq '106'){'Unknown author (as stated by source)'}else{$record.creator});
        creator_original_metadata=$record.creator;
        attribution_note=$(if($note.candidate_id -eq '049'){'Author verified from Commons file wikitext: Self Cc-by-sa-3.0 author=User:Sevela.p, Pavel Sevela'}elseif($note.candidate_id -eq '106'){'Source states Unknown author; archive credit retained'}else{'As stated by source metadata'});
        credit=$record.credit;license=$record.license;license_url=$record.license_url;
        source_page=$record.source_page;image_url=$record.image_url;download_url=$record.download_url;
        original_width=$record.original_width;original_height=$record.original_height;width=$width;height=$height;
        bytes=(Get-Item -LiteralPath $destination).Length;original_sha1=$record.original_sha1;
        download_sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $destination).Hash;
        acquired_utc=$record.acquired_utc;review_status='visually_reviewed';review_method='Manual visual inspection; garment interpretation from visible regions; not AI training';
        modifications='None; downloaded Wikimedia thumbnail, copied without image edits'
    }
}
if(@($final.original_sha1 | Select-Object -Unique).Count -ne 100 -or @($final.download_sha256 | Select-Object -Unique).Count -ne 100){throw 'Duplicate image content detected'}
if(@(Get-ChildItem -LiteralPath $imageDir -File).Count -ne 100){throw 'Final image folder must contain exactly 100 files'}
$json=ConvertTo-Json -InputObject @($final) -Depth 8
[IO.File]::WriteAllText((Join-Path $root 'manifest.json'),$json,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $root 'data.js'),('window.MODEL_WEAR_DATA='+$json+';'),[Text.UTF8Encoding]::new($false))
$final | Export-Csv -LiteralPath (Join-Path $root 'manifest.csv') -NoTypeInformation -Encoding UTF8
$selected=@($notes.candidate_id)
$records | ForEach-Object {[PSCustomObject]@{candidate_id=$_.id;selected=($_.id -in $selected);reason=if($_.id -in $selected){'Reviewed garment-fit reference'}else{'Excluded: visibility, subject relevance, or redundant view'}}} | Export-Csv -LiteralPath (Join-Path $root 'selection_audit.csv') -NoTypeInformation -Encoding UTF8
$audit=[PSCustomObject]@{
    checked_utc=[DateTime]::UtcNow.ToString('o');candidate_count=$records.Count;selected_count=$final.Count;
    unique_original_hashes=@($final.original_sha1 | Select-Object -Unique).Count;
    unique_download_hashes=@($final.download_sha256 | Select-Object -Unique).Count;
    decoded_image_count=$final.Count;bytes=($final | Measure-Object bytes -Sum).Sum;
    full_body_count=@($final | Where-Object coverage -eq 'full').Count;partial_body_count=@($final | Where-Object coverage -eq 'partial').Count;
    category_counts=@($final | Group-Object category | ForEach-Object {[PSCustomObject]@{category=$_.Name;count=$_.Count}});
    license_counts=@($final | Group-Object license | ForEach-Object {[PSCustomObject]@{license=$_.Name;count=$_.Count}});
    all_have_creator_field=(@($final | Where-Object {-not $_.creator}).Count -eq 0);
    unknown_creator_count=@($final | Where-Object creator -Like 'Unknown author*').Count;
    all_have_source=(@($final | Where-Object {-not $_.source_page}).Count -eq 0);
    all_have_license=(@($final | Where-Object {-not $_.license}).Count -eq 0);
    ai_training_performed=$false;runtime_code_changed=$false
}
$audit | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $root 'validation.json') -Encoding UTF8
$audit | ConvertTo-Json -Depth 6
