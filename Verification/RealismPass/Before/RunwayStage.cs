using UnityEngine;

namespace FashionFit
{
    // A restrained atelier environment: warm stone, charcoal and brass accents.
    public static class RunwayStage
    {
        public static Material Matte(string name, Color color, float smooth = .2f, float metallic = 0)
        {
            var m = new Material(Resources.Load<Shader>("Runway/RunwaySurface")) { name = name };
            m.SetColor("_Color", color); m.SetFloat("_Solid", 1); m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if(Application.isPlaying)Object.Destroy(go.GetComponent<Collider>());else Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }

        public static TextMesh Text(Transform parent, string text, Vector3 position, float size, Color color)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 100; label.characterSize = size / 10;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color; return label;
        }

        public static void Build(Transform root)
        {
            var room = new GameObject("Fashion Unity atelier - 8m x 8m").transform; room.SetParent(root, false);
            var floor = Matte("Warm limestone", new Color(.43f, .405f, .36f), .20f);
            var wall = Matte("Ivory walls", new Color(.78f, .735f, .65f), .15f);
            var charcoal = Matte("Charcoal inset", new Color(.055f, .062f, .068f), .36f);
            var brass = Matte("Brushed brass", new Color(.42f, .29f, .105f), .48f, .52f);
            var owned = room.gameObject.AddComponent<FittingRoomResources>(); owned.Materials = new[] { floor, wall, charcoal, brass };
            Box(room, "Floor", new Vector3(0, -.055f, 0), new Vector3(8, .1f, 8), floor);
            Box(room, "Back wall", new Vector3(0, 1.65f, -4), new Vector3(8, 3.3f, .10f), wall);
            Box(room, "Left wall", new Vector3(-4, 1.65f, 0), new Vector3(.10f, 3.3f, 8), wall);
            Box(room, "Right wall", new Vector3(4, 1.65f, 0), new Vector3(.10f, 3.3f, 8), wall);
            Box(room, "Front wall", new Vector3(0, 1.65f, 4), new Vector3(8, 3.3f, .10f), wall);
            Box(room, "Atelier backdrop", new Vector3(0, 1.55f, -3.91f), new Vector3(3.15f, 2.72f, .035f), charcoal);
            Box(room, "Presentation plinth", new Vector3(0, -.015f, 0), new Vector3(1.55f, .035f, 1.55f), charcoal);
            Box(room, "Brass line left", new Vector3(-1.61f, 1.55f, -3.865f), new Vector3(.025f, 2.72f, .018f), brass);
            Box(room, "Brass line right", new Vector3(1.61f, 1.55f, -3.865f), new Vector3(.025f, 2.72f, .018f), brass);
            AddLight(room, "Warm key", Quaternion.Euler(43, -32, 0), 1.02f, new Color(1f, .88f, .72f), true);
            AddLight(room, "Cool fill", Quaternion.Euler(56, 137, 0), .42f, new Color(.72f, .82f, 1f), false);
            AddLight(room, "Rim light", Quaternion.Euler(28, 190, 0), .33f, new Color(1f, .78f, .56f), false);
        }

        static void AddLight(Transform parent, string name, Quaternion rotation, float intensity, Color color, bool shadow)
        {
            var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(parent, false); light.transform.rotation = rotation;
            light.type = LightType.Directional; light.color = color; light.intensity = intensity;
            light.shadows = shadow ? LightShadows.Soft : LightShadows.None; light.shadowStrength = .55f; light.shadowBias = .015f; light.shadowNormalBias = .02f;
        }
    }

    public sealed class FittingRoomResources : MonoBehaviour
    {
        public Material[] Materials;
        void OnDestroy() { if (Materials != null) foreach (var material in Materials) if (material) Destroy(material); }
    }
}
