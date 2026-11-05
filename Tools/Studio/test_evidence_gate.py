"""Meaningful regression tests: an orchestrator must not produce false passes."""
from pathlib import Path
import argparse, contextlib, io, json, os, sys, tempfile, unittest
sys.path.insert(0,str(Path(__file__).resolve().parent))
import verify

class EvidenceGate(unittest.TestCase):
    def test_empty_scope_is_running(self):
        self.assertIsNone(verify.validate({'status':'passed','checks':1}))
        for d in ({'status':'failed','checks':100}, {'status':'passed','checks':0}, {'status':'passed','checks':10,'exceptions':1}):
            with self.assertRaises(ValueError): verify.validate(d)

    def run_case(self, script, old=None):
        base=Path(os.environ.get('FASHIONFIT_TEST_OUTPUT',str(Path(os.environ.get('LOCALAPPDATA',str(Path.home())))/'FashionFit/EvidenceGateCases'))).resolve()
        base.mkdir(parents=True,exist_ok=True)
        root=Path(tempfile.mkdtemp(prefix='gate-',dir=base))
        # Retain tiny reproduction artifacts; no recursive cleanup of user paths.
        project=root/'Project'; project.mkdir()
        s=verify.Suite(argparse.Namespace(unity_project=project,output=root/'Runs',suite='trial'))
        s.save(); self.assertEqual(verify.read(s.out/'summary.json')['status'],'RUNNING')
        result=root/'evidence.json'
        if old:
            result.write_text(json.dumps(old),encoding='utf-8'); os.utime(result,(1,1))
        job=root/'emit.py'; job.write_text(script.replace('OUTPUT_PATH',repr(str(result))),encoding='utf-8')
        with contextlib.redirect_stdout(io.StringIO()):
            try: s.command('probe',[sys.executable,str(job)],evidence=result)
            except RuntimeError: pass
        return s.rows[-1]['status'],verify.read(s.out/'summary.json')['status']

    def test_missing_file_never_passes(self):
        row,summary=self.run_case('pass')
        self.assertEqual((row,summary),('failed','failed'))

    def test_old_green_report_never_passes(self):
        row,_=self.run_case('pass',{'status':'passed','checks':999})
        self.assertEqual(row,'failed')

    def test_failed_process_never_passes(self):
        row,_=self.run_case('import sys; sys.exit(1)')
        self.assertEqual(row,'failed')

    def test_missing_dependency_is_not_assessed(self):
        row,summary=self.run_case("import pathlib,json,sys; pathlib.Path(OUTPUT_PATH).write_text(json.dumps({'status':'NOT_ASSESSED','reason':'Missing test dependency'})); sys.exit(2)")
        self.assertEqual((row,summary),('NOT_ASSESSED','INCOMPLETE'))

    def test_fresh_green_stays_running_until_complete(self):
        row,summary=self.run_case("import pathlib,json; pathlib.Path(OUTPUT_PATH).write_text(json.dumps({'status':'passed','checks':10,'exceptions':0}))")
        self.assertEqual((row,summary),('passed','RUNNING'))

if __name__=='__main__': unittest.main()
