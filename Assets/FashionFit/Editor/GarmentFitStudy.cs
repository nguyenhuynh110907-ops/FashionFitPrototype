#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FashionFit.Editor
{
    public static class GarmentFitStudy
    {
        [Serializable] sealed class Profile
        {
            public string source="Measured from project technical body rest mesh; arm-weighted triangles excluded.";
            public int directions=64;
            public Section[] sections;
        }
        [Serializable] sealed class Section
        {
            public float y,centreZ,circumference;
            public float[] radius;
        }
        struct Segment{public Vector2 a,b;public Segment(Vector2 x,Vector2 y){a=x;b=y;}}
        [Serializable] sealed class FitMeasurements
        {
            public string scope="Rest-pose garment proportions and mesh normals in Unity; waist ease is measured against the baked convex body section.";
            public FitLook[] looks=new FitLook[5];
        }
        [Serializable] sealed class FitLook
        {
            public string name;public float waistY,meanWaistRadialEase,waistCircumference,hemWidth,bodiceSeamOverlap,outwardBodiceNormalFraction;
            public bool hasBodice;
        }

        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        static List<Segment> ConvexEnvelope(List<Segment> input)
        {
            var points=new List<Vector2>(input.Count*2);foreach(Segment edge in input){points.Add(edge.a);points.Add(edge.b);}
            points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            var hull=new List<Vector2>(points.Count*2);
            foreach(Vector2 point in points){while(hull.Count>=2&&Cross(hull[hull.Count-1]-hull[hull.Count-2],point-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(point);}
            int lower=hull.Count;
            for(int i=points.Count-2;i>=0;i--){Vector2 point=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-1]-hull[hull.Count-2],point-hull[hull.Count-1])<=0)hull.RemoveAt(hull.Count-1);hull.Add(point);}
            var edges=new List<Segment>(hull.Count);for(int i=0;i<hull.Count-1;i++)edges.Add(new Segment(hull[i],hull[i+1]));return edges;
        }
        static void Intersect(Vector3 a,Vector3 b,float y,List<Vector2> points)
        {
            if((a.y<=y&&b.y>y)||(b.y<=y&&a.y>y))
            {
                Vector3 p=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));points.Add(new Vector2(p.x,p.z));
            }
        }
        static float ArmWeight(BoneWeight weight,Transform[] bones)
        {
            float sum=0;
            int[] index={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
            float[] value={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
            for(int i=0;i<4;i++)if(index[i]>=0&&index[i]<bones.Length)
            {
                string name=bones[index[i]].name;
                if(name.Contains("Arm")||name.Contains("Hand"))sum+=value[i];
            }
            return sum;
        }

        [MenuItem("Fashion Fit/Study Garment Fit/Measure Technical Body")]
        public static void Measure()
        {
            var go=new GameObject("Temporary body measurement"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                var body=go.AddComponent<RunwayAvatar>();body.Load(null,null,"");
                Vector3[] vertices=body.BodyVertices;int[] triangles=body.BodyMesh.triangles;BoneWeight[] weights=body.BodyMesh.boneWeights;
                var arm=new float[vertices.Length];for(int i=0;i<arm.Length;i++)arm[i]=ArmWeight(weights[i],body.Bones);
                var profile=new Profile{sections=new Section[38]};
                var points=new List<Vector2>(3);var segments=new List<Segment>(2048);
                for(int row=0;row<profile.sections.Length;row++)
                {
                    float y=.72f+row*.02f;segments.Clear();float minZ=float.PositiveInfinity,maxZ=float.NegativeInfinity;
                    for(int n=0;n<triangles.Length;n+=3)
                    {
                        int a=triangles[n],b=triangles[n+1],c=triangles[n+2];
                        if((arm[a]+arm[b]+arm[c])/3>.35f)continue;
                        points.Clear();Intersect(vertices[a],vertices[b],y,points);Intersect(vertices[b],vertices[c],y,points);Intersect(vertices[c],vertices[a],y,points);
                        if(points.Count!=2)continue;segments.Add(new Segment(points[0],points[1]));
                        minZ=Mathf.Min(minZ,Mathf.Min(points[0].y,points[1].y));maxZ=Mathf.Max(maxZ,Mathf.Max(points[0].y,points[1].y));
                    }
                    if(segments.Count<3)throw new InvalidOperationException("Missing body section "+y);
                    // Garments bridge the cleavage/leg gap and armhole cuts.
                    // Close the measured section using a convex outer envelope.
                    segments=ConvexEnvelope(segments);
                    var section=new Section{y=y,centreZ=(minZ+maxZ)*.5f,radius=new float[profile.directions]};
                    Vector2 centre=new Vector2(0,section.centreZ);
                    for(int direction=0;direction<profile.directions;direction++)
                    {
                        float angle=direction*Mathf.PI*2/profile.directions;Vector2 ray=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));float distance=0;
                        foreach(Segment segment in segments)
                        {
                            Vector2 edge=segment.b-segment.a,offset=segment.a-centre;float denominator=Cross(ray,edge);
                            if(Mathf.Abs(denominator)<1e-9f)continue;
                            float t=Cross(offset,edge)/denominator,u=Cross(offset,ray)/denominator;
                            if(t>=0&&u>=0&&u<=1)distance=Mathf.Max(distance,t);
                        }
                        section.radius[direction]=Mathf.Max(.025f,distance);
                    }
                    for(int direction=0;direction<profile.directions;direction++)
                    {
                        int next=(direction+1)%profile.directions;float a=direction*Mathf.PI*2/profile.directions,b=next*Mathf.PI*2/profile.directions;
                        section.circumference+=Vector2.Distance(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*section.radius[direction],new Vector2(Mathf.Cos(b),Mathf.Sin(b))*section.radius[next]);
                    }
                    profile.sections[row]=section;
                    UnityEngine.Debug.Log("BODY_SECTION y="+y.ToString("F3")+" circumference="+section.circumference.ToString("F3")+" width="+(section.radius[0]+section.radius[32]).ToString("F3")+" depth="+(section.radius[16]+section.radius[48]).ToString("F3")+" centreZ="+section.centreZ);
                }
                string folder=Path.Combine(Application.dataPath,"../References/FitStudy");Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder,"Body_Profile.json"),JsonUtility.ToJson(profile,true));
                var code=new System.Text.StringBuilder("// Generated by GarmentFitStudy.Measure from the project rest body.\nnamespace FashionFit\n{\n    internal static class TechnicalBodyFitData\n    {\n        internal const float StartY=.72f, StepY=.02f;\n        internal const int Directions=64;\n");
                Action<string,Func<Section,float[]>> writeArray=(name,selector)=>
                {
                    code.Append("        internal static readonly float[] "+name+"={\n");
                    foreach(Section section in profile.sections)
                    {
                        code.Append("            ");foreach(float value in selector(section))code.Append(value.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"f,");code.Append("\n");
                    }
                    code.Append("        };\n");
                };
                writeArray("Radius",section=>section.radius);writeArray("CentreZ",section=>new[]{section.centreZ});writeArray("Circumference",section=>new[]{section.circumference});
                code.Append("    }\n}\n");File.WriteAllText(Path.Combine(Application.dataPath,"FashionFit/Runtime/TechnicalBodyFitData.cs"),code.ToString());
                UnityEngine.Debug.Log("GARMENT_BODY_MEASURE_OK");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        static void Light(Transform parent,Vector3 angles,float intensity)
        {
            var light=new GameObject("Fit study light").AddComponent<Light>();light.transform.SetParent(parent,false);light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(angles);light.intensity=intensity;light.cullingMask=1<<30;
        }
        public static void Capture(string label)
        {
            var root=new GameObject("Fit study capture"){hideFlags=HideFlags.HideAndDontSave};Material fabric=null,blouseFabric=null;
            Color savedAmbient=RenderSettings.ambientLight;
            try
            {
                var body=new GameObject("Body").AddComponent<RunwayAvatar>();body.transform.SetParent(root.transform,false);body.Load(null,null,"");
                var camera=new GameObject("Fit camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);
                camera.orthographic=true;camera.orthographicSize=.96f;camera.nearClipPlane=.05f;camera.farClipPlane=10;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.17f,.19f);camera.cullingMask=1<<30;
                Light(root.transform,new Vector3(28,-28,0),1.1f);Light(root.transform,new Vector3(35,140,0),.65f);
                RenderSettings.ambientLight=new Color(.38f,.38f,.38f);
                fabric=new Material(Resources.Load<Shader>("Runway/DressFabric"));fabric.SetFloat("_Glossiness",.3f);
                blouseFabric=new Material(fabric);blouseFabric.SetColor("_Color",new Color(.90f,.86f,.78f));
                Color[] colors={new Color(.07f,.26f,.25f),new Color(.03f,.23f,.13f),new Color(.48f,.07f,.12f),new Color(.04f,.15f,.45f),new Color(.20f,.23f,.24f)};
                string directory=Path.Combine(Application.dataPath,"../Verification/FitStudy");Directory.CreateDirectory(directory);
                var measurements=new FitMeasurements();
                for(int look=0;look<5;look++)
                {
                    body.Animate(0,0,1);var garment=new GameObject("Garment");garment.transform.SetParent(body.transform,false);
                    var cloth=garment.AddComponent<FittingCloth>();cloth.Initialize(body,(DressPreset)look);fabric.SetColor("_Color",colors[look]);cloth.SetMaterial(fabric);
                    InteractiveShirt shirt=null;
                    if(look==4){var top=new GameObject("Blouse");top.transform.SetParent(garment.transform,false);shirt=top.AddComponent<InteractiveShirt>();shirt.Initialize(null,body,blouseFabric);}
                    for(int frame=0;frame<40;frame++){cloth.Step(1f/60f);if(shirt!=null)shirt.Step(1f/60f);}
                    var fit=new FitLook{name=((DressPreset)look).ToString(),waistY=BodyFitProfile.WaistY};measurements.looks[look]=fit;
                    Vector3[] surface=garment.GetComponent<MeshFilter>().sharedMesh.vertices;
                    float minX=float.PositiveInfinity,maxX=float.NegativeInfinity;
                    for(int i=0;i<SampleSkirtGeometry.Segments;i++)
                    {
                        Vector3 point=surface[i];float z=point.z-BodyFitProfile.CentreZ(point.y);
                        float angle=Mathf.Atan2(z,point.x);
                        fit.meanWaistRadialEase+=(Mathf.Sqrt(point.x*point.x+z*z)-BodyFitProfile.Radius(point.y,angle))/SampleSkirtGeometry.Segments;
                        fit.waistCircumference+=Vector3.Distance(point,surface[(i+1)%SampleSkirtGeometry.Segments]);
                        Vector3 hem=surface[(SampleSkirtGeometry.Rings-1)*SampleSkirtGeometry.Segments+i];minX=Mathf.Min(minX,hem.x);maxX=Mathf.Max(maxX,hem.x);
                    }
                    fit.hemWidth=maxX-minX;
                    if(cloth.BodiceSurface!=null)
                    {
                        fit.hasBodice=true;Mesh bodice=cloth.BodiceSurface.sharedMesh;Vector3[] bv=bodice.vertices,bn=bodice.normals;int outward=0;
                        for(int i=0;i<bv.Length;i++){Vector3 radial=new Vector3(bv[i].x,0,bv[i].z-BodyFitProfile.CentreZ(bv[i].y));if(Vector3.Dot(radial,bn[i])>0)outward++;}
                        fit.outwardBodiceNormalFraction=outward/(float)bv.Length;
                        fit.bodiceSeamOverlap=surface[0].y-bv[0].y;
                        if(fit.outwardBodiceNormalFraction<.95f)throw new InvalidOperationException("Inward bodice normals "+fit.name);
                        if(fit.bodiceSeamOverlap<0||fit.bodiceSeamOverlap>.015f)throw new InvalidOperationException("Waist seam mismatch "+fit.name);
                    }
                    if(fit.meanWaistRadialEase<.004f||fit.meanWaistRadialEase>.040f)throw new InvalidOperationException("Waist fit out of range "+fit.name);
                    foreach(Transform child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=30;
                    for(int view=0;view<2;view++)
                    {
                        camera.transform.position=view==0?new Vector3(0,1,3):new Vector3(3,1,0);
                        camera.transform.LookAt(new Vector3(0,.84f,0));
                        var target=new RenderTexture(600,900,24);camera.targetTexture=target;camera.Render();
                        RenderTexture previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(600,900,TextureFormat.RGB24,false);
                        texture.ReadPixels(new Rect(0,0,600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,label+"_Look"+(look+1)+"_"+(view==0?"Front":"Side")+".png"),texture.EncodeToPNG());
                        RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);
                    }
                    UnityEngine.Object.DestroyImmediate(garment);
                }
                File.WriteAllText(Path.Combine(directory,label+"_Fit_Measurements.json"),JsonUtility.ToJson(measurements,true));
                if(measurements.looks[2].hemWidth<measurements.looks[1].hemWidth*1.4f)throw new InvalidOperationException("Flared and fitted silhouettes are not distinct");
                UnityEngine.Debug.Log("GARMENT_FIT_CAPTURE_OK "+label);
            }
            finally{RenderSettings.ambientLight=savedAmbient;if(fabric!=null)UnityEngine.Object.DestroyImmediate(fabric);if(blouseFabric!=null)UnityEngine.Object.DestroyImmediate(blouseFabric);UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void CaptureBeforeBatch(){try{Measure();Capture("Before");EditorApplication.Exit(0);}catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void CaptureAfterBatch(){try{Capture("After");RunwayTrialVerification.Run();EditorApplication.Exit(0);}catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
#endif
