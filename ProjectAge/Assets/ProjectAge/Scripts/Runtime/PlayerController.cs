using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAge
{
    /// <summary>
    /// Local third-person controller: camera-relative movement (kinematic Rigidbody) plus
    /// left/right mouse to grab/release with each hand.
    ///
    /// Network-ready shape: input is polled in Update and gathered into a <see cref="GrabInput"/>,
    /// but all physics-affecting work (movement, grab joint creation) happens in FixedUpdate.
    /// Swapping local polling for a networked input source later touches only Update.
    /// </summary>
    [RequireComponent(typeof(Grabber))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 4f;
        public float turnSpeed = 12f;
        public Transform cameraTransform;

        Grabber _grabber;
        Rigidbody _body;

        GrabInput _input = GrabInput.Empty;
        Vector2 _move;
        bool _leftDown, _leftUp, _rightDown, _rightUp;

        void Awake()
        {
            _grabber = GetComponent<Grabber>();
            _body = GetComponent<Rigidbody>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            var kb = Keyboard.current;
            _move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) _move.y += 1f;
                if (kb.sKey.isPressed) _move.y -= 1f;
                if (kb.dKey.isPressed) _move.x += 1f;
                if (kb.aKey.isPressed) _move.x -= 1f;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) _leftDown = true;
                if (mouse.leftButton.wasReleasedThisFrame) _leftUp = true;
                if (mouse.rightButton.wasPressedThisFrame) _rightDown = true;
                if (mouse.rightButton.wasReleasedThisFrame) _rightUp = true;
            }

            if (cameraTransform != null)
            {
                _input.AimOrigin = cameraTransform.position;
                _input.AimDirection = cameraTransform.forward;
            }
            _input.GrabLeftHeld = _grabber.IsGrabbing(Hand.Left);
            _input.GrabRightHeld = _grabber.IsGrabbing(Hand.Right);
        }

        void FixedUpdate()
        {
            MoveStep();
            GrabStep();
        }

        void MoveStep()
        {
            Vector3 fwd = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            Vector3 right = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized
                : Vector3.right;

            Vector3 dir = fwd * _move.y + right * _move.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 delta = dir * moveSpeed * Time.fixedDeltaTime;

            if (_body != null && _body.isKinematic)
            {
                _body.MovePosition(_body.position + delta);
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                    _body.MoveRotation(Quaternion.Slerp(_body.rotation, look, turnSpeed * Time.fixedDeltaTime));
                }
            }
            else
            {
                transform.position += delta;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir, Vector3.up), turnSpeed * Time.fixedDeltaTime);
            }
        }

        void GrabStep()
        {
            if (_leftDown) { _grabber.TryGrabByAim(Hand.Left, _input.AimOrigin, _input.AimDirection); _leftDown = false; }
            if (_leftUp) { _grabber.Release(Hand.Left); _leftUp = false; }
            if (_rightDown) { _grabber.TryGrabByAim(Hand.Right, _input.AimOrigin, _input.AimDirection); _rightDown = false; }
            if (_rightUp) { _grabber.Release(Hand.Right); _rightUp = false; }
        }
    }
}
