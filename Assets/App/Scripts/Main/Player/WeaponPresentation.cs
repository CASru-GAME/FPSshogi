using App.Audio;
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
        bool nockPending;
        BowRig bowRig;
        float releasedDraw;
        float guardStarted = -100f, guardUntil = -100f;
        float aimStarted = -100f, aimFrom;
        bool aiming;
        SkillMotion skill;
        float skillStarted = -100f, skillDuration, skillActionTime;
        BoxCollider hitbox;
        Bounds meshBounds;
        Vector3 gripLocal;
        Pose meleeRest, meleeWindup, meleeContact, meleeFinish;
        Vector3 attackFromGrip;
        Quaternion attackFromRotation;
        public bool IsAttacking => Time.time >= attackStarted && Time.time - attackStarted < attackDuration;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.TransformPoint(muzzleLocal);
        public Vector3 GripLocal => gripLocal;
        bool IsMelee => motion is Motion.Thrust or Motion.Slash or Motion.Knife;
        Vector3 ScaledGrip => Vector3.Scale(gripLocal, visual.localScale);
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
            CacheMeleePoses();
            if (bowString != null)
            {
                bowRig = new BowRig(visual, bowString, nockedArrow);
                bowRig.SetPose(0f, true);
            }
        }
        public void Bind(Player p)
        {
            owner = p;
            view = p.GetComponentInChildren<Camera>(true);
            body = p.GetComponent<Rigidbody>();
            CacheMeleePoses();
            RefreshPose();
        }

        // Positions are hand positions in the view frame. The same grip pivot drives
        // the rendered mesh, its trigger and the contact box sampled by combat.
        void CacheMeleePoses()
        {
            if (!IsMelee || visual == null) return;
            var rest = Quaternion.Euler(holdEuler);
            Vector3 grip = holdPosition + rest * ScaledGrip;
            meleeRest = new Pose(grip, rest);
            switch (motion)
            {
                case Motion.Thrust:
                    meleeWindup = new Pose(grip + new Vector3(.03f, .08f, -.62f), Quaternion.Euler(holdEuler + new Vector3(-4, 3, -3)));
                    meleeContact = new Pose(grip + rest * Vector3.up * 1.08f, rest);
                    meleeFinish = new Pose(meleeContact.position + new Vector3(-.035f, .028f, -.26f), rest);
                    break;
                case Motion.Slash:
                    bool pole = owner != null && (owner.PieceType == App.Main.ShogiThings.PieceType.Gin || owner.PieceType == App.Main.ShogiThings.PieceType.Kin);
                    meleeWindup = pole
                        ? new Pose(new Vector3(.27f, .07f, 1.30f), Quaternion.Euler(54, -24, -49))
                        : new Pose(new Vector3(.24f, .02f, 1.26f), Quaternion.Euler(40, -19, -44));
                    meleeContact = pole
                        ? new Pose(new Vector3(.27f, -.55f, 1.77f), Quaternion.Euler(75, 13, 23))
                        : new Pose(new Vector3(.22f, -.59f, 1.47f), Quaternion.Euler(70, 10, 22));
                    meleeFinish = pole
                        ? new Pose(new Vector3(-.13f, -.58f, 1.88f), Quaternion.Euler(79, 34, 75))
                        : new Pose(new Vector3(-.16f, -.56f, 1.71f), Quaternion.Euler(80, 33, 72));
                    break;
                case Motion.Knife:
                    meleeWindup = new Pose(grip + new Vector3(.09f, .25f, -.36f), Quaternion.Euler(40, -20, -26));
                    meleeContact = new Pose(grip + new Vector3(-.105f, .16f, .83f), Quaternion.Euler(80, -9, 6));
                    meleeFinish = new Pose(meleeContact.position + new Vector3(-.06f, -.025f, -.21f), Quaternion.Euler(77, -10, 11));
                    break;
            }
        }

        static Pose Blend(Pose from, Pose to, float weight) => new Pose(
            Vector3.Lerp(from.position, to.position, weight), Quaternion.Slerp(from.rotation, to.rotation, weight));

        Pose MeleePoseAt(float age)
        {
            if (age < hitStart) return Blend(meleeRest, meleeWindup, Between(age, 0, hitStart));
            float impact = Mathf.Lerp(hitStart, hitEnd, .6f);
            var stroke = Blend(meleeWindup, meleeContact, Between(age, hitStart, impact));
            // Start carrying a slash through while it is still crossing the aim.
            float followStart = motion == Motion.Slash ? Mathf.Lerp(hitStart, hitEnd, .44f) : impact;
            return Blend(stroke, meleeFinish, Between(age, followStart, hitEnd));
        }

        /// <summary>The oriented contact box in weapon-root space at a time within the attack's hit window.</summary>
        public void GetMeleeBox(float age, out Vector3 localCenter, out Vector3 localHalfExtents, out Quaternion localRotation)
        {
            var pose = MeleePoseAt(Mathf.Clamp(age, hitStart, hitEnd));
            localRotation = pose.rotation;
            ModelBox(pose, out localCenter, out localHalfExtents);
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        void ModelBox(Pose grip, out Vector3 center, out Vector3 halfExtents)
        {
            Vector3 scale = visual.localScale;
            center = grip.position + grip.rotation * Vector3.Scale(meshBounds.center - gripLocal, scale);
            halfExtents = Vector3.Scale(meshBounds.extents, Abs(scale)) + Vector3.one * .06f;
        }
        public float Attack()
        {
            if (visual != null)
            {
                attackFromRotation = visual.localRotation;
                attackFromGrip = visual.localPosition + attackFromRotation * ScaledGrip;
            }
            attackStarted = Time.time;
            nockPending=bowString!=null;
            releasedDraw = owner != null ? owner.BowDrawAmount : 0f;
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
            skill = SkillMotion.None; aiming = false; aimFrom = 0; nockPending=false;
            releasedDraw = 0f;
            bowRig?.SetPose(0f, true);
        }
        void OnDisable() => ResetMotion();
        void OnDestroy() => bowRig?.Dispose();
        void FixedUpdate() => RefreshPose();
        void LateUpdate() => RefreshPose();

        public void RefreshPose()
        {
            if (view != null) transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
            if (visual == null) return;
            Quaternion rest = Quaternion.Euler(holdEuler), rotation = rest;
            Vector3 grip = holdPosition + rest * ScaledGrip;
            float aim = AimWeight;
            if (motion == Motion.Gun || motion == Motion.Bow)
            {
                grip += new Vector3(-.18f, .07f, -.10f) * aim;
                rotation = Quaternion.Slerp(rotation, Quaternion.Euler(motion == Motion.Gun ? new Vector3(-1, 0, 0) : new Vector3(0, -3, -4)), aim);
            }
            float draw = owner != null ? owner.BowDrawAmount : 0f;
            if (draw > 0f && motion == Motion.Bow)
            {
                grip += new Vector3(-.13f, .08f, .09f) * draw;
                rotation = Quaternion.Slerp(rotation, Quaternion.Euler(0, -2, -3), draw * .80f);
            }
            Vector3 attackBaseGrip = grip;
            Quaternion attackBaseRotation = rotation;
            float guard = Mathf.Min(Ease((Time.time - guardStarted) / .09f), Ease((guardUntil - Time.time) / .14f));
            if (guard > 0)
            {
                var guardRotation = Quaternion.Euler(35, -12, 65);
                // Keep the blade across the view. Rotating a long shaft around its
                // low resting grip without raising that grip put it below the screen.
                var guardGrip = new Vector3(0, -.12f, 1.5f) + guardRotation * ScaledGrip;
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
                ApplyAttack(age, ref grip, ref rotation);
                // Enter from the visible pose, but keep the contact phase on the normal
                // blade path. Recover to a guard/skill only while it is still active.
                float enter = Between(age, 0, motion == Motion.Gun ? .035f : motion == Motion.Bow ? .025f : hitStart);
                grip += (attackFromGrip - attackBaseGrip) * (1 - enter);
                rotation = Quaternion.Slerp(attackFromRotation * Quaternion.Inverse(attackBaseRotation), Quaternion.identity, enter) * rotation;
                float recover = Between(age, hitEnd, attackDuration);
                grip = Vector3.Lerp(grip, returnGrip, recover);
                rotation = Quaternion.Slerp(rotation, returnRotation, recover);
            }
            visual.localPosition = grip - rotation * ScaledGrip;
            visual.localRotation = rotation;
            UpdateBow(draw);
            if (hitbox != null && IsMelee)
            {
                ModelBox(new Pose(grip, rotation), out var center, out var half);
                hitbox.center = center;
                hitbox.size = 2f * (Abs(rotation * Vector3.right) * half.x +
                    Abs(rotation * Vector3.up) * half.y + Abs(rotation * Vector3.forward) * half.z);
            }
            else if (hitbox != null)
            {
                var matrix = transform.worldToLocalMatrix * visual.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(x, y, z))));
                hitbox.center = bounds.center; hitbox.size = bounds.size + Vector3.one * .12f;
            }
        }
        void ApplyAttack(float age, ref Vector3 grip, ref Quaternion rotation)
        {
            if (IsMelee)
            {
                var pose = MeleePoseAt(age);
                grip = pose.position;
                rotation = pose.rotation;
                return;
            }
            switch (motion)
            {
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
                    var rest = rotation;
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
        void UpdateBow(float draw)
        {
            if (bowRig == null) return;
            float age = Time.time - attackStarted;
            if(nockPending&&age>=.36f){nockPending=false;GameAudio.Play(SoundCue.BowNock,owner);}
            float tension = IsAttacking ? releasedDraw * (1f - Between(age, 0f, .055f)) : draw;
            bowRig.SetPose(tension, !IsAttacking || age >= .36f);
        }
    }
}
