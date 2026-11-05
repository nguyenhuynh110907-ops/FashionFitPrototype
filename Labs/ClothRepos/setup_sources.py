"""Reproduce the pinned CPU backend in an explicit private virtual environment."""
from pathlib import Path
import os, sys, json, hashlib, urllib.request, zipfile, shutil, subprocess, sysconfig

ROOT=Path(__file__).resolve().parent

def download(url,path,expected):
    path.parent.mkdir(parents=True,exist_ok=True)
    if not path.exists():
        with urllib.request.urlopen(url,timeout=180) as response,path.open('wb') as target:
            shutil.copyfileobj(response,target)
    actual=hashlib.sha256(path.read_bytes()).hexdigest()
    if actual!=expected: raise RuntimeError(f'Checksum mismatch: {path.name}')

def main():
    if os.name!='nt': raise RuntimeError('This reproduction wrapper targets Windows x64.')
    if sys.prefix==sys.base_prefix: raise RuntimeError('Create and use the private .venv documented in README.md.')
    for row in json.loads((ROOT/'SourceReceipt.json').read_text()):
        repo,commit=row['repository'],row['commit']
        name=repo.split('/')[-1]
        archive=ROOT/'Downloads'/f'{name}-{commit}.zip'
        download(f'https://codeload.github.com/{repo}/zip/{commit}',archive,row['archiveSha256'])
        target=(ROOT/'Sources'/name).resolve()
        target.mkdir(parents=True,exist_ok=True)
        with zipfile.ZipFile(archive) as bundle:
            for member in bundle.infolist():
                dest=(target/Path(*Path(member.filename).parts[1:])).resolve()
                if not dest.is_relative_to(target): raise RuntimeError('Unsafe archive member')
                if member.is_dir(): dest.mkdir(parents=True,exist_ok=True)
                else:
                    dest.parent.mkdir(parents=True,exist_ok=True)
                    with bundle.open(member) as source,dest.open('wb') as out: shutil.copyfileobj(source,out)
    garment=ROOT/'Sources/GarmentCode'
    warning=garment/'pygarment/meshgen/boxmeshgen.py'
    text=warning.read_text(encoding='utf-8')
    old="{self.__class__.__name__}::{self.name}::WARNING::Detected edge represented only by two vertices.."
    new="{self.__class__.__name__}::WARNING::Detected edge represented only by two vertices.."
    if old not in text: raise RuntimeError('Expected upstream warning line was not found')
    warning.write_text(text.replace(old,new),encoding='utf-8')
    if not (garment/'system.json').exists(): shutil.copy2(garment/'system.template.json',garment/'system.json')
    warp=ROOT/'Sources/NvidiaWarp-GarmentCode'
    environment=dict(os.environ)
    environment.pop('CUDA_PATH',None)
    environment.pop('CUDA_HOME',None)
    subprocess.run([sys.executable,'build_lib.py','--no_standalone'],cwd=warp,env=environment,check=True)
    if not (warp/'warp/bin/warp.dll').is_file(): raise RuntimeError('Warp CPU build did not produce warp.dll')
    compiler=json.loads((ROOT/'CompilerReceipt.json').read_text())
    wheel=ROOT/'Downloads/warp_lang-1.0.0b6-py3-none-win_amd64.whl'
    download(compiler['url'],wheel,compiler['archiveSha256'])
    with zipfile.ZipFile(wheel) as bundle: data=bundle.read('warp/bin/warp-clang.dll')
    if hashlib.sha256(data).hexdigest()!=compiler['compilerSha256']: raise RuntimeError('Compiler DLL checksum mismatch')
    (warp/'warp/bin/warp-clang.dll').write_bytes(data)
    (Path(sysconfig.get_path('purelib'))/'fashionfit_trial_sources.pth').write_text(str(garment)+'\n'+str(warp)+'\n',encoding='utf-8')
    subprocess.run([sys.executable,'-c',"import warp as wp; wp.init(); wp.set_device('cpu'); from pygarment.meshgen.simulation import run_sim; print('CPU backend ready')"],cwd=garment,check=True)
    print('Ready. Open Open_Repo_Trials.bat.')

if __name__=='__main__': main()
