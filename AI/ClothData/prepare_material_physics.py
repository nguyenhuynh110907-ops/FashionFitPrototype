"""Read MIT's original workbook without modifying it; retain units/provenance.
Only mass and relative bending inform the runtime pilot. Other solver fields
are explicitly authored calibration, not laboratory measurements or ML output.
"""
import hashlib
import json
from pathlib import Path
import statistics
import zipfile
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
FOLDER=Path(__file__).resolve().parent/'Data/Physics/MIT'
NAMES=['Lycra','Faux Fur','Silk','Silk','Cotton','Wool','Taffeta','Linen','Corduroy','Cotton','Velvet','Fleece','Denim','Upholstery','Upholstery','Pleather','Minky','Damask Upholstery','Flannel Backed Vinyl','Upholstery','Outdoor Polyester','Silk','Wool','Canvas','Nylon Rip Stop','Terry Knit','Lycra','Laminated Cotton','Lycra','Upholstery']

def run():
    ns={'s':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'};rows=[]
    with zipfile.ZipFile(FOLDER/'matprop.xlsx') as z:
        strings=[''.join(t.itertext()) for t in ET.fromstring(z.read('xl/sharedStrings.xml')).findall('s:si',ns)]
        for row in ET.fromstring(z.read('xl/worksheets/sheet1.xml')).findall('.//s:sheetData/s:row',ns):
            cells={}
            for c in row.findall('s:c',ns):
                v=c.find('s:v',ns)
                if v is not None:cells[''.join(x for x in c.get('r') if x.isalpha())]=strings[int(v.text)] if c.get('t')=='s' else v.text
            if not cells.get('A','').isdigit():continue
            i=int(cells['A']);assert 1<=i<=30
            b,t,d,w=map(float,[cells[k] for k in ('C','D','E','F')]);confidence=int(cells['G'])
            assert min(b,t,d,w)>0 and 1<=confidence<=4
            # Independent dimensional consistency check on the workbook's area weight.
            unit_error=abs(w-d*t*16*36**2)/w
            assert unit_error<.03 # Some original records round density/thickness.
            rows.append(dict(fabric_id=i,name=NAMES[i-1],sample=cells['B'],excel_row=int(row.get('r')),bending_lbf_in2=b,bending_N_m2=b*4.4482216152605*.0254**2,thickness_mm=t*25.4,area_weight_oz_yd2=w,areal_mass_kg_m2=w*.028349523125/.9144**2,confidence=confidence,area_weight_unit_check_relative_error=unit_error))
    fabrics=[]
    for i in range(1,31):
        group=[r for r in rows if r['fabric_id']==i];assert group
        fabrics.append(dict(fabric_id=i,name=NAMES[i-1],samples=len(group),areal_mass_kg_m2=statistics.mean(r['areal_mass_kg_m2'] for r in group),bending_lbf_in2=statistics.mean(r['bending_lbf_in2'] for r in group),thickness_mm=statistics.mean(r['thickness_mm'] for r in group)))
    result=dict(source='https://people.csail.mit.edu/klbouman/pw/projects/materialproperties/dataset.html',download='https://people.csail.mit.edu/klbouman/pw/data/materialproperties/material_properties.zip',workbook_sha256=hashlib.sha256((FOLDER/'matprop.xlsx').read_bytes()).hexdigest(),citation='Bouman, Xiao, Battaglia, Freeman. Estimating the Material Properties of Fabric from Video. ICCV 2013.',usage='Research dataset; redistribution/commercial license not specified on source page. Original workbook remains outside Assets.',units_note='Bending EI is beam stiffness N m^2; specimen width is not provided, so it is NOT relabelled as plate rigidity N m. Sample letters are not labelled warp/weft.',rows=rows,fabrics=fabrics)
    (FOLDER/'measurements_si.json').write_text(json.dumps(result,indent=2))
    pilot=[]
    for name,i,damping,friction,rough in [('Lua nhe',3,.8,.18,.32),('Cotton',5,1.4,.32,.72),('Len co cau truc',6,2.0,.45,.86)]:
        f=fabrics[i-1];ratio=f['bending_lbf_in2']/.01
        pilot.append(dict(label=name,mit_fabric_id=i,arealMass=f['areal_mass_kg_m2'],bendCompliance=6/ratio,damping=damping,friction=friction,roughness=rough,stretchWarp=2e-5,stretchWeft=3e-5,shear=4e-4,airDrag=1.2))
    catalog=dict(schema_version=1,measured_fields=['arealMass'],relative_reference_fields=['bendCompliance'],authored_fields=['stretchWarp','stretchWeft','shear','damping','friction','airDrag','roughness'],note='Prototype calibration, not a garment-specific material identification or trained model.',profiles=pilot)
    target=ROOT/'Assets/Resources/Fitting/cloth-materials.json';target.write_text(json.dumps(catalog,indent=2));print('Fabric measurements:',len(rows),'specimens:',len(fabrics));print(json.dumps(pilot,indent=2))

if __name__=='__main__':run()
