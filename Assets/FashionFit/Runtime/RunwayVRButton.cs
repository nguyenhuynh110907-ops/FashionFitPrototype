using System;
using UnityEngine;
namespace FashionFit
{
    public sealed class RunwayVRButton:MonoBehaviour
    {
        public Action Action;
        Renderer surface;TextMesh label;bool hovered,selected;
        MaterialPropertyBlock block;
        public bool IsHovered=>hovered;
        public string Label=>label?label.text:gameObject.name;
        public void Configure(Renderer renderer,TextMesh text){surface=renderer;label=text;Paint();}
        public void SetHovered(bool value){if(hovered==value)return;hovered=value;Paint();}
        public void SetContent(string text,bool active=false){if(label)label.text=text;selected=active;Paint();}
        void Paint(){if(!surface)return;if(block==null)block=new MaterialPropertyBlock();block.SetColor("_Color",hovered?new Color(.30f,.25f,.15f):selected?new Color(.17f,.25f,.22f):new Color(.085f,.105f,.125f));surface.SetPropertyBlock(block);if(label)label.color=hovered?new Color(1,.88f,.59f):selected?new Color(.70f,1,.86f):new Color(.94f,.95f,.96f);}
        public void Activate()=>Action?.Invoke();
    }
}
