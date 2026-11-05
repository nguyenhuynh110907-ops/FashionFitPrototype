using System;
using UnityEngine;

namespace FashionFit
{
    // Source materials stay referenced: no texture baking, atlas, remesh or UV replacement.
    public sealed class TransferredGarmentAsset:ScriptableObject
    {
        [Serializable] public sealed class Part {public string name,fittedGeometryKey;public Mesh mesh;public Material[] materials;public TransferMetrics metrics;}
        public int schema=1;
        public string displayName,targetBodyKey,sourceDescription,algorithm="paired-body-affine-residual-v1";
        public string[] targetBones;
        public Part[] parts;
        public bool stationarySamplesPassed;
        public bool continuousCollisionTested=false,selfCollisionTested=false;
        public bool CanPreview=>schema==1&&stationarySamplesPassed&&parts!=null&&parts.Length>0;
        public string Check(RunwayAvatar avatar)
        {
            if(!CanPreview)return "Gói chuyển chưa qua kiểm tra hình học tĩnh.";
            if(!avatar||targetBodyKey!=TailoredPoloStudy.GeometryKey(avatar.BodyMesh))return "Cơ thể đích đã khác bản dùng để chuyển. Cần căn và kiểm tra lại.";
            if(targetBones==null||targetBones.Length!=avatar.Bones.Length)return "Bộ xương đích không khớp.";
            for(int i=0;i<targetBones.Length;i++)if(targetBones[i]!=avatar.Bones[i].name)return "Thứ tự xương đích không khớp.";
            foreach(var part in parts){if(part==null||!part.mesh||part.materials==null||part.materials.Length!=part.mesh.subMeshCount||part.metrics==null||!part.metrics.Passes)return "Thiếu mesh, vật liệu hoặc kết quả kiểm tra phần trang phục.";if(part.fittedGeometryKey!=TailoredPoloStudy.GeometryKey(part.mesh))return "Mesh đã thay đổi sau kiểm tra. Cần tạo lại gói chuyển.";foreach(var material in part.materials)if(!material)return "Thiếu vật liệu gốc.";}
            return null;
        }
        public string CheckNeutralRig(RunwayAvatar avatar)
        {
            foreach(var part in parts){var bind=part.mesh.bindposes;if(bind.Length!=avatar.Bones.Length)return "Thiếu bind pose đích.";for(int b=0;b<bind.Length;b++){Matrix4x4 m=avatar.transform.worldToLocalMatrix*avatar.Bones[b].localToWorldMatrix*bind[b];for(int row=0;row<4;row++)for(int col=0;col<4;col++)if(Mathf.Abs(m[row,col]-(row==col?1:0))>.0001f)return "Tư thế nghỉ/bộ xương đã thay đổi. Cần tạo lại bản chuyển.";}}
            return null;
        }
    }
}
