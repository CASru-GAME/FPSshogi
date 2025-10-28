using App.Main.GameMaster;
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

        // Climb（単純実装）
        [SerializeField] private float climbSpeed = 3f;    // 上方向に与える速度
        [SerializeField] private float climbDetectDistance = 0.5f; // 足元から前方へ飛ばすレイの距離
        private bool climbInput = false;

        private Rigidbody rb;
        private PlayerInput pi;
        private Vector2 moveInput = Vector2.zero;
        private Vector2 lookInput = Vector2.zero;
        private float yaw = 0f;
        private float pitch = 0f;

        public PlayerStatus playerStatus;
        private ISkill currentSkill;
        private IPrimaryAction currentPrimaryAction;
        private ISecondaryAction currentSecondaryAction;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
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
            playerStatus = new PlayerStatus(hpMax: 100, attackPointDefault: 10, moveSpeedDefault: moveSpeed);
        }

        public void SetPlayerStatus(PlayerStatus status)
        {
            playerStatus = status;
        }

        public void SetSkill(ISkill skill)
        {
            currentSkill = skill;
        }

        public void SetPrimaryAction(IPrimaryAction action)
        {
            currentPrimaryAction = action;
        }

        public void SetSecondaryAction(ISecondaryAction action)
        {
            currentSecondaryAction = action;
        }

        public void OnDestroy()
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

                case "Climb":
                    if (context.phase == InputActionPhase.Performed || context.phase == InputActionPhase.Started)
                        climbInput = true;
                    else if (context.phase == InputActionPhase.Canceled)
                        climbInput = false;
                    break;
                case "Skill":
                    if (context.phase == InputActionPhase.Performed && currentSkill != null)
                    {
                        
                        currentSkill.UseSkill(playerStatus);
                    }
                    break;
                case "WeaponActionMain":
                    if (context.phase == InputActionPhase.Performed && currentPrimaryAction != null)
                    {
                        currentPrimaryAction.PrimaryAction(playerStatus);
                    }
                    break;
                case "WeaponActionSub":
                    if (context.phase == InputActionPhase.Performed && currentSecondaryAction != null)
                    {
                        currentSecondaryAction.SecondaryAction(playerStatus);
                    }
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

            // 足元の最も低い座標を取得（Collider があれば bounds.min を使用）
            Vector3 bottom;
            var col = GetComponent<Collider>();
            if (col != null)
                bottom = col.bounds.min;
            else
                bottom = transform.position;

            // レイの発射位置は足元少し上（コライダー内部から出るのを防ぐ）
            Vector3 rayOrigin = bottom + Vector3.up * 0.05f;
            // レイ方向はプレイヤーの前方（カメラの向きの水平方向を使用）
            Vector3 rayDir = forward;
            rayDir.y = 0f;
            rayDir.Normalize();

            bool detect = Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, climbDetectDistance);

            // 検出中かつ Climb アクションが押されている間だけ上方向に速度を与える
            if (detect && climbInput)
            {
                // 現在の水平成分は保持して Y を上げる
                Vector3 v = new Vector3(rb.linearVelocity.x, climbSpeed, rb.linearVelocity.z);
                rb.linearVelocity = v;
            }
            else
            {
                // 通常移動：重力を有効にして Y 成分は現状を維持
                Vector3 v = targetVel;
                v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;
            }
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