using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
namespace FashionFit
{
    public static class RunwayOBJ
    {
        public static Mesh Read(string path)
        {
            if(!string.Equals(Path.GetExtension(path),".obj",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Bản này nhận OBJ. FBX/GLB cần chuyển sang OBJ trước.");
            if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("OBJ tối đa 20 MB.");
            var positions=new List<Vector3>();var texcoords=new List<Vector2>();var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();var map=new Dictionary<string,int>();
            foreach(string line in File.ReadLines(path)){
                string clean=line.Split('#')[0].Trim();if(clean.Length==0)continue;string[] s=clean.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
                if(s[0]=="v"&&s.Length>=4){positions.Add(new Vector3(-F(s[1]),F(s[2]),F(s[3])));if(positions.Count>100000)throw new InvalidDataException("OBJ tối đa 100.000 đỉnh.");}
                else if(s[0]=="vt"&&s.Length>=3)texcoords.Add(new Vector2(F(s[1]),F(s[2])));
                else if(s[0]=="f"&&s.Length>=4){
                    if(s.Length>256)throw new InvalidDataException("Mặt OBJ quá phức tạp. Hãy triangulate trước.");
                    var face=new int[s.Length-1];
                    for(int j=1;j<s.Length;j++){
                        // Resolve relative indices before caching: -1 can refer to different vertices on later lines.
                        string[] refParts=s[j].Split('/');int pi=Index(refParts[0],positions.Count),ti=refParts.Length>1&&refParts[1].Length>0?Index(refParts[1],texcoords.Count):-1;string key=pi+"/"+ti;
                        if(!map.TryGetValue(key,out int vi)){vi=vertices.Count;map[key]=vi;vertices.Add(positions[pi]);uv.Add(ti>=0?texcoords[ti]:Vector2.zero);if(vertices.Count>150000)throw new InvalidDataException("OBJ có quá nhiều đường UV.");}face[j-1]=vi;
                    }
                    for(int j=1;j<face.Length-1;j++){triangles.Add(face[0]);triangles.Add(face[j+1]);triangles.Add(face[j]);}
                    if(triangles.Count>600000)throw new InvalidDataException("OBJ tối đa 200.000 tam giác.");
                }
            }
            if(triangles.Count==0)throw new InvalidDataException("Không tìm thấy bề mặt trong OBJ.");
            var mesh=new Mesh{name=Path.GetFileName(path),indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static float F(string s){float n=float.Parse(s,CultureInfo.InvariantCulture);if(float.IsNaN(n)||float.IsInfinity(n)||Mathf.Abs(n)>1e6f)throw new InvalidDataException("Tọa độ OBJ không hợp lệ.");return n;}
        static int Index(string s,int count){int n=int.Parse(s,CultureInfo.InvariantCulture);n=n<0?count+n:n-1;if(n<0||n>=count)throw new InvalidDataException("Chỉ số OBJ nằm ngoài dữ liệu.");return n;}
    }
}
