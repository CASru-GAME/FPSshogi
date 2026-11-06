using UnityEngine;

namespace App.Main.Player
{
    /// <summary>Weapon-specific grip poses, driven by the same clock as attacks and skills.</summary>
    public sealed class WeaponPresentation : MonoBehaviour
    {
        public enum Motion { Thrust, Slash, Gun, Bow, Knife, Command }
        public enum SkillMotion { None, Leap, Throw, Dash, Ascend, Field, Command }
        public Motion motion;
        public Transform visual;
        public Vector3 holdPosition = new(.55f, -.45f, 1.45f);
        public Vector3 holdEuler;
        public float attackDuration = .65f;
        public float hitStart = .12f;
        public float hitEnd = .38f;
        public Vector3 muzzleLocal = new(.5f, -.35f, 2.5f);
        public Transform muzzle;
        public LineRenderer bowString;
        public Transform nockedArrow;
        Player owner;
        Camera view;
        Rigidbody body;
        float attackStarted = -100f;
        float guardStarted = -100f, guardUntil = -100f;
        float aimStarted = -100f, aimFrom;
        bool aiming;
        SkillMotion skill;
        float skillStarted = -100f, skillDuration, skillActionTime;
        BoxCollider hitbox;
        Bounds meshBounds;
        Vector3 gripLocal, arrowRest;
        Vector3 attackFromGrip;
        Quaternion attackFromRotation;
        public bool IsAttacking => Time.time >= attackStarted && Time.time - attackStarted < attackDuration;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.TransformPoint(muzzleLocal);
        public Vector3 GripLocal => gripLocal;
        float AimWeight => Mathf.Lerp(aimFrom, aiming ? 1 : 0, Ease((Time.time - aimStarted) / .16f));
        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        static float Between(float t, float a, float b) => Ease((t - a) / Mathf.Max(.001f, b - a));
        static float Envelope(float t, float duration, float enter = .10f, float exit = .18f)
            => t < 0 || t >= duration ? 0 : Mathf.Min(Ease(t / enter), Ease((duration - t) / exit));

        void Awake()
        {
            hitbox = GetComponent<BoxCollider>();
            if (visual != null && visual.TryGetComponent<MeshFilter>(out var filter)) meshBounds = filter.sharedMesh.bounds;
            gripLocal = motion switch {
                Motion.Thrust or Motion.Slash or Motion.Knife => Vector3.up * meshBounds.min.y * .65f,
                Motion.Gun => Vector3.forward * meshBounds.min.z * .65f,
                Motion.Command => Vector3.down * .4f,
                _ => Vector3.zero
            };
            if (nockedArrow != null) arrowRest = nockedArrow.localPosition;
        }
        public void Bind(Player p) { owner = p; view = p.GetComponentInChildren<Camera>(true); body = p.GetComponent<Rigidbody>(); RefreshPose(); }
        public float Attack()
        {
            if (visual != null)
            {
                attackFromRotation = visual.localRotation;
                attackFromGrip = visual.localPosition + attackFromRotation * gripLocal;
            }
            attackStarted = Time.time;
            return attackDuration;
        }
        public void Guard(float seconds)
        {
            if (Time.time >= guardUntil) guardStarted = Time.time;
            guardUntil = Mathf.Max(guardUntil, Time.time + seconds);
        }
        public void SetAiming(bool value)
        {
            if (value == aiming) return;
            aimFrom = AimWeight; aimStarted = Time.time; aiming = value;
        }
        public void PlaySkill(SkillMotion value, float duration, float actionTime = 0)
        {
            skill = value; skillStarted = Time.time; skillDuration = Mathf.Max(.01f, duration); skillActionTime = actionTime;
        }
        public void ResetMotion()
        {
            attackStarted = guardStarted = guardUntil = skillStarted = aimStarted = -100f;
            skill = SkillMotion.None; aiming = false; aimFrom = 0;
        }
        void OnDisable() => ResetMotion();
        void FixedUpdate() => RefreshPose();
        void LateUpdate() => RefreshPose();

        public void RefreshPose()
        {
            if (view != null) transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
            if (visual == null) return;
            Quaternion rest = Quaternion.Euler(holdEuler), rotation = rest;
            Vector3 grip = holdPosition + rest * gripLocal;
            float aim = AimWeight;
            if (motion == Motion.Gun || motion == Motion.Bow)
            {
                grip += new Vector3(-.18f, .07f, -.10f) * aim;
                rotation = Quaternion.Slerp(rotation, Quaternion.Euler(motion == Motion.Gun ? new Vector3(-1, 0, 0) : new Vector3(0, -3, -4)), aim);
            }
            Vector3 attackBaseGrip = grip;
            Quaternion attackBaseRotation = rotation;
            float guard = Mathf.Min(Ease((Time.time - guardStarted) / .09f), Ease((guardUntil - Time.time) / .14f));
            if (guard > 0)
            {
                var guardRotation = Quaternion.Euler(35, -12, 65);
                // Keep the blade across the view. Rotating a long shaft around its
                // low resting grip without raising that grip put it below the screen.
                var guardGrip = new Vector3(0, -.12f, 1.5f) + guardRotation * gripLocal;
                grip = Vector3.Lerp(grip, guardGrip, guard);
                rotation = Quaternion.Slerp(rotation, guardRotation, guard);
            }
            else if (!IsAttacking)
            {
                float speed = body != null ? new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude : 0;
                grip.y += Mathf.Sin(Time.time * (speed > .2f ? 9 : 2)) * (speed > .2f ? .018f : .004f);
            }
            ApplySkill(ref grip, ref rotation);
            if (IsAttacking)
            {
                Vector3 returnGrip = grip;
                Quaternion returnRotation = rotation;
                grip = attackBaseGrip; rotation = attackBaseRotation;
                float age = Time.time - attackStarted;
                ApplyAttack(age, ref grip, ref rotation, rest);
                // Enter from the visible pose, but keep the contact phase on the normal
                // blade path. Recover to a guard/skill only while it is still active.
                float enter = Between(age, 0, motion == Motion.Gun ? .035f : motion == Motion.Bow ? .025f : hitStart);
                grip += (attackFromGrip - attackBaseGrip) * (1 - enter);
                rotation = Quaternion.Slerp(attackFromRotation * Quaternion.Inverse(attackBaseRotation), Quaternion.identity, enter) * rotation;
                float recover = Between(age, hitEnd, attackDuration);
                grip = Vector3.Lerp(grip, returnGrip, recover);
                rotation = Quaternion.Slerp(rotation, returnRotation, recover);
            }
            visual.localPosition = grip - rotation * gripLocal;
            visual.localRotation = rotation;
            UpdateBow();
            if (hitbox != null)
            {
                var matrix = transform.worldToLocalMatrix * visual.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(x, y, z))));
                hitbox.center = bounds.center; hitbox.size = bounds.size + Vector3.one * .12f;
            }
        }
        void ApplyAttack(float age, ref Vector3 grip, ref Quaternion rotation, Quaternion rest)
        {
            float wind = Between(age, 0, hitStart);
            float strike = Between(age, hitStart, Mathf.Lerp(hitStart, hitEnd, .6f));
            float recover = Between(age, hitEnd, attackDuration);
            switch (motion)
            {
                case Motion.Thrust:
                    // The entire shaft translates on its own axis. Tilting it during the
                    // stroke used to lift the spearhead away from the aimed target.
                    rotation = rest;
                    grip += rest * Vector3.up * Mathf.Lerp(Mathf.Lerp(-.20f * wind, .62f, strike), 0, recover);
                    break;
                case Motion.Slash:
                    bool pole = owner != null && (owner.PieceType == App.Main.ShogiThings.PieceType.Gin || owner.PieceType == App.Main.ShogiThings.PieceType.Kin);
                    var raised = Quaternion.Euler(pole ? new Vector3(63, -32, -58) : new Vector3(43, -22, -48));
                    var cut = Quaternion.Euler(pole ? new Vector3(52, 15, 78) : new Vector3(48, 15, 72));
                    rotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(rest, raised, wind), cut, strike), rest, recover);
                    // Raise the hand into the cut: keeping the low idle grip made
                    // long blades disappear below the view through the contact phase.
                    var idleGrip = grip;
                    grip = Vector3.Lerp(Vector3.Lerp(Vector3.Lerp(idleGrip, new Vector3(.55f, -.45f, 1.35f), wind), new Vector3(.55f, -.28f, 1.65f), strike), idleGrip, recover);
                    break;
                case Motion.Knife:
                    var point = Quaternion.Euler(82, -10, -3);
                    rotation = Quaternion.Slerp(Quaternion.Slerp(rest, point, wind), rest, recover);
                    grip += (point * Vector3.up) * Mathf.Lerp(Mathf.Lerp(-.12f * wind, .52f, strike), 0, recover);
                    break;
                case Motion.Gun:
                    float kick = age < .035f ? Between(age, 0, .035f) : 1 - Between(age, .035f, attackDuration);
                    float distance = meshBounds.size.z > 2.2f ? .28f : .19f;
                    grip.z -= distance * kick;
                    rotation *= Quaternion.Euler(-7 * kick, 0, 0);
                    break;
                case Motion.Bow:
                    float release = Envelope(age, .28f, .025f, .20f);
                    grip.z -= .09f * release; rotation *= Quaternion.Euler(0, 3 * release, -2 * release);
                    break;
                case Motion.Command:
                    float lift = Between(age, 0, .18f), pointOut = Between(age, .18f, .4f), back = Between(age, .5f, attackDuration);
                    rotation = Quaternion.Slerp(Quaternion.Slerp(rest, Quaternion.Euler(10, -10, -30), lift), Quaternion.Euler(55, -10, -10), pointOut);
                    rotation = Quaternion.Slerp(rotation, rest, back);
                    grip += Vector3.Lerp(Vector3.Lerp(new Vector3(0, .25f, 0) * lift, new Vector3(.08f, .08f, .25f), pointOut), Vector3.zero, back);
                    break;
            }
        }
        void ApplySkill(ref Vector3 grip, ref Quaternion rotation)
        {
            float t = Time.time - skillStarted, weight = Envelope(t, skillDuration);
            if (weight <= 0) return;
            switch (skill)
            {
                case SkillMotion.Leap:
                    grip += new Vector3(0, -.10f, -.18f) * weight;
                    rotation = Quaternion.Slerp(rotation, Quaternion.Euler(Mathf.Lerp(72, 92, Between(t, skillDuration * .35f, skillDuration * .65f)), -12, -3), weight);
                    break;
                case SkillMotion.Throw:
                    grip += new Vector3(.05f, -.4f, -.12f) * weight;
                    rotation *= Quaternion.Euler(22 * weight, 0, 8 * weight);
                    break;
                case SkillMotion.Dash:
                    float pull = Between(t, 0, skillActionTime * .4f), sweep = Between(t, skillActionTime * .4f, skillActionTime);
                    float back = Between(t, skillActionTime + .09f, skillDuration);
                    var rest = Quaternion.Euler(holdEuler);
                    var dashRotation = Quaternion.Slerp(Quaternion.Slerp(Quaternion.Slerp(rest, Quaternion.Euler(40, -25, -60), pull), Quaternion.Euler(52, 15, 80), sweep), rest, back);
                    rotation = Quaternion.Slerp(rotation, dashRotation, weight);
                    var dashGrip = Vector3.Lerp(new Vector3(.55f, -.4f, 1.35f), new Vector3(.55f, -.28f, 1.65f), sweep);
                    grip = Vector3.Lerp(grip, dashGrip, weight * (1 - back));
                    break;
                case SkillMotion.Ascend:
                    grip += new Vector3(.03f, -.24f, -.17f) * weight;
                    rotation *= Quaternion.Euler(8 * weight, 0, 8 * weight);
                    break;
                case SkillMotion.Field:
                    grip += new Vector3(-.12f, -.12f, .25f) * weight;
                    rotation *= Quaternion.Euler(13 * weight, 0, 12 * weight);
                    break;
                case SkillMotion.Command:
                    grip += new Vector3(.06f, -.6f, -.15f) * weight;
                    rotation *= Quaternion.Euler(28 * weight, 0, -15 * weight);
                    break;
            }
        }
        void UpdateBow()
        {
            if (bowString == null) return;
            float age = Time.time - attackStarted;
            float stringZ = -.3f;
            if (IsAttacking)
            {
                stringZ = -.3f * (1 - Between(age, 0, .04f));
                stringZ -= .3f * Between(age, .36f, attackDuration);
            }
            bowString.SetPosition(1, new Vector3(-.08f, 0, stringZ));
            if (nockedArrow != null)
            {
                nockedArrow.gameObject.SetActive(!IsAttacking || age >= .36f);
                nockedArrow.localPosition = arrowRest + Vector3.forward * (stringZ + .3f);
            }
        }
    }
}
