"""CPU trial using the pinned upstream GarmentCode pattern and cloth solver."""
import os, sys, json, time, argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'Sources/GarmentCode'
os.chdir(SOURCE)
sys.path.insert(0, str(SOURCE))

def main():
    import yaml, numpy as np, trimesh
    import warp as wp
    from assets.bodies.body_params import BodyParameters
    from assets.garment_programs.meta_garment import MetaGarment
    from pygarment.meshgen.boxmeshgen import BoxMesh
    from pygarment.meshgen.simulation import run_sim
    from pygarment.meshgen.sim_config import PathCofig
    from pygarment.data_config import Properties
    wp.init()
    wp.set_device('cpu')
    parser = argparse.ArgumentParser()
    parser.add_argument('name',choices=['tee','skirt','custom'],default='tee',nargs='?')
    parser.add_argument('--width',type=float,default=0.2)
    parser.add_argument('--front',type=float,default=0.4)
    parser.add_argument('--back',type=float,default=0.0)
    args = parser.parse_args()
    if not (-0.2 <= args.width <= 0.5 and 0.3 <= args.front <= 1.0 and 0 <= args.back <= 0.5):
        parser.error('Collar parameters outside the trial range')
    name = args.name
    design = yaml.safe_load(Path('assets/design_params/t-shirt.yaml').read_text())['design']
    for key,value in [('width',args.width),('fc_depth',args.front),('bc_depth',args.back)]:
        design['collar'][key]['v'] = value
    if name == 'skirt':
        design['meta']['upper']['v'] = None
        design['meta']['wb']['v'] = 'StraightWB'
        design['meta']['bottom']['v'] = 'SkirtCircle'
    body = BodyParameters('assets/bodies/mean_all.yaml')
    program = MetaGarment(name, body, design)
    pattern = program.assembly()
    inputs = ROOT / 'Outputs/Patterns'
    folder = Path(pattern.serialize(inputs, tag='', to_subfolder=True, with_3d=False,
                                    with_text=False, view_ids=False, with_printable=False))
    body.save(folder)
    (folder/'design_params.yaml').write_text(yaml.safe_dump({'design':design}))
    props = Properties('assets/Sim_props/gui_sim_props.yaml')
    props['sim']['config'].update(resolution_scale=2.0, max_sim_steps=1000,
        max_frame_time=None, max_sim_time=900, optimize_storage=False)
    props['render']['config']['uv_texture']['dpi'] = 200
    props['render']['config']['resolution'] = [640, 640]
    props.set_section_stats('sim', fails={},sim_time={},spf={},fin_frame={},body_collisions={},self_collisions={})
    props.set_section_stats('render',render_time={})
    paths = PathCofig(folder, ROOT/'Outputs/Draped', name, body_name='mean_all')
    started = time.perf_counter()
    mesh = BoxMesh(paths.in_g_spec, props['sim']['config']['resolution_scale'])
    mesh.load()
    mesh.serialize(paths, store_panels=False, uv_config=props['render']['config']['uv_texture'])
    run_sim(name, props, paths, store_usd=False, optimize_storage=False)
    props.serialize(paths.element_sim_props)
    garment = trimesh.load_mesh(paths.g_sim, process=False)
    scene = trimesh.Scene()
    body_mesh = trimesh.load_mesh(paths.in_body_obj, process=False)
    # Source body is metres; garment solver exports centimetres.
    garment.apply_scale(0.01)
    garment.visual = trimesh.visual.ColorVisuals(garment, vertex_colors=[107,163,194,255])
    body_mesh.visual = trimesh.visual.ColorVisuals(body_mesh, vertex_colors=[85,85,90,255])
    scene.add_geometry(body_mesh, node_name='body')
    scene.add_geometry(garment, node_name='garment')
    output = ROOT/'Outputs'/f'{name}.glb'
    scene.export(output)
    report = dict(garment=name, seconds=time.perf_counter()-started,
        vertices=len(garment.vertices),triangles=len(garment.faces),finite=bool(np.isfinite(garment.vertices).all()),
        sim=props['sim'],output=str(output), solver='upstream GarmentCode + its Warp fork CPU',
        overrides={'resolution_cm':2.0,'max_frames':1000,'timeout_seconds':900,'texture_dpi':200},
        neckline_parameters={'width':args.width,'front_depth':args.front,'back_depth':args.back})
    (ROOT/'Outputs'/f'{name}_result.json').write_text(json.dumps(report,indent=2))
    print('TRIAL_COMPLETE', json.dumps(report), flush=True)

if __name__ == '__main__':
    main()
