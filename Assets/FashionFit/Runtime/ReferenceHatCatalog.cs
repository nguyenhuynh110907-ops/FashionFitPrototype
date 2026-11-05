using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    // Rebuilt from the ten complete hats visible in the user's reference.
    // Meshes are authored geometry, not original brand CAD or an inferred scan.
    public sealed class ReferenceHatCatalog
    {
        public static readonly string[] Labels={"01 · Xanh vành phẳng","02 · Lưỡi trai đen","03 · Top hat đỏ","04 · Vành be · đai khóa","05 · Nón chong chóng","06 · Cowboy da nâu","07 · Lưỡi trai cam","08 · Fedora nâu","09 · Nỉ xám · đai bện","10 · Denim thêu chữ"};
        readonly List<Mesh> ownedMeshes;
        readonly List<Material> ownedMaterials;
        readonly Dictionary<string,Material> materialCache=new Dictionary<string,Material>();
        public ReferenceHatCatalog(List<Mesh> meshes,List<Material> materials){ownedMeshes=meshes;ownedMaterials=materials;}
        Material Fabric(string name,Color color,float kind=0,float smooth=.15f,float metallic=0)
        {
            if(materialCache.TryGetValue(name,out Material cached))return cached;
            var m=new Material(Resources.Load<Shader>("Runway/HatFabric")){name=name};m.SetColor("_Color",color);m.SetFloat("_TextureKind",kind);m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metallic);
            WovenFabricDetail.Apply(m,metallic<.1f&&(kind==1||kind==3));ownedMaterials.Add(m);materialCache.Add(name,m);return m;
        }
        public GameObject Build(int index,Transform parent,out Bounds bounds,out Vector3 fitOffset)
        {
            var root=new GameObject(Labels[index]);root.transform.SetParent(parent,false);var b=new Builder();
            var black=Fabric("Black twill",new Color(.019f,.022f,.025f),1);
            var stitches=Fabric("Muted dark thread",new Color(.028f,.032f,.036f),1);
            var silver=Fabric("Buckle brushed silver",new Color(.46f,.48f,.49f),0,.52f,.75f);
            fitOffset=new Vector3(0,-.045f,0);
            if(index==0||index==1||index==4||index==6||index==9){
                Material fabric=index==0?Fabric("Blue ash woven cotton",new Color(.30f,.52f,.70f),1):index==6?Fabric("Orange cotton",new Color(.88f,.28f,.045f),1):index==9?Fabric("Washed blue denim",new Color(.29f,.40f,.50f),3):black;
                Material thread=index==0?Fabric("Blue cap topstitch",new Color(.33f,.54f,.70f),1):index==6?Fabric("Orange cap topstitch",new Color(.78f,.245f,.04f),1):index==9?Fabric("Denim ivory thread",new Color(.52f,.55f,.53f),1):stitches;
                Material[] panels=null;
                if(index==4)panels=new[]{Fabric("Propeller cap orange",new Color(.91f,.31f,.035f),1),Fabric("Propeller cap yellow",new Color(.93f,.72f,.08f),1),Fabric("Propeller cap green",new Color(.12f,.44f,.24f),1),Fabric("Propeller cap blue",new Color(.08f,.27f,.60f),1),Fabric("Propeller cap red",new Color(.67f,.10f,.07f),1),Fabric("Propeller cap orange",new Color(.91f,.31f,.035f),1)};
                float curve=index==0?0:index==9?.7f:1;
                Cap(b,fabric,thread,panels,index==4?panels[2]:fabric,curve);
                if(index==0){b.Box(new Vector3(0,.05f,.108f),new Vector3(.024f,.028f,.002f),black);Label(root.transform,"686",new Vector3(0,.05f,.110f),.0011f,Color.white);}
                if(index==9){Label(root.transform,"KIMES",new Vector3(0,.048f,.112f),.0025f,new Color(.08f,.11f,.14f));Label(root.transform,"KIMES",new Vector3(0,.049f,.114f),.00225f,new Color(.77f,.79f,.73f));
                    var flourish=new List<Vector3>();for(int i=0;i<=32;i++){float x=Mathf.Lerp(-.048f,.048f,i/32f);flourish.Add(new Vector3(x,.019f-.006f*Mathf.Cos(x/.048f*Mathf.PI),.112f));}b.Tube(flourish,.0011f,thread);}
                fitOffset=new Vector3(0,-.078f,0);
                if(index==4){
                    b.Tube(new[]{new Vector3(0,.128f,0),new Vector3(0,.185f,0)},.003f,silver);
                    var rotor=new GameObject("Blue spinning propeller");rotor.transform.SetParent(root.transform,false);rotor.transform.localPosition=new Vector3(0,.18f,0);
                    var rb=new Builder();var blue=Fabric("Propeller blue",new Color(.06f,.31f,.72f),0,.4f);
                    rb.Surface(2,20,(r,s)=>{float z=Mathf.Lerp(-.155f,.155f,s/20f);float width=.012f+.008f*Mathf.Sin((s/20f)*Mathf.PI);return new Vector3((r==0?-1:1)*width,.003f*Mathf.Sin(z*14),z);},blue);
                    rb.Surface(2,20,(r,s)=>{float x=Mathf.Lerp(-.13f,.13f,s/20f);return new Vector3(x,0,(r==0?-1:1)*.01f);},blue);
                    Publish(rb,rotor,"Propeller rotor");rotor.AddComponent<RunwayHatPropeller>();
                    b.Tube(new[]{new Vector3(0,.18f,0),new Vector3(0,.212f,0)},.0025f,silver);
                    var red=Fabric("Propeller star red",new Color(.67f,.07f,.045f));b.Star(new Vector3(0,.224f,0),.014f,red);
                }
            }else{
                bool top=index==2,cowboy=index==5,beige=index==3,narrow=index==7;
                var felt=top?Fabric("Red wool felt",new Color(.52f,.035f,.11f)):beige?Fabric("Bone beige wool felt",new Color(.71f,.64f,.53f)):cowboy?Fabric("Weathered saddle leather",new Color(.25f,.135f,.065f),2,.28f):narrow?Fabric("Warm brown fedora felt",new Color(.32f,.22f,.13f)):Fabric("Moab grey brown wool",new Color(.29f,.265f,.225f));
                float height=top?.28f:cowboy?.155f:narrow?.135f:.165f;
                var profile=top?new[]{new Vector2(1,0),new Vector2(.99f,.025f),new Vector2(.94f,.15f),new Vector2(1.04f,.27f),new Vector2(1.03f,.28f),new Vector2(.75f,.282f),new Vector2(0,.282f)}:
                    new[]{new Vector2(1,0),new Vector2(1.0f,.035f),new Vector2(.94f,height*.72f),new Vector2(.83f,height),new Vector2(.6f,height+.002f),new Vector2(.3f,height-.012f),new Vector2(0,height-.018f)};
                b.Surface(profile.Length,64,(r,s)=>{
                    float a=2*Mathf.PI*s/64;float x=.101f*profile[r].x*Mathf.Cos(a),z=.119f*profile[r].x*Mathf.Sin(a),y=profile[r].y;
                    if(!top){float amount=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.055f,height,y));float pinch=Mathf.Exp(-Mathf.Pow((z-.07f)/.045f,2));x*=1-.20f*amount*pinch;y-=.018f*amount*Mathf.Exp(-x*x/.0012f);}
                    return new Vector3(x,y,z);
                },felt);
                float outer=top?.194f:cowboy?.23f:narrow?.168f:beige?.217f:.202f;
                Func<int,int,Vector3> brim=(r,s)=>{float t=r/8f,a=2*Mathf.PI*s/64;float radius=Mathf.Lerp(.098f,outer,t);float roll=cowboy?.065f*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),3):beige?.016f*Mathf.Cos(a)*Mathf.Cos(a):top?.008f:.018f*Mathf.Cos(a)*Mathf.Cos(a);float y=Mathf.Pow(t,3)*(roll-.014f*Mathf.Sin(a)*Mathf.Sin(a));return new Vector3(Mathf.Lerp(.101f,outer,t)*Mathf.Cos(a),y,Mathf.Lerp(.119f,outer*(top?.83f:1.12f),t)*Mathf.Sin(a));};
                b.Surface(9,64,brim,felt,true);
                b.Surface(9,64,(r,s)=>brim(r,s)-Vector3.up*.003f,felt);
                b.Surface(2,64,(r,s)=>brim(0,s)-Vector3.up*(r*.003f),felt);
                var edge=new List<Vector3>();for(int s=0;s<=64;s++)edge.Add(brim(8,s));b.Tube(edge,.0024f,felt);
                var band=top?Fabric("Red top hat grosgrain",new Color(.64f,.045f,.17f),1):beige?Fabric("Burgundy leather belt",new Color(.24f,.045f,.035f),2,.36f):narrow?black:Fabric("Brown leather hat band",new Color(.16f,.09f,.045f),2,.3f);
                b.Surface(3,64,(r,s)=>{float a=s*2*Mathf.PI/64,y=.018f+r*(top?.019f:.012f);return new Vector3(.103f*Mathf.Cos(a),y,.121f*Mathf.Sin(a));},band);
                if(!top){
                    if(index==8||cowboy){for(int strand=0;strand<2;strand++){var braid=new List<Vector3>();for(int s=0;s<=256;s++){float a=s*2*Mathf.PI/256;float weave=a*32+strand*Mathf.PI;braid.Add(new Vector3((.105f+.0008f*Mathf.Cos(weave))*Mathf.Cos(a),.035f+.004f*Mathf.Sin(weave),(.123f+.0008f*Mathf.Cos(weave))*Mathf.Sin(a)));}b.Tube(braid,.0012f,index==8?Fabric("Braided pale leather",new Color(.37f,.29f,.20f),2):band);}}
                    // Visible side buckle, positioned slightly forward on the wearer's left.
                    Vector3 centre=new Vector3(-.096f,.035f,.052f);Quaternion rotation=Quaternion.Euler(0,-64,0);
                    var bucklePath=new[]{new Vector3(-.012f,-.012f,0),new Vector3(.012f,-.012f,0),new Vector3(.012f,.012f,0),new Vector3(-.012f,.012f,0),new Vector3(-.012f,-.012f,0)};
                    for(int i=0;i<bucklePath.Length;i++)bucklePath[i]=centre+rotation*bucklePath[i];b.Tube(bucklePath,.0018f,silver);
                    b.Tube(new[]{centre+rotation*new Vector3(-.011f,0,0),centre+rotation*new Vector3(.011f,0,0)},.001f,silver);
                    if(cowboy){for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;b.Sphere(new Vector3(.105f*Mathf.Cos(a),.031f,.123f*Mathf.Sin(a)),new Vector3(.003f,.003f,.003f),silver,6,4);}}
                }
            }
            bounds=Publish(b,root,"Reference hat "+index);
            if(index==4){bounds.Encapsulate(new Vector3(.155f,.239f,.155f));bounds.Encapsulate(new Vector3(-.155f,.13f,-.155f));}
            return root;
        }
        void Cap(Builder b,Material cloth,Material thread,Material[] panels,Material visor,float curve)
        {
            const int sectors=6;
            Func<float,float,Vector3> crown=(a,beta)=>new Vector3(.094f*Mathf.Cos(beta)*Mathf.Cos(a),.126f*Mathf.Sin(beta),.110f*Mathf.Cos(beta)*Mathf.Sin(a));
            for(int section=0;section<sectors;section++){
                int sector=section;
                b.Surface(17,12,(r,s)=>crown((sector+s/12f)*2*Mathf.PI/6,r/16f*Mathf.PI*.5f),panels==null?cloth:panels[section]);
                for(int side=-1;side<=1;side+=2){var seam=new List<Vector3>();for(int r=0;r<=24;r++){float a=section*2*Mathf.PI/6+side*.013f;var p=crown(a,r/24f*Mathf.PI*.5f);p.x*=1.004f;p.z*=1.004f;seam.Add(p);}b.Tube(seam,.00055f,thread);}
                float theta=(section+.5f)*Mathf.PI/3;Vector3 eye=crown(theta,.35f);Vector3 n=new Vector3(Mathf.Cos(theta),.25f,Mathf.Sin(theta)).normalized;
                b.Ring(eye+n*.001f,n,.003f,.001f,thread);
            }
            Func<float,float,Vector3> bill=(u,v)=>{float width=Mathf.Sqrt(Mathf.Max(0,1-u*u));float inner=.110f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(.113f*u/.094f,2)))-.005f;return new Vector3(.113f*u,-.005f-.018f*curve*u*u-.010f*curve*v,inner+.17f*v*width);};
            // Sew the curved visor to the crown; a floating visor leaves a
            // visible air gap when the cap is inspected from the side.
            b.Surface(2,48,(r,s)=>{float a=s*Mathf.PI/48;var p=crown(a,0);float u=p.x/.113f;float inner=.110f*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(.113f*u/.094f,2)))-.005f;float v=Mathf.Clamp01((p.z-inner)/(.17f*Mathf.Sqrt(Mathf.Max(.001f,1-u*u))));if(r==1)p.y=bill(u,v).y-.001f;return p;},cloth);
            b.Surface(9,32,(r,s)=>bill(Mathf.Lerp(-1,1,s/32f),r/8f),visor);
            // Separate underside and perimeter give a visible 3 mm brim thickness.
            b.Surface(9,32,(r,s)=>bill(Mathf.Lerp(-1,1,s/32f),r/8f)-Vector3.up*.003f,visor);
            var outline=new List<Vector3>();for(int s=0;s<=48;s++)outline.Add(bill(Mathf.Lerp(-1,1,s/48f),1));b.Tube(outline,.0015f,thread);
            for(int k=0;k<3;k++){var stitch=new List<Vector3>();for(int s=0;s<=40;s++)stitch.Add(bill(Mathf.Lerp(-.94f,.94f,s/40f),.79f-k*.12f)+Vector3.up*.0008f);b.Tube(stitch,.00045f,thread);}
            b.Sphere(new Vector3(0,.128f,0),new Vector3(.007f,.0035f,.007f),cloth,16,6);
            b.Box(new Vector3(0,.015f,-.109f),new Vector3(.055f,.014f,.003f),thread);
            var band=new List<Vector3>();for(int s=0;s<=64;s++){float a=s*2*Mathf.PI/64;band.Add(crown(a,.014f));}b.Tube(band,.001f,thread);
        }
        static void Label(Transform parent,string text,Vector3 p,float size,Color color)
        {
            var label=RunwayStage.Text(parent,text,p,size*10,color);label.fontStyle=FontStyle.Bold;label.transform.localRotation=Quaternion.Euler(0,180,0);
        }
        Bounds Publish(Builder builder,GameObject go,string name)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var map=new int[builder.Vertices.Count];var unique=new Dictionary<Vector3Int,int>();for(int i=0;i<map.Length;i++){var p=builder.Vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*1000000),Mathf.RoundToInt(p.y*1000000),Mathf.RoundToInt(p.z*1000000));if(!unique.TryGetValue(key,out int id)){id=vertices.Count;unique.Add(key,id);vertices.Add(p);uv.Add(builder.UV[i]);}map[i]=id;}
            var mesh=new Mesh{name=name};if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=builder.Materials.Count;
            for(int i=0;i<builder.Materials.Count;i++){var triangles=new List<int>();var source=builder.Triangles[i];for(int k=0;k<source.Count;k+=3){int a=map[source[k]],b=map[source[k+1]],c=map[source[k+2]];if(a!=b&&b!=c&&a!=c)triangles.AddRange(new[]{a,b,c});}mesh.SetTriangles(triangles,i);}
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=builder.Materials.ToArray();ownedMeshes.Add(mesh);return mesh.bounds;
        }
        sealed class Builder
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();
            public readonly List<Vector2> UV=new List<Vector2>();
            public readonly List<Material> Materials=new List<Material>();
            public readonly List<List<int>> Triangles=new List<List<int>>();
            int Slot(Material material){int i=Materials.IndexOf(material);if(i<0){i=Materials.Count;Materials.Add(material);Triangles.Add(new List<int>());}return i;}
            public void Surface(int rows,int segments,Func<int,int,Vector3> point,Material material,bool reverse=false)
            {
                int start=Vertices.Count;var t=Triangles[Slot(material)];
                for(int r=0;r<rows;r++)for(int s=0;s<=segments;s++){
                    Vertices.Add(point(r,s));UV.Add(new Vector2(s/(float)segments,r/(float)(rows-1)));
                    if(r==0||s==segments)continue;int i=start+r*(segments+1)+s,j=i-(segments+1);t.Add(j);t.Add(reverse?i+1:i);t.Add(reverse?i:i+1);t.Add(j);t.Add(reverse?j+1:i+1);t.Add(reverse?i+1:j+1);
                }
            }
            public void Tube(IList<Vector3> path,float radius,Material material)
            {
                Surface(path.Count,5,(r,s)=>{Vector3 direction=(path[Mathf.Min(r+1,path.Count-1)]-path[Mathf.Max(0,r-1)]).normalized;
                    Vector3 x=Vector3.Cross(direction,Mathf.Abs(direction.y)>.8f?Vector3.forward:Vector3.up).normalized,y=Vector3.Cross(direction,x);float a=s*2*Mathf.PI/5;return path[r]+radius*(x*Mathf.Cos(a)+y*Mathf.Sin(a));},material);
            }
            public void Sphere(Vector3 centre,Vector3 size,Material material,int segments,int rings)
            {Surface(rings+1,segments,(r,s)=>{float a=s*2*Mathf.PI/segments,b=-Mathf.PI*.5f+r*Mathf.PI/rings;return centre+Vector3.Scale(size,new Vector3(Mathf.Cos(b)*Mathf.Cos(a),Mathf.Sin(b),Mathf.Cos(b)*Mathf.Sin(a)));},material);}
            public void Ring(Vector3 centre,Vector3 normal,float major,float minor,Material material)
            {Vector3 x=Vector3.Cross(normal,Vector3.up).normalized,y=Vector3.Cross(normal,x);var p=new List<Vector3>();for(int i=0;i<=16;i++){float a=i*Mathf.PI*2/16;p.Add(centre+major*(x*Mathf.Cos(a)+y*Mathf.Sin(a)));}Tube(p,minor,material);}
            public void Box(Vector3 centre,Vector3 size,Material material)
            {
                var t=Triangles[Slot(material)];int start=Vertices.Count;
                for(int i=0;i<8;i++){Vertices.Add(centre+Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));UV.Add(Vector2.zero);}
                int[] faces={0,2,3,0,3,1,4,5,7,4,7,6,0,1,5,0,5,4,2,6,7,2,7,3,0,4,6,0,6,2,1,3,7,1,7,5};foreach(int i in faces)t.Add(start+i);
            }
            public void Star(Vector3 centre,float radius,Material material)
            {
                var t=Triangles[Slot(material)];int start=Vertices.Count;Vertices.Add(centre);UV.Add(Vector2.zero);
                for(int i=0;i<=10;i++){float a=i*Mathf.PI/5+Mathf.PI*.5f;float r=i%2==0?radius:radius*.45f;Vertices.Add(centre+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));UV.Add(Vector2.zero);if(i>0){t.Add(start);t.Add(start+i);t.Add(start+i+1);}}
            }
        }
    }
    public sealed class RunwayHatPropeller : MonoBehaviour
    {
        void Update(){transform.Rotate(Vector3.up,180*Time.deltaTime,Space.Self);}
    }
}
