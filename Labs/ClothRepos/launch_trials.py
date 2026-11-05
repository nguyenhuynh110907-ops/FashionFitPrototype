"""Open the isolated lab; reuse only servers reporting the expected identity."""
from pathlib import Path
import sys, subprocess, time, json, urllib.request, webbrowser
ROOT=Path(__file__).resolve().parent

def ready(port, expected):
    try:
        with urllib.request.urlopen(f'http://127.0.0.1:{port}/api/health',timeout=2) as response:
            return json.load(response).get('application') == expected
    except Exception:
        return False

def start(script,port,expected,logname):
    if ready(port,expected):
        return
    with (ROOT/f'{logname}.log').open('a',encoding='utf-8') as log, (ROOT/f'{logname}Error.log').open('a',encoding='utf-8') as error:
        proc=subprocess.Popen([sys.executable,'-u','-X','utf8',str(ROOT/script)],cwd=ROOT,
            stdout=log,stderr=error,creationflags=subprocess.CREATE_NO_WINDOW)
    (ROOT/f'{logname}.pid').write_text(str(proc.pid))
    for _ in range(100):
        if ready(port,expected):
            return
        if proc.poll() is not None:
            raise RuntimeError(f'{script} could not start. Read {logname}Error.log.')
        time.sleep(.2)
    raise RuntimeError(f'{script} startup timed out. Read {logname}Error.log.')

if __name__=='__main__':
    start('trial_hub.py',8765,'FashionFit Repo Trials','Hub')
    start('serve_garmentcode.py',8766,'GarmentCode CPU Trial','Gui')
    if '--check' not in sys.argv:
        webbrowser.open('http://127.0.0.1:8765')
    print('Ready: http://127.0.0.1:8765')
