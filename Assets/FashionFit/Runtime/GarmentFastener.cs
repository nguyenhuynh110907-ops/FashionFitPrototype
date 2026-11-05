using System.Collections.Generic;
using UnityEngine;
namespace FashionFit
{
    // Physical garment closure, deliberately unrelated to the four-metre menu ray.
    public sealed class GarmentFastener:MonoBehaviour
    {
        static readonly List<GarmentFastener> all=new List<GarmentFastener>();
        DoffableBlouse blouse;int index,hoverMask,lastFrame=-1;Renderer visual;MaterialPropertyBlock properties;
        public int Index=>index;
        public string Hint=>"Nút "+(index+1)+" · "+(blouse.Solver.Closed[index]?"Trigger để mở":"Trigger để cài lại");
        public void Initialize(DoffableBlouse owner,int id,Material material){blouse=owner;index=id;visual=GetComponent<Renderer>();visual.sharedMaterial=material;properties=new MaterialPropertyBlock();all.Add(this);}
        public static GarmentFastener FindNear(Vector3 hand)
        {
            GarmentFastener best=null;float d=.035f;
            foreach(var f in all)if(f&&f.isActiveAndEnabled&&f.blouse.CanTouchFastener(hand,f.index)){
                float distance=Vector3.Distance(hand,f.transform.position);if(distance<d){d=distance;best=f;}
            }return best;
        }
        public bool Activate(Vector3 hand){if(lastFrame==Time.frameCount||!blouse.CanTouchFastener(hand,index))return false;if(!blouse.ToggleFastener(index))return false;lastFrame=Time.frameCount;return true;}
        public void SetHovered(int side,bool value){if(value)hoverMask|=1<<side;else hoverMask&=~(1<<side);Refresh();}
        public void Refresh(){if(!visual||!blouse.Ready)return;properties.SetColor("_Color",hoverMask!=0?new Color(1,.85f,.35f):blouse.Solver.Closed[index]?new Color(.65f,.48f,.23f):new Color(.3f,.7f,.55f));visual.SetPropertyBlock(properties);}
        public void Unregister(){all.Remove(this);}
        void OnDestroy(){Unregister();}
    }
}
