using System;using System.IO;using UnityEngine;
namespace FashionFit {
 public static class DoffableMeshAsset {
  public sealed class Data {public Vector3[] rest;public Vector2[] uv;public int[] triangles,left,right;}
  public static Data Load(){
   var asset=Resources.Load<TextAsset>("Fitting/DoffableBlouseMesh");if(!asset)throw new InvalidOperationException("Thiếu lưới áo cài nút đã kiểm tra");
   using(var r=new BinaryReader(new MemoryStream(asset.bytes))){
    if(r.ReadInt32()!=0x44424d31)throw new InvalidDataException("Invalid blouse mesh");int n=r.ReadInt32(),ni=r.ReadInt32();if(n<10||n>3000||ni<3||ni>30000||ni%3!=0)throw new InvalidDataException("Invalid blouse counts");
    var data=new Data{rest=new Vector3[n],uv=new Vector2[n],triangles=new int[ni]};for(int i=0;i<n;i++)data.rest[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    for(int i=0;i<ni;i++){data.triangles[i]=r.ReadInt32();if(data.triangles[i]<0||data.triangles[i]>=n)throw new InvalidDataException("Blouse index out of range");}
    for(int i=0;i<n;i++)data.uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());data.left=Bank(r,n);data.right=Bank(r,n);return data;
   }
  }
  static int[] Bank(BinaryReader r,int vertices){int n=r.ReadInt32();if(n<4||n>vertices)throw new InvalidDataException("Invalid front bank");var bank=new int[n];for(int i=0;i<n;i++){bank[i]=r.ReadInt32();if(bank[i]<0||bank[i]>=vertices)throw new InvalidDataException("Invalid front bank index");}return bank;}
 }
}
