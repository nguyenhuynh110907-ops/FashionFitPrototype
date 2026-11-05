using UnityEngine;

namespace FashionFit
{
    public static class SkinnedBounds
    {
        // Unity evaluates a skinned renderer's culling box in root-bone space.
        // Convert the measured model-space box, including all eight corners.
        public static void SetFromModel(SkinnedMeshRenderer renderer,Transform model,Bounds bounds,float padding)
        {
            bounds.Expand(padding*2);
            Transform root=renderer.rootBone!=null?renderer.rootBone:renderer.transform;
            Matrix4x4 matrix=root.worldToLocalMatrix*model.localToWorldMatrix;
            Bounds local=new Bounds(matrix.MultiplyPoint3x4(bounds.center),Vector3.zero);
            Vector3 c=bounds.center,e=bounds.extents;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                local.Encapsulate(matrix.MultiplyPoint3x4(c+Vector3.Scale(e,new Vector3(x,y,z))));
            renderer.localBounds=local;
        }
    }
}
