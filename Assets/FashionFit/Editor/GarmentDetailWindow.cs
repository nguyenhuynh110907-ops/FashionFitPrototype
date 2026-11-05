#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit.Editor
{
    public sealed class GarmentDetailWindow : EditorWindow
    {
#pragma warning disable 0649
        [Serializable] sealed class Bundle
        {
            public int schemaVersion;
            public string id, source;
            public string[] panelNames;
            public int[] facePanels, triangles, seamEdges, anchorIndices, bodyTriangles;
            public float[] restVertices, referenceVertices, uv, bodyVertices, waistOrigin, garmentCentre;
            public bool nonpenetrationCertified;
        }
#pragma warning restore 0649
        PreviewRenderUtility preview;
        Bundle bundle;
        Mesh garment, body, seamLines, anchorLines;
        Material bodyMaterial, lineMaterial;
        Material[] panelMaterials;
        string error;
        int state, selectedPanel;
        bool showBody = true, showSeams = true, showSupports, colorPanels = true;
        Vector3 origin, offset, viewTarget = new Vector3(0, -.22f, 0);
        float yaw, scale = 1, viewYaw = 15, viewPitch = 6, distance = 3.3f;
        string PathToBundle { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AI", "ClothData", "Data", "DetailTraining", "detail_preview.json")); } }

        [MenuItem("Fashion Fit/AI/Inspect Detailed Garment")]
        static void Open() { GetWindow<GarmentDetailWindow>("Chi tiết trang phục").Show(); }
        void OnEnable() { minSize = new Vector2(640, 650); LoadBundle(); }
        void OnDisable() { Release(); }
        static Vector3 Point(float[] values, int offset = 0) { return new Vector3(-values[offset], values[offset + 1], values[offset + 2]); }
        static Vector3[] Points(float[] values)
        {
            if (values == null || values.Length % 3 != 0) throw new InvalidDataException("Tọa độ không hợp lệ.");
            var result = new Vector3[values.Length / 3]; for (int i = 0; i < result.Length; i++) result[i] = Point(values, i * 3); return result;
        }
        static int[] UnityFaces(int[] source)
        {
            var result = (int[])source.Clone(); for (int i = 0; i < result.Length; i += 3) { int value = result[i + 1]; result[i + 1] = result[i + 2]; result[i + 2] = value; } return result;
        }
        static Material Solid(Color color)
        {
            var material = new Material(Shader.Find("Standard")) { hideFlags = HideFlags.HideAndDontSave, color = color };
            material.SetFloat("_Glossiness", .22f); return material;
        }
        void LoadBundle()
        {
            Release(); error = null;
            try {
                if (!File.Exists(PathToBundle)) throw new FileNotFoundException("Chạy Prepare_Detailed_Garment_Training.bat trước để tạo mẫu kiểm tra.");
                if (new FileInfo(PathToBundle).Length > 30 * 1024 * 1024) throw new InvalidDataException("Mẫu xem trước vượt 30 MB.");
                bundle = JsonUtility.FromJson<Bundle>(File.ReadAllText(PathToBundle));
                if (bundle == null || bundle.schemaVersion != 1 || bundle.panelNames == null) throw new InvalidDataException("Sai định dạng mẫu chi tiết.");
                origin = Point(bundle.waistOrigin);
                preview = new PreviewRenderUtility(); preview.cameraFieldOfView = 35; preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 20;
                preview.camera.clearFlags = CameraClearFlags.Color; preview.camera.backgroundColor = new Color(.83f, .84f, .82f);
                preview.lights[0].intensity = 1.1f; preview.lights[0].transform.rotation = Quaternion.Euler(45, 35, 0);
                preview.lights[1].intensity = .7f; preview.lights[1].transform.rotation = Quaternion.Euler(330, 210, 0);
                preview.ambientColor = new Color(.45f, .45f, .45f);
                bodyMaterial = Solid(new Color(.68f, .65f, .60f));
                panelMaterials = new Material[bundle.panelNames.Length + 1];
                for (int i = 0; i < panelMaterials.Length; i++) panelMaterials[i] = Solid(Color.HSVToRGB((i * .618034f) % 1, .34f, .76f));
                lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                lineMaterial.SetColor("_Color", new Color(.95f, .66f, .08f)); lineMaterial.SetInt("_Cull", 0); lineMaterial.SetInt("_ZWrite", 0);
                body = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32, vertices = Points(bundle.bodyVertices), triangles = UnityFaces(bundle.bodyTriangles) };
                body.RecalculateNormals(); body.RecalculateBounds(); Rebuild();
            } catch (Exception exception) { error = exception.Message; }
        }
        void Rebuild()
        {
            if (bundle == null) return;
            if (garment) DestroyImmediate(garment); if (seamLines) DestroyImmediate(seamLines); if (anchorLines) DestroyImmediate(anchorLines);
            Vector3[] vertices = Points(state == 0 ? bundle.referenceVertices : bundle.restVertices);
            garment = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32, vertices = vertices };
            var uv = new Vector2[vertices.Length]; for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(bundle.uv[i * 2], bundle.uv[i * 2 + 1]); garment.uv = uv;
            int[] faces = UnityFaces(bundle.triangles); var groups = new List<int>[panelMaterials.Length];
            for (int i = 0; i < groups.Length; i++) groups[i] = new List<int>();
            for (int i = 0; i < faces.Length / 3; i++) {
                int panel = bundle.facePanels[i]; if (panel < 0 || panel >= bundle.panelNames.Length) panel = bundle.panelNames.Length;
                groups[panel].Add(faces[i * 3]); groups[panel].Add(faces[i * 3 + 1]); groups[panel].Add(faces[i * 3 + 2]);
            }
            garment.subMeshCount = groups.Length; for (int i = 0; i < groups.Length; i++) garment.SetTriangles(groups[i], i);
            garment.RecalculateNormals(); garment.RecalculateBounds();
            seamLines = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = IndexFormat.UInt32, vertices = vertices };
            seamLines.SetIndices(bundle.seamEdges, MeshTopology.Lines, 0); seamLines.RecalculateBounds();
            var marks = new List<Vector3>(); var indices = new List<int>();
            foreach (int id in bundle.anchorIndices) foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward }) {
                indices.Add(marks.Count); marks.Add(vertices[id] - axis * .004f); indices.Add(marks.Count); marks.Add(vertices[id] + axis * .004f);
            }
            anchorLines = new Mesh { hideFlags = HideFlags.HideAndDontSave, vertices = marks.ToArray() };
            anchorLines.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            selectedPanel = Mathf.Clamp(selectedPanel, 0, garment.subMeshCount);
            FitView();
        }
        void FitView()
        {
            if (!garment || preview == null) return;
            Matrix4x4 matrix = Matrix4x4.TRS(offset, Quaternion.Euler(0, yaw, 0), Vector3.one * scale) * Matrix4x4.Translate(-origin);
            Vector3[] vertices = garment.vertices;
            int[] indices = selectedPanel == 0 ? garment.triangles : garment.GetTriangles(selectedPanel - 1);
            if (indices.Length == 0) return;
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[indices[0]]), Vector3.zero);
            foreach (int id in indices) bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[id]));
            if (selectedPanel == 0 && showBody && body) foreach (Vector3 point in body.vertices) bounds.Encapsulate(point - origin);
            viewTarget = bounds.center;
            distance = Mathf.Clamp(bounds.extents.magnitude / Mathf.Sin(preview.cameraFieldOfView * Mathf.Deg2Rad * .5f) * 1.12f, .35f, 8);
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Mẫu từ pattern/mesh mô phỏng GarmentCodeData. Đây là dữ liệu tham chiếu, chưa phải kết quả AI hay trang phục đã retarget vào mannequin trong game.", MessageType.Info);
            if (GUILayout.Button("Nạp lại mẫu chi tiết")) LoadBundle();
            if (!string.IsNullOrEmpty(error)) { EditorGUILayout.HelpBox(error, MessageType.Error); return; }
            if (bundle == null || preview == null) return;
            EditorGUILayout.LabelField(bundle.id, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Cấu trúc", bundle.panelNames.Length + " panel · UV gốc · đường may và vùng neo nguồn");
            int selected = GUILayout.Toolbar(state, new[] { "Mặc theo mẫu 3D", "Mesh trước drape" });
            if (selected != state) { state = selected; Rebuild(); }
            var choices = new string[bundle.panelNames.Length + 1]; choices[0] = "Tất cả panel";
            for (int i = 0; i < bundle.panelNames.Length; i++) choices[i + 1] = bundle.panelNames[i];
            int panel = EditorGUILayout.Popup("Xem riêng mảnh may", Mathf.Clamp(selectedPanel, 0, choices.Length - 1), choices);
            if (panel != selectedPanel) { selectedPanel = panel; FitView(); }
            if (GUILayout.Button("Phóng vừa mảnh / toàn bộ đồ")) FitView();
            EditorGUILayout.BeginHorizontal(); showBody = GUILayout.Toggle(showBody, "Body nguồn"); showSeams = GUILayout.Toggle(showSeams, "Đường may"); showSupports = GUILayout.Toggle(showSupports, "Nhãn neo"); colorPanels = GUILayout.Toggle(colorPanels, "Màu panel"); EditorGUILayout.EndHorizontal();
            offset = EditorGUILayout.Vector3Field("Dịch áo/váy để đối chiếu (m)", offset);
            yaw = EditorGUILayout.Slider("Quay trước–sau", yaw, -180, 180); scale = EditorGUILayout.Slider("Tỉ lệ đồng đều", scale, .7f, 1.3f);
            if (GUILayout.Button("Căn lại theo eo body · giữ nguyên phom")) { offset = Vector3.zero; yaw = 0; scale = 1; FitView(); }
            Rect rect = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Event input = Event.current;
            if (rect.Contains(input.mousePosition)) {
                if (input.type == EventType.MouseDrag && input.button == 0) { viewYaw += input.delta.x; viewPitch = Mathf.Clamp(viewPitch - input.delta.y, -65, 65); input.Use(); Repaint(); }
                if (input.type == EventType.ScrollWheel) { distance = Mathf.Clamp(distance + input.delta.y * .08f, .35f, 8); input.Use(); Repaint(); }
            }
            if (Event.current.type == EventType.Repaint) {
                preview.BeginPreview(rect, GUIStyle.none);
                DrawGeometry(); GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            }
            EditorGUILayout.LabelField("Kéo chuột trái để xoay · con lăn để zoom. Dịch/quay/tỉ lệ chỉ thay đổi bản xem, không ghi đè nguồn.", EditorStyles.wordWrappedMiniLabel);
        }
        void DrawGeometry()
        {
                Matrix4x4 bodyMatrix = Matrix4x4.Translate(-origin);
                Matrix4x4 garmentMatrix = Matrix4x4.TRS(offset, Quaternion.Euler(0, yaw, 0), Vector3.one * scale) * bodyMatrix;
                if (showBody) preview.DrawMesh(body, bodyMatrix, bodyMaterial, 0);
                for (int i = 0; i < garment.subMeshCount; i++) if (selectedPanel == 0 || i == selectedPanel - 1) preview.DrawMesh(garment, garmentMatrix, panelMaterials[colorPanels ? i : 0], i);
                if (showSeams && selectedPanel == 0) preview.DrawMesh(seamLines, garmentMatrix, lineMaterial, 0);
                if (showSupports && selectedPanel == 0) preview.DrawMesh(anchorLines, garmentMatrix, lineMaterial, 0);
                preview.camera.transform.position = viewTarget + Quaternion.Euler(viewPitch, viewYaw, 0) * new Vector3(0, 0, distance);
                preview.camera.transform.LookAt(viewTarget); preview.Render();
        }
        // CLI verification renders the same scene code without driving desktop UI.
        public static void VerifyBatch()
        {
            var window = CreateInstance<GarmentDetailWindow>();
            try {
                if (window.bundle == null || !string.IsNullOrEmpty(window.error)) throw new InvalidDataException(window.error ?? "Detail bundle missing");
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Verification"));
                Directory.CreateDirectory(output);
                for (int mode = 0; mode < 2; mode++) {
                    int frontPanel = Array.FindIndex(window.bundle.panelNames, name => name.Contains("ftorso"));
                    window.selectedPanel = mode == 0 ? 0 : Mathf.Max(0, frontPanel) + 1;
                    window.showBody = mode == 0;
                    window.FitView();
                    window.preview.BeginStaticPreview(new Rect(0, 0, 900, 900)); window.DrawGeometry();
                    Texture2D image = window.preview.EndStaticPreview();
                    try { File.WriteAllBytes(Path.Combine(output, mode == 0 ? "GarmentDetail-Preview.png" : "GarmentDetail-OnePanel.png"), image.EncodeToPNG()); }
                    finally { DestroyImmediate(image); }
                }
                Debug.Log("GARMENT_DETAIL_RENDER_OK " + window.bundle.id + " panels=" + window.bundle.panelNames.Length + " renderVertices=" + window.garment.vertexCount);
            } finally { DestroyImmediate(window); }
        }
        void Release()
        {
            if (preview != null) { preview.Cleanup(); preview = null; }
            foreach (Mesh mesh in new[] { garment, body, seamLines, anchorLines }) if (mesh) DestroyImmediate(mesh);
            if (bodyMaterial) DestroyImmediate(bodyMaterial); if (lineMaterial) DestroyImmediate(lineMaterial);
            if (panelMaterials != null) foreach (Material material in panelMaterials) if (material) DestroyImmediate(material);
            garment = body = seamLines = anchorLines = null; bundle = null;
        }
    }
}
#endif
