using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    [DefaultExecutionOrder(150)]
    public sealed class GarmentContactWorld : MonoBehaviour
    {
        sealed class Body
        {
            public RunwayAvatar avatar;
            public readonly List<CapsuleCollider> capsules = new List<CapsuleCollider>();
            public SphereCollider head;
            public readonly List<SphereCollider> hands = new List<SphereCollider>();
        }
        sealed class Segment
        {
            public Transform bone;
            public Vector3 a, b;
            public CapsuleCollider collider;
        }
        readonly List<Body> bodies = new List<Body>();
        readonly List<Segment> segments = new List<Segment>();
        readonly List<MannequinGarmentHold> holds = new List<MannequinGarmentHold>();
        FashionFitApp app;
        SphereCollider floor;
        bool initialized;
        public bool ManualDressing { get; private set; }
        public readonly List<ContactGarment> Garments = new List<ContactGarment>();
        public static GarmentContactWorld Get(FashionFitApp owner)
        {
            var world = owner.GetComponent<GarmentContactWorld>();
            if (!world) world = owner.gameObject.AddComponent<GarmentContactWorld>();
            world.app = owner;
            return world;
        }
        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            var avatars = new List<RunwayAvatar> { app.Model };
            foreach (var rig in app.PoseStudio.Rigs) avatars.Add(rig.Avatar);
            foreach (var avatar in avatars) Build(avatar);
            floor = Sphere(transform, "Cloth floor", new Vector3(0, -1000, 0), 1000);
            foreach (var rig in app.PoseStudio.Rigs)
                foreach (string side in new[] { "Left", "Right" })
                {
                    var hand = rig.Avatar.FindJoint(side + "Hand");
                    var hold = hand.gameObject.AddComponent<MannequinGarmentHold>();
                    hold.Initialize(this, rig, hand);
                    holds.Add(hold);
                }
            Refresh();
        }
        static SphereCollider Sphere(Transform parent, string label, Vector3 position, float radius)
        {
            var go = new GameObject(label); go.layer = 2;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var sphere = go.AddComponent<SphereCollider>(); sphere.isTrigger = true; sphere.radius = radius;
            return sphere;
        }
        void Capsule(Body body, int bone, Vector3 a, Vector3 b, float radius)
        {
            var go = new GameObject("Cloth contact " + body.avatar.name); go.layer = 2;
            go.transform.SetParent(transform, false);
            var collider = go.AddComponent<CapsuleCollider>(); collider.isTrigger = true;
            collider.radius = radius; collider.direction = 1;
            body.capsules.Add(collider);
            segments.Add(new Segment { bone = body.avatar.Bones[bone], a = body.avatar.GarmentBindposes[bone].MultiplyPoint3x4(a), b = body.avatar.GarmentBindposes[bone].MultiplyPoint3x4(b), collider = collider });
        }
        void Build(RunwayAvatar avatar)
        {
            var body = new Body { avatar = avatar }; bodies.Add(body);
            var vertices = avatar.BodyVertices;
            var weights = avatar.BodyMesh.boneWeights;
            string[] starts = { "LeftArm", "LeftForeArm", "RightArm", "RightForeArm", "LeftUpLeg", "LeftLeg", "RightUpLeg", "RightLeg" };
            string[] ends = { "LeftForeArm", "LeftHand", "RightForeArm", "RightHand", "LeftLeg", "LeftFoot", "RightLeg", "RightFoot" };
            for (int k = 0; k < starts.Length; k++)
            {
                int bone = avatar.BoneIndex(starts[k]);
                Vector3 a = FittedGarmentGeometry.Neutral(avatar, starts[k]), b = FittedGarmentGeometry.Neutral(avatar, ends[k]), line = b - a;
                float radius = .025f;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    if (w.boneIndex0 != bone || w.weight0 < .6f) continue;
                    float u = Vector3.Dot(vertices[i] - a, line) / line.sqrMagnitude;
                    if (u > .18f && u < .82f) radius = Mathf.Max(radius, Vector3.Distance(vertices[i], a + line * u));
                }
                Capsule(body, bone, a + line * .08f, b - line * .08f, radius + .008f);
            }
            float height = avatar.BodyMesh.bounds.max.y;
            for (int row = 0; row < 5; row++)
            {
                float y = height * Mathf.Lerp(.54f, .83f, row / 4f), x = .08f, front = .06f, back = -.06f;
                int bone = avatar.BoneIndex(row < 2 ? "Hips" : row < 4 ? "Spine1" : "Spine2");
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (Mathf.Abs(vertices[i].y - y) > .025f || Mathf.Abs(vertices[i].x) > .3f) continue;
                    string joint = avatar.Bones[weights[i].boneIndex0].name;
                    if (joint.Contains("Arm") || joint.Contains("Hand")) continue;
                    x = Mathf.Max(x, Mathf.Abs(vertices[i].x)); front = Mathf.Max(front, vertices[i].z); back = Mathf.Min(back, vertices[i].z);
                }
                float radius = (front - back) * .5f, span = Mathf.Max(.005f, x - radius);
                // Bound the full section, including off-centre chest support.
                // z-extrema alone leave the sides of an elliptical body outside
                // a capsule with the same maximum width and depth.
                for (int i = 0; i < vertices.Length; i++)
                {
                    var p = vertices[i];
                    if (Mathf.Abs(p.y - y) > height * .044f || Mathf.Abs(p.x) > .3f) continue;
                    string joint = avatar.Bones[weights[i].boneIndex0].name;
                    if (joint.Contains("Arm") || joint.Contains("Hand")) continue;
                    float dx = p.x - Mathf.Clamp(p.x, -span, span), dz = p.z - (front + back) * .5f;
                    radius = Mathf.Max(radius, Mathf.Sqrt(dx * dx + dz * dz));
                }
                radius += .025f;
                Capsule(body, bone, new Vector3(-span, y, (front + back) * .5f), new Vector3(span, y, (front + back) * .5f), radius);
            }
            var head = avatar.FindJoint("Head");
            var bounds = new Bounds(new Vector3(0, height * .93f, .015f), Vector3.zero);
            foreach (var p in vertices) if (p.y >= height * .86f) bounds.Encapsulate(p);
            Vector3 centre = bounds.center;
            float headRadius = 0;
            foreach (var p in vertices) if (p.y >= height * .86f) headRadius = Mathf.Max(headRadius, Vector3.Distance(p, centre));
            int headIndex = avatar.BoneIndex("Head");
            body.head = Sphere(head, "Cloth head", avatar.GarmentBindposes[headIndex].MultiplyPoint3x4(centre), headRadius + .022f);
            foreach (string side in new[] { "Left", "Right" }) body.hands.Add(Sphere(avatar.FindJoint(side + "Hand"), "Cloth palm", Vector3.zero, .055f));
        }
        void Update() { if (initialized) Refresh(); }
        public void Refresh()
        {
            foreach (var part in segments)
            {
                Vector3 a = part.bone.TransformPoint(part.a), b = part.bone.TransformPoint(part.b);
                part.collider.transform.SetPositionAndRotation((a + b) * .5f, Quaternion.FromToRotation(Vector3.up, b - a));
                part.collider.height = Vector3.Distance(a, b) + 2 * part.collider.radius;
            }
        }
        public void Configure(Cloth cloth)
        {
            EnsureInitialized();
            var spheres = new List<ClothSphereColliderPair> { new ClothSphereColliderPair(floor) };
            foreach (var body in bodies)
            {
                spheres.Add(new ClothSphereColliderPair(body.head));
                foreach (var hand in body.hands) spheres.Add(new ClothSphereColliderPair(hand));
            }
            cloth.sphereColliders = spheres.ToArray();
            RefreshClothCollisions(cloth, cloth.vertices);
        }
        public void RefreshClothCollisions(Cloth cloth, Vector3[] points)
        {
            if (points.Length == 0) return;
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            var available = new List<CapsuleCollider>();
            foreach (var body in bodies) available.AddRange(body.capsules);
            Vector3 centre = bounds.center;
            available.Sort((a, b) =>
            {
                float first = a.bounds.SqrDistance(centre) + (a.bounds.center - centre).sqrMagnitude * .01f;
                float second = b.bounds.SqrDistance(centre) + (b.bounds.center - centre).sqrMagnitude * .01f;
                int order = first.CompareTo(second);
                return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            // Reserve ten spheres for the floor, three heads and six palms.
            // Each selected capsule uses two sphere endpoints.
            int count = Mathf.Min(11, available.Count);
            var current = cloth.capsuleColliders;
            bool changed = current.Length != count;
            for (int i = 0; i < count && !changed; i++) changed = current[i] != available[i];
            if (!changed) return;
            var selected = new CapsuleCollider[count];
            available.CopyTo(0, selected, 0, count);
            cloth.capsuleColliders = selected;
        }
        public void ToggleManualDressing() { SetManualDressing(!ManualDressing); }
        public void SetManualDressing(bool value)
        {
            if (app.Player) app.Player.EndActiveGrab();
            ManualDressing = value;
            if (value) EnsureInitialized();
            foreach (var part in segments) part.collider.enabled = value;
            foreach (var body in bodies)
            {
                body.head.enabled = value;
                foreach (var hand in body.hands) hand.enabled = value;
            }
            if (floor) floor.enabled = value;
            foreach (var garment in Garments) if (garment) garment.SetInteractive(value);
            app.Report(value ? "MẶC/CỞI VẬT LÝ · Grip kéo áo, hai tay giữ hai vùng · Trigger gần tay mannequin để giữ/thả vải · bản thử"
                : "Đã về phom ban đầu và chế độ kéo đàn hồi");
        }
        public Vector3 ProjectHeldPoint(Vector3 point)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                foreach (var part in segments)
                {
                    var capsule = part.collider;
                    Vector3 centre = capsule.transform.position;
                    Vector3 axis = capsule.transform.up;
                    float half = Mathf.Max(0, capsule.height * .5f - capsule.radius);
                    Vector3 nearest = centre + axis * Mathf.Clamp(Vector3.Dot(point - centre, axis), -half, half);
                    Vector3 delta = point - nearest;
                    if (delta.sqrMagnitude < (capsule.radius + .005f) * (capsule.radius + .005f))
                        point = nearest + (delta.sqrMagnitude > 1e-10f ? delta.normalized : Vector3.forward) * (capsule.radius + .005f);
                }
                foreach (var body in bodies)
                {
                    Vector3 centre = body.head.transform.TransformPoint(body.head.center);
                    Vector3 delta = point - centre;
                    if (delta.magnitude < body.head.radius + .005f)
                        point = centre + delta.normalized * (body.head.radius + .005f);
                }
            }
            point.y = Mathf.Max(.01f, point.y);
            return point;
        }
        public bool ToggleHandNear(Vector3 controller)
        {
            EnsureInitialized(); MannequinGarmentHold selected = null; float best = .11f;
            foreach (var hand in holds)
            {
                float d = Vector3.Distance(controller, hand.Position);
                if (d < best) { selected = hand; best = d; }
            }
            return selected && selected.Toggle();
        }
        public bool ToggleHand(RunwayPoseRig rig, string side)
        {
            EnsureInitialized();
            foreach (var hand in holds) if (hand.Rig == rig && hand.Hand.name.Contains(side + "Hand")) return hand.Toggle();
            return false;
        }
    }
    public sealed class MannequinGarmentHold : MonoBehaviour
    {
        GarmentContactWorld world;
        ContactGarment garment;
        public RunwayPoseRig Rig { get; private set; }
        public Transform Hand { get; private set; }
        public Vector3 Position => Hand.position;
        public bool IsHolding => garment;
        public void Initialize(GarmentContactWorld owner, RunwayPoseRig rig, Transform hand) { world = owner; Rig = rig; Hand = hand; }
        public bool Toggle()
        {
            if (garment) { garment.ReleaseSupport(this); garment = null; return true; }
            if (!world.ManualDressing) return false;
            ContactGarment nearest = null; float best = .095f;
            foreach (var candidate in world.Garments)
                if (candidate && candidate.NearestParticle(Position, best, out _, out Vector3 point))
                { nearest = candidate; best = Vector3.Distance(Position, point); }
            if (!nearest) return false;
            nearest.ActivateAt(Position);
            if (!nearest.NearestParticle(Position, .095f, out int particle, out _) || !nearest.AddSupport(this, particle)) return false;
            garment = nearest; return true;
        }
        void OnDestroy() { if (garment) garment.ReleaseSupport(this); }
    }
}
