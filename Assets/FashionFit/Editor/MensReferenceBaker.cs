#if UNITY_EDITOR
using System;using System.IO;using System.Collections.Generic;using System.Reflection;using UnityEngine;using UnityEngine.Rendering;using UnityEditor;
namespace FashionFit.Editor {
 public static class MensReferenceBaker {
  const string Folder="Assets/Resources/MensReference";
  static RunwayAvatar body;static GarmentSurfaceTransfer.Surface field;
  static Vector3[] clothFront,pantFront;static int[] clothTriangles,pantTriangles;static List<Vector3> v;static List<Vector2> uv;static List<BoneWeight> skin;static List<int>[] faces;
  static BoneWeight Binding(Vector3 p){var n=field.Nearest(p);var t=body.BodyMesh.triangles;var w=body.BodyMesh.boneWeights;return FittedGarmentGeometry.Average(w[t[n.triangle*3]],w[t[n.triangle*3+1]],w[t[n.triangle*3+2]]);}
  static int Add(Vector3 p){int i=v.Count;v.Add(p);uv.Add(new Vector2((p.x+.4f)/.8f,p.y));skin.Add(Binding(p));return i;}
  static void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material){int start=Add(a);Add(b);Add(c);Add(d);faces[material].AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
  static void Append(Mesh mesh,int material,Func<Vector3,Vector3> shape,Func<Vector3,bool> keep=null,int limit=int.MaxValue){var p=mesh.vertices;var w=mesh.boneWeights;var tri=mesh.triangles;var remap=new Dictionary<int,int>();for(int f=0;f<tri.Length;f+=3){int a=tri[f],b=tri[f+1],c=tri[f+2];if(a>=limit||b>=limit||c>=limit||(keep!=null&&(!keep(p[a])||!keep(p[b])||!keep(p[c]))))continue;for(int k=0;k<3;k++){int index=tri[f+k];if(!remap.TryGetValue(index,out int id)){id=v.Count;remap.Add(index,id);var q=shape(p[index]);v.Add(q);uv.Add(new Vector2((q.x+.4f)/.8f,q.y));skin.Add(w[index]);}faces[material].Add(id);}}}
  static void CollarPanel(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material){
   const int steps=6;int first=v.Count;
   for(int row=0;row<=steps;row++)for(int col=0;col<=steps;col++){
    float u=col/(float)steps,t=row/(float)steps;
    Vector3 p=Vector3.Lerp(Vector3.Lerp(a,b,u),Vector3.Lerp(d,c,u),t);
    Add(field.ProjectOutside(p,.022f));
    if(row>0&&col<steps){int x=first+(row-1)*(steps+1)+col,y=x+steps+1;faces[material].AddRange(new[]{x,x+1,y,x+1,y+1,y});}
   }
  }
  static void PocketStrip(float side,float x,float y){
   const int steps=12;int first=v.Count;
   for(int row=0;row<=steps;row++){
    float t=row/(float)steps,px=Mathf.Lerp(x-side*.035f,x+side*.025f,t),py=Mathf.Lerp(y+.05f,y-.03f,t);
    for(int col=0;col<2;col++){float xx=px+side*col*.003f;Add(field.ProjectOutside(new Vector3(xx,py,Front(xx,py,pantFront)),.022f));}
    if(row>0){int a=first+(row-1)*2,b=a+2;faces[3].AddRange(new[]{a,a+1,b,a+1,b+1,b});}
   }
  }
  static void Ring(float y,int mat,float width=.016f){
   var envelope=new GarmentCloseFit.TorsoEnvelope(body,y-width,y);
   Vector3 Point(float angle,float level){var direction=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));float radius=envelope.Radius(direction,level,.022f);return new Vector3(direction.x*radius,level,direction.y*radius);}
   for(int i=0;i<48;i++){float a=2*Mathf.PI*i/48,b=2*Mathf.PI*(i+1)/48;Quad(Point(a,y),Point(b,y),Point(b,y-width),Point(a,y-width),mat);}
  }
  static bool RayFront(Vector3[] points,int[] triangles,float x,float y,out float z){
   z=float.NegativeInfinity;
   for(int i=0;i<triangles.Length;i+=3){
    var a=points[triangles[i]];var b=points[triangles[i+1]];var c=points[triangles[i+2]];
    float determinant=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
    if(Mathf.Abs(determinant)<1e-10f)continue;
    float u=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/determinant;
    float v=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/determinant;
    if(u<-.0001f||v<-.0001f||u+v>1.0001f)continue;
    z=Mathf.Max(z,u*a.z+v*b.z+(1-u-v)*c.z);
   }
   return !float.IsNegativeInfinity(z);
  }
  static float Front(float x,float y,Vector3[] source=null){
   var points=source??clothFront;var triangles=source==pantFront?pantTriangles:clothTriangles;
   bool hasShirt=RayFront(points,triangles,x,y,out float z);
   if(RayFront(body.BodyVertices,body.BodyMesh.triangles,x,y,out float bodyZ))return Mathf.Max(hasShirt?z+.003f:float.NegativeInfinity,bodyZ+.025f);
   return hasShirt?z+.003f:field.Nearest(new Vector3(x,y,.3f)).point.z+.025f;
  }
  static void Button(float x,float y,float z){int center=Add(new Vector3(x,y,z+.003f));for(int i=0;i<14;i++){float a=i*2*Mathf.PI/14,b=(i+1)*2*Mathf.PI/14;int p=Add(new Vector3(x+Mathf.Cos(a)*.0045f,y+Mathf.Sin(a)*.0045f,z)),q=Add(new Vector3(x+Mathf.Cos(b)*.0045f,y+Mathf.Sin(b)*.0045f,z));faces[3].AddRange(new[]{center,p,q});}}
  static Mesh Build(int style){v=new List<Vector3>();uv=new List<Vector2>();skin=new List<BoneWeight>();faces=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>()};bool longSleeves=style>=2;float h=body.BodyMesh.bounds.max.y;FittedGarmentGeometry.NeckProfile(body,out float neckY,out float rx,out float rz,out float cz);neckY=GarmentCloseFit.NecklineHeight(body);
   var top=ContinuousShirtShell.Build(body,longSleeves);float ease=style==0?.006f:style==2?.008f:style==3?.001f:.003f;
   Append(top,0,p=>{float trunk=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.33f,Mathf.Abs(p.x)));p.x+=Mathf.Sign(p.x)*ease*trunk;p.z+=Mathf.Sign(p.z)*ease*trunk;return p;},p=>p.y<=neckY+.003f,int.MaxValue);UnityEngine.Object.DestroyImmediate(top);
   clothFront=v.ToArray();clothTriangles=faces[0].ToArray();
   // Two leg panels meet at front/back rise seams, then form one hip outline.
   // The cloth is authored as trousers, without copying anatomical crotch faces.
   var hipEnvelope=new GarmentCloseFit.TorsoEnvelope(body,.78f,h*.60f+.018f);int pantsStart=v.Count;for(int side=-1;side<=1;side+=2){int first=v.Count,columns=32,rows=30;for(int row=0;row<=rows;row++){float y=Mathf.Lerp(.18f,h*.60f+.018f,row/(float)rows),rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,.94f,y));var section=GarmentCloseFit.LegSection(body,side,y,style==0||style==2?.014f:.010f);for(int col=0;col<=columns;col++){float a=2*Mathf.PI*col/columns;Vector3 leg=new Vector3(section.x-side*section.z*Mathf.Cos(a),y,section.y+section.w*Mathf.Sin(a));Vector2 direction=new Vector2(side*Mathf.Sin(a*.5f),Mathf.Cos(a*.5f));float radius=hipEnvelope.Radius(direction,y,.012f);Vector3 hip=new Vector3(direction.x*radius,y,direction.y*radius);Add(Vector3.Lerp(leg,hip,rise));if(row>0&&col<columns){int b=first+(row-1)*(columns+1)+col,c=b+columns+1;faces[1].AddRange(new[]{b,b+1,c,b+1,c+1,c});}}}}
   pantFront=v.GetRange(pantsStart,v.Count-pantsStart).ToArray();pantTriangles=faces[1].ConvertAll(index=>index-pantsStart).ToArray();
   // Rib neck binding or folded collar: geometry rather than a painted line.
   Ring(neckY+.002f,2,style==0?.014f:.018f);
   if(style==1||style==2){for(int side=-1;side<=1;side+=2){float x=side*(rx+.012f),tipX=side*(rx+.035f);CollarPanel(new Vector3(side*.008f,neckY-.008f,Front(side*.008f,neckY-.008f)),new Vector3(x,neckY-.006f,Front(x,neckY-.006f)),new Vector3(tipX,neckY-.048f,Front(tipX,neckY-.048f)),new Vector3(side*.038f,neckY-.065f,Front(side*.038f,neckY-.065f)),2);}}
   if(style==1||style==2||style==3){float topY=neckY-.010f,bottomY=topY-(style==3?.17f:.145f);int rows=14;for(int i=0;i<rows;i++){float a=Mathf.Lerp(topY,bottomY,i/(float)rows),b=Mathf.Lerp(topY,bottomY,(i+1)/(float)rows);Quad(new Vector3(-.013f,a,Front(0,a)),new Vector3(.013f,a,Front(0,a)),new Vector3(.013f,b,Front(0,b)),new Vector3(-.013f,b,Front(0,b)),2);}for(int i=0;i<3;i++){float y=topY-.035f-i*.045f;Button(0,y,Front(0,y)+.009f);}}
   // Subtle real seam strips and pocket mouths, independently shaded.
   float waist=h*.60f+.006f;for(int side=-1;side<=1;side+=2)PocketStrip(side,side*.14f,waist-.09f);
   // Adjacent seam rows must share particles. Independent coincident quads
   // otherwise receive different contact corrections and open visible cracks.
   var unique=new Dictionary<Vector3Int,int>();var vv=new List<Vector3>();var uu=new List<Vector2>();var ww=new List<BoneWeight>();var map=new int[v.Count];for(int i=0;i<v.Count;i++){var p=v[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));if(!unique.TryGetValue(key,out int id)){id=vv.Count;unique.Add(key,id);vv.Add(p);uu.Add(uv[i]);ww.Add(skin[i]);}map[i]=id;}for(int m=0;m<4;m++){var compact=new List<int>();for(int k=0;k<faces[m].Count;k+=3){int a=map[faces[m][k]],b=map[faces[m][k+1]],c=map[faces[m][k+2]];if(a!=b&&b!=c&&a!=c)compact.AddRange(new[]{a,b,c});}faces[m]=compact;}v=vv;uv=uu;skin=ww;
   var mesh=new Mesh{name="Male reference "+MensReferenceCollection.Names[style],indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.boneWeights=skin.ToArray();mesh.bindposes=body.GarmentBindposes;mesh.subMeshCount=4;for(int i=0;i<4;i++)mesh.SetTriangles(faces[i],i);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
  }
  static void Save(UnityEngine.Object obj,string path){var old=AssetDatabase.LoadMainAssetAtPath(path);if(old){EditorUtility.CopySerialized(obj,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(obj);}else AssetDatabase.CreateAsset(obj,path);}
  static Material Fabric(string name,Color color,Texture2D pattern,Texture2D normal,float smooth=.14f){var m=new Material(Resources.Load<Shader>("Runway/DressFabric")){name=name};m.SetColor("_Color",color);m.SetFloat("_Glossiness",smooth);m.SetFloat("_Weave",.7f);if(pattern){m.SetTexture("_BaseMap",pattern);m.SetFloat("_UseBase",1);}m.SetTexture("_ClothNormal",normal);m.SetFloat("_UseClothDetail",1);m.SetFloat("_ClothDetailStrength",.14f);return m;}
  static Texture2D Texture(string name,int kind){int n=256;var tex=new Texture2D(n,n,TextureFormat.RGB24,true,kind!=0);var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){float a=x/(float)n,b=y/(float)n;if(kind==0){bool stripe=Mathf.FloorToInt(a*8)%2==0;pixels[y*n+x]=stripe?new Color(.72f,.12f,.21f):new Color(.11f,.12f,.14f);}else{float sx=kind==2?Mathf.Sin(a*Mathf.PI*2*40)*.10f:Mathf.Sin(a*Mathf.PI*2*32)*.035f,sy=Mathf.Sin(b*Mathf.PI*2*32)*.035f;pixels[y*n+x]=new Color(.5f+sx,.5f+sy,1);}}tex.SetPixels(pixels);tex.Apply();string path=Folder+"/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=kind==0;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  public static void RunBatch(){GameObject root=null;try{Directory.CreateDirectory(Folder);AssetDatabase.Refresh();root=new GameObject("Author five mens reference outfits");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new Exception(app.FatalError);body=app.PoseStudio.Rigs[1].Avatar;field=new GarmentSurfaceTransfer.Surface(body.BodyVertices,body.BodyMesh.triangles);
   var stripe=Texture("AuthoredRugbyStripes",0);var weave=Texture("AuthoredPiqueNormal",1);var rib=Texture("AuthoredRibNormal",2);
   Color[] topColors={new Color(.82f,.81f,.75f),new Color(.065f,.13f,.22f),Color.white,new Color(.28f,.31f,.21f),new Color(.75f,.73f,.68f)};
   Color[] pantColors={new Color(.25f,.36f,.46f),new Color(.53f,.43f,.29f),new Color(.11f,.18f,.25f),new Color(.14f,.15f,.16f),new Color(.26f,.29f,.20f)};
   for(int i=0;i<5;i++){string stem=Folder+"/Male"+(i+1).ToString("00");var mesh=Build(i);Save(mesh,stem+"_Mesh.asset");var materials=new Material[4];Material[] authored={Fabric("Top "+i,topColors[i],i==2?stripe:null,i==3?rib:weave),Fabric("Pants "+i,pantColors[i],null,weave,.12f),Fabric("Collar and placket "+i,i==2?new Color(.83f,.82f,.77f):topColors[i]*.94f,null,rib),Fabric("Buttons and stitching "+i,new Color(.45f,.39f,.28f),null,weave,.22f)};for(int m=0;m<4;m++){Save(authored[m],stem+"_Material"+m+".mat");materials[m]=AssetDatabase.LoadAssetAtPath<Material>(stem+"_Material"+m+".mat");}var asset=ScriptableObject.CreateInstance<PoseOutfitAsset>();asset.look=i+7;asset.label=MensReferenceCollection.Label(i+7)+" · bản dựng tham chiếu";asset.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(stem+"_Mesh.asset");asset.materials=materials;Save(asset,stem+".asset");}
   var polo=AssetDatabase.LoadAssetAtPath<PoseOutfitAsset>(Folder+"/Male02.asset");
   var legacy=AssetDatabase.LoadAssetAtPath<PoseOutfitAsset>("Assets/Resources/PoseWardrobe/Male06.asset");
   if(legacy&&legacy.mesh&&polo&&polo.mesh){
    EditorUtility.CopySerialized(polo.mesh,legacy.mesh);EditorUtility.SetDirty(legacy.mesh);
    legacy.materials=polo.materials;EditorUtility.SetDirty(legacy);
   }
   AssetDatabase.SaveAssets();UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}}
 }
}
#endif
