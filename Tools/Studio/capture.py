"""Launch a real user-driven capture. Does not inject controller input."""
from pathlib import Path
import argparse, csv, datetime as dt, json, os, subprocess, sys

def main():
    here = Path(__file__).resolve().parent
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--player', type=Path); p.add_argument('--seconds', type=int, default=600)
    p.add_argument('--output', type=Path); p.add_argument('--check', action='store_true')
    a = p.parse_args(); local = here / 'studio.local.json'
    if not a.player and local.exists(): a.player = Path(json.loads(local.read_text(encoding='utf-8'))['installed_player'])
    if not a.player or not a.player.is_file(): print('Missing V089 player. Set --player or studio.local.json.'); return 1
    if not (a.player.parent / 'FashionFittingRoom_Data').is_dir(): print('Player Data folder is missing.'); return 1
    if a.check: print('Performance capture launcher ready:', a.player); return 0
    if os.name == 'nt':
        info = subprocess.STARTUPINFO(); info.dwFlags |= subprocess.STARTF_USESHOWWINDOW; info.wShowWindow = 0
        running = subprocess.run(['tasklist', '/FI', 'IMAGENAME eq FashionFittingRoom.exe', '/FO', 'CSV', '/NH'], capture_output=True, text=True, startupinfo=info)
        if running.returncode: print('Cannot inspect existing player process. Close the old game before recording.'); return 1
        if any(row and row[0].casefold() == 'fashionfittingroom.exe' for row in csv.reader(running.stdout.splitlines())):
            print('Close the existing FashionFit game before starting capture.'); return 3
    folder = (a.output or Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'FashionFit/Performance').resolve() / dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    folder.mkdir(parents=True, exist_ok=False)
    command = [str(a.player), '-fashionPerformance', str(folder), '-fashionPerformanceSeconds', str(max(10, min(1800, a.seconds))), '-logFile', str(folder / 'Player.log')]
    subprocess.Popen(command, cwd=a.player.parent)
    print('Capture started. Enter VR using the existing game VR control. Use Touch normally.')
    print('The first 5 seconds after Ready are warmup. Close game normally to save a partial capture.')
    print('Output:', folder)
    return 0

if __name__ == '__main__': sys.exit(main())
