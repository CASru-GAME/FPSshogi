using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Main.Player
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Transform cameraTransform; // InspectorでMainCameraを割り当てる
        [SerializeField] private float lookSensitivity = 2.0f;
        [SerializeField] private float gamepadLookMultiplier = 150f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private Rigidbody rb;
        private PlayerInput pi;
        private Vector2 moveInput = Vector2.zero;
        private Vector2 lookInput = Vector2.zero;
        private float yaw = 0f;
        private float pitch = 0f;

        void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            pi = GetComponent<PlayerInput>();
            if (pi != null)
                pi.onActionTriggered += OnActionTriggered;

            yaw = transform.eulerAngles.y;
            if (cameraTransform != null)
            {
                pitch = cameraTransform.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
        }

        private void OnDestroy()
        {
            if (pi != null)
                pi.onActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (context.action == null) return;

            switch (context.action.name)
            {
                case "Move":
                    if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started)
                        moveInput = context.ReadValue<Vector2>();
                    else if (context.phase == InputActionPhase.Canceled)
                        moveInput = Vector2.zero;
                    break;

                case "Look":
                    if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started)
                    {
                        var device = context.control?.device;
                        bool isGamepad = device is Gamepad;
                        var raw = context.ReadValue<Vector2>();
                        lookInput = isGamepad ? raw * gamepadLookMultiplier : raw;
                    }
                    else if (context.phase == InputActionPhase.Canceled)
                        lookInput = Vector2.zero;
                    break;
            }
        }

        void FixedUpdate()
        {
            if (rb == null) return;

            Vector3 forward = (cameraTransform != null) ? cameraTransform.forward : transform.forward;
            Vector3 right = (cameraTransform != null) ? cameraTransform.right : transform.right;

            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector3 desired = right * moveInput.x + forward * moveInput.y;
            Vector3 targetVel = desired * moveSpeed;
            targetVel.y = rb.linearVelocity.y; // 重力を保持

            rb.linearVelocity = targetVel;
        }

        void Update()
        {
            float deltaX = lookInput.x * lookSensitivity * Time.deltaTime;
            float deltaY = lookInput.y * lookSensitivity * Time.deltaTime;

            yaw += deltaX;
            pitch -= deltaY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}