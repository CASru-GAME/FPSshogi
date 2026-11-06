#if FPS_VISUAL_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using App.Main.GameMaster;
using App.Main.Player;
using App.Main.ShogiThings;

// Compiled only into the separate visual verification player, never the delivered game.
public sealed class VisualPlayerSmoke : MonoBehaviour
{
    readonly List<string> results = new();
    readonly List<string> errors = new();
    string output;
    Gamepad pad1, pad2;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch() { DontDestroyOnLoad(new GameObject("VisualPlayerSmoke").AddComponent<VisualPlayerSmoke>()); }
    void Awake()
    {
        var arguments=Environment.GetCommandLineArgs();int option=Array.IndexOf(arguments,"-visualReviewOutput");
        string config=Path.Combine(Application.dataPath,"../visual-review-output.txt");
        output=option>=0&&option+1<arguments.Length?Path.GetFullPath(arguments[option+1]):File.Exists(config)?Path.GetFullPath(File.ReadAllText(config).Trim()):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../VisualReview/PlayerSmoke"));Directory.CreateDirectory(output);
        Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+trace);};
    }
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static void Set(object value,string field,object data)=>value.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(value,data);
    static object Get(object value,string field)=>value.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
    void Check(bool pass,string name){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(Path.Combine(output,"results.txt"),results);}
    void Shot(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
    static IPiece Piece(int index,PlayerType player)=>index switch {0=>new Hisya(player),1=>new Kyosya(player),2=>new Gin(player),3=>new King(player),4=>new Fuhyo(player),5=>new Keima(player),6=>new Kin(player),_=>new Kakugyo(player)};
    IEnumerator Start()
    {
        pad1=InputSystem.AddDevice<Gamepad>("Native visual one");pad2=InputSystem.AddDevice<Gamepad>("Native visual two");
        yield return new WaitForSeconds(.5f);SceneManager.LoadScene("Title");yield return new WaitForSeconds(.7f);
        Shot("title");yield return new WaitForSeconds(.2f);
        var join=SelectionTestInput.TwoGamepads(pad1,pad2);while(join.MoveNext())yield return new WaitForSecondsRealtime((float)(double)join.Current);
        Shot("board");
        foreach(string name in new[]{"Transparent","SlowField","Glow"})
        {
            var material=Resources.Load<Material>("Combat/"+name);
            Check(material!=null&&material.shader!=null&&material.shader.isSupported,"Built player shader supported: "+name);
        }
        for(int index=0;index<8;index++)
        {
            SceneManager.LoadScene("Main");yield return new WaitForSeconds(.7f);
            var board=Find<ShogiBoard>();var grid=board.GetBoard();grid[4,4]=Piece(index,PlayerType.PlayerOne);grid[4,5]=Piece(index,PlayerType.PlayerTwo);
            Set(board,"savedFromX",4);Set(board,"savedFromY",4);Set(board,"savedToX",4);Set(board,"savedToY",5);Set(board,"currentPlayer",PlayerType.PlayerOne);
            Find<GameStateHolder>().ChangeStateIntoDuel();yield return new WaitForSeconds(3.8f);
            var one=Find<PlayerManager>().PlayerOne.GetComponent<Player>();
            Check(one.WeaponObject.GetComponent<WeaponPresentation>()!=null,"Native weapon present "+index);
            Shot("weapon-"+index+"-idle");yield return new WaitForSeconds(.1f);
            if(index==0)
            {
                var bow=one.WeaponObject.GetComponent<WeaponPresentation>();
                ((IPrimaryAction)Get(one,"currentPrimaryAction")).PrimaryAction(one,one.playerStatus);
                var arrow=UnityEngine.Object.FindObjectsByType<ProjectileArrow>(FindObjectsSortMode.None).FirstOrDefault();
                Check(arrow!=null&&arrow.GetComponent<MeshFilter>().sharedMesh==bow.nockedArrow.GetComponent<MeshFilter>().sharedMesh,"Native held and flying arrows share mesh");
                Check(arrow!=null&&Vector3.Distance(arrow.transform.lossyScale,bow.nockedArrow.lossyScale)<.001f,"Native arrow retains held scale");
                yield return new WaitForSeconds(.05f);Shot("bow-release");yield return new WaitForSeconds(.7f);Shot("bow-redrawn");yield return new WaitForSeconds(.2f);
                Set(one,"pitch",30f);yield return new WaitForSeconds(.1f);
            }
            if(index==1||index>=4)((IPrimaryAction)Get(one,"currentPrimaryAction")).PrimaryAction(one,one.playerStatus);
            else ((ISkill)Get(one,"currentSkill")).UseSkill(one,one.playerStatus);
            yield return new WaitForSeconds(index==1||index>=4?.03f:.22f);
            if(index<4)Check(UnityEngine.Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None).Length>0,"Native visual effect present "+index);
            else Check(one.WeaponObject.GetComponent<WeaponPresentation>().IsAttacking,"Native attack animates "+index);
            Shot(index<4?new[]{"slow-field","gun-shot","silver-buff","king-fan"}[index]:"weapon-"+index+"-attack");yield return new WaitForSeconds(.5f);
        }
        SceneManager.LoadScene("Title");yield return new WaitForSeconds(.5f);
        Check(UnityEngine.Object.FindObjectsByType<CombatVisualLifetime>(FindObjectsSortMode.None).Length==0,"Native scene change cleans effects");
        InputSystem.RemoveDevice(pad1);InputSystem.RemoveDevice(pad2);
        foreach(string path in Directory.GetFiles(output,"*.png"))
        {
            var capture=new Texture2D(2,2);capture.LoadImage(File.ReadAllBytes(path));
            var pixels=capture.GetPixels32();bool visible=pixels.Any(c=>c.r>25||c.g>25||c.b>25);
            Check(visible,"Native frame rendered: "+Path.GetFileName(path));Destroy(capture);
        }
        Check(errors.Count==0,"No native runtime errors");File.WriteAllLines(Path.Combine(output,"errors.txt"),errors);
        File.WriteAllText(Path.Combine(output,"complete.txt"),DateTime.Now.ToString("O"));
        Application.Quit(results.Exists(row=>row.StartsWith("FAIL"))?1:0);
    }
}
#endif
