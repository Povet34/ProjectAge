using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAge
{
    /// <summary>
    /// Minimal self-contained third-person orbit camera (no Cinemachine dependency, keeps the
    /// prototype lean). Mouse orbits, follows the target at a fixed distance/height.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5f;
        public float height = 1.6f;
        public float sensitivity = 0.12f;
        public float minPitch = -15f;
        public float maxPitch = 70f;

        float _yaw;
        float _pitch = 15f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (target != null) _yaw = target.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (target == null) return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue();
                _yaw += d.x * sensitivity;
                _pitch = Mathf.Clamp(_pitch - d.y * sensitivity, minPitch, maxPitch);
            }

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            transform.position = focus - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
    }
}
