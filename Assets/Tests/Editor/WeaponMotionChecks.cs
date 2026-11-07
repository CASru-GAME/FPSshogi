using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Checks observable motion in the actual duel, not a duplicate of the motion curves.
public static class WeaponMotionChecks
{
    static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly PieceType[] Types={PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.Kakugyo,PieceType.Hisya,PieceType.King};
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object obj,string name)=>obj.GetType().GetField(name,Fields)?.GetValue(obj);
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Fields).SetValue(obj,value);
    static Player One()=>Find<PlayerManager>().PlayerOne.GetComponent<Player>();
    static Player Two()=>Find<PlayerManager>().PlayerTwo.GetComponent<Player>();
    static void Duel(PieceType type,bool promoted=false)
    {
        var board=Find<ShogiBoard>();var grid=board.GetBoard();
        var first=PiecePresentation.Create(type);var second=type switch {
            PieceType.Fuhyo=>(IPiece)new Fuhyo(PlayerType.PlayerTwo),PieceType.Kyosya=>new Kyosya(PlayerType.PlayerTwo),
            PieceType.Keima=>new Keima(PlayerType.PlayerTwo),PieceType.Gin=>new Gin(PlayerType.PlayerTwo),
            PieceType.Kin=>new Kin(PlayerType.PlayerTwo),PieceType.Kakugyo=>new Kakugyo(PlayerType.PlayerTwo),
            PieceType.Hisya=>new Hisya(PlayerType.PlayerTwo),_=>new King(PlayerType.PlayerTwo)};
        if(promoted){first.Promote();second.Promote();}
        grid[4,4]=first;grid[4,5]=second;
        Set(board,"savedFromX",4);Set(board,"savedFromY",4);Set(board,"savedToX",4);Set(board,"savedToY",5);Set(board,"currentPlayer",PlayerType.PlayerOne);
        Find<GameStateHolder>().ChangeStateIntoDuel();
    }
    static Vector3 Tip(WeaponPresentation w)=>w.visual.TransformPoint(Vector3.up*w.visual.GetComponent<MeshFilter>().sharedMesh.bounds.max.y);

    public static void CheckMeleeStroke(WeaponPresentation weapon, Camera camera, Action<bool,string> check)
    {
        if (weapon.motion is not (WeaponPresentation.Motion.Thrust or WeaponPresentation.Motion.Slash or WeaponPresentation.Motion.Knife)) return;
        float previousStart = (float)Field(weapon, "attackStarted");
        Vector3 previousGrip = (Vector3)Field(weapon, "attackFromGrip");
        Quaternion previousRotation = (Quaternion)Field(weapon, "attackFromRotation");
        float previousFov = camera.fieldOfView;
        var mesh = weapon.visual.GetComponent<MeshFilter>().sharedMesh.bounds;
        string name = weapon.gameObject.name;

        void Sample(float age)
        {
            Set(weapon, "attackStarted", Time.time - age);
            weapon.RefreshPose();
        }
        Vector3 LocalGrip() => weapon.transform.InverseTransformPoint(weapon.visual.TransformPoint(weapon.GripLocal));
        Vector3 LocalTip() => weapon.transform.InverseTransformPoint(Tip(weapon));
        try
        {
            if (!weapon.IsAttacking)
            {
                Set(weapon, "attackFromGrip", LocalGrip());
                Set(weapon, "attackFromRotation", weapon.visual.localRotation);
            }
            Sample(weapon.hitStart);
            Vector3 windGrip = LocalGrip(), windTip = LocalTip();
            Sample(Mathf.Lerp(weapon.hitStart, weapon.hitEnd, .6f));
            Vector3 contactGrip = LocalGrip(), contactTip = LocalTip();
            Sample(weapon.hitEnd);
            Vector3 finishGrip = LocalGrip(), finishTip = LocalTip();

            if (weapon.motion == WeaponPresentation.Motion.Slash)
            {
                check(windGrip.x - finishGrip.x > .32f && windGrip.y - finishGrip.y > .30f,
                    name + " hand carries the cut diagonally from high right to low left");
                check(contactGrip.z - windGrip.z > .18f, name + " extends the hand forward as the blade crosses the aim");
            }
            else
            {
                float travel = weapon.motion == WeaponPresentation.Motion.Thrust ? 1.60f : 1.10f;
                check(contactGrip.z - windGrip.z > travel && Vector3.Angle(windTip, contactTip) > 7f,
                    name + " pulls back visibly before extending toward the aim");
            }
            check(Vector3.Distance(contactTip, finishTip) > .14f,
                name + " continues moving through the last part of the contact window");

            var trigger = weapon.GetComponent<BoxCollider>();
            bool sameBox = true, poseUntouched = true, triggerCoversBlade = true;
            float farthestForward = 0;
            for (int frame = 0; frame <= 8; frame++)
            {
                float age = Mathf.Lerp(weapon.hitStart, weapon.hitEnd, frame / 8f);
                Vector3 oldPosition = weapon.visual.localPosition, oldCenter = trigger.center, oldSize = trigger.size;
                Quaternion oldRotation = weapon.visual.localRotation;
                weapon.GetMeleeBox(age, out var center, out var half, out var rotation);
                poseUntouched &= weapon.visual.localPosition == oldPosition && weapon.visual.localRotation == oldRotation &&
                    trigger.center == oldCenter && trigger.size == oldSize;

                Sample(age);
                Vector3 MeshPoint(Vector3 local) => weapon.transform.InverseTransformPoint(weapon.visual.TransformPoint(local));
                Vector3 actualCenter = MeshPoint(mesh.center);
                sameBox &= Vector3.Distance(center, actualCenter) < .001f &&
                    Quaternion.Angle(rotation, weapon.visual.localRotation) < .1f;
                for (int axis = 0; axis < 3; axis++)
                {
                    Vector3 face = mesh.center;
                    face[axis] += mesh.extents[axis];
                    sameBox &= Mathf.Abs(Vector3.Distance(MeshPoint(face), actualCenter) + .06f - half[axis]) < .001f;
                }
                Bounds cover = new Bounds(trigger.center, trigger.size + Vector3.one * .002f);
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 offset = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                        (corner & 2) == 0 ? -half.y : half.y, (corner & 4) == 0 ? -half.z : half.z);
                    Vector3 point = center + rotation * offset;
                    triggerCoversBlade &= cover.Contains(point);
                    farthestForward = Mathf.Max(farthestForward, point.z);
                }
            }
            check(sameBox, name + " predicted contact matches the displayed, padded blade throughout the hit window");
            check(poseUntouched, name + " prediction preserves the current visual and trigger pose");
            check(triggerCoversBlade, name + " trigger covers the corners of the moving contact box");
            bool polearm = weapon.GetComponentInParent<Player>()?.PieceType is PieceType.Gin or PieceType.Kin;
            float expectedReach = weapon.motion switch {
                WeaponPresentation.Motion.Thrust => 4.60f, WeaponPresentation.Motion.Knife => 3.10f,
                _ => polearm ? 4.24f : 3.96f
            };
            check(Mathf.Abs(farthestForward - expectedReach) < .10f,
                $"{name} reaches its forward contact range ({farthestForward:F2} weapon-local units)");

            foreach (float fov in new[] { 50f, 60f, 90f, 140f })
            {
                camera.fieldOfView = fov;
                int leastVisible = 13;
                for (int frame = 0; frame <= 24; frame++)
                {
                    Sample(weapon.attackDuration * frame / 24f);
                    int visible = 0;
                    for (int part = 0; part <= 12; part++)
                    {
                        float alongBlade = Mathf.Lerp(mesh.min.y, mesh.max.y, part / 12f);
                        Vector3 mark = weapon.visual.TransformPoint(new Vector3(mesh.center.x, alongBlade, mesh.center.z));
                        Vector3 p = camera.WorldToViewportPoint(mark);
                        if (p.z > camera.nearClipPlane && p.x > .01f && p.x < .99f && p.y > .02f && p.y < .98f) visible++;
                    }
                    leastVisible = Mathf.Min(leastVisible, visible);
                }
                check(leastVisible >= 3, $"{name} keeps a visible section in the split view at {fov}° ({leastVisible}/13 samples)");
            }
        }
        finally
        {
            camera.fieldOfView = previousFov;
            Set(weapon, "attackStarted", previousStart);
            Set(weapon, "attackFromGrip", previousGrip);
            Set(weapon, "attackFromRotation", previousRotation);
            weapon.RefreshPose();
        }
    }

    public static IEnumerator Flight(Action<bool,string> check,Action<string> shot)
    {
        SceneManager.LoadScene("Main");yield return .7d;Duel(PieceType.Hisya);yield return 3.9d;
        var p=One();var view=p.GetComponentInChildren<Camera>();
        // Clear air gives gravity time to change the trajectory before any collision.
        var go=UnityEngine.Object.Instantiate(p.SubWeaponObject,new Vector3(0,1000,0),Quaternion.identity);
        var projectile=go.AddComponent<ProjectileArrow>();projectile.Initialize(p,p.playerStatus,1,100,200,.5f,~0);
        projectile.Launch(Vector3.forward,12,3);var rb=go.GetComponent<Rigidbody>();
        yield return .4d;
        check(rb.linearVelocity.y < -2,"Arrow flight has a gravity-curved trajectory");
        check(Vector3.Dot(go.transform.forward,rb.linearVelocity.normalized)>.998f,"Arrow model follows the curved flight direction");
        check(go.transform.forward.y<-.15f,"Arrow tip pitches down after falling");
        UnityEngine.Object.Destroy(go);yield return .1d;
        shot("flight-check-duel");yield return .2d;
    }
    static bool Changed(WeaponPresentation w,Vector3 p,Quaternion r)=>Vector3.Distance(w.visual.localPosition,p)>.045f||Quaternion.Angle(w.visual.localRotation,r)>4;
    static void Sample(WeaponPresentation w,string phase,List<string> trace,Action<string> shot,string prefix)
    {
        trace.Add($"{phase}: time={Time.time:F4}; pos={w.visual.localPosition:F4}; rot={w.visual.localEulerAngles:F3}; axis={w.transform.InverseTransformDirection(w.visual.up):F4}; active={w.GetComponent<Collider>().enabled}");
        shot(prefix+"-"+phase);
    }
    public static IEnumerator Run(Action<bool,string> check,Action<string> shot,string dir,Gamepad one,Gamepad two)
    {
        foreach(var type in Types.Where(t=>(!dir.Contains("Cover")||t==PieceType.Hisya)&&(!dir.Contains("Slash")||t==PieceType.Keima||t==PieceType.Gin||t==PieceType.Kin)))
        {
            SceneManager.LoadScene("Main");yield return .7d;Duel(type);yield return 3.9d;
            var p=One();var other=Two();var w=p.WeaponObject.GetComponent<WeaponPresentation>();var w2=other.WeaponObject.GetComponent<WeaponPresentation>();
            var body=p.GetComponent<Rigidbody>();var constraints=body.constraints;body.constraints=RigidbodyConstraints.FreezeAll;
            other.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
            p.SetPlayerStatus(new PlayerStatus(10000,10,5,p));other.SetPlayerStatus(new PlayerStatus(10000,10,5,other));
            var primary=(IPrimaryAction)Field(p,"currentPrimaryAction");var skill=(ISkill)Field(p,"currentSkill");
            var idlePos=w.visual.localPosition;var idleRot=w.visual.localRotation;
            var trace=new List<string>();Sample(w,"00-idle",trace,shot,type.ToString());yield return .1d;
            Vector3 bowOrigin = Vector3.zero;
            if (primary is BowPrimary chargingBow)
            {
                chargingBow.BeginDraw(p,p.playerStatus);
                yield return .5d;
                Sample(w,"draw-half",trace,shot,type.ToString());
                yield return .85d;
                Sample(w,"draw-full",trace,shot,type.ToString());
                yield return .12d;
                w.RefreshPose();
                bowOrigin = w.nockedArrow.position;
                chargingBow.ReleaseDraw();
            }
            else primary.PrimaryAction(p,p.playerStatus);
            if(type==PieceType.Hisya)
            {
                var arrow=UnityEngine.Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).FirstOrDefault(a=>a.GetComponent<CombatVisualLifetime>()?.owner==p);
                check(arrow!=null&&Vector3.Distance(arrow.transform.position,bowOrigin)<.08f,"Bow projectile starts at the visible nocked arrow");
                if(arrow!=null)
                {
                    check(arrow.GetComponentInChildren<MeshFilter>().sharedMesh==w.nockedArrow.GetComponent<MeshFilter>().sharedMesh,"Fired arrow retains the visible arrow geometry");
                    check(Vector3.Distance(arrow.transform.lossyScale,w.nockedArrow.lossyScale)<.001f,"Fired arrow retains the visible arrow scale");
                    check(Vector3.Dot(arrow.transform.forward,arrow.GetComponent<Rigidbody>().linearVelocity.normalized)>.999f,"Fired arrow points in its travel direction");
                    check(Mathf.Abs(arrow.GetComponent<Rigidbody>().linearVelocity.magnitude - 64f) < .1f,"Fully drawn arrow launches at 64 m/s");
                }
            }
            var attackStart=Field(w,"attackStarted");
            primary.PrimaryAction(p,p.playerStatus);check(Equals(Field(w,"attackStarted"),attackStart),type+" cooldown input does not restart attack");
            // Sampling a screenshot can take longer than an attack phase on this editor.
            // Pause the simulation and sample the real presentation's clock at known times;
            // the input/physics checks below still run with the ordinary game clock.
            var session=GameSessionUI.Instance;session.enabled=false;Time.timeScale=0;
            CheckMeleeStroke(w, p.GetComponentInChildren<Camera>(), check);
            void Pose(float elapsed,string label)
            {
                Set(w,"attackStarted",Time.time-elapsed);w.SendMessage("LateUpdate");Sample(w,label,trace,shot,type.ToString());
                if(type==PieceType.Hisya)
                {
                    var vertices=w.visual.GetComponent<MeshFilter>().sharedMesh.vertices;
                    check(new[]{0,2}.All(i=>vertices.Min(v=>Vector3.Distance(v,w.bowString.GetPosition(i)))<.015f),"Bow limb/string contact "+label);
                    if(w.nockedArrow.gameObject.activeSelf)
                    {
                        var m=w.nockedArrow.GetComponent<MeshFilter>().sharedMesh;
                        var tail=w.nockedArrow.localPosition+Vector3.forward*m.bounds.min.z;
                        check(Vector3.Distance(tail,w.bowString.GetPosition(1))<.015f,"Arrow nock follows string during redraw "+label);
                    }
                    if(elapsed<.36f)
                    {
                        var a=w.bowString.GetPosition(0);var b=w.bowString.GetPosition(2);var c=w.bowString.GetPosition(1);
                        check(Vector3.Cross(c-a,(b-a).normalized).magnitude<.015f,"Released bow string is straight "+label);
                    }
                }
            }
            Pose(.08f,"01-windup");Vector3 early=Tip(w);yield return .1d;
            Pose(.16f,"02-active");yield return .1d;
            Pose(.28f,"03-strike");yield return .1d;
            if(type==PieceType.Keima||type==PieceType.Gin||type==PieceType.Kin){
                var center=p.GetComponentInChildren<Camera>().WorldToViewportPoint(w.visual.GetComponent<Renderer>().bounds.center);
                check(center.z>0&&center.x>.02f&&center.x<.98f&&center.y>.15f&&center.y<.85f,type+" cutting stroke stays visible across the view");
            }
            if(type==PieceType.Fuhyo){
                check(Vector3.Dot(w.visual.up,p.GetComponentInChildren<Camera>().transform.forward)>.94f,"Spear blade points along the aim during its thrust");
                Vector3 travel=w.transform.InverseTransformVector(Tip(w)-early);
                check(travel.z>.3f&&new Vector2(travel.x,travel.y).magnitude<travel.z*.35f,"Spear tip advances along its shaft instead of lifting sideways");
            }
            Pose(.4f,"04-follow-through");yield return .1d;
            if(type==PieceType.Keima||type==PieceType.Gin||type==PieceType.Kin){
                var from=w.transform.InverseTransformPoint(early);var to=w.transform.InverseTransformPoint(Tip(w));
                check(from.x>.15f&&to.x<-.15f,type+" blade sweeps from the right through the aim to the left");
            }
            Pose(.55f,"05-recovery");yield return .1d;
            Set(w,"attackStarted",attackStart);session.enabled=true;Time.timeScale=1;
            yield return .85d;Sample(w,"06-idle",trace,shot,type.ToString());
            check(!w.IsAttacking&&!w.GetComponent<Collider>().enabled,type+" attack returns to idle with hitbox closed");
            check(Quaternion.Angle(w.visual.localRotation,idleRot)<.5f,type+" attack restores its resting orientation");
            InputSystem.QueueStateEvent(two,new GamepadState{rightTrigger=1});yield return .1d;
            if (type == PieceType.Hisya)
            {
                // The previous frame is captured to disk. Wait for the simulation,
                // rather than the editor clock, to process the queued trigger.
                double deadline = Time.realtimeSinceStartupAsDouble + 3;
                while (other.BowDrawAmount < .12f && Time.realtimeSinceStartupAsDouble < deadline) yield return .04d;
                check(other.BowDrawAmount > 0f && !w2.IsAttacking,
                    $"Second player draws before the bow releases (draw={other.BowDrawAmount:F2}, firing={w2.IsAttacking})");
            }
            InputSystem.QueueStateEvent(two,new GamepadState());yield return .08d;
            check(w2.IsAttacking,type+" second player has the same attack motion");yield return .8d;
            var secondary=(ISecondaryAction)Field(p,"currentSecondaryAction");secondary.SecondaryAction(p,p.playerStatus);yield return .18d;
            Sample(w,"07-secondary",trace,shot,type.ToString());
            check(Changed(w,idlePos,idleRot),type+" secondary action changes the weapon stance");
            if(p.SecondaryName!="ズーム"){
                var center=p.GetComponentInChildren<Camera>().WorldToViewportPoint(w.visual.GetComponent<Renderer>().bounds.center);
                check(center.z>0&&center.x>.05f&&center.x<.95f&&center.y>.1f&&center.y<.9f,type+" defensive weapon remains visible in the viewport");
            }
            if(type==PieceType.Gin||type==PieceType.Kin){
                var guardedRotation=w.visual.localRotation;primary.PrimaryAction(p,p.playerStatus);w.RefreshPose();
                check(Quaternion.Angle(guardedRotation,w.visual.localRotation)<3,type+" attack starts from the current defensive stance without snapping");
            }
            if(type==PieceType.Keima){
                var guardedRotation=w.visual.localRotation;new KeimaSkill().UseSkill(p,p.playerStatus);w.RefreshPose();
                check(Quaternion.Angle(guardedRotation,w.visual.localRotation)<3,"Keima dash starts smoothly from an active guard");
                var start=Field(w,"skillStarted");session.enabled=false;Time.timeScale=0;
                float duration = (float)Field(w,"skillDuration");
                Set(w,"skillStarted",Time.time-duration+.01f);w.RefreshPose();var beforeEnd=w.visual.localRotation;
                Set(w,"skillStarted",Time.time-duration-.01f);w.RefreshPose();
                check(Quaternion.Angle(beforeEnd,w.visual.localRotation)<3,"Keima dash recovers smoothly to its still-active guard");
                Set(w,"skillStarted",start);session.enabled=true;Time.timeScale=1;
            }
            yield return 1.1d;if(p.SecondaryName=="ズーム"){secondary.SecondaryAction(p,p.playerStatus);yield return .3d;}
            body.constraints=constraints;
            if(type==PieceType.Hisya){Set(p,"pitch",30f);yield return .15d;}
            idlePos=w.visual.localPosition;idleRot=w.visual.localRotation;skill.UseSkill(p,p.playerStatus);
            float cooldown=skill.cooldownTimer;yield return .12d;
            w.RefreshPose();
            Sample(w,"08-skill-start",trace,shot,type.ToString());
            bool skillMoved = Changed(w,idlePos,idleRot);
            skill.UseSkill(p,p.playerStatus);check(skill.cooldownTimer<=cooldown,type+" cooldown skill input does not restart the effect");
            yield return .12d;w.RefreshPose();Sample(w,"09-skill-action",trace,shot,type.ToString());
            check(cooldown > 0f && (skillMoved || Changed(w,idlePos,idleRot)),type+" accepted skill animates its held weapon");
            var pausedPos=w.visual.localPosition;var pausedRot=w.visual.localRotation;
            GameSessionUI.Instance.OpenPause();yield return .25d;
            check(Vector3.Distance(w.visual.localPosition,pausedPos)<.03f&&Quaternion.Angle(w.visual.localRotation,pausedRot)<2,type+" pause freezes the current motion");
            GameSessionUI.Instance.Resume();yield return .45d;Sample(w,"10-skill-recovery",trace,shot,type.ToString());
            yield return .8d;Sample(w,"11-skill-held",trace,shot,type.ToString());
            if(type==PieceType.Gin||type==PieceType.Kin){
                check(Changed(w,idlePos,idleRot)&&p.playerStatus.DefensePoint.Current>0,type+" defensive stance remains while the actual buff is active after one second");
                primary.PrimaryAction(p,p.playerStatus);var start=Field(w,"attackStarted");
                session.enabled=false;Time.timeScale=0;Set(w,"attackStarted",Time.time-w.attackDuration+.01f);w.RefreshPose();var beforeEnd=w.visual.localRotation;
                Set(w,"attackStarted",Time.time-w.attackDuration-.01f);w.RefreshPose();
                check(Quaternion.Angle(beforeEnd,w.visual.localRotation)<3,type+" attack recovers smoothly to a still-active defensive stance");
                Set(w,"attackStarted",start);session.enabled=true;Time.timeScale=1;yield return .8d;
            }
            if(type==PieceType.King){
                var turret=UnityEngine.Object.FindObjectsByType<KingTurret>(FindObjectsSortMode.None).First();
                var gun=turret.transform.Find("Gun");var muzzle=turret.transform.Find("Muzzle")??gun.Find("Muzzle");
                other.transform.position=turret.transform.position+Vector3.forward*8+Vector3.up*6;Physics.SyncTransforms();
                Set(turret,"nextFireTime",-1f);turret.SendMessage("Update");
                var aim=(other.GetComponentInChildren<Camera>().transform.position-muzzle.position).normalized;
                check(Vector3.Dot(gun.forward,aim)>.99f,"King turret barrel points at the actual elevated firing target");
                var restGun=gun.localPosition;yield return .06d;
                check(Vector3.Distance(gun.localPosition,restGun)>.01f,"King turret shot produces barrel recoil");
            }
            check(!w.GetComponent<Collider>().enabled,type+" skill does not create an unintended primary hitbox");
            if(type==PieceType.Hisya){
                Set(p,"pitch",0f);p.transform.position=new Vector3(2000,200,2000);body.constraints=RigidbodyConstraints.FreezeAll;yield return .15d;
                var view=p.GetComponentInChildren<Camera>();w.RefreshPose();
                other.transform.position=p.transform.position+view.transform.forward*6;Physics.SyncTransforms();
                var wall=new GameObject("Bow nock obstruction").AddComponent<BoxCollider>();
                wall.transform.position=view.transform.position+view.transform.forward*.8f;wall.transform.rotation=view.transform.rotation;wall.size=new Vector3(10,10,.02f);
                Physics.SyncTransforms();int hp=other.playerStatus.Hp.Current;
                new BowPrimary().PrimaryAction(p,p.playerStatus);yield return .45d;
                check(other.playerStatus.Hp.Current==hp,type+" nock behind thin cover cannot damage the target beyond it");
                check(!UnityEngine.Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).Any(a=>a.GetComponent<CombatVisualLifetime>()?.owner==p),type+" shot is consumed by cover between camera and bow");
                UnityEngine.Object.Destroy(wall.gameObject);
                yield return .8d;
                var heldBow = (BowPrimary)primary;
                heldBow.BeginDraw(p,p.playerStatus);
                yield return 1.35d;
                w.RefreshPose();
                var toNock=w.nockedArrow.position-view.transform.position;
                wall=new GameObject("Low cover across nock only").AddComponent<BoxCollider>();
                wall.transform.position=view.transform.position+toNock*.6f;wall.transform.rotation=Quaternion.LookRotation(toNock);wall.size=new Vector3(2,.12f,.02f);
                Physics.SyncTransforms();hp=other.playerStatus.Hp.Current;
                check(!ShotPhysics.Raycast(new Ray(view.transform.position,view.transform.forward),10,~0,p,out var reticleHit)||reticleHit.collider!=wall,"Low cover leaves the reticle aim unobstructed");
                heldBow.ReleaseDraw();
                var blockedArrow=UnityEngine.Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).FirstOrDefault(a=>a.GetComponent<CombatVisualLifetime>()?.owner==p);
                check(blockedArrow==null||!Physics.ComputePenetration(blockedArrow.GetComponent<Collider>(),blockedArrow.transform.position,blockedArrow.transform.rotation,wall,wall.transform.position,wall.transform.rotation,out _,out _),"Bow does not spawn its solid projectile intersecting low cover");
                yield return .45d;
                check(other.playerStatus.Hp.Current==hp,"Bow nock below the reticle cannot shoot through low cover");
                check(!UnityEngine.Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).Any(a=>a.GetComponent<CombatVisualLifetime>()?.owner==p),"Bow launch against low cover is consumed at the obstruction");
                UnityEngine.Object.Destroy(wall.gameObject);
                yield return .8d;other.transform.position=p.transform.position+view.transform.forward*2;Physics.SyncTransforms();hp=other.playerStatus.Hp.Current;
                new BowPrimary().PrimaryAction(p,p.playerStatus);yield return .15d;
                check(other.playerStatus.Hp.Current==hp-10,"Bow still damages a point-blank opponent between camera and nock once");
            }
            File.WriteAllLines(dir+type+"-motion.txt",trace);
        }
        SceneManager.LoadScene("Main");yield return .7d;
        check(UnityEngine.Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None).Length==0,"Changing duel clears temporary skill visuals");
        if(dir.Contains("Cover")||dir.Contains("Slash"))yield break;
        foreach(var type in Types.Where(t=>t!=PieceType.Kin&&t!=PieceType.King))
        {
            Duel(type,true);yield return 3.9d;
            var p=One();var w=p.WeaponObject.GetComponent<WeaponPresentation>();
            InputSystem.QueueStateEvent(one,new GamepadState{rightTrigger=1});yield return .1d;
            if (type == PieceType.Hisya)
            {
                check(p.BowDrawAmount > 0f && !w.IsAttacking,"Promoted rook draws while RT is held");
                InputSystem.QueueStateEvent(one,new GamepadState());yield return .1d;
            }
            check(w.IsAttacking&&p.PieceType==type,type+" promoted piece retains its weapon attack");
            InputSystem.QueueStateEvent(one,new GamepadState());
            SceneManager.LoadScene("Main");yield return .7d;
        }
    }
    public static IEnumerator Capture(Action<bool,string> check,string dir,Gamepad pad)
    {
        foreach(var type in Types.Where(t=>!dir.Contains("Slash")||t==PieceType.Keima||t==PieceType.Gin||t==PieceType.Kin))
        {
            SceneManager.LoadScene("Main");yield return .7d;Duel(type);yield return 3.9d;
            var p=One();var other=Two();
            p.SetPlayerStatus(new PlayerStatus(10000,10,5,p));other.SetPlayerStatus(new PlayerStatus(10000,10,5,other));
            other.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
            var recorder=new GameObject("Motion recorder").AddComponent<WeaponMotionRecorder>();
            var actions=new[]{"attack","secondary","skill"};
            foreach(var action in actions)
            {
                if(type==PieceType.Hisya&&action=="skill"){Set(p,"pitch",30f);yield return .2d;}
                var state=action=="attack" ? new GamepadState{rightTrigger=1} : action=="secondary" ? new GamepadState{leftTrigger=1} : new GamepadState().WithButton(GamepadButton.East);
                File.WriteAllText(dir+"progress.txt",type+"-"+action);
                recorder.Record(dir+"VideoFrames/"+type+"-"+action,action=="skill"?90:60,
                    ()=>InputSystem.QueueStateEvent(pad,state),()=>InputSystem.QueueStateEvent(pad,new GamepadState()));
                while(recorder.Busy)yield return .1d;
                check(File.Exists(dir+"VideoFrames/"+type+"-"+action+"/0059.jpg"),type+" "+action+" captured at 30 frames per game second");
                if(action=="secondary"&&p.SecondaryName=="ズーム")
                {
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});yield return .1d;
                    InputSystem.QueueStateEvent(pad,new GamepadState());yield return .25d;
                }
            }
            UnityEngine.Object.Destroy(recorder.gameObject);
        }
    }
}
