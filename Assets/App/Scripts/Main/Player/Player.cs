using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Main.Player
{
    public class Player : MonoBehaviour
    {
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
        private bool movementOverrideActive = false;
        private Vector3 movementOverrideVelocity = Vector3.zero;
        private float movementOverrideRemaining = 0f;
        private bool movementOverridePreserveY = true;

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
            playerStatus = new PlayerStatus(hpMax: 100, attackPointDefault: 10, moveSpeedDefault: 5f, this);
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
                        currentSkill.UseSkill(this, playerStatus);
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

            // カメラ基準の前右ベクトル等（既存コード）
            Vector3 forward = (cameraTransform != null) ? cameraTransform.forward : transform.forward;
            Vector3 right = (cameraTransform != null) ? cameraTransform.right : transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            Vector3 desired = right * moveInput.x + forward * moveInput.y;
            Vector3 targetVel = desired * playerStatus.MoveSpeed.Current;

            // ここで移動上書きがあるかチェックする
            if (movementOverrideActive)
            {
                // 上書き速度適用（必要に応じて Y 成分を保持）
                Vector3 v = movementOverrideVelocity;
                if (movementOverridePreserveY)
                    v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;

                // タイマー減算（FixedUpdate のため fixedDeltaTime を使う）
                movementOverrideRemaining -= Time.fixedDeltaTime;
                if (movementOverrideRemaining <= 0f)
                {
                    ClearMovementOverride();
                }
            }
            else
            {
                // 既存の通常移動処理（Y は現状維持）
                Vector3 v = targetVel;
                v.y = rb.linearVelocity.y;
                rb.linearVelocity = v;
            }

            currentSkill?.UpdateSkill();
        }

        public void SetMovementOverride(Vector3 velocity, float durationSeconds, bool preserveY = true)
        {
            movementOverrideVelocity = velocity;
            movementOverrideRemaining = Mathf.Max(0f, durationSeconds);
            movementOverridePreserveY = preserveY;
            movementOverrideActive = movementOverrideRemaining > 0f;
        }

        public void ClearMovementOverride()
        {
            movementOverrideActive = false;
            movementOverrideRemaining = 0f;
        }

        void Update()
        {
            playerStatus.EffectList.UpdateEffects();
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
