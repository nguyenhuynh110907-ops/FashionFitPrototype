#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FashionFit.Editor
{
    public sealed class FemaleAvatarSetupWindow:EditorWindow
    {
        GameObject prefab;RuntimeAnimatorController controller;string walkState="Base Layer.Walk",message;
        [MenuItem("Fashion Fit/Prepare Original Female Avatar")]
        static void Open()=>GetWindow<FemaleAvatarSetupWindow>("Female avatar");
        void OnGUI()
        {
            GUILayout.Label("Gắn nhân vật nữ gốc",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Cần tệp 3D thật, không dùng ảnh PNG. Nhập FBX vào Assets, chọn Rig = Humanoid, bật Read/Write, chuẩn đơn vị mét, bàn chân ở Y = 0. Chuẩn bị Animator Controller có trạng thái đi bộ tại chỗ. Chưa gắn nhân vật khi chưa có dữ liệu này.",MessageType.Info);
            prefab=(GameObject)EditorGUILayout.ObjectField("Model / prefab",prefab,typeof(GameObject),false);
            controller=(RuntimeAnimatorController)EditorGUILayout.ObjectField("Walk controller",controller,typeof(RuntimeAnimatorController),false);
            walkState=EditorGUILayout.TextField("Full walk state",walkState);
            if(GUILayout.Button("Kiểm tra và gắn vào scene"))Apply();
            if(GUILayout.Button("Tiếp tục dùng ma-nơ-canh tạm"))ApplyTemporary();
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
            EditorGUILayout.HelpBox("Nhân vật gốc được xem và đi tại chỗ trước. Bộ căn đồ tự khóa cho đến khi các mốc cơ thể và vùng va chạm được hiệu chỉnh theo mesh thật. Không tái dùng số đo mẫu tạm một cách âm thầm.",MessageType.None);
        }
        FashionFitApp App()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){message="Dừng Play trước khi sửa nhân vật để thay đổi được lưu.";return null;}
            var app=Object.FindObjectOfType<FashionFitApp>();if(app==null)message="Mở scene Assets/FashionFit/Scenes/FashionFitPrototype.unity trước.";return app;
        }
        void Apply()
        {
            var app=App();if(app==null)return;
            if(prefab==null||controller==null||string.IsNullOrWhiteSpace(walkState)){message="Chọn đủ model, controller và tên trạng thái đi bộ.";return;}
            var a=prefab.GetComponentInChildren<Animator>();var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(a==null||a.avatar==null||!a.avatar.isValid||!a.avatar.isHuman){message="Avatar chưa phải Humanoid hợp lệ. Kiểm tra Rig > Configure.";return;}
            if(skins.Length==0){message="Không có mesh gắn xương.";return;}
            foreach(var s in skins)if(s.sharedMesh!=null&&!s.sharedMesh.isReadable){message="Bật Read/Write cho mesh: "+s.sharedMesh.name;return;}
            Undo.RecordObject(app,"Attach original female avatar");app.FemaleAvatarPrefab=prefab;app.WalkController=controller;app.WalkState=walkState.Trim();EditorUtility.SetDirty(app);EditorSceneManager.MarkSceneDirty(app.gameObject.scene);EditorSceneManager.SaveScene(app.gameObject.scene);
            message="Đã lưu liên kết nhân vật. Bấm Play để kiểm tra; Console sẽ báo nếu thiếu trạng thái đi hoặc tỷ lệ chưa đúng.";
        }
        void ApplyTemporary(){var app=App();if(app==null)return;Undo.RecordObject(app,"Use temporary mannequin");app.FemaleAvatarPrefab=null;app.WalkController=null;EditorUtility.SetDirty(app);EditorSceneManager.MarkSceneDirty(app.gameObject.scene);EditorSceneManager.SaveScene(app.gameObject.scene);message="Đã chọn lại mẫu nữ tạm; dữ liệu gốc không bị xóa.";}
    }
}
#endif
