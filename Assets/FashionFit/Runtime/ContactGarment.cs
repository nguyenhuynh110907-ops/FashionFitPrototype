using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    [DefaultExecutionOrder(240)]
    public sealed class ContactGarment : MonoBehaviour
    {
        GarmentContactWorld world;
        ElasticSurface surface;
        Vector3[] reference, merged;
        readonly List<ContactClothPart> parts = new List<ContactClothPart>();
        readonly ContactClothPart[] grips = new ContactClothPart[2];
        readonly Dictionary<MannequinGarmentHold, ContactClothPart> supports = new Dictionary<MannequinGarmentHold, ContactClothPart>();
                public bool Active => parts.Count > 0;
        public int SupportCount => supports.Count;
        public int ParticleCount { get { int count = 0; foreach (var part in parts) count += part.ParticleCount; return count; } }
        public double Milliseconds { get; private set; }
        public float MaximumStrain { get; private set; }
        public float AcceptedTravel { get; private set; }
        public static ContactGarment Attach(FashionFitApp owner, ElasticSurface garment)
        {
            var motion = garment.gameObject.AddComponent<ContactGarment>();
            motion.surface = garment;
            motion.world = GarmentContactWorld.Get(owner);
            motion.world.Garments.Add(motion);
            garment.SetContactMotion(motion.world.ManualDressing ? motion : null);
            motion.reference = (Vector3[])garment.CurrentPoints.Clone();
            motion.merged = new Vector3[motion.reference.Length];
            return motion;
        }
        public string Summary => "Vải tự do · dịch điểm giữ " + (AcceptedTravel * 100).ToString("F1") + " cm · ε đỉnh "
            + (MaximumStrain * 100).ToString("F1") + "% · tay mannequin giữ " + SupportCount;
        public string Readout => "MẶC/CỞI VẬT LÝ · BẢN THỬ\n"
            + "Điểm mô phỏng " + ParticleCount + " · g " + Physics.gravity.magnitude.ToString("F2") + " m/s²\n"
            + "Dịch chuyển điểm giữ " + (AcceptedTravel * 100).ToString("F1") + " cm\n"
            + "Strain lớn nhất trên lưới mô phỏng " + (MaximumStrain * 100).ToString("F1") + "%\n"
            + "Tay VR giữ " + surface.GrabCount + " · tay mannequin giữ " + SupportCount + "\n"
            + "Thả Grip để vải chịu trọng lực và tiếp xúc\n"
            + (MaximumStrain > surface.Material.maxStrain ? "VƯỢT PRESET · cơ học vật liệu chưa đạt\n" : "")
            + "Độ cứng/ma sát là preset thử, chưa hiệu chuẩn lực N";
        public void SetInteractive(bool value)
        {
            if (!value) { ResetPlacement(); surface.ResetShape(); }
            surface.SetContactMotion(value ? this : null);
        }
        ContactClothPart Part(int renderIndex)
        {
            foreach (var part in parts) if (part.Covers(renderIndex)) return part;
            var added = new ContactClothPart(world, surface);
            added.Activate(renderIndex);
            parts.Add(added);
            return added;
        }
        public void Activate() { Part(0); }
        public void ActivateAt(Vector3 worldPoint)
        {
            var points = surface.CurrentPoints; int selected = 0;
            for (int i = 1; i < points.Length; i++)
                if ((transform.TransformPoint(points[i]) - worldPoint).sqrMagnitude < (transform.TransformPoint(points[selected]) - worldPoint).sqrMagnitude) selected = i;
            Part(selected);
        }
        public bool BeginGrip(int slot, int renderIndex, Vector3 controller)
        {
            var part = Part(renderIndex);
            if (!part.BeginGrip(slot, renderIndex, controller)) return false;
            grips[slot] = part; return true;
        }
        public void MoveGrip(int slot, Vector3 p) { grips[slot]?.MoveGrip(slot, p); }
        public void EndGrip(int slot) { grips[slot]?.EndGrip(slot); grips[slot] = null; }
        public bool NearestParticle(Vector3 p, float reach, out int index, out Vector3 point)
        {
            index = -1; point = p;
            bool onSurface = surface.Nearest(p, reach, out point);
            if (!Active) return onSurface;
            bool found = false;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].NearestParticle(p, reach, out int particle, out Vector3 hit))
                { index = i * 65536 + particle; point = hit; reach = Vector3.Distance(p, hit); found = true; }
            return found || onSurface;
        }
        public bool AddSupport(MannequinGarmentHold hand, int encoded)
        {
            var part = parts[encoded / 65536];
            if (supports.ContainsKey(hand) || !part.AddSupport(hand, encoded % 65536)) return false;
            supports.Add(hand, part); return true;
        }
        public void ReleaseSupport(MannequinGarmentHold hand)
        {
            if (!supports.TryGetValue(hand, out var part)) return;
            part.ReleaseSupport(hand); supports.Remove(hand);
        }
        void Update() { foreach (var part in parts) part.Update(); }
        public void Step(float dt) { Publish(); }
        void LateUpdate() { Publish(); }
        void Publish()
        {
            if (!Active) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Array.Copy(surface.CurrentPoints, merged, merged.Length);
            AcceptedTravel = MaximumStrain = 0;
            foreach (var part in parts)
            {
                part.Publish(merged);
                AcceptedTravel = Mathf.Max(AcceptedTravel, part.AcceptedTravel);
                MaximumStrain = Mathf.Max(MaximumStrain, part.MaximumStrain);
            }
            surface.AcceptPose(merged, reference, 1, AcceptedTravel);
            var box = GetComponent<BoxCollider>();
            if (box) { box.center = surface.MeshAt(0).bounds.center; box.size = surface.MeshAt(0).bounds.size + Vector3.one * .035f; }
            Milliseconds = timer.Elapsed.TotalMilliseconds;
        }
        public void ResetPlacement()
        {
            foreach (var part in parts) part.Dispose();
            parts.Clear(); supports.Clear(); grips[0] = grips[1] = null;
        }
        void OnDestroy() { if (world) world.Garments.Remove(this); ResetPlacement(); }
    }
}
