using App.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Main.Player
{
    public class Player : MonoBehaviour
    {
        public const float DefaultLookSensitivity = 2f;
        public const float InitialZoomFraction = .4f;
        public const float MinLookSensitivity = .5f;
        public const float MinZoomSensitivity = .1f;
        public const float MaxLookSensitivity = 5f;
        [SerializeField] private Transform cameraTransform; // InspectorでMainCameraを割り当てる
        [SerializeField] private float lookSensitivity = DefaultLookSensitivity;
        private float gamepadZoomSensitivity = DefaultLookSensitivity * InitialZoomFraction;
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
        private InputDevice lookDevice;
        private float yaw = 0f;
        private float pitch = 0f;
        private float normalFieldOfView = 60f;

        public PlayerStatus playerStatus;
        public float PrimaryCooldown => currentPrimaryAction?.CooldownRemaining ?? 0f;
        public float SecondaryCooldown => currentSecondaryAction?.CooldownRemaining ?? 0f;
        public float SkillCooldown => Mathf.Max(0f, currentSkill?.cooldownTimer ?? 0f);
        public float PrimaryCooldownDuration => currentPrimaryAction?.CooldownDuration ?? 0f;
        public float SecondaryCooldownDuration => currentSecondaryAction?.CooldownDuration ?? 0f;
        public float SkillCooldownDuration => currentSkill?.cooldownTime ?? 0f;
        public bool ClimbAvailable {
            get { var facing=Quaternion.Euler(0,yaw,0);return !movementOverrideActive&&!movementYOverrideActive&&CanClimb(facing*new Vector3(moveInput.x,0,moveInput.y),facing*Vector3.forward); }
        }
        public string SecondaryName => currentSecondaryAction?.secondaryActionName == "SecondaryZoom" ? "ズーム" : "防御";
        public App.Main.ShogiThings.PieceType PieceType { get; set; }
        public int BodyLayer { get; set; }
        public float LookSensitivity { get => lookSensitivity; set => lookSensitivity = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity); }
        public float GamepadZoomSensitivity { get => gamepadZoomSensitivity; set => gamepadZoomSensitivity = Mathf.Clamp(value, MinZoomSensitivity, MaxLookSensitivity); }
        public bool IsClimbing { get; private set; }
        public void ResetInput() { moveInput = lookInput = Vector2.zero; climbInput = IsClimbing = false; }
        public void ResetDuelPresentation()
        {
            if (WeaponObject != null) WeaponObject.GetComponent<WeaponPresentation>()?.ResetMotion();
            ResetInput(); ClearMovementOverride(); StopAllCoroutines();
            GetComponent<PlayerFoley>()?.ResetTracking();
            playerStatus?.EffectList.ClearEffects();
            playerStatus?.DefensePoint.Reset();
            CombatVisuals.Clear(this);
            GameAudio.StopCombat(this);
            yaw=transform.eulerAngles.y; pitch=0;
            if(cameraTransform!=null)
            {
                cameraTransform.localRotation=Quaternion.identity;
                var camera=cameraTransform.GetComponent<Camera>();
                if(camera!=null)camera.fieldOfView=normalFieldOfView;
            }
            currentSkill=null;currentPrimaryAction=null;currentSecondaryAction=null;
            SubWeaponObject=null;ObjectForKingSkill=null;
            if(rb!=null)rb.linearVelocity=Vector3.zero;
        }
        private ISkill currentSkill;
        private IPrimaryAction currentPrimaryAction;
        private ISecondaryAction currentSecondaryAction;
        private bool movementOverrideActive = false;
        private Vector3 movementOverrideVelocity = Vector3.zero;
        private float movementOverrideRemaining = 0f;
        private bool movementOverridePreserveY = true;

        // Y-only 上書き（滞空中に水平入力を許可するため）
        private bool movementYOverrideActive = false;
        private float movementYOverrideVelocity = 0f;
        private float movementYOverrideRemaining = 0f;

        public GameObject WeaponObject { get; private set; }
        public GameObject SubWeaponObject { get; private set; }
        public GameObject ObjectForKingSkill { get; set; }

        public void Initialize()
        {
            if(GetComponent<PlayerFoley>()==null)gameObject.AddComponent<PlayerFoley>();
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
                var camera=cameraTransform.GetComponent<Camera>();
                if(camera!=null)normalFieldOfView=camera.fieldOfView;
                pitch = cameraTransform.localEulerAngles.x;
                if (pitch > 180f) pitch -= 360f;
            }
            playerStatus = new PlayerStatus(hpMax: 100, attackPointDefault: 10, moveSpeedDefault: 5f, this);
        }

        public void SetObjectForKingSkill(GameObject obj)
        {
            ObjectForKingSkill = obj;
        }

        public void SetWeaponObject(GameObject weapon)
        {
            if(WeaponObject!=null)Destroy(WeaponObject);
            WeaponObject = weapon;
            if (WeaponObject != null)
            {
                //インスタンス化する
                WeaponObject = Instantiate(WeaponObject);
                WeaponObject.transform.SetParent(this.transform);
                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;

                var presentation=WeaponObject.GetComponent<WeaponPresentation>();
                if(presentation!=null)
                {
                    WeaponObject.transform.localScale=Vector3.one;
                    presentation.Bind(this);
                    WeaponObject.GetComponent<PlayerWeapon>()?.SetOwner(this);
                    return;
                }

                // ローカルで90度回転させる
                WeaponObject.transform.Rotate(0, 90, 0);
                // ローカルでplayerの少し前に出させる
                WeaponObject.transform.localPosition = new Vector3(1.0f, -0.5f, 3.5f);

                //サイズを大きくする
                WeaponObject.transform.localScale = new Vector3(2f, 2f, 2f);
            }
        }

        public void SetSubWeaponObject(GameObject subWeapon)
        {
            SubWeaponObject = subWeapon;
        }

        public void SetPlayerStatus(PlayerStatus status)
        {
            playerStatus?.EffectList.ClearEffects();
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
            playerStatus?.EffectList.ClearEffects();
            if (pi != null)
                pi.onActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (App.UI.GameSessionUI.BlocksGameplay) { ResetInput(); return; }
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
                        lookDevice = context.control?.device;
                        lookInput = context.ReadValue<Vector2>();
                        if (lookDevice is Gamepad) lookInput *= gamepadLookMultiplier;
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
                        currentPrimaryAction.PrimaryAction(this, playerStatus);
                    }
                    break;
                case "WeaponActionSub":
                    if (context.phase == InputActionPhase.Performed && currentSecondaryAction != null)
                    {
                        currentSecondaryAction.SecondaryAction(this, playerStatus);
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

            // 優先度: Y-only 上書き > フル上書き > 通常移動
            if (movementYOverrideActive)
            {
                // 水平方向は通常移動（入力）で決め、Yだけ上書きする
                Vector3 v = targetVel;
                v.y = movementYOverrideVelocity;
                rb.linearVelocity = v;

                // タイマー減算
                movementYOverrideRemaining -= Time.fixedDeltaTime;
                if (movementYOverrideRemaining <= 0f)
                {
                    movementYOverrideActive = false;
                }
            }
            else if (movementOverrideActive)
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

            IsClimbing = climbInput && !movementOverrideActive && !movementYOverrideActive && CanClimb(desired, forward);
            if (IsClimbing)
            {
                var velocity=rb.linearVelocity;velocity.y=climbSpeed;rb.linearVelocity=velocity;
            }

            currentSkill?.UpdateSkill();
            currentSecondaryAction?.UpdateSecondaryAction();
        }

        private bool CanClimb(Vector3 movement, Vector3 forward)
        {
            var body=GetComponent<Collider>();if(body==null)return false;
            var bounds=body.bounds;
            var direction=movement.sqrMagnitude>.01f?movement.normalized:forward;
            if(direction.sqrMagnitude<.01f)return false;
            var origin=bounds.center;origin.y=bounds.min.y+Mathf.Min(.2f,bounds.extents.y*.25f);
            float reach=Mathf.Max(bounds.extents.x,bounds.extents.z)+climbDetectDistance;
            return ShotPhysics.Raycast(new Ray(origin,direction),reach,~0,this,out var hit) &&
                Mathf.Abs(hit.normal.y)<.6f && hit.collider.GetComponentInParent<Player>()==null;
        }

        public void SetMovementOverride(Vector3 velocity, float durationSeconds, bool preserveY = true)
        {
            movementOverrideVelocity = velocity;
            movementOverrideRemaining = Mathf.Max(0f, durationSeconds);
            movementOverridePreserveY = preserveY;
            movementOverrideActive = movementOverrideRemaining > 0f;
        }

        // Yのみ上書きを設定（滞空中に水平操作を許可したい場合に使う）
        public void SetMovementYOverride(float yVelocity, float durationSeconds)
        {
            movementYOverrideVelocity = yVelocity;
            movementYOverrideRemaining = Mathf.Max(0f, durationSeconds);
            movementYOverrideActive = movementYOverrideRemaining > 0f;
        }

        public void ClearMovementOverride()
        {
            movementOverrideActive = false;
            movementOverrideRemaining = 0f;
            movementYOverrideActive = false;
            movementYOverrideRemaining = 0f;
        }

        void Update()
        {
            if(playerStatus==null)return;
            playerStatus.EffectList.UpdateEffects();
            // Mouse input is displacement per frame; a stick expresses a turn rate.
            // .02 retains approximately the old mouse feel at 50 Hz without FPS dependence.
            bool mouseLook = lookDevice is Mouse;
            float lookStep=mouseLook ? .02f : Time.deltaTime;
            if(Time.timeScale<=0)lookStep=0;
            bool zoomedStick = lookDevice is Gamepad && currentSecondaryAction is SecondaryZoom { IsZoomed: true };
            float sensitivity = zoomedStick ? gamepadZoomSensitivity : lookSensitivity;
            float deltaX = lookInput.x * sensitivity * lookStep;
            float deltaY = lookInput.y * sensitivity * lookStep;
            if(mouseLook)lookInput=Vector2.zero;

            yaw += deltaX;
            pitch -= deltaY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
