using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public sealed class RunwayVRPanel : MonoBehaviour
    {
        FashionFitApp app;RunwayPlayer player;
        Mesh rounded;Material background,buttonMaterial;
        TextMesh subtitle,footer,readout;
        readonly List<RunwayVRButton> buttons=new List<RunwayVRButton>();
        readonly RunwayVRButton[] tabs=new RunwayVRButton[3],actions=new RunwayVRButton[6];
        RunwayVRButton poseModeButton;bool lastPoseMode;int lastLook=-1;bool lastPlaying,lastSnap,lastLow;float lastSpeed,lastMotionSpeed;
        public int Page {get;private set;}
        public bool Visible=>gameObject.activeSelf;
        public IReadOnlyList<RunwayVRButton> Buttons=>buttons;
        public IReadOnlyList<RunwayVRButton> ActionButtons=>actions;
        public const float Distance=1.3f;
        public void Initialize(FashionFitApp owner,RunwayPlayer rig)
        {
            app=owner;player=rig;transform.SetParent(app.transform,false);
            var shader=Resources.Load<Shader>("Runway/VRPanel");background=new Material(shader){name="VR panel charcoal"};background.SetColor("_Color",new Color(.024f,.034f,.047f));buttonMaterial=new Material(shader){name="VR button surface"};
            rounded=Rounded();Visual("Panel backing",new Vector3(0,0,.04f),new Vector2(1.04f,.90f),background);
            var backing=gameObject.AddComponent<BoxCollider>();backing.center=new Vector3(0,0,.04f);backing.size=new Vector3(1.04f,.90f,.018f);gameObject.layer=8;
            Label("FASHION UNITY",new Vector3(-.12f,.36f,-.006f),.050f,new Color(.91f,.79f,.56f));
            subtitle=Label("",new Vector3(0,.285f,-.006f),.026f,new Color(.72f,.78f,.83f));
            string[] tabNames={"THỬ ĐỒ","TRANG PHỤC","CÀI ĐẶT"};for(int i=0;i<3;i++){int page=i;tabs[i]=Button(tabNames[i],new Vector3((i-1)*.327f,.20f,0),new Vector2(.305f,.09f),.031f,()=>SelectPage(page));}
            Button("ẨN",new Vector3(.43f,.36f,0),new Vector2(.115f,.07f),.028f,Hide);
            for(int i=0;i<6;i++)actions[i]=Button("",new Vector3(i%2==0?-.245f:.245f,.04f-(i/2)*.155f,0),new Vector2(.465f,.13f),.036f,null);
            Button("ÁO DÀI / LỚP VẢI",new Vector3(-.24f,.127f,-.003f),new Vector2(.46f,.035f),.024f,()=>SelectPage(9));
            Button("ĐÍCH MẶC / VẬT LIỆU",new Vector3(.24f,.127f,-.003f),new Vector2(.46f,.035f),.024f,()=>SelectPage(7));
            poseModeButton=Button("",new Vector3(.235f,.36f,-.003f),new Vector2(.23f,.07f),.022f,()=>app.PoseStudio.ToggleHandles());
            Button("HOLLISTER NAM / 5",new Vector3(0,-.355f,-.003f),new Vector2(.465f,.036f),.024f,()=>SelectPage(11));
            footer=Label("Trigger hai tay: chọn · Grip gần: cầm · Y: menu · X: về trước",new Vector3(0,-.40f,-.006f),.024f,new Color(.67f,.73f,.79f));
            readout=Label("",new Vector3(0,-.045f,-.012f),.024f,new Color(.80f,.91f,.91f));readout.lineSpacing=1.15f;readout.gameObject.SetActive(false);
            foreach(var child in GetComponentsInChildren<Transform>())child.gameObject.layer=8;
            Refresh();Show();
        }
        public void SelectPage(int page){Page=Mathf.Clamp(page,0,12);Refresh();if(Page==12)PlaceInFront();}
        public void Refresh()
        {
            if(!app||!app.Wardrobe)return;
            for(int i=0;i<6;i++)actions[i].gameObject.SetActive(Page!=12||i>=4);if(readout)readout.gameObject.SetActive(Page==12);
            lastPoseMode=app.PoseStudio.HandlesVisible;if(poseModeButton)poseModeButton.SetContent(lastPoseMode?"CHỈNH KHỚP\nđổi sang VẢI":"KÉO VẢI\nđổi sang KHỚP",lastPoseMode);
            lastLook=app.Look;lastPlaying=app.Playing;lastSnap=player.VRSnapTurn;lastLow=player.LowQuality;lastSpeed=player.VRWalkSpeed;lastMotionSpeed=app.Speed;
            string name=app.Look>=1&&app.Look<=6?"Đang mặc: "+app.Look.ToString("00")+" · "+RunwayShowroom.LookNames[app.Look-1]:app.Wardrobe.HasGarment?app.Wardrobe.GarmentName:"Chưa chọn trang phục";subtitle.text=name.Length>53?name.Substring(0,50)+"…":name;
            for(int i=0;i<3;i++)tabs[i].SetContent(i==0?"THỬ ĐỒ":i==1?"TRANG PHỤC":"CÀI ĐẶT",(Page==i||((Page==3||Page==5)&&i==2)||(Page==4&&i==0)||((Page==6||Page==7||Page==8)&&i==1)));
            if(Page==0){
                Set(0,"Đứng yên\nđể thử đồ",app.StandForFitting,app.FittingPose);
                Set(1,app.Playing?"Dừng\ntrình diễn":"Đi bộ\n0,50×",()=>{if(app.Playing)app.TogglePlay();else app.StartPresentationAt(FashionFitApp.DefaultMotionSpeed);},app.Playing);
                Set(2,"Chọn\ntrang phục",()=>SelectPage(1));
                Set(3,"Đến bàn\nvà tủ đồ",()=>{Hide();player.FocusShowroom();});
                Set(4,"Trả phụ kiện\nvề bàn / tủ",()=>{player.EndActiveGrab();app.Showroom.ResetProps();});
                Set(5,"Nhịp\ntrình diễn",()=>SelectPage(4));
            }else if(Page==1){subtitle.text="MẶC CHO: "+app.MaterialLab.TargetLabel;for(int i=0;i<6;i++){int look=i+1;Set(i,look.ToString("00")+" · "+RunwayShowroom.LookNames[i]+"\n"+(app.Showroom.TargetLook==look?"ĐANG MẶC":"Chọn mặc"),()=>app.Showroom.WearLook(look),app.Showroom.TargetLook==look);}}
            else if(Page==11){subtitle.text="HOLLISTER NAM · bản dựng tham chiếu";for(int i=0;i<5;i++){int index=i;Set(i,(i+1).ToString("00")+" · "+MensReferenceCollection.Names[i]+"\nMặc cho pose NAM",()=>{MensReferenceCollection.Wear(app,index);Refresh();},app.PoseStudio.Wearables[1].Look==7+i);}Set(5,"Quay lại\ntrang phục",()=>SelectPage(1));}
            else if(Page==2){
                Set(0,player.VRSnapTurn?"Quay 45°\nđổi sang mượt":"Quay mượt\nđổi sang 45°",player.ToggleVRTurn,player.VRSnapTurn);
                Set(1,"Tốc độ đi\n"+player.VRWalkSpeed.ToString("0.00")+" m/s",player.CycleVRSpeed);
                Set(2,player.LowQuality?"Đồ họa nhẹ\nđổi chi tiết":"Đồ họa chi tiết\nđổi sang nhẹ",player.ToggleQuality,player.LowQuality);
                Set(3,"Bề mặt vải\n6 mẫu texture",()=>SelectPage(5));
                Set(4,"Mẫu nam / nữ\nchỉnh khớp",()=>SelectPage(3));
                Set(5,player.InVR?"Thoát VR\nvề desktop":"Đóng\nbản xem thử",()=>{if(player.InVR)player.StopVR();else Hide();});
            }else if(Page==3){
                Set(0,"Mẫu nữ 3D\nnhìn gần",()=>{Hide();app.PoseStudio.Focus(0);});
                Set(1,"Mẫu nam 3D\nnhìn gần",()=>{Hide();app.PoseStudio.Focus(1);});
                Set(2,app.PoseStudio.HandlesVisible?"Ẩn điểm khớp\ngiữ tư thế":"Hiện điểm khớp\nGrip để chỉnh",()=>{app.PoseStudio.ToggleHandles();Refresh();},app.PoseStudio.HandlesVisible);
                Set(3,"Đặt lại\nmẫu nữ",()=>app.PoseStudio.ResetPose(0));
                Set(4,"Đặt lại\nmẫu nam",()=>app.PoseStudio.ResetPose(1));
                Set(5,"Quay lại\ncài đặt",()=>SelectPage(2));
            }else if(Page==7){
                subtitle.text="CHỌN NHÂN VẬT · "+app.MaterialLab.TargetLabel;
                Set(0,"Mẫu đi bộ\nmặc / đội",()=>{app.Showroom.SelectOutfitTarget(0);SelectPage(1);},app.Showroom.OutfitTarget==0);
                Set(1,"Pose nữ\nmặc / đội",()=>{app.Showroom.SelectOutfitTarget(1);SelectPage(1);},app.Showroom.OutfitTarget==1);
                Set(2,"Pose nam\nmặc / đội",()=>{app.Showroom.SelectOutfitTarget(2);SelectPage(1);},app.Showroom.OutfitTarget==2);
                Set(3,"Căng / nén\nvật liệu",()=>SelectPage(8));
                Set(4,GarmentContactWorld.Get(app).ManualDressing?"Về phom ban đầu\nkết thúc bản thử":"MẶC/CỞI BẰNG GRIP\nbật vật lý thử",()=>{app.PoseStudio.SetHandles(false);GarmentContactWorld.Get(app).ToggleManualDressing();Refresh();});
                Set(5,"Quay lại\ntrang phục",()=>SelectPage(1));
            }else if(Page==9){
                subtitle.text="ÁO DÀI · "+app.MaterialLab.TargetLabel;
                Set(0,"Mặc áo dài\ncho mẫu chọn",()=>app.LayerLab.WearSelected());
                Set(1,app.LayerLab.Current?"Chọn lớp\n"+app.LayerLab.Current.LayerLabel:"Chọn lớp\nmặc áo dài trước",()=>{app.LayerLab.CycleLayer();Refresh();});
                Set(2,"Dài / rộng tà\nchỉnh kích thước",()=>SelectPage(10));
                Set(3,app.LayerLab.Current&&app.LayerLab.Current.ArrangementMode?"Đang ghim tà\nđổi sang rủ tự nhiên":"Rủ tự nhiên\nđổi sang ghim điểm",()=>{app.LayerLab.ToggleArrangement();Refresh();});
                Set(4,"Đặt lại tà\nbỏ điểm ghim",app.LayerLab.ResetDrapeSelected);
                Set(5,"Áo cài nút\ncởi blouse",()=>SelectPage(6));
            }else if(Page==10){
                subtitle.text=app.LayerLab.Current?app.LayerLab.Current.Summary:"MẶC ÁO DÀI TRƯỚC";
                Set(0,"Đổi chiều dài\n"+(app.LayerLab.Current?app.LayerLab.Current.LengthScale.ToString("F2"):""),()=>{app.LayerLab.CycleLength();Refresh();});
                Set(1,"Đổi độ rộng\n"+(app.LayerLab.Current?app.LayerLab.Current.WidthScale.ToString("F2"):""),()=>{app.LayerLab.CycleWidth();Refresh();});
                Set(2,"Vật liệu\nkéo / nén",()=>SelectPage(8));
                Set(3,"Grip: nhấc nón\nHai tay: kéo giãn",()=>app.Report("Grip thường nhấc nón, kể cả trên đầu. Hai tay kéo ngược để giãn. Trigger + Grip để chỉnh nón tại chỗ."));
                Set(4,"Chỉnh khớp\nhoặc kéo vải",()=>SelectPage(3));
                Set(5,"Quay lại\náo dài",()=>SelectPage(9));
            }else if(Page==12){subtitle.text="THÔNG SỐ · "+app.MaterialLab.ProfileLabel;readout.text=app.MaterialLab.DetailedReadout;Set(4,"Đổi vật liệu\n"+app.MaterialLab.ProfileLabel,()=>{app.MaterialLab.CycleProfile();Refresh();});Set(5,"Quay lại\nvật liệu",()=>SelectPage(8));
            }else if(Page==8){
                subtitle.text="VẬT LIỆU · "+app.MaterialLab.ProfileLabel;
                Set(0,"Đổi vật liệu\n"+app.MaterialLab.ProfileLabel,()=>{app.MaterialLab.CycleProfile();Refresh();});
                Set(1,"Cỡ nón\n"+app.MaterialLab.SizeLabel,()=>{app.MaterialLab.CycleHatSize();Refresh();});
                Set(2,"Đặt lại\nbiến dạng",app.MaterialLab.ResetShapes);
                Set(3,"Xem rõ thông số\nkéo / nén trực tiếp",()=>SelectPage(12));
                Set(4,app.MaterialLab.HatMode?"Đang đo NÓN\nđổi sang trang phục":"Đang đo ĐỒ\nđổi sang nón",()=>{app.MaterialLab.ToggleFocus();Refresh();});
                Set(5,"Quay lại\ntrang phục",()=>SelectPage(1));
            }else if(Page==6){
                subtitle.text=app.Wardrobe.Blouse?"ÁO CÀI NÚT · "+app.Wardrobe.Blouse.OpenCount+"/4 nút đã mở":"BẢN THỬ · áo mở toàn thân, tay ngắn";
                Set(0,"Mặc áo\ncài 4 nút",app.Wardrobe.WearDoffableBlouse);
                Set(1,"Bắt đầu\ncởi áo",app.Wardrobe.BeginDoffBlouse);
                Set(2,"Đặt lại áo\nlên mẫu",app.Wardrobe.ResetDoffableBlouse);
                Set(3,"Grip giữ vải\nTrigger sát nút",()=>app.Report("Một tay Grip gần cổ/vai áo; tay kia sát nút dưới 3,5 cm và bóp Trigger. Mở 4 nút, kéo nhẹ hoặc chọn Bắt đầu cởi."));
                Set(4,"Dọn áo rời\nđã thả",app.Wardrobe.ClearFreeBlouses);
                Set(5,"Quay lại\ntrang phục",()=>SelectPage(1));
            }else if(Page==5){
                for(int i=0;i<6;i++){int index=i;bool exists=i<app.FabricTrial.Samples.Length;Set(i,exists?app.FabricTrial.Samples[i].label+"\nÁp lên trang phục":"Thiếu mẫu vải",()=>app.FabricTrial.ApplySample(index));}
                subtitle.text="TEXTURE CC0 · thay bề mặt, giữ nguyên vật lý";
            }else{
                Set(0,"Đứng yên\nkiểm tra vải",app.StandForFitting,app.FittingPose);
                Set(1,"Chậm\n0,18×",()=>app.StartSlowPresentation(),app.Playing&&Mathf.Abs(app.Speed-FashionFitApp.SlowMotionSpeed)<.001f);
                Set(2,"Nhanh hơn\n0,75×",()=>app.StartPresentationAt(FashionFitApp.FasterMotionSpeed),app.Playing&&Mathf.Abs(app.Speed-FashionFitApp.FasterMotionSpeed)<.001f);
                Set(3,"Đi bình thường\n1,00×",()=>app.StartPresentationAt(FashionFitApp.MaximumMotionSpeed),app.Playing&&Mathf.Abs(app.Speed-FashionFitApp.MaximumMotionSpeed)<.001f);
                Set(4,"Dừng\ntrình diễn",()=>{if(app.Playing)app.TogglePlay();});
                Set(5,"Quay lại\nthử đồ",()=>SelectPage(0));
            }
        }
        void Set(int index,string text,Action action,bool selected=false){actions[index].Action=action;actions[index].SetContent(text,selected);}
        public void Show(){gameObject.SetActive(true);PlaceInFront();Refresh();}
        public void Hide(){foreach(var b in buttons)b.SetHovered(false);gameObject.SetActive(false);}
        public void Toggle(){Vector3 delta=transform.position-player.View.transform.position;if(Visible&&delta.magnitude<2.6f&&Vector3.Angle(player.View.transform.forward,delta)<70)Hide();else Show();}
        public void PlaceInFront()
        {
            Vector3 forward=player.View.transform.forward;forward.y=0;if(forward.sqrMagnitude<.001f)forward=player.transform.forward;forward.Normalize();
            Vector3 position=player.View.transform.position+forward*Distance-Vector3.up*.18f;if(Page==12)position+=Vector3.Cross(Vector3.up,forward)*.90f;Vector3 facing=position-player.View.transform.position;facing.y=0;transform.SetPositionAndRotation(position,Quaternion.LookRotation(facing.normalized,Vector3.up));
        }
        float materialRefresh;void Update(){if((Page==8||Page==12)&&app){materialRefresh+=Time.unscaledDeltaTime;if(materialRefresh>.2f){materialRefresh=0;if(Page==12)readout.text=app.MaterialLab.DetailedReadout;else subtitle.text="VẬT LIỆU · "+app.MaterialLab.ProfileLabel;}}if(app&&(app.PoseStudio.HandlesVisible!=lastPoseMode||app.Look!=lastLook||app.Playing!=lastPlaying||player.VRSnapTurn!=lastSnap||player.LowQuality!=lastLow||Mathf.Abs(player.VRWalkSpeed-lastSpeed)>.001f||Mathf.Abs(app.Speed-lastMotionSpeed)>.001f))Refresh();}
        TextMesh Label(string text,Vector3 position,float size,Color color){var label=RunwayStage.Text(transform,text,position,size,color);label.fontStyle=FontStyle.Normal;return label;}
        Renderer Visual(string name,Vector3 position,Vector2 size,Material material)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=new Vector3(size.x,size.y,1);go.AddComponent<MeshFilter>().sharedMesh=rounded;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;return renderer;}
        RunwayVRButton Button(string text,Vector3 position,Vector2 size,float font,Action action)
        {
            var root=new GameObject(text);root.transform.SetParent(transform,false);root.transform.localPosition=position;root.layer=8;
            var visual=new GameObject("Rounded button");visual.transform.SetParent(root.transform,false);visual.transform.localScale=new Vector3(size.x,size.y,1);visual.AddComponent<MeshFilter>().sharedMesh=rounded;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=buttonMaterial;
            var collider=root.AddComponent<BoxCollider>();collider.size=new Vector3(size.x,size.y,.018f);
            var label=RunwayStage.Text(root.transform,text,new Vector3(0,0,-.012f),font,Color.white);label.lineSpacing=1.05f;
            var button=root.AddComponent<RunwayVRButton>();button.Configure(renderer,label);button.Action=action;buttons.Add(button);return button;
        }
        static Mesh Rounded()
        {
            const float radius=.07f;var p=new List<Vector3>{Vector3.zero};var t=new List<int>();
            Vector2[] centres={new Vector2(.5f-radius,.5f-radius),new Vector2(-.5f+radius,.5f-radius),new Vector2(-.5f+radius,-.5f+radius),new Vector2(.5f-radius,-.5f+radius)};
            for(int corner=0;corner<4;corner++)for(int i=0;i<=6;i++){float a=(corner*90+i*15)*Mathf.Deg2Rad;p.Add(centres[corner]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
            for(int i=1;i<p.Count;i++){t.Add(0);t.Add(i);t.Add(i==p.Count-1?1:i+1);}var mesh=new Mesh{name="Rounded VR panel quad"};mesh.SetVertices(p);mesh.SetTriangles(t,0);mesh.RecalculateBounds();return mesh;
        }
        void OnDestroy(){Release(rounded);Release(background);Release(buttonMaterial);}
        static void Release(UnityEngine.Object value){if(value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}}
    }
}
