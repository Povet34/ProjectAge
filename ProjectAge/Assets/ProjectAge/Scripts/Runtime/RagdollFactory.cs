using System.Collections.Generic;
using UnityEngine;

namespace ProjectAge
{
    /// <summary>
    /// Builds a simple primitive humanoid ragdoll at runtime (hips/chest/head/arms/legs),
    /// wired with a <see cref="RagdollController"/>, a <see cref="Grabber"/>, and two
    /// kinematic hand anchors. Asset-free, so tests and the local multi-grab sandbox can
    /// spawn characters without depending on the Synty rig.
    /// </summary>
    public static class RagdollFactory
    {
        public static GameObject CreateDummy(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var colliders = new List<Collider>();

            var hips = MakeBone(root.transform, "Hips", PrimitiveType.Cube,
                new Vector3(0f, 0f, 0f), new Vector3(0.30f, 0.25f, 0.20f), 8f, colliders, null, out var hipsRb);

            MakeBone(root.transform, "Chest", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.35f, 0.22f), 10f, colliders, hipsRb, out var chestRb);

            MakeBone(root.transform, "Head", PrimitiveType.Sphere,
                new Vector3(0f, 0.85f, 0f), new Vector3(0.28f, 0.28f, 0.28f), 4f, colliders, chestRb, out _);

            MakeBone(root.transform, "ArmL", PrimitiveType.Capsule,
                new Vector3(-0.35f, 0.60f, 0f), new Vector3(0.14f, 0.35f, 0.14f), 3f, colliders, chestRb, out _);
            MakeBone(root.transform, "ArmR", PrimitiveType.Capsule,
                new Vector3(0.35f, 0.60f, 0f), new Vector3(0.14f, 0.35f, 0.14f), 3f, colliders, chestRb, out _);

            MakeBone(root.transform, "LegL", PrimitiveType.Capsule,
                new Vector3(-0.15f, -0.50f, 0f), new Vector3(0.16f, 0.45f, 0.16f), 5f, colliders, hipsRb, out _);
            MakeBone(root.transform, "LegR", PrimitiveType.Capsule,
                new Vector3(0.15f, -0.50f, 0f), new Vector3(0.16f, 0.45f, 0.16f), 5f, colliders, hipsRb, out _);

            // Don't let a ragdoll's own bones collide with each other (prevents jitter/blowup).
            for (int i = 0; i < colliders.Count; i++)
                for (int j = i + 1; j < colliders.Count; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j], true);

            var leftHand = MakeHandAnchor(root.transform, "HandAnchor_L", new Vector3(-0.40f, 0.30f, 0.30f));
            var rightHand = MakeHandAnchor(root.transform, "HandAnchor_R", new Vector3(0.40f, 0.30f, 0.30f));

            root.AddComponent<RagdollController>();
            var grabber = root.AddComponent<Grabber>();
            grabber.leftHandBody = leftHand;
            grabber.rightHandBody = rightHand;

            // Components' Awake already ran on AddComponent; make sure bones are cached.
            root.GetComponent<RagdollController>().Initialize();

            return root;
        }

        static Transform MakeBone(Transform parent, string name, PrimitiveType prim,
            Vector3 localPos, Vector3 scale, float mass, List<Collider> colliders,
            Rigidbody connectedTo, out Rigidbody rb)
        {
            var go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            colliders.Add(go.GetComponent<Collider>());

            rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;

            if (connectedTo != null)
            {
                var joint = go.AddComponent<CharacterJoint>();
                joint.connectedBody = connectedTo;
                joint.enableProjection = true;
            }

            return go.transform;
        }

        static Rigidbody MakeHandAnchor(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.AddComponent<GrabHandAnchor>();
            return rb;
        }
    }
}
