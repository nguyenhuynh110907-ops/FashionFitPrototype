"""Tiny authored import fixtures, not training data or third-party clothing."""
import io
import json
from pathlib import Path
import struct
import sys
from PIL import Image

folder = Path(sys.argv[1]) / 'Assets/FashionFit/TransferSamples'
folder.mkdir(parents=True, exist_ok=True)
image = Image.new('RGB', (2, 2))
image.putdata([(210, 50, 80), (30, 160, 120), (30, 160, 120), (210, 50, 80)])
buffer = io.BytesIO()
image.save(buffer, format='PNG')
png = buffer.getvalue()
(folder/'checker.png').write_bytes(png)

positions = [(-.12,1.10,.30),(-.02,1.10,.30),(-.12,1.20,.30),(.02,1.10,.30),(.12,1.10,.30),(.12,1.20,.30)]
data = bytearray()
views, accessors = [], []
def view(blob, target=None):
    while len(data) % 4:
        data.append(0)
    result = len(views)
    item = dict(buffer=0, byteOffset=len(data), byteLength=len(blob))
    if target:
        item['target'] = target
    views.append(item)
    data.extend(blob)
    return result
def attribute(values, kind, width):
    flat = [v for row in values for v in row]
    index = view(struct.pack('<'+'f'*len(flat), *flat), 34962)
    accessors.append(dict(bufferView=index, componentType=5126, count=len(values), type=kind))
    if kind == 'VEC3':
        accessors[-1]['min'] = [min(row[a] for row in values) for a in range(width)]
        accessors[-1]['max'] = [max(row[a] for row in values) for a in range(width)]
    return len(accessors)-1
primitives = []
for part in range(2):
    attrs = dict(POSITION=attribute(positions[part*3:part*3+3], 'VEC3', 3),
                 NORMAL=attribute([(0,0,1)]*3, 'VEC3', 3),
                 TANGENT=attribute([(1,0,0,1)]*3, 'VEC4', 4),
                 TEXCOORD_0=attribute([(0,0),(1,0),(0,1)], 'VEC2', 2),
                 TEXCOORD_1=attribute([(.1,.1),(.8,.1),(.1,.8)], 'VEC2', 2))
    indices = view(struct.pack('<3H',0,1,2),34963)
    accessors.append(dict(bufferView=indices, componentType=5123, count=3, type='SCALAR'))
    primitives.append(dict(attributes=attrs, indices=len(accessors)-1, material=part, mode=4))
image_view = view(png)
document = dict(asset=dict(version='2.0', generator='FashionFit authored import fixture'),
                scene=0, scenes=[dict(nodes=[0])], nodes=[dict(name='TwoMaterialDetail',mesh=0)],
                meshes=[dict(name='TwoMaterialDetail',primitives=primitives)],
                materials=[dict(name='PatternedFabric',pbrMetallicRoughness=dict(baseColorTexture=dict(index=0),roughnessFactor=.7,metallicFactor=0)),
                           dict(name='GoldTrim',pbrMetallicRoughness=dict(baseColorFactor=[.8,.55,.12,1],roughnessFactor=.3,metallicFactor=.8))],
                images=[dict(bufferView=image_view,mimeType='image/png')], textures=[dict(source=0)],
                buffers=[dict(byteLength=len(data))], bufferViews=views, accessors=accessors)
header = json.dumps(document,separators=(',',':')).encode()
while len(header)%4:
    header += b' '
while len(data)%4:
    data.append(0)
glb = struct.pack('<III',0x46546c67,2,12+8+len(header)+8+len(data)) + struct.pack('<II',len(header),0x4e4f534a)+header+struct.pack('<II',len(data),0x004e4942)+data
(folder/'DetailFixture.glb').write_bytes(glb)
obj = ['mtllib DetailFixture.mtl','o TwoMaterialDetail']
obj += ['v '+' '.join(map(str,p)) for p in positions]
obj += ['vt 0 0','vt 1 0','vt 0 1','vn 0 0 1','usemtl PatternedFabric','f 1/1/1 2/2/1 3/3/1','usemtl GoldTrim','f 4/1/1 5/2/1 6/3/1']
(folder/'DetailFixture.obj').write_text('\n'.join(obj)+'\n',encoding='utf-8')
(folder/'DetailFixture.mtl').write_text('newmtl PatternedFabric\nKd 1 1 1\nmap_Kd checker.png\nnewmtl GoldTrim\nKd 0.8 0.55 0.12\nNs 60\n',encoding='utf-8')
vertices = ','.join(str(x) for p in positions for x in p)
fbx = '''; FBX 7.4.0 project file — authored import fixture
FBXHeaderExtension:  { FBXHeaderVersion: 1003
    FBXVersion: 7400
}
GlobalSettings:  { Version: 1000
    Properties70:  {
        P: "UpAxis", "int", "Integer", "",1
        P: "UpAxisSign", "int", "Integer", "",1
        P: "FrontAxis", "int", "Integer", "",2
        P: "FrontAxisSign", "int", "Integer", "",-1
        P: "CoordAxis", "int", "Integer", "",0
        P: "CoordAxisSign", "int", "Integer", "",1
        P: "UnitScaleFactor", "double", "Number", "",100
    }
}
Documents:  { Count: 1
    Document: 100, "Scene", "Scene" { RootNode: 0 }
}
Definitions:  { Version: 100
    Count: 4
    ObjectType: "Model" { Count: 1 }
    ObjectType: "Geometry" { Count: 1 }
    ObjectType: "Material" { Count: 2 }
}
Objects:  {
    Geometry: 1001, "Geometry::TwoMaterialDetail", "Mesh" {
        GeometryVersion: 124
        Vertices: *18 { a: VERTICES }
        PolygonVertexIndex: *6 { a: 0,1,-3,3,4,-6 }
        LayerElementNormal: 0 { Version: 101
            Name: ""
            MappingInformationType: "ByPolygonVertex"
            ReferenceInformationType: "Direct"
            Normals: *18 { a: 0,0,1,0,0,1,0,0,1,0,0,1,0,0,1,0,0,1 }
        }
        LayerElementUV: 0 { Version: 101
            Name: "UVMap"
            MappingInformationType: "ByPolygonVertex"
            ReferenceInformationType: "IndexToDirect"
            UV: *6 { a: 0,0,1,0,0,1 }
            UVIndex: *6 { a: 0,1,2,0,1,2 }
        }
        LayerElementMaterial: 0 { Version: 101
            Name: ""
            MappingInformationType: "ByPolygon"
            ReferenceInformationType: "IndexToDirect"
            Materials: *2 { a: 0,1 }
        }
        Layer: 0 { Version: 100
            LayerElement: { Type: "LayerElementNormal"
                TypedIndex: 0
            }
            LayerElement: { Type: "LayerElementUV"
                TypedIndex: 0
            }
            LayerElement: { Type: "LayerElementMaterial"
                TypedIndex: 0
            }
        }
    }
    Model: 1002, "Model::TwoMaterialDetail", "Mesh" { Version: 232
        Properties70:  {
            P: "Lcl Translation", "Lcl Translation", "", "A",0,0,0
            P: "Lcl Rotation", "Lcl Rotation", "", "A",0,0,0
            P: "Lcl Scaling", "Lcl Scaling", "", "A",1,1,1
        }
        Shading: T
        Culling: "CullingOff"
    }
    Material: 1003, "Material::PatternedFabric", "" { Version: 102
        ShadingModel: "phong"
        MultiLayer: 0
        Properties70:  { P: "DiffuseColor", "Color", "", "A",0.8,0.2,0.3 }
    }
    Material: 1004, "Material::GoldTrim", "" { Version: 102
        ShadingModel: "phong"
        MultiLayer: 0
        Properties70:  { P: "DiffuseColor", "Color", "", "A",0.8,0.55,0.12 }
    }
}
Connections:  {
    C: "OO",1001,1002
    C: "OO",1002,0
    C: "OO",1003,1002
    C: "OO",1004,1002
}
'''.replace('VERTICES',vertices)
fbx = fbx.replace('{', '{\n').replace('}', '\n}\n')
(folder/'DetailFixture.fbx').write_text(fbx,encoding='ascii',errors='replace')
print(json.dumps({'folder':str(folder),'formats':['fbx','glb','obj'],'glb_bytes':len(glb)}))
