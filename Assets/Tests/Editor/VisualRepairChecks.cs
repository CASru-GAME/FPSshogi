using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;
using App.Main.ViewManager;

// Runs against the real Title/Main flow. Virtual controllers are removed on every exit.
[InitializeOnLoad]
public static class VisualRepairChecks
{
    const string Trigger = ".utmp/visual-fix/check.txt";
    static string dir;
    static IEnumerator routine;
    static double next;
    static Gamepad pad1, pad2;
    static readonly List<string> results = new();
    static readonly List<string> errors = new();
    static VisualRepairChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += s =>
        {
            if(s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VisualChecks", false))
            {
                SessionState.SetBool("VisualChecks", false);
                dir = "VisualReview/" + SessionState.GetString("VisualChecksDir", "After") + "/";
                Directory.CreateDirectory(dir); results.Clear(); errors.Clear();
                Application.logMessageReceived += Log;
                routine = dir.Contains("Motion") ? RunMotion() : dir.Contains("Art") ? RunArt() : Run(); next = EditorApplication.timeSinceStartup + 1;
            }
            if(s == PlayModeStateChange.ExitingPlayMode && routine != null) Finish(false);
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling) return;
        if(File.Exists(Trigger) && !EditorApplication.isPlayingOrWillChangePlaymode && routine == null)
        {
            SessionState.SetString("VisualChecksDir", File.ReadAllText(Trigger).Trim()); File.Delete(Trigger);
            SessionState.SetBool("VisualChecks", true);
            EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity"); EditorApplication.isPlaying = true;
        }
        if(routine == null || EditorApplication.timeSinceStartup < next) return;
        try { if(!routine.MoveNext()){ Finish(true); return; } next=EditorApplication.timeSinceStartup+(routine.Current is double d ? d : .05); }
        catch(Exception e) { errors.Add(e.ToString()); Finish(true); }
    }
    static void Log(string message,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+trace); }
    static void Finish(bool stop)
    {
        routine=null;
        if(pad1 != null && pad1.added)InputSystem.RemoveDevice(pad1);
        if(pad2 != null && pad2.added)InputSystem.RemoveDevice(pad2);
        Application.logMessageReceived -= Log;
        File.WriteAllLines(dir+"results.txt",results);File.WriteAllLines(dir+"errors.txt",errors);
        File.WriteAllText(dir+"complete.txt",DateTime.Now.ToString("O"));Time.timeScale=1;
        if(stop)EditorApplication.isPlaying=false;
    }
    static void Check(bool success,string name) { results.Add((success?"PASS ":"FAIL ")+name);File.WriteAllLines(dir+"results.txt",results); }
    static T Find<T>() where T: UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static Player One()=>Find<PlayerManager>().PlayerOne.GetComponent<Player>();
    static Player Two()=>Find<PlayerManager>().PlayerTwo.GetComponent<Player>();
    static void Secondary(Player p)=>((ISecondaryAction)Field(p,"currentSecondaryAction")).SecondaryAction(p,p.playerStatus);
    static void Skill(Player p)=>((ISkill)Field(p,"currentSkill")).UseSkill(p,p.playerStatus);
    static void Primary(Player p)=>((IPrimaryAction)Field(p,"currentPrimaryAction")).PrimaryAction(p,p.playerStatus);
    static IPiece Piece(PieceType t,PlayerType p)=>t switch { PieceType.Fuhyo=>new Fuhyo(p),PieceType.Kyosya=>new Kyosya(p),PieceType.Keima=>new Keima(p),PieceType.Gin=>new Gin(p),PieceType.Kin=>new Kin(p),PieceType.Kakugyo=>new Kakugyo(p),PieceType.Hisya=>new Hisya(p),_=>new King(p) };
    static void Duel(PieceType type)
    {
        var b=Find<ShogiBoard>();var grid=b.GetBoard();grid[4,4]=Piece(type,PlayerType.PlayerOne);grid[4,5]=Piece(type,PlayerType.PlayerTwo);
        Set(b,"savedFromX",4);Set(b,"savedFromY",4);Set(b,"savedToX",4);Set(b,"savedToY",5);Set(b,"currentPlayer",PlayerType.PlayerOne);
        Find<GameStateHolder>().ChangeStateIntoDuel();
    }
    static void Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(dir+name+".png");
        File.WriteAllText(dir+"progress.txt",name);
    }
    static IEnumerator RunMotion()
    {
        pad1=InputSystem.AddDevice<Gamepad>("Motion check one");pad2=InputSystem.AddDevice<Gamepad>("Motion check two");yield return .4d;
        SceneManager.LoadScene("Title");yield return .6d;
        var join=SelectionTestInput.TwoGamepads(pad1,pad2);while(join.MoveNext())yield return join.Current;
        var checks=dir.Contains("Flight") ? WeaponMotionChecks.Flight(Check,Shot) : dir.Contains("Capture") ? WeaponMotionChecks.Capture(Check,dir,pad1) : WeaponMotionChecks.Run(Check,Shot,dir,pad1,pad2);
        while(checks.MoveNext())yield return checks.Current;
        Check(errors.Count==0,"No motion runtime errors");
    }
    static IEnumerator Run()
    {
        pad1=InputSystem.AddDevice<Gamepad>("Visual repair one");pad2=InputSystem.AddDevice<Gamepad>("Visual repair two");yield return .4;
        SceneManager.LoadScene("Title");yield return .6;
        var join=SelectionTestInput.TwoGamepads(pad1,pad2);while(join.MoveNext())yield return join.Current;
        var cells=Find<ViewPiece>().boardCellPositions;
        float spacing=Vector3.Distance(cells[0,0],cells[1,0]);
        Check(Enumerable.Range(1,8).All(i=>Mathf.Abs(Vector3.Distance(cells[i-1,0],cells[i,0])-spacing)<.01f),"Board cells equally spaced");
        Shot("board");yield return .3;
        Duel(PieceType.Kyosya);yield return 4.0;
        float normalFov=One().FieldOfView;
        Secondary(One());yield return .1;
        Check(One().GetComponentInChildren<Camera>().fieldOfView<normalFov,"Zoom enters");
        Set(One(),"pitch",35f);yield return .1;Two().playerStatus.Hp.Subtract(100000);yield return 1.0;
        Duel(PieceType.Fuhyo);yield return 4.0;
        Check(Mathf.Abs(One().GetComponentInChildren<Camera>().fieldOfView-normalFov)<.01,"FOV resets on next duel");
        Check(Mathf.Abs((float)Field(One(),"pitch"))<.01,"Pitch resets on next duel");
        Shot("consecutive-duel");yield return .3;
        SceneManager.LoadScene("Main");yield return 1.0;Duel(PieceType.King);yield return 4.0;
        var king=(KingSkill)Field(One(),"currentSkill");
        Check(king.gunbaiPrefab!=null,"King has separate gunbai prefab");Skill(One());yield return .25;Shot("king-skill");yield return .3;
        SceneManager.LoadScene("Main");yield return 1.0;Duel(PieceType.Hisya);yield return 4.0;
        Set(One(),"pitch",30f);yield return .2;Skill(One());yield return .2;
        var slow=GameObject.Find("Hisya_SlowField");var mat=slow?.GetComponentInChildren<Renderer>().sharedMaterial;
        Check(mat!=null && mat.shader.name.StartsWith("Universal Render Pipeline/") && mat.renderQueue>=3000,"Slow field uses URP transparency");
        Shot("slow-field");yield return .4;
        if(dir.Contains("BeforeFix"))yield break;
        foreach(var type in new[]{PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.Kakugyo,PieceType.Hisya,PieceType.King})
        {
            SceneManager.LoadScene("Main");yield return .8;Duel(type);yield return 4.0;
            var p=One();var view=p.GetComponentInChildren<Camera>();var w=p.WeaponObject.GetComponent<WeaponPresentation>();
            Check(w!=null,type+" display component");if(w==null)continue;
            Check(!w.IsAttacking,type+" spawns at idle");
            int bodyLayer=30;
            Check((view.cullingMask&(1<<bodyLayer))==0 && (Two().GetComponentInChildren<Camera>().cullingMask&(1<<bodyLayer))!=0,type+" own body hidden only in own camera");
            var bounds=p.WeaponObject.GetComponentInChildren<Renderer>().bounds;var screen=view.WorldToViewportPoint(bounds.center);
            Check(screen.z>view.nearClipPlane && screen.x>0 && screen.x<1 && screen.y>0 && screen.y<1,type+" weapon centre visible");
            Shot(type+"-idle");yield return .25;
            InputSystem.QueueStateEvent(pad1,new GamepadState{rightTrigger=1});yield return .10;
            if (type == PieceType.Hisya) { InputSystem.QueueStateEvent(pad1,new GamepadState());yield return .06; }
            Check(w.IsAttacking,type+" primary input animates");Shot(type+"-attack");yield return .12;
            InputSystem.QueueStateEvent(pad2,new GamepadState{rightTrigger=1});yield return .1;
            if (type == PieceType.Hisya) { InputSystem.QueueStateEvent(pad2,new GamepadState());yield return .06; }
            Check(Two().WeaponObject.GetComponent<WeaponPresentation>().IsAttacking,type+" player two primary input animates");
            InputSystem.QueueStateEvent(pad2,new GamepadState());
            InputSystem.QueueStateEvent(pad1,new GamepadState());yield return .9;
            Check(!w.IsAttacking,type+" returns to idle");
            Check(!p.WeaponObject.GetComponent<Collider>().enabled,type+" hitbox closes");
            Set(p,"pitch",55f);yield return .15;
            Check(Quaternion.Angle(w.transform.rotation,view.transform.rotation)<.1f,type+" follows downward aim");Shot(type+"-look-down");yield return .25;
            Set(p,"pitch",-55f);yield return .15;
            Check(Quaternion.Angle(w.transform.rotation,view.transform.rotation)<.1f,type+" follows upward aim");Shot(type+"-look-up");yield return .25;
            Set(p,"pitch",0f);yield return .15;Secondary(p);yield return .1;Shot(type+"-secondary");yield return .25;
            if(p.SecondaryName=="ズーム")
            {
                float fov=view.fieldOfView;App.UI.GameSessionUI.Instance.OpenPause();yield return .2;
                Check(Mathf.Abs(view.fieldOfView-fov)<.01f,type+" pause preserves zoom");App.UI.GameSessionUI.Instance.Resume();yield return .2;Secondary(p);
            }
            if(type==PieceType.King)
            {
                var target=Two();Vector3 original=target.transform.position;int hp=target.playerStatus.Hp.Current;
                // Player root scale is 2 and the opponent capsule radius is 1 world unit.
                // Seven units is beyond the animated knife, but inside the former fixed box.
                target.transform.position=p.transform.position+p.transform.forward*7f;target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;yield return .1;
                Primary(p);yield return 1.1;Check(target.playerStatus.Hp.Current==hp,"Knife misses a target beyond its visible reach");
                target.transform.position=p.transform.position+p.transform.forward*1.8f+p.transform.right*.25f;target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;yield return .1;
                Primary(p);yield return .55;Check(target.playerStatus.Hp.Current<hp,"Knife damages a target within blade reach");target.transform.position=original;yield return .6;
            }
            if(type==PieceType.Keima)
            {
                var target=Two();Vector3 original=target.transform.position;int hp=target.playerStatus.Hp.Current;
                target.transform.position=p.transform.position+p.transform.forward*3.8f-p.transform.right*2.2f;target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;yield return .1;
                Primary(p);yield return .55;Check(target.playerStatus.Hp.Current<hp,"Slash contacts a target to the left of idle blade");target.transform.position=original;yield return .6;
            }
            if(type==PieceType.Hisya){Set(p,"pitch",30f);yield return .15;}
            InputSystem.QueueStateEvent(pad1,new GamepadState().WithButton(GamepadButton.East));yield return .22;
            if (type == PieceType.Kyosya)
            {
                InputSystem.QueueStateEvent(pad1,new GamepadState());Shot("Kyosya-aim");yield return .15;
                InputSystem.QueueStateEvent(pad1,new GamepadState{rightTrigger=1});yield return .1;
            }
            InputSystem.QueueStateEvent(pad1,new GamepadState());Shot(type+"-skill");yield return .35;
            if(type==PieceType.King)
            {
                Check(UnityEngine.Object.FindObjectsByType<KingTurret>(FindObjectsSortMode.None).Length==3,"King summons exactly three turrets");
                Check(GameObject.Find("Gunbai_Command")!=null,"King command fan visible");
                Check(UnityEngine.Object.FindObjectsByType<KingTurret>(FindObjectsSortMode.None).All(t=>t.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y)<view.transform.position.y),"Turrets stay below eye level");
            }
            if(type==PieceType.Gin||type==PieceType.Kin)Check(GameObject.Find(type+"Skill_Aura")!=null,type+" buff visible");
            if(type==PieceType.Fuhyo)
            {
                var target=Two();target.transform.position=p.transform.position+p.transform.forward*3;target.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;yield return .1;
                int before=target.playerStatus.Hp.Current;Primary(p);yield return .5;
                int after=target.playerStatus.Hp.Current;Check(after<before,"Melee active interval damages opponent");yield return .5;
                Check(target.playerStatus.Hp.Current==after,"Melee recovery has no extra damage");
            }
            Two().playerStatus.Hp.Subtract(100000);yield return .6;
            Check(UnityEngine.Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None).Length==0,type+" duel exit cleans effects");
        }
        yield return .1;
        var boardCheck=CheckBoard();while(boardCheck.MoveNext())yield return boardCheck.Current;
    }
    static IEnumerator RunArt()
    {
        pad1=InputSystem.AddDevice<Gamepad>("Visual art one");pad2=InputSystem.AddDevice<Gamepad>("Visual art two");yield return .4;
        SceneManager.LoadScene("Title");yield return .6;
        var join=SelectionTestInput.TwoGamepads(pad1,pad2);while(join.MoveNext())yield return join.Current;
        Shot("board");yield return .3;
        var overview=Camera.main;
        if(overview!=null)
        {
            float size=overview.orthographicSize,fov=overview.fieldOfView;
            if(overview.orthographic)overview.orthographicSize*=1.7f;else overview.fieldOfView=Mathf.Min(90,fov*1.5f);
            var lods=UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            for(int level=0;level<3;level++){foreach(var item in lods)item.ForceLOD(level);yield return .2;Shot("room-overview-LOD"+level);yield return .3;}
            foreach(var item in lods)item.ForceLOD(-1);overview.orthographicSize=size;overview.fieldOfView=fov;
        }
        Duel(PieceType.Fuhyo);yield return 4.0;
        var chest=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).First(f=>f.name=="箪笥");
        var group=chest.GetComponent<LODGroup>();
        for(int level=0;level<3;level++){group.ForceLOD(level);yield return .3;Shot("chest-LOD"+level);yield return .3;}
        SceneManager.LoadScene("Main");yield return .8;Duel(PieceType.King);yield return 4.0;
        Skill(One());yield return .22;Shot("King-skill");yield return .4;
        foreach(var type in new[]{PieceType.Gin,PieceType.Kin})
        {
            SceneManager.LoadScene("Main");yield return .8;Duel(type);yield return 4.0;
            var mesh=One().WeaponObject.GetComponentInChildren<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var indices=mesh.GetTriangles(2);float area=0;
            for(int i=0;i<indices.Length;i+=3)area+=Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).magnitude*.5f;
            Check(area>.1f,type+" blade has broad visible faces");Shot(type+"-idle");yield return .25;
            Primary(One());yield return .18;Shot(type+"-attack");yield return .8;Secondary(One());yield return .1;Shot(type+"-secondary");yield return .2;
            Skill(One());yield return .2;Shot(type+"-skill");yield return .3;
        }
        var boardCheck=CheckBoard();while(boardCheck.MoveNext())yield return boardCheck.Current;
    }
    static IEnumerator CheckBoard()
    {
        SceneManager.LoadScene("Main");yield return .9;
        var board=Find<ShogiBoard>().GetBoard();var types=new[]{PieceType.Fuhyo,PieceType.Kyosya,PieceType.Keima,PieceType.Gin,PieceType.Kin,PieceType.Kakugyo,PieceType.Hisya,PieceType.King};
        for(int x=0;x<9;x++)for(int y=0;y<9;y++)board[x,y]=null;
        for(int x=0;x<8;x++){board[x,3]=Piece(types[x],PlayerType.PlayerOne);board[x,5]=Piece(types[x],PlayerType.PlayerTwo);board[x,4]=Piece(types[x],PlayerType.PlayerOne);if(board[x,4].IsPromotable)board[x,4].Promote();}
        yield return .5;Shot("board-all-promotions");yield return .4;
        var vp=Find<ViewPiece>();var pieces=(GameObject[,])Field(vp,"pieceObjects");
        Check(pieces[0,3].GetComponentInChildren<Renderer>().bounds.size.z < pieces[7,3].GetComponentInChildren<Renderer>().bounds.size.z*.9f,"Board preserves pawn and king size hierarchy");
        for(int x=0;x<8;x++)for(int y=3;y<=5;y++)
        {
            var rs=pieces[x,y].GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var c=vp.boardCellPositions[x,y];Check(Mathf.Abs(b.center.x-c.x)<.01f&&Mathf.Abs(b.center.z-c.z)<.01f&&b.size.x<4.6f&&b.size.z<4.6f,"Piece fits grid "+x+","+y);
        }
    }
}
