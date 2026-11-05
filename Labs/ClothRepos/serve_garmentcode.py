"""Start upstream GarmentCode GUI on loopback with CPU-friendly defaults."""
from pathlib import Path
import os, sys, runpy, copy
ROOT = Path(__file__).resolve().parent
SOURCE = ROOT/'Sources/GarmentCode'
os.chdir(SOURCE)
sys.path.insert(0, str(SOURCE))
import yaml
from nicegui import ui, app
@app.get('/api/health')
def health():
    return {'application':'GarmentCode CPU Trial','version':1}
import warp as wp
wp.init()
wp.set_device('cpu')
# Keep upstream source/config defaults intact. Patch only the configuration
# reader for GUI CPU runs; parameters are recorded in the trial report.
import pygarment.data_config as dc
OriginalProperties = dc.Properties
class CpuProperties(OriginalProperties):
    def __init__(self, filename=None, *args, **kwargs):
        super().__init__(filename, *args, **kwargs)
        if filename and Path(filename).name == 'gui_sim_props.yaml':
            self['sim']['config'].update(resolution_scale=2.0,max_sim_steps=1000,
                max_frame_time=None,max_sim_time=900)
            self['render']['config']['uv_texture']['dpi'] = 200
dc.Properties = CpuProperties
runpy.run_path('gui.py',run_name='garmentcode_trial_gui')
ui.run(host='127.0.0.1',port=8766,reload=False,show=False,
       title='GarmentCode - FashionFit CPU Trial')
