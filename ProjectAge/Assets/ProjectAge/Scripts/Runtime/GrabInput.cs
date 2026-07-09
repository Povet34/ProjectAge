using UnityEngine;

namespace ProjectAge
{
    public enum Hand
    {
        Left,
        Right
    }

    /// <summary>
    /// Per-tick input snapshot. This struct is the boundary between input polling and
    /// physics simulation: no Input.* calls happen inside the physics/grab logic — the
    /// consumer is fed one of these each FixedUpdate/tick instead.
    ///
    /// Network note: when we move to FishNet this becomes the [Replicate] payload almost
    /// verbatim, so keep it a plain, serializable value type.
    /// </summary>
    [System.Serializable]
    public struct GrabInput
    {
        public bool GrabLeftHeld;
        public bool GrabRightHeld;
        public bool FreezeToggle;

        public Vector3 AimOrigin;
        public Vector3 AimDirection;
        public Vector2 SwingDelta;

        public static GrabInput Empty => new GrabInput
        {
            AimDirection = Vector3.forward
        };
    }
}
