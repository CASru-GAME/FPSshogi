using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class AudioRegression
{
    const string Trigger=".utmp/audio/run.txt";
    static IEnumerator routine;
    static double next;
    static string output;
    static readonly List<string> results=new(),errors=new();
    static AudioRegression()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("AudioChecks",false))
            {
                SessionState.SetBool("AudioChecks",false);output="AudioReview/"+SessionState.GetString("AudioChecksDir","Editor");
                Directory.CreateDirectory(output);results.Clear();errors.Clear();
                Application.logMessageReceived+=Log;routine=AudioRegressionScenario.Run(Check,output,false);next=EditorApplication.timeSinceStartup+.8;
            }
            if(state==PlayModeStateChange.ExitingPlayMode&&routine!=null)Finish(false);
        };
    }
    static void Check(bool pass,string name){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(output+"/results.txt",results);}
    static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
    static void Tick()
    {
        if(EditorApplication.isCompiling)return;
        if(File.Exists(Trigger)&&!EditorApplication.isPlayingOrWillChangePlaymode&&routine==null)
        {
            string command=File.ReadAllText(Trigger).Trim();File.Delete(Trigger);
            if(command=="build"){Build();return;}
            SessionState.SetString("AudioChecksDir",command);SessionState.SetBool("AudioChecks",true);
            EditorSceneManager.OpenScene("Assets/App/Scenes/Title.unity");EditorApplication.isPlaying=true;
        }
        if(routine==null||EditorApplication.timeSinceStartup<next)return;
        try{if(!routine.MoveNext()){Check(errors.Count==0,"No runtime exceptions");Finish(true);return;}next=EditorApplication.timeSinceStartup+(routine.Current is double delay?delay:.05);}
        catch(Exception e){errors.Add(e.ToString());Check(false,"Audio scenario completes");Finish(true);}
    }
    static void Finish(bool stop)
    {
        (routine as IDisposable)?.Dispose();routine=null;Application.logMessageReceived-=Log;
        File.WriteAllLines(output+"/errors.txt",errors);File.WriteAllText(output+"/complete.txt",DateTime.Now.ToString("O"));
        if(stop)EditorApplication.isPlaying=false;
    }
    static void Build()
    {
        Directory.CreateDirectory("Builds/AudioSmoke");Directory.CreateDirectory("AudioReview");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/App/Scenes/Title.unity","Assets/App/Scenes/Main.unity"},
            locationPathName="Builds/AudioSmoke/FPSshogi.exe",target=BuildTarget.StandaloneWindows64,
            options=BuildOptions.None,extraScriptingDefines=new[]{"FPS_AUDIO_SMOKE"}});
        File.WriteAllText("AudioReview/native-build.txt",$"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nTime: {report.summary.totalTime}\n");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Audio verification build failed");
    }
}
