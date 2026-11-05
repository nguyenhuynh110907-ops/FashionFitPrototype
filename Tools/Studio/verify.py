"""One command: fresh Editor + player evidence, contact checks and trial telemetry.

All outputs go to a NEW run directory. No game process is killed or overwritten.
"""
from pathlib import Path
import argparse, datetime as dt, hashlib, json, os, subprocess, sys, time

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[1]
def utc(): return dt.datetime.now(dt.timezone.utc).isoformat()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def manifest(project):
    rows = []
    for root in ('Assets/FashionFit', 'Assets/Resources', 'Packages'):
        for p in sorted((project / root).rglob('*')):
            if p.is_file(): rows.append({'path': p.relative_to(project).as_posix(), 'sha256': sha(p)})
    return rows

def validate(data, minimum=1):
    if data.get('status') != 'passed': raise ValueError('Evidence status is not passed')
    if data.get('checks', 0) < minimum: raise ValueError('Evidence assertion count too small')
    if data.get('exceptions', 0) != 0: raise ValueError('Evidence contains exceptions')

class Suite:
    def __init__(self, args):
        self.args = args; self.project = args.unity_project.resolve(); self.rows = []; self.completed = False
        self.out = (args.output or Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'FashionFit/StudioRuns').resolve() / dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
        self.out.mkdir(parents=True, exist_ok=False); self.started = utc(); self.source = manifest(self.project)
        (self.out / 'source_manifest.json').write_text(json.dumps(self.source, indent=2), encoding='utf-8')

    def save(self):
        states = [r['status'] for r in self.rows]
        status = 'failed' if 'failed' in states else 'INCOMPLETE' if 'NOT_ASSESSED' in states else 'passed' if self.completed and states else 'RUNNING'
        r = dict(schema=1, status=status, startedUtc=self.started, updatedUtc=utc(), suite=self.args.suite,
                 project=str(self.project), output=str(self.out), checks=self.rows,
                 physicalHeadset='NOT_ASSESSED', realControllerAcceptance='NOT_ASSESSED', materialCalibration='NOT_ASSESSED',
                 continuousCollision='NOT_ASSESSED', note='A passed automated suite does not certify all poses, headset comfort, textile strength or 72 Hz.')
        (self.out / 'summary.json').write_text(json.dumps(r, indent=2), encoding='utf-8')
        lines = ['# FashionFit Studio verification', '', f'Status: **{status}** | Suite: {self.args.suite}', '', '| Check | Status | Evidence |', '|---|---|---|']
        for row in self.rows: lines.append(f"| {row['id']} | {row['status']} | {row.get('checks', '')} {row.get('reason', '')} |")
        lines += ['', 'Physical headset / real controller acceptance / material calibration / continuous collision: **NOT_ASSESSED**.', '', 'See each evidence JSON, Player.log and frames.csv. Performance is measured app timing; missing data is not zero.']
        (self.out / 'REPORT.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')

    def command(self, name, command, cwd=None, timeout=900, evidence=None, minimum=1, expected_status='passed', visible=False):
        start = time.time_ns(); row = {'id': name, 'status': 'failed', 'startedUtc': utc()}
        try:
            info = None
            if os.name == 'nt' and not visible:
                info = subprocess.STARTUPINFO(); info.dwFlags |= subprocess.STARTF_USESHOWWINDOW; info.wShowWindow = 0
            with (self.out / (name + '.console.log')).open('w', encoding='utf-8') as log:
                run = subprocess.run(list(map(str, command)), cwd=cwd, stdout=log, stderr=subprocess.STDOUT,
                                     timeout=timeout, startupinfo=info)
            row['exitCode'] = run.returncode
            if run.returncode:
                if run.returncode == 2 and evidence and evidence.is_file() and evidence.stat().st_mtime_ns >= start and read(evidence).get('status') == 'NOT_ASSESSED':
                    raise FileNotFoundError(read(evidence).get('reason', 'Required evidence unavailable'))
                raise ValueError(f'Exit {run.returncode}; see {name}.console.log')
            if evidence:
                if not evidence.is_file() or evidence.stat().st_mtime_ns < start: raise ValueError('Missing or stale evidence')
                data = read(evidence)
                if expected_status == 'passed': validate(data, minimum)
                elif data.get('status') != expected_status: raise ValueError('Unexpected evidence status')
                row['checks'] = data.get('checks', len(data.get('cases', [])))
                row['evidence'] = str(evidence); row['sha256'] = sha(evidence)
                target = self.out / (name + '.json')
                if target != evidence: target.write_bytes(evidence.read_bytes())
            row['status'] = 'passed'
        except (FileNotFoundError, PermissionError) as e: row.update(status='NOT_ASSESSED', reason=str(e))
        except Exception as e: row['reason'] = str(e)
        row['seconds'] = round((time.time_ns() - start) / 1e9, 3); self.rows.append(row); self.save()
        print(name, row['status'], row.get('checks', ''), row.get('reason', ''), flush=True)
        if row['status'] != 'passed': raise RuntimeError(name)

    def editor(self, name, method, evidence, minimum, extra=()):
        self.command(name, [self.args.unity, '-batchmode', '-projectPath', self.project,
                           '-logFile', self.out / (name + '.unity.log'), '-executeMethod', method, *extra],
                     evidence=self.project / evidence, minimum=minimum, timeout=1800)

    def player(self, name, flag, file, minimum, extra=(), visible=False):
        folder = self.out / name; folder.mkdir()
        self.command(name, [self.exe, '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '900',
                           flag, folder, '-logFile', folder / 'Player.log', *extra], cwd=self.exe.parent,
                     evidence=folder / file, minimum=minimum, timeout=240, visible=visible)

    def contact(self, name, count, layers=False):
        folder = self.out / name / 'Contact'
        cmd = [sys.executable, '-X', 'utf8', HERE / 'contact_check.py', folder, '--expected', count]
        if layers: cmd.append('--layers')
        if self.args.qa_deps: cmd += ['--qa-deps', self.args.qa_deps]
        # Contact files report cases instead of an assertion count.
        self.command(name + '_contact', cmd, evidence=folder / 'independent_contact.json', minimum=0)

    def run(self):
        version = (self.project / 'ProjectSettings/ProjectVersion.txt').read_text(encoding='utf-8')
        if 'm_EditorVersion: 2022.3.62f1' not in version: raise ValueError('This adapter pins Unity 2022.3.62f1')
        if (self.project / 'Temp/UnityLockfile').exists(): raise FileNotFoundError('Unity project is already open; close its Editor or use --unity-project with an isolated copy')
        if not self.args.unity.is_file(): raise FileNotFoundError('Pinned Unity Editor is missing; specify --unity')
        if not (self.project / 'Assets/FashionFit/Runtime/StudioTelemetry.cs').is_file(): raise FileNotFoundError('Studio runtime source is missing in target project')
        self.command('evidence_gate', [sys.executable, '-X', 'utf8', HERE / 'test_evidence_gate.py'])
        self.editor('vr_config', 'FashionFit.Editor.VRReadyVerification.RunBatch', 'Verification/VRReady/verification.json', 37)
        self.editor('picking_equivalence', 'FashionFit.Editor.ResponseOptimizationVerification.RunBatch', 'Verification/ResponseOptimization/picking.json', 660)
        self.editor('readout_equivalence', 'FashionFit.Editor.ResponseReadoutVerification.RunBatch', 'Verification/ResponseOptimization/native_readout.json', 168)
        self.editor('bimanual', 'FashionFit.Editor.BimanualVerification.RunBatch', 'Verification/Bimanual/verification.json', 91)
        self.editor('pose_joints', 'FashionFit.Editor.ContactPoseVerification.RunBatch', 'Verification/ContactPose/verification.json', 68)
        fit = self.out / 'fit_shape'
        self.command('fit_shape', [self.args.unity, '-batchmode', '-projectPath', self.project,
                     '-logFile', self.out / 'fit_shape.unity.log', '-executeMethod',
                     'FashionFit.Editor.FitShapeVerification.RunBatch', '-fashionFitOutput', fit],
                     evidence=fit / 'verification.json', minimum=7, timeout=1800)
        self.command('fit_shape_contact', [sys.executable, '-X', 'utf8', HERE / 'fit_shape_check.py', fit,
                     '--qa-deps', self.args.qa_deps or ''], evidence=fit / 'fit_metrics.json', minimum=7)
        if self.args.suite == 'full':
            submission = self.out / 'student_submission'
            self.command('student_submission', [self.args.unity, '-batchmode', '-projectPath', self.project,
                         '-logFile', self.out / 'student_submission.unity.log', '-executeMethod',
                         'FashionFit.Editor.StudentGarmentVerification.RunBatch', '-fashionSubmissionQA', submission],
                         evidence=submission / 'verification.json', minimum=162, timeout=1800)
            self.command('student_submission_contact', [sys.executable, '-X', 'utf8', HERE / 'contact_check.py',
                         submission / 'Contact', '--expected', '12', '--qa-deps', self.args.qa_deps or ''],
                         evidence=submission / 'Contact/independent_contact.json', minimum=0)
        self.exe = self.out / 'Player/FashionFittingRoom.exe'
        self.editor('build', 'FashionFit.Editor.StudioBuild.RunBatch', 'Verification/ProjectRecovery/build.json', 0, ['-fashionBuildPath', self.exe])
        # Build also writes the authoritative asset/settings changes through Unity.
        receipt = dict(source=manifest(self.project), exeSha256=sha(self.exe), builtUtc=utc())
        (self.out / 'build_receipt.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
        self.player('surface', '-fashionSurfaceVerify', 'verification.json', 28); self.contact('surface', 8)
        self.player('pose_hat', '-fashionPoseBalanceVerify', 'verification.json', 29)
        self.player('rendered_trial', '-fashionStudioTrial', 'trial.json', 19,
                    ['-fashionPerformance', self.out / 'rendered_trial', '-fashionPerformanceSeconds', '120', '-fashionPerformanceWarmup', '1'], visible=True)
        perf = self.out / 'rendered_trial/performance.json'
        if not perf.is_file() or read(perf).get('renderedFrames', 0) < 60 or read(perf).get('status') != 'RECORDED': raise ValueError('Rendered trial did not produce valid camera-rendered performance data')
        self.command('capture_analysis', [sys.executable, '-X', 'utf8', HERE / 'analyze_capture.py', perf.parent], evidence=perf.parent / 'capture_analysis.json', minimum=0)
        if self.args.suite == 'full':
            self.player('mens', '-fashionMensVerify', 'verification.json', 55); self.contact('mens', 20)
            self.player('layers', '-fashionLayeredVerify', 'player.json', 18); self.contact('layers', 5, True)
        (self.out / 'player_manifest.json').write_text(json.dumps([{'path': p.relative_to(self.exe.parent).as_posix(), 'sha256': sha(p)} for p in sorted(self.exe.parent.rglob('*')) if p.is_file()], indent=2), encoding='utf-8')
        self.completed = True; self.save()

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--unity-project', type=Path, default=PROJECT)
    p.add_argument('--unity', type=Path, default=Path(os.environ.get('ProgramFiles', r'C:\Program Files')) / 'Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe')
    p.add_argument('--output', type=Path); p.add_argument('--suite', choices=('trial', 'full'), default='full')
    p.add_argument('--qa-deps', type=Path, default=os.environ.get('FASHIONFIT_QA_DEPS'))
    a = p.parse_args(); config = HERE / 'studio.local.json'
    if not a.qa_deps and config.exists(): a.qa_deps = Path(read(config)['qa_dependencies'])
    s = Suite(a); print('OUTPUT', s.out, flush=True)
    try: s.run()
    except Exception as e:
        if not s.rows or s.rows[-1]['status'] == 'passed': s.rows.append(dict(id='preflight_or_evidence', status='NOT_ASSESSED' if isinstance(e, (FileNotFoundError, PermissionError)) else 'failed', reason=str(e)))
        s.save(); print('STOP', e, flush=True); return 1
    return 0

if __name__ == '__main__': sys.exit(main())
