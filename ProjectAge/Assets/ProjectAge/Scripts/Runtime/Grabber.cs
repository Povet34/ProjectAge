using UnityEngine;

namespace ProjectAge
{
    /// <summary>Marker so <see cref="RagdollController"/> ignores kinematic hand-anchor bodies.</summary>
    public class GrabHandAnchor : MonoBehaviour { }

    /// <summary>
    /// Grabs another character's ragdoll body with a left/right hand and holds it via a
    /// SpringJoint to a kinematic hand anchor. The grabbed body hangs from the hand and is
    /// swung by moving the grabber.
    ///
    /// Authority model: whoever owns the physics (locally = us, later = host) calls
    /// <see cref="Grab"/>/<see cref="Release"/>. Ownership of the target is NOT transferred —
    /// two grabbers on one target simply add two joints (the A -> B &lt;- C case).
    /// </summary>
    [DisallowMultipleComponent]
    public class Grabber : MonoBehaviour
    {
        [Header("Hand anchors (kinematic rigidbodies that follow the hands)")]
        public Rigidbody leftHandBody;
        public Rigidbody rightHandBody;

        [Header("Grab tuning")]
        public float grabRange = 3f;
        public float grabSpring = 800f;
        public float grabDamper = 30f;
        // Finite -> the joint auto-breaks under extreme A<->C tension (one way to resolve
        // the tug-of-war). Infinity -> never breaks, springs just balance.
        public float tensionBreakForce = Mathf.Infinity;

        RagdollController _selfRagdoll;

        class Grip
        {
            public SpringJoint joint;
            public RagdollController target;
            public Rigidbody body;
        }

        Grip _left, _right;

        void Awake() => _selfRagdoll = GetComponent<RagdollController>();

        Grip GetGrip(Hand h) => h == Hand.Left ? _left : _right;
        void SetGrip(Hand h, Grip g) { if (h == Hand.Left) _left = g; else _right = g; }
        Rigidbody HandBody(Hand h) => h == Hand.Left ? leftHandBody : rightHandBody;

        public bool IsGrabbing(Hand h) { var g = GetGrip(h); return g != null && g.joint != null; }
        public RagdollController GetGrabbed(Hand h) { var g = GetGrip(h); return g != null ? g.target : null; }

        /// <summary>Direct grab — authority/tests call this with a resolved target body.</summary>
        public void Grab(Hand hand, RagdollController target, Rigidbody targetBody)
        {
            if (target == null || targetBody == null) return;

            var handBody = HandBody(hand);
            if (handBody == null)
            {
                Debug.LogWarning($"[Grabber] {name}: no hand anchor assigned for {hand}");
                return;
            }

            Release(hand);

            target.SetRagdoll(true);

            var sj = targetBody.gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = handBody;
            sj.autoConfigureConnectedAnchor = false;
            sj.anchor = Vector3.zero;
            sj.connectedAnchor = Vector3.zero;
            sj.spring = grabSpring;
            sj.damper = grabDamper;
            sj.minDistance = 0f;
            sj.maxDistance = 0f;
            sj.enableCollision = false;
            sj.breakForce = tensionBreakForce;
            sj.breakTorque = Mathf.Infinity;

            SetGrip(hand, new Grip { joint = sj, target = target, body = targetBody });
        }

        public void Release(Hand hand)
        {
            var g = GetGrip(hand);
            if (g == null) return;
            if (g.joint != null) Destroy(g.joint);
            SetGrip(hand, null);
        }

        /// <summary>Raycast from an aim ray and grab whatever ragdoll character it hits.</summary>
        public bool TryGrabByAim(Hand hand, Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction.normalized, out var hit, grabRange))
            {
                var rc = hit.collider.GetComponentInParent<RagdollController>();
                if (rc != null && rc != _selfRagdoll)
                {
                    var body = hit.collider.attachedRigidbody != null
                        ? hit.collider.attachedRigidbody
                        : rc.GetNearestBody(hit.point);
                    Grab(hand, rc, body);
                    return true;
                }
            }
            return false;
        }

        void FixedUpdate()
        {
            // A joint destroyed by breakForce leaves a dangling grip -> clear it.
            CleanupIfBroken(Hand.Left);
            CleanupIfBroken(Hand.Right);
        }

        void CleanupIfBroken(Hand h)
        {
            var g = GetGrip(h);
            if (g != null && g.joint == null) SetGrip(h, null);
        }
    }
}
