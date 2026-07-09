using System.Collections.Generic;
using UnityEngine;

namespace ProjectAge
{
    /// <summary>
    /// Toggles a rigged character between animated (kinematic) and ragdoll (dynamic) state,
    /// and manages the bone rigidbodies. Sits on the character root.
    ///
    /// Ragdoll bones = every child Rigidbody that is NOT a <see cref="GrabHandAnchor"/>.
    /// Movement is expected to use a CharacterController (not a Rigidbody), so there is no
    /// separate "control body" to exclude.
    /// </summary>
    [DisallowMultipleComponent]
    public class RagdollController : MonoBehaviour
    {
        public bool IsRagdoll { get; private set; }
        public bool IsFrozen { get; private set; }

        Rigidbody[] _bodies;
        Rigidbody _hips;
        Animator _animator;
        Collider _standingCollider; // root body capsule, on while standing / off while ragdoll

        public IReadOnlyList<Rigidbody> Bodies => _bodies;
        public Rigidbody HipsBody => _hips;

        void Awake() => Initialize();

        /// <summary>Idempotent; safe to call again after building bones at runtime.</summary>
        public void Initialize()
        {
            if (_bodies != null) return;

            _animator = GetComponentInChildren<Animator>();

            var bodyList = new List<Rigidbody>();
            foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb.gameObject == gameObject) continue;               // root movement body
                if (rb.GetComponent<GrabHandAnchor>() != null) continue; // hand anchors
                bodyList.Add(rb);
            }
            _bodies = bodyList.ToArray();

            _standingCollider = GetComponent<Collider>(); // capsule on the root, if any

            // Hips = the one bone without a CharacterJoint (the ragdoll root).
            foreach (var b in _bodies)
            {
                if (b.GetComponent<CharacterJoint>() == null) { _hips = b; break; }
            }
            if (_hips == null && _bodies.Length > 0) _hips = _bodies[0];

            IgnoreSelfCollisions();
            ApplyRagdoll(false);
        }

        /// <summary>
        /// Stop the ragdoll's own bones from colliding with each other. Adjacent bones are
        /// already excluded by their CharacterJoint, but non-adjacent pairs (left arm vs right
        /// arm, head vs arms, limbs vs torso) are not — at the bind pose those capsules overlap,
        /// and without this the solver blasts the ragdoll apart into stretched spikes.
        ///
        /// Physics.IgnoreCollision is runtime-only state (it does NOT survive a play-mode enter
        /// or scene load), so it must run here at Awake rather than at build time.
        /// </summary>
        void IgnoreSelfCollisions()
        {
            var cols = new List<Collider>();
            foreach (var b in _bodies)
            {
                if (b == null) continue;
                var c = b.GetComponent<Collider>();
                if (c != null) cols.Add(c);
            }

            for (int i = 0; i < cols.Count; i++)
                for (int j = i + 1; j < cols.Count; j++)
                    Physics.IgnoreCollision(cols[i], cols[j], true);
        }

        public void SetRagdoll(bool on) => ApplyRagdoll(on);

        void ApplyRagdoll(bool on)
        {
            IsRagdoll = on;
            IsFrozen = false;

            if (_animator != null) _animator.enabled = !on;
            if (_standingCollider != null) _standingCollider.enabled = !on;

            foreach (var b in _bodies)
            {
                if (b == null) continue;
                b.isKinematic = !on;
                b.interpolation = RigidbodyInterpolation.Interpolate;
                if (on)
                {
                    b.linearVelocity = Vector3.zero;
                    b.angularVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// Freeze the ragdoll in its current pose (E key). Bones become kinematic but the
        /// animator stays off, so a chain of frozen + non-frozen segments behaves like a
        /// flexible chain-mace when swung.
        /// </summary>
        public void SetFrozen(bool frozen)
        {
            if (!IsRagdoll) return;
            IsFrozen = frozen;
            foreach (var b in _bodies)
            {
                if (b != null) b.isKinematic = frozen;
            }
        }

        public Rigidbody GetNearestBody(Vector3 worldPoint)
        {
            Rigidbody best = _hips;
            float bestSqr = float.MaxValue;
            foreach (var b in _bodies)
            {
                if (b == null) continue;
                float d = (b.worldCenterOfMass - worldPoint).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = b; }
            }
            return best;
        }
    }
}
