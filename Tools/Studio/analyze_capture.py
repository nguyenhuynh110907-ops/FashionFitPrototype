"""Independent CSV/JSON consistency and per-phase timing analysis."""
from pathlib import Path
import argparse,csv,hashlib,json,math,sys

def metric(values):
    v=sorted(values)
    if not v: return dict(status='NOT_ASSESSED',samples=0)
    return dict(status='RECORDED',samples=len(v),mean=sum(v)/len(v),p50=v[math.ceil(len(v)*.5)-1],p95=v[math.ceil(len(v)*.95)-1],p99=v[math.ceil(len(v)*.99)-1],max=v[-1])

def analyze(folder):
    report=json.loads((folder/'performance.json').read_text(encoding='utf-8-sig'))
    with (folder/'frames.csv').open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
    if len(rows)!=report['frames'] or not rows: raise ValueError('CSV/JSON frame count mismatch or empty capture')
    rendered=[r for r in rows if r['camera_rendered']=='1']
    if len(rendered)!=report['renderedFrames']: raise ValueError('Render count mismatch')
    counters=[('wall_frame_ms','wallFrameMs',0),('cpu_frame_including_wait_ms','cpuFrameIncludingWaitMs',0),('cpu_main_thread_ms','cpuMainThreadMs',0),('gpu_ms','gpuFrameMs',0),('gc_allocated_bytes','gcAllocatedBytes',-1),('last_pose_step_ms','lastPoseStepMs',0)]
    for column,key in [('unique_pose_solve_ms','uniquePoseSolveMs'),('unique_pose_publish_ms','uniquePosePublishMs')]:
        if key in report: counters.append((column,key,-1))
    for column,key,minimum in counters:
        values=[float(r[column]) for r in rows]
        if not all(math.isfinite(v) for v in values): raise ValueError('Nonfinite capture values')
        measured=metric([v for v in values if v>minimum]); original=report[key]
        if measured['samples']!=original['samples'] or measured['status']!=original['status']: raise ValueError('Counter availability mismatch: '+key)
        if measured['samples']:
            for q in ('p50','p95','p99','max','mean'):
                if abs(measured[q]-original[q])>.00011: raise ValueError('Metric mismatch: '+key+'/'+q)
    phases=[]
    for phase in dict.fromkeys(r['phase'] for r in rows):
        scope=[r for r in rendered if r['phase']==phase]
        phases.append(dict(phase=phase,renderedWallFrameMs=metric([float(r['wall_frame_ms']) for r in scope]),
                           cpuMainThreadMs=metric([float(r['cpu_main_thread_ms']) for r in scope if float(r['cpu_main_thread_ms'])>0])))
    r=dict(status='passed',csvSha256=hashlib.sha256((folder/'frames.csv').read_bytes()).hexdigest(),frames=len(rows),renderedFrames=len(rendered),
           fullyRenderedFraction=len(rendered)/len(rows),syntheticInput=report['syntheticInput'],headsetAssessment=report['headsetAssessment'],
           note='Validates recording and math, not performance acceptance. Phase tags and delayed platform CPU/GPU timings are not exact per-phase attribution.',phases=phases)
    (folder/'capture_analysis.json').write_text(json.dumps(r,indent=2),encoding='utf-8');return r

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('folder',type=Path);a=p.parse_args()
    try:
        r=analyze(a.folder);print(json.dumps(r,indent=2));sys.exit(0)
    except Exception as e: print('FAILED',e);sys.exit(1)
