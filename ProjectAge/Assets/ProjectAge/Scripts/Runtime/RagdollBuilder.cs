using UnityEngine;

namespace ProjectAge
{
    /// <summary>
    /// Builds a CharacterJoint ragdoll on a Humanoid rig, driven purely by
    /// <see cref="Animator.GetBoneTransform"/> so it works on any humanoid regardless of the
    /// asset's bone naming (Synty uses Shoulder_L/Elbow_L/etc. — we don't care).
    ///
    /// Uses capsules for every limb/torso segment (oriented along the bone-to-child axis, so
    /// it is robust to whatever local axis convention the rig uses) plus a sphere for the head.
    /// Runtime API so it can be called from the editor (menu / execute_code) or at runtime.
    /// </summary>
    public static class RagdollBuilder
    {
        public static bool Build(GameObject root, float totalMass = 22f)
        {
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError($"[RagdollBuilder] {root.name}: requires a Humanoid Animator.");
                return false;
            }

            Transform B(HumanBodyBones b) => animator.GetBoneTransform(b);

            var hips = B(HumanBodyBones.Hips);
            var spine = B(HumanBodyBones.Spine);
            var chest = B(HumanBodyBones.Chest) != null ? B(HumanBodyBones.Chest) : spine;
            var head = B(HumanBodyBones.Head) != null ? B(HumanBodyBones.Head) : B(HumanBodyBones.Neck);

            var lUA = B(HumanBodyBones.LeftUpperArm); var lLA = B(HumanBodyBones.LeftLowerArm); var lHnd = B(HumanBodyBones.LeftHand);
            var rUA = B(HumanBodyBones.RightUpperArm); var rLA = B(HumanBodyBones.RightLowerArm); var rHnd = B(HumanBodyBones.RightHand);
            var lUL = B(HumanBodyBones.LeftUpperLeg); var lLL = B(HumanBodyBones.LeftLowerLeg); var lFt = B(HumanBodyBones.LeftFoot);
            var rUL = B(HumanBodyBones.RightUpperLeg); var rLL = B(HumanBodyBones.RightLowerLeg); var rFt = B(HumanBodyBones.RightFoot);

            if (hips == null || chest == null || head == null)
            {
                Debug.LogError($"[RagdollBuilder] {root.name}: missing core humanoid bones (hips/chest/head).");
                return false;
            }

            float m = totalMass;

            // Torso (pelvis is the ragdoll root, no joint).
            AddCapsule(hips, chest, 0.45f, m * 0.15f, null);
            AddCapsule(chest, head, 0.40f, m * 0.20f, RB(hips));
            AddSphere(head, chest, 0.30f, m * 0.08f, RB(chest));

            // Arms.
            AddCapsule(lUA, lLA, 0.30f, m * 0.05f, RB(chest));
            AddCapsule(lLA, lHnd != null ? lHnd : lLA, 0.28f, m * 0.03f, RB(lUA));
            AddCapsule(rUA, rLA, 0.30f, m * 0.05f, RB(chest));
            AddCapsule(rLA, rHnd != null ? rHnd : rLA, 0.28f, m * 0.03f, RB(rUA));

            // Legs.
            AddCapsule(lUL, lLL, 0.28f, m * 0.11f, RB(hips));
            AddCapsule(lLL, lFt != null ? lFt : lLL, 0.26f, m * 0.08f, RB(lUL));
            AddCapsule(rUL, rLL, 0.28f, m * 0.11f, RB(hips));
            AddCapsule(rLL, rFt != null ? rFt : rLL, 0.26f, m * 0.08f, RB(rUL));

            return true;
        }

        static Rigidbody RB(Transform t) => t != null ? t.GetComponent<Rigidbody>() : null;

        static void AddCapsule(Transform bone, Transform toward, float radiusFrac, float mass, Rigidbody connectedTo)
        {
            if (bone == null) return;

            Vector3 local = (toward != null && toward != bone) ? bone.InverseTransformPoint(toward.position) : Vector3.up * 0.1f;
            float len = local.magnitude;
            if (len < 1e-4f) len = 0.1f;

            var rb = GetOrAdd<Rigidbody>(bone.gameObject);
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var cap = GetOrAdd<CapsuleCollider>(bone.gameObject);
            cap.direction = DominantAxis(local);
            cap.center = local * 0.5f;
            cap.radius = Mathf.Max(0.03f, len * radiusFrac);
            cap.height = len + cap.radius * 2f;

            if (connectedTo != null) AddJoint(bone.gameObject, connectedTo);
        }

        static void AddSphere(Transform bone, Transform refBone, float radiusFrac, float mass, Rigidbody connectedTo)
        {
            if (bone == null) return;

            float d = refBone != null ? Vector3.Distance(bone.position, refBone.position) : 0.12f;
            float scale = Mathf.Abs(bone.lossyScale.x);
            if (scale < 1e-4f) scale = 1f;

            var rb = GetOrAdd<Rigidbody>(bone.gameObject);
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var sph = GetOrAdd<SphereCollider>(bone.gameObject);
            sph.center = Vector3.zero;
            sph.radius = Mathf.Max(0.05f, d * radiusFrac) / scale;

            if (connectedTo != null) AddJoint(bone.gameObject, connectedTo);
        }

        static void AddJoint(GameObject go, Rigidbody connectedTo)
        {
            var j = GetOrAdd<CharacterJoint>(go);
            j.connectedBody = connectedTo;
            j.enableProjection = true;
            j.axis = Vector3.right;
            j.swingAxis = Vector3.up;
            j.lowTwistLimit = new SoftJointLimit { limit = -20f };
            j.highTwistLimit = new SoftJointLimit { limit = 20f };
            j.swing1Limit = new SoftJointLimit { limit = 35f };
            j.swing2Limit = new SoftJointLimit { limit = 35f };
        }

        static int DominantAxis(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            if (a.x >= a.y && a.x >= a.z) return 0;
            if (a.y >= a.x && a.y >= a.z) return 1;
            return 2;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
