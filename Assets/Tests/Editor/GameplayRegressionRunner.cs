using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameplayRegressionRunner
{
    const string Trigger=".utmp/gameplay-fix/run.txt";
    static IEnumerator routine;
    static string dir;
    static double next;
    static Gamepad one,two;
    static readonly List<string> results=new(),errors=new();
    static GameplayRegressionRunner()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("GameplayChecks",false))
            {
                SessionState.SetBool("GameplayChecks",false);
                dir="GameplayReview/"+SessionState.GetString("GameplayChecksDir","After")+"/";
                Directory.CreateDirectory(dir);results.Clear();errors.Clear();
                if(File.Exists(dir+"complete.txt"))File.Delete(dir+"complete.txt");
                Application.logMessageReceived+=Log;routine=Run();next=EditorApplication.timeSinceStartup+1;
            }
            if(state==PlayModeStateChange.ExitingPlayMode&&routine!=null)Finish(false);
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists(Trigger)&&!EditorApplication.isPlayingOrWillChangePlaymode&&routine==null)
        {
            SessionState.SetString("GameplayChecksDir",File.ReadAllText(Trigger).Trim());File.Delete(Trigger);
            SessionState.SetBool("GameplayChecks",true);EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity");EditorApplication.isPlaying=true;
        }
        if(routine==null||EditorApplication.timeSinceStartup<next)return;
        try{if(!routine.MoveNext()){Finish(true);return;}next=EditorApplication.timeSinceStartup+(routine.Current is double delay?delay:.05);}
        catch(Exception e){errors.Add(e.ToString());Finish(true);}
    }
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    public static void Check(string name,bool pass){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(dir+"results.txt",results);}
    public static void Shot(string name){ScreenCapture.CaptureScreenshot(dir+name+".png");}
    static IEnumerator Run()
    {
        one=InputSystem.AddDevice<Gamepad>("Gameplay test one");two=InputSystem.AddDevice<Gamepad>("Gameplay test two");yield return .3d;
        SceneManager.LoadScene("Title");yield return .5d;
        var join=SelectionTestInput.TwoGamepads(one,two);while(join.MoveNext())yield return join.Current;
        // The scene load and release of the lobby input gate can span several editor frames.
        var deadline = EditorApplication.timeSinceStartup + 8;
        bool BoardReady() => SceneManager.GetActiveScene().name == "Main" && !App.UI.GameSessionUI.BlocksGameplay;
        while (!BoardReady() && EditorApplication.timeSinceStartup < deadline) yield return .05d;
        Check("Both players reach the playable board", BoardReady());
        foreach(string name in new[]{"GameplayBoardChecks","GameplayMovementChecks","GameplayEffectsChecks","GameplayProjectileChecks","GameplayMeleeChecks","GameplayDuelChecks","GameplayBalanceChecks"})
        {
            var type=typeof(GameplayRegressionRunner).Assembly.GetType(name);if(type==null)continue;
            File.WriteAllText(dir+"progress.txt",name);
            var checks=(IEnumerator)type.GetMethod("Run").Invoke(null,new object[]{(Action<string,bool>)Check});
            while(true)
            {
                bool more;
                try{more=checks.MoveNext();}catch(Exception e){errors.Add(name+": "+e);Check(name+" completed without exception",false);break;}
                if(!more)break;yield return checks.Current;
            }
        }
        Check("No runtime errors or exceptions",errors.Count==0);
    }
    static void Finish(bool stop)
    {
        routine=null;if(one!=null&&one.added)InputSystem.RemoveDevice(one);if(two!=null&&two.added)InputSystem.RemoveDevice(two);
        Application.logMessageReceived-=Log;File.WriteAllLines(dir+"results.txt",results);File.WriteAllLines(dir+"errors.txt",errors);
        File.WriteAllText(dir+"complete.txt",DateTime.Now.ToString("O"));Time.timeScale=1;
        if(stop)EditorApplication.isPlaying=false;
    }
}
