#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public sealed class StudentGarmentWindow : EditorWindow
    {
        GameObject frame;
        string specPath = "", status = "";
        int bodyChoice;
        Vector2 scroll;

        [MenuItem("Fashion Fit/3D to 3D/Student Submission")]
        public static void Open() => GetWindow<StudentGarmentWindow>("Trang phục sinh viên");

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("TRANG PHỤC 3D THEO BODY MẪU", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sinh viên dựng đồ trên body tham chiếu và gửi body, nhánh Garment, vật liệu/texture cùng submission.json. Bản xem đứng yên giữ chi tiết mesh gốc.", MessageType.Info);
            bodyChoice = EditorGUILayout.Popup("Body mẫu", bodyChoice, GarmentBodyReference.Ids);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("Xuất body và tạo mẫu nộp trong Scene"))
                {
                    try
                    {
                        string id = GarmentBodyReference.Ids[bodyChoice];
                        string prefab = GarmentBodyReference.Export(id);
                        frame = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                        Undo.RegisterCreatedObjectUndo(frame, "Create student garment reference");
                        specPath = Path.GetFullPath("Samples/StudentGarments/" + id + "/submission.json");
                        Selection.activeGameObject = frame;
                        status = "Đã xuất BodyReference.obj và submission.json tại Samples/StudentGarments/" + id;
                    }
                    catch (Exception error) { status = error.Message; }
                }
            EditorGUILayout.Space();
            frame = (GameObject)EditorGUILayout.ObjectField("Frame bài nộp trong Scene", frame, typeof(GameObject), true);
            specPath = EditorGUILayout.TextField("Tệp submission.json", specPath);
            if (GUILayout.Button("Chọn submission.json"))
            {
                string selected = EditorUtility.OpenFilePanel("Chọn mô tả bài nộp", "", "json");
                if (!string.IsNullOrEmpty(selected)) specPath = selected;
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || !frame || !File.Exists(specPath)))
                if (GUILayout.Button("Kiểm tra và tạo gói 3D"))
                {
                    StudentGarmentPipeline.Result result = null;
                    try
                    {
                        if (EditorUtility.IsPersistent(frame)) throw new InvalidDataException("Kéo prefab vào Scene trước khi kiểm tra.");
                        var spec = StudentGarmentPipeline.ReadSpec(specPath);
                        result = StudentGarmentPipeline.Process(frame, spec);
                        string destination = StudentGarmentPipeline.Save(result, spec);
                        var preview = StudentGarmentPipeline.CreatePreview(result, spec);
                        preview.transform.position = frame.transform.position + Vector3.right * 1.6f;
                        Undo.RegisterCreatedObjectUndo(preview, "Create transferred garment preview");
                        status = result.report.status + "\n" + destination + "\n" + string.Join("\n", result.report.reviewReasons);
                        Selection.activeGameObject = preview;
                    }
                    catch (Exception error) { status = "Chưa nhận bài nộp.\n" + error.Message; }
                    finally { if (result != null) StudentGarmentPipeline.Release(result.package); }
                }
            EditorGUILayout.HelpBox("Body khác hình học cần đăng ký tương ứng trước. Gói đạt kiểm tra tĩnh cần kiểm thử riêng trước khi cho đi bộ, chỉnh pose hoặc kéo vải VR.", MessageType.None);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.TextArea(status, GUILayout.MinHeight(100));
            EditorGUILayout.EndScrollView();
        }

        // Command-line importer for already imported FBX/OBJ/GLB prefabs.
        public static void RunBatch()
        {
            GameObject root = null;
            StudentGarmentPipeline.Result result = null;
            string output = StudioTelemetry.Argument("-fashionSubmissionReport");
            try
            {
                if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output)) throw new InvalidDataException("Explicit absolute -fashionSubmissionReport required.");
                var spec = StudentGarmentPipeline.ReadSpec(StudioTelemetry.Argument("-fashionSubmissionSpec"));
                string model = StudioTelemetry.Argument("-fashionSubmissionPrefab");
                if (string.IsNullOrEmpty(model) || !model.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidDataException("Imported model must be an Assets/ prefab path.");
                if (AssetImporter.GetAtPath(model) is ModelImporter) GarmentImportPreparation.Prepare(model);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
                if (!prefab) throw new InvalidDataException("Imported prefab missing " + model);
                root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                result = StudentGarmentPipeline.Process(root, spec);
                StudentGarmentPipeline.Save(result, spec);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(result.report, true));
                bool ready = result.report.stationarySamplesPassed;
                UnityEngine.Object.DestroyImmediate(root);
                EditorApplication.Exit(ready ? 0 : 2);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (root) UnityEngine.Object.DestroyImmediate(root);
                if (!string.IsNullOrEmpty(output) && Path.IsPathRooted(output))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    File.WriteAllText(output, JsonUtility.ToJson(new Failure { reason = error.Message }, true));
                }
                EditorApplication.Exit(1);
            }
            finally { if (result != null) StudentGarmentPipeline.Release(result.package); }
        }

        [Serializable] sealed class Failure { public string status = "REJECTED", reason; }
    }
}
#endif
