"""Record exactly which main-project sources match the isolated Unity trial."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEST = ROOT / 'Verification/CloseViewProject'
FILES = [
    'Assets/Resources/Fitting/female-mesh.bytes',
    'Assets/Resources/Fitting/female-rig.json',
    'Assets/Resources/Fitting/polo-contact-fit.json',
    'Assets/Resources/Fitting/cloth-materials.json',
    'Assets/Resources/Runway/DressFabric.shader',
] + [f'Assets/FashionFit/Runtime/{name}.cs' for name in (
    'ClothMaterialProfile', 'FittingClothSolver', 'FittingCloth',
    'InteractiveShirt', 'TailoredSurfaceDynamics', 'TailoredPoloStudy',
    'RunwayWardrobe', 'FashionFitApp', 'RunwayUI', 'RunwayAvatar',
    'TechnicalBodyFitData', 'WovenFabricDetail',
)] + [f'Assets/FashionFit/Editor/{name}.cs' for name in (
    'PoloFitCacheRefresh', 'MaterialMotionVerification', 'UnderarmVerification',
)]

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def run():
    # Editor verifier names can differ across historical trials; discover the
    # underarm verifier by its actual source path rather than inventing a file.
    files = [f for f in FILES if not f.endswith('/UnderarmVerification.cs')]
    files += [str(p.relative_to(ROOT)).replace('\\', '/')
              for p in (ROOT / 'Assets/FashionFit/Editor').glob('*Underarm*.cs')]
    result = {}
    for name in files:
        value = digest(ROOT / name)
        result[name] = {'sha256': value, 'matches_test_project': value == digest(TEST / name)}
    target = ROOT / 'Verification/MaterialMotion/source_snapshot.json'
    target.write_text(json.dumps(result, indent=2), encoding='utf-8')
    assert all(v['matches_test_project'] for v in result.values()), 'Main/test source mismatch; inspect snapshot'
    assert result[FILES[0]]['sha256'] == '1483998b04e614cb9541099f5a39964e6f141c70e539de87082dd774fc342b58', 'Unexpected body change'
    print(f'{len(result)} source files match isolated trial; body unchanged')

if __name__ == '__main__':
    run()
